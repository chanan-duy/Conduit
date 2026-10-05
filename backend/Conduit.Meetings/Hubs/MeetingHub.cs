using Conduit.Meetings.Contracts;
using Conduit.Meetings.Data;
using Conduit.Meetings.Services;
using Conduit.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Meetings.Hubs;

[Authorize]
public class MeetingHub(MeetingAccess access, MeetingsDb db) : Hub<IMeetingClient>
{
	public const int HistoryPageSize = 50;

	public async Task SubscribeToMeeting(Guid meetingId)
	{
		var meeting = await RequireAccessAsync(meetingId, ConduitPermissions.ChatSubscribe, allowBot: true);
		await Groups.AddToGroupAsync(Context.ConnectionId, RecipientGroup(meetingId, meeting.Role, Context.User!.GetUserId()));
	}

	public async Task UnsubscribeFromMeeting(Guid meetingId)
	{
		var group = RecipientGroup(meetingId, MeetingRole.Participant, Context.User!.GetUserId());
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, RecipientGroup(meetingId, MeetingRole.RecorderBot, Guid.Empty));
	}

	public async Task<ChatMessageDto[]> RetrieveChatMessagesHistory(Guid meetingId, long afterSequence)
	{
		await RequireAccessAsync(meetingId, ConduitPermissions.ChatHistory, allowBot: true);

		return await db.Messages
			.AsNoTracking()
			.Where(m => m.MeetingId == meetingId && m.Sequence > afterSequence)
			.OrderBy(m => m.Sequence)
			.Take(HistoryPageSize)
			.Select(m => new ChatMessageDto(m.Id, m.Sequence, m.Content, m.AuthorId, m.AuthorName, m.CreatedAt))
			.ToArrayAsync();
	}

	public async Task<ChatMessageDto> SendChatMessage(Guid meetingId, string content)
	{
		if (string.IsNullOrWhiteSpace(content) || content.Length > ChatMessage.MaxContentLength)
		{
			throw new HubException("invalid_input");
		}

		await RequireAccessAsync(meetingId, ConduitPermissions.ChatSend, allowBot: false);

		await using var transaction = await db.Database.BeginTransactionAsync();

		var sequence = await db.Database
			.SqlQuery<long>($"""
			                 UPDATE "Meetings" SET "LastMessageSequence" = "LastMessageSequence" + 1
			                 WHERE "Id" = {meetingId} AND "DeletedAt" IS NULL
			                 RETURNING "LastMessageSequence" AS "Value"
			                 """)
			.ToListAsync();
		if (sequence.Count == 0)
		{
			throw new HubException("not_found");
		}

		var user = Context.User!;
		var message = new ChatMessage
		{
			Id = Guid.CreateVersion7(),
			MeetingId = meetingId,
			Sequence = sequence[0],
			AuthorId = user.GetUserId(),
			AuthorName = user.GetName(),
			Content = content,
			CreatedAt = DateTime.UtcNow,
		};
		db.Messages.Add(message);
		await db.SaveChangesAsync();
		await transaction.CommitAsync();

		var dto = new ChatMessageDto(message.Id, message.Sequence, message.Content, message.AuthorId, message.AuthorName, message.CreatedAt);
		await Clients.Groups(await CurrentRecipientGroupsAsync(meetingId)).ReceiveChatMessage(dto);

		return dto;
	}

	private async Task<MeetingView> RequireAccessAsync(Guid meetingId, string permission, bool allowBot)
	{
		var user = Context.User!;
		if (!user.HasPermission(permission))
		{
			throw new HubException("forbidden");
		}

		var meeting = await access.FindAsync(user, meetingId);
		if (meeting is not { Role: MeetingRole.Owner or MeetingRole.Participant or MeetingRole.RecorderBot } ||
		    (meeting.Role == MeetingRole.RecorderBot && !allowBot))
		{
			throw new HubException("forbidden");
		}

		return meeting;
	}

	private async Task<string[]> CurrentRecipientGroupsAsync(Guid meetingId)
	{
		var members = await db.Meetings
			.AsNoTracking()
			.Where(m => m.Id == meetingId)
			.Select(m => new { m.CreatorId, Participants = m.Participants.Select(p => p.UserId).ToList() })
			.SingleAsync();

		return members.Participants
			.Append(members.CreatorId)
			.Select(userId => RecipientGroup(meetingId, MeetingRole.Participant, userId))
			.Append(RecipientGroup(meetingId, MeetingRole.RecorderBot, Guid.Empty))
			.ToArray();
	}

	private static string RecipientGroup(Guid meetingId, MeetingRole role, Guid userId)
	{
		return role == MeetingRole.RecorderBot ? $"{meetingId}:bots" : $"{meetingId}:{userId}";
	}
}
