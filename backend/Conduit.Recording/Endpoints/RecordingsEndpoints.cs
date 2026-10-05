using System.Security.Claims;
using Conduit.Recording.Clients;
using Conduit.Recording.Contracts;
using Conduit.Recording.Data;
using Conduit.Recording.Services;
using Conduit.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Recording.Endpoints;

public static class RecordingsEndpoints
{
	public static void MapRecordingsEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/v1/recordings").WithTags("Recordings").RequirePermission(ConduitPermissions.RecordingsView);

		group.MapGet("/", GetRecordings);
		group.MapGet("/{id:guid}/file", GetRecordingFile);
	}

	private static async Task<Results<Ok<RecordingResponse[]>, NotFound>> GetRecordings(
		Guid meetingId,
		ClaimsPrincipal user,
		MeetingsClient meetings,
		RecordingDb db
	)
	{
		var meeting = await meetings.GetStateAsync(meetingId, user.GetUserId());
		if (meeting is not { CanAccess: true })
		{
			return TypedResults.NotFound();
		}

		var recordings = await db.Sessions
			.AsNoTracking()
			.Where(s => s.MeetingId == meetingId)
			.OrderByDescending(s => s.CreatedAt)
			.Select(s => new RecordingResponse(s.Id, s.Status.ToString(), s.CreatedAt, s.UpdatedAt, s.Error))
			.ToArrayAsync();

		return TypedResults.Ok(recordings);
	}

	private static async Task<Results<PhysicalFileHttpResult, NotFound>> GetRecordingFile(
		Guid id,
		ClaimsPrincipal user,
		MeetingsClient meetings,
		RecordingDb db,
		RecordingFiles files
	)
	{
		var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.Status == SessionStatus.Completed);
		if (session == null || !files.Exists(session.FileKey))
		{
			return TypedResults.NotFound();
		}

		var meeting = await meetings.GetStateAsync(session.MeetingId, user.GetUserId());
		if (meeting is not { CanAccess: true })
		{
			return TypedResults.NotFound();
		}

		var downloadName = $"meeting-{session.CreatedAt:yyyy-MM-dd_HH-mm}.mp4";
		return TypedResults.PhysicalFile(Path.GetFullPath(files.GetPath(session.FileKey)), "video/mp4", downloadName, enableRangeProcessing: true);
	}
}
