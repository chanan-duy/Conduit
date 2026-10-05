using Conduit.Meetings.Clients;
using Conduit.Meetings.Data;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Meetings.Services;

public class MeetingCleanupWorker(IServiceScopeFactory scopeFactory, LiveKitRooms liveKit, ILogger<MeetingCleanupWorker> logger)
	: BackgroundService
{
	private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(Interval);
		do
		{
			try
			{
				await CleanUpDeletedMeetingsAsync(stoppingToken);
			}
			catch (Exception e) when (!stoppingToken.IsCancellationRequested)
			{
				logger.LogWarning("Cleanup pass failed: {Message}", e.Message);
			}
		} while (await timer.WaitForNextTickAsync(stoppingToken));
	}

	private async Task CleanUpDeletedMeetingsAsync(CancellationToken cancellationToken)
	{
		await using var scope = scopeFactory.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<MeetingsDb>();
		var recording = scope.ServiceProvider.GetRequiredService<RecordingClient>();

		var meetingIds = await db.Meetings
			.Where(m => m.DeletedAt != null && m.CleanedAt == null)
			.OrderBy(m => m.DeletedAt)
			.Select(m => m.Id)
			.Take(20)
			.ToListAsync(cancellationToken);

		foreach (var meetingId in meetingIds)
		{
			var roomClosed = await TryAsync(meetingId, "close the video room", () => liveKit.CloseRoomAsync(meetingId));

			var recordingsCleaned = false;
			await TryAsync(meetingId, "clean up recordings", async () => recordingsCleaned = await recording.TryCleanUpMeetingAsync(meetingId));

			if (!roomClosed || !recordingsCleaned)
			{
				continue;
			}

			await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
			await db.Messages.Where(m => m.MeetingId == meetingId).ExecuteDeleteAsync(cancellationToken);
			await db.Participants.Where(p => p.MeetingId == meetingId).ExecuteDeleteAsync(cancellationToken);
			var now = DateTime.UtcNow;
			await db.Meetings.Where(m => m.Id == meetingId).ExecuteUpdateAsync(s => s.SetProperty(m => m.CleanedAt, now), cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			logger.LogInformation("Meeting {MeetingId} cleaned up", meetingId);
		}
	}

	private async Task<bool> TryAsync(Guid meetingId, string step, Func<Task> action)
	{
		try
		{
			await action();
			return true;
		}
		catch (Exception e)
		{
			logger.LogWarning("Meeting {MeetingId}: could not {Step}, will retry: {Message}", meetingId, step, e.Message);
			return false;
		}
	}
}
