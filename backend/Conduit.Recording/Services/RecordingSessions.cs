using Conduit.Recording.Clients;
using Conduit.Recording.Data;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Recording.Services;

public class RecordingSessions(
	RecordingDb db,
	MeetingsClient meetings,
	IdentityClient identity,
	EgressClient egress,
	RecordingFiles files,
	ILogger<RecordingSessions> logger
)
{
	private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(2);

	public async Task StartAsync(Guid meetingId, string roomSid)
	{
		var meeting = await meetings.GetStateAsync(meetingId);
		if (meeting is not { Active: true })
		{
			logger.LogInformation("Meeting {MeetingId} is not active, recording skipped", meetingId);
			return;
		}

		var sessionId = Guid.CreateVersion7();
		var session = new RecordingSession
		{
			Id = sessionId,
			MeetingId = meetingId,
			RoomSid = roomSid,
			Status = SessionStatus.Starting,
			FileKey = $"{sessionId}.mp4",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
		};

		await using (var transaction = await db.Database.BeginTransactionAsync())
		{
			var room = await LockRoomAsync(meetingId);
			if (room.DeletedAt != null || await db.Sessions.AnyAsync(s => s.RoomSid == roomSid))
			{
				return;
			}

			db.Sessions.Add(session);
			await db.SaveChangesAsync();
			await transaction.CommitAsync();
		}

		try
		{
			var exchangeToken = await identity.CreateBotGrantAsync(meetingId, session.Id);
			session.EgressId = await egress.StartAsync(meetingId, exchangeToken, session.FileKey);
			logger.LogInformation("Recording {SessionId} started for meeting {MeetingId}", session.Id, meetingId);
		}
		catch (Exception e)
		{
			session.Status = SessionStatus.Failed;
			session.Error = e.Message;
			logger.LogWarning("Recording {SessionId} could not start: {Message}", session.Id, e.Message);
		}

		session.UpdatedAt = DateTime.UtcNow;
		await db.SaveChangesAsync();
	}

	public async Task ApplyEgressUpdateAsync(EgressInfo info)
	{
		var session = await db.Sessions.FirstOrDefaultAsync(s => s.EgressId == info.EgressId) ??
		              await db.Sessions.FirstOrDefaultAsync(s => s.EgressId == null && s.RoomSid == info.RoomId);
		if (session == null)
		{
			return;
		}

		session.EgressId ??= info.EgressId;
		ApplyEgressStatus(session, info);
		await db.SaveChangesAsync();
	}

	public async Task<bool> TryCleanUpMeetingAsync(Guid meetingId)
	{
		RecordingRoom room;
		await using (var transaction = await db.Database.BeginTransactionAsync())
		{
			room = await LockRoomAsync(meetingId);
			room.DeletedAt ??= DateTime.UtcNow;
			await db.SaveChangesAsync();
			await transaction.CommitAsync();
		}

		if (room.CleanedAt != null)
		{
			return true;
		}

		var sessions = await db.Sessions.Where(s => s.MeetingId == meetingId).ToListAsync();
		foreach (var session in sessions.Where(s => s.IsActive))
		{
			await StopAsync(session);
		}

		await db.SaveChangesAsync();
		if (sessions.Any(s => s.IsActive))
		{
			return false;
		}

		foreach (var session in sessions)
		{
			files.Delete(session.FileKey);
		}

		await using (var transaction = await db.Database.BeginTransactionAsync())
		{
			await db.Sessions.Where(s => s.MeetingId == meetingId).ExecuteDeleteAsync();
			var now = DateTime.UtcNow;
			await db.Rooms.Where(r => r.MeetingId == meetingId).ExecuteUpdateAsync(s => s.SetProperty(r => r.CleanedAt, now));
			await transaction.CommitAsync();
		}

		logger.LogInformation("Recordings of meeting {MeetingId} removed", meetingId);
		return true;
	}

	private async Task StopAsync(RecordingSession session)
	{
		if (session.EgressId == null)
		{
			if (DateTime.UtcNow - session.CreatedAt > StartTimeout)
			{
				session.Status = SessionStatus.Failed;
				session.Error = "Recording did not start.";
				session.UpdatedAt = DateTime.UtcNow;
			}

			return;
		}

		var info = await egress.FindAsync(session.EgressId);
		if (info == null)
		{
			session.Status = SessionStatus.Failed;
			session.Error = "Egress has no record of this recording.";
			session.UpdatedAt = DateTime.UtcNow;
			return;
		}

		ApplyEgressStatus(session, info);
		if (session.IsActive && info.Status != EgressStatus.EgressEnding)
		{
			await egress.StopAsync(session.EgressId);
		}
	}

	private void ApplyEgressStatus(RecordingSession session, EgressInfo info)
	{
		if (!session.IsActive)
		{
			return;
		}

		var status = EgressClient.ToSessionStatus(info.Status);
		if (status == SessionStatus.Completed && !files.Exists(session.FileKey))
		{
			status = SessionStatus.Failed;
			info.Error = "Egress finished, but the file is missing.";
		}

		session.Status = status;
		session.Error = status == SessionStatus.Failed ? info.Error : null;
		session.UpdatedAt = DateTime.UtcNow;
	}

	private async Task<RecordingRoom> LockRoomAsync(Guid meetingId)
	{
		await db.Database.ExecuteSqlAsync($"""
		                                   INSERT INTO "Rooms" ("MeetingId") VALUES ({meetingId}) ON CONFLICT DO NOTHING
		                                   """);
		return await db.Rooms
			.FromSql($"""SELECT * FROM "Rooms" WHERE "MeetingId" = {meetingId} FOR UPDATE""")
			.SingleAsync();
	}
}
