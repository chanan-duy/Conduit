using System.Security.Claims;
using Conduit.Meetings.Clients;
using Conduit.Meetings.Contracts;
using Conduit.Meetings.Data;
using Conduit.Meetings.Services;
using Conduit.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Meetings.Endpoints;

public static class MeetingsEndpoints
{
	public static void MapMeetingsEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/v1/meetings").WithTags("Meetings");

		group.MapGet("/", GetMeetings).RequirePermission(ConduitPermissions.MeetingsView);
		group.MapGet("/{id:guid}", GetMeeting).RequirePermission(ConduitPermissions.MeetingsView);
		group.MapPost("/", CreateMeeting).RequirePermission(ConduitPermissions.MeetingsManage);
		group.MapPatch("/{id:guid}", UpdateMeeting).RequirePermission(ConduitPermissions.MeetingsManage);
		group.MapDelete("/{id:guid}", DeleteMeeting).RequirePermission(ConduitPermissions.MeetingsManage);
		group.MapGet("/{id:guid}/token", GetJoinToken).RequirePermission(ConduitPermissions.MeetingsJoin);
		group.MapGet("/{id:guid}/participants", GetParticipants).RequirePermission(ConduitPermissions.MeetingsView);
		group.MapPost("/{id:guid}/participants", AddParticipant).RequirePermission(ConduitPermissions.MeetingsManage);
		group.MapDelete("/{id:guid}/participants/{userId:guid}", RemoveParticipant).RequirePermission(ConduitPermissions.MeetingsManage);
	}

	private static async Task<Ok<MeetingResponse[]>> GetMeetings(ClaimsPrincipal user, MeetingsDb db)
	{
		var userId = user.GetUserId();

		var meetings = await db.Meetings
			.AsNoTracking()
			.Where(m => m.DeletedAt == null && (m.CreatorId == userId || m.Participants.Any(p => p.UserId == userId)))
			.OrderBy(m => m.StartDate)
			.Select(m => new MeetingResponse(m.Id, m.Title, m.StartDate, m.EndDate, m.CreatorId == userId))
			.ToArrayAsync();

		return TypedResults.Ok(meetings);
	}

	private static async Task<Results<Ok<MeetingResponse>, NotFound>> GetMeeting(Guid id, ClaimsPrincipal user, MeetingAccess access)
	{
		var meeting = await access.FindAsync(user, id);
		if (meeting is not { Role: MeetingRole.Owner or MeetingRole.Participant })
		{
			return TypedResults.NotFound();
		}

		return TypedResults.Ok(ToResponse(meeting));
	}

	private static async Task<Results<Ok<CreateMeetingResponse>, ValidationProblem>> CreateMeeting(
		CreateMeetingRequest request,
		ClaimsPrincipal user,
		MeetingsDb db
	)
	{
		var title = request.Title.Trim();
		if (ValidateTitle(title) is { } titleProblem)
		{
			return titleProblem;
		}

		var startDate = request.StartDate.ToUniversalTime();
		var endDate = request.EndDate.ToUniversalTime();
		if (endDate <= startDate)
		{
			return Invalid("endDate", "The meeting must end after it starts.");
		}

		var meeting = new Meeting
		{
			Id = Guid.CreateVersion7(),
			CreatorId = user.GetUserId(),
			Title = title,
			CreatedAt = DateTime.UtcNow,
			StartDate = startDate,
			EndDate = endDate,
		};
		db.Meetings.Add(meeting);
		await db.SaveChangesAsync();

		return TypedResults.Ok(new CreateMeetingResponse(meeting.Id));
	}

	private static async Task<Results<Ok<MeetingResponse>, NotFound, ForbidHttpResult, ValidationProblem>> UpdateMeeting(
		Guid id,
		UpdateMeetingRequest request,
		ClaimsPrincipal user,
		MeetingAccess access,
		MeetingsDb db
	)
	{
		var title = request.Title.Trim();
		if (ValidateTitle(title) is { } titleProblem)
		{
			return titleProblem;
		}

		var meeting = await access.FindAsync(user, id);
		if (meeting is not { Role: MeetingRole.Owner or MeetingRole.Participant })
		{
			return TypedResults.NotFound();
		}

		if (meeting.Role != MeetingRole.Owner)
		{
			return TypedResults.Forbid();
		}

		await db.Meetings
			.Where(m => m.Id == id && m.DeletedAt == null)
			.ExecuteUpdateAsync(s => s.SetProperty(m => m.Title, title));

		return TypedResults.Ok(ToResponse(meeting with { Title = title }));
	}

	private static async Task<Results<NoContent, NotFound>> DeleteMeeting(Guid id, ClaimsPrincipal user, MeetingsDb db)
	{
		var userId = user.GetUserId();
		var exists = await db.Meetings.AnyAsync(m => m.Id == id && m.CreatorId == userId);
		if (!exists)
		{
			return TypedResults.NotFound();
		}

		var now = DateTime.UtcNow;
		await db.Meetings
			.Where(m => m.Id == id && m.DeletedAt == null)
			.ExecuteUpdateAsync(s => s.SetProperty(m => m.DeletedAt, now));

		return TypedResults.NoContent();
	}

	private static async Task<Results<Ok<JoinTokenResponse>, NotFound>> GetJoinToken(
		Guid id,
		ClaimsPrincipal user,
		MeetingAccess access,
		LiveKitRooms liveKit
	)
	{
		var meeting = await access.FindAsync(user, id);
		if (meeting is not { Role: MeetingRole.Owner or MeetingRole.Participant })
		{
			return TypedResults.NotFound();
		}

		var token = liveKit.CreateJoinToken(id, user.GetUserId(), user.GetName());
		return TypedResults.Ok(new JoinTokenResponse(token, liveKit.PublicUrl));
	}

	private static async Task<Results<Ok<ParticipantResponse[]>, NotFound>> GetParticipants(
		Guid id,
		ClaimsPrincipal user,
		MeetingAccess access,
		MeetingsDb db
	)
	{
		var meeting = await access.FindAsync(user, id);
		if (meeting is not { Role: MeetingRole.Owner or MeetingRole.Participant })
		{
			return TypedResults.NotFound();
		}

		var participants = await db.Participants
			.AsNoTracking()
			.Where(p => p.MeetingId == id)
			.OrderBy(p => p.AddedAt)
			.Select(p => new ParticipantResponse(p.UserId, p.Email, p.DisplayName))
			.ToArrayAsync();

		return TypedResults.Ok(participants);
	}

	private static async Task<Results<Ok<ParticipantResponse>, NotFound, ForbidHttpResult, ValidationProblem>> AddParticipant(
		Guid id,
		AddParticipantRequest request,
		ClaimsPrincipal user,
		MeetingAccess access,
		IdentityClient identity,
		MeetingsDb db
	)
	{
		var meeting = await access.FindAsync(user, id);
		if (meeting is not { Role: MeetingRole.Owner or MeetingRole.Participant })
		{
			return TypedResults.NotFound();
		}

		if (meeting.Role != MeetingRole.Owner)
		{
			return TypedResults.Forbid();
		}

		var found = await identity.FindByEmailAsync(request.Email);
		if (found == null)
		{
			return Invalid("email", "No user with this email.");
		}

		if (found.Id == meeting.CreatorId)
		{
			return Invalid("email", "The owner is already in the meeting.");
		}

		var existing = await db.Participants.AsNoTracking().FirstOrDefaultAsync(p => p.MeetingId == id && p.UserId == found.Id);
		if (existing == null)
		{
			existing = new MeetingParticipant
			{
				MeetingId = id,
				UserId = found.Id,
				Email = found.Email,
				DisplayName = found.DisplayName,
				AddedAt = DateTime.UtcNow,
			};
			db.Participants.Add(existing);
			await db.SaveChangesAsync();
		}

		return TypedResults.Ok(new ParticipantResponse(existing.UserId, existing.Email, existing.DisplayName));
	}

	private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> RemoveParticipant(
		Guid id,
		Guid userId,
		ClaimsPrincipal user,
		MeetingAccess access,
		MeetingsDb db,
		LiveKitRooms liveKit,
		ILogger<LiveKitRooms> logger
	)
	{
		var meeting = await access.FindAsync(user, id);
		if (meeting is not { Role: MeetingRole.Owner or MeetingRole.Participant })
		{
			return TypedResults.NotFound();
		}

		if (meeting.Role != MeetingRole.Owner)
		{
			return TypedResults.Forbid();
		}

		await db.Participants.Where(p => p.MeetingId == id && p.UserId == userId).ExecuteDeleteAsync();

		try
		{
			await liveKit.RemoveParticipantAsync(id, userId);
		}
		catch (Exception e)
		{
			logger.LogWarning("Could not disconnect {UserId} from meeting {MeetingId}: {Message}", userId, id, e.Message);
		}

		return TypedResults.NoContent();
	}

	private static MeetingResponse ToResponse(MeetingView meeting)
	{
		return new MeetingResponse(meeting.Id, meeting.Title, meeting.StartDate, meeting.EndDate, meeting.Role == MeetingRole.Owner);
	}

	private static ValidationProblem? ValidateTitle(string title)
	{
		if (title.Length is < Meeting.MinTitleLength or > Meeting.MaxTitleLength)
		{
			return Invalid("title", $"Title must be from {Meeting.MinTitleLength} to {Meeting.MaxTitleLength} characters.");
		}

		return null;
	}

	private static ValidationProblem Invalid(string field, string message)
	{
		return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
	}
}
