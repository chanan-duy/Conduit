using Conduit.Recording.Data;
using Conduit.Recording.Services;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Conduit.Recording.Endpoints;

public static class WebhookEndpoints
{
	public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
	{
		app.MapPost("/webhooks/livekit", ReceiveLiveKitEvent).ExcludeFromDescription();
	}

	private static async Task<Results<Ok, UnauthorizedHttpResult>> ReceiveLiveKitEvent(
		HttpRequest request,
		LiveKitWebhooks webhooks,
		RecordingDb db,
		RecordingSessions sessions,
		ILogger<LiveKitWebhooks> logger
	)
	{
		using var reader = new StreamReader(request.Body);
		var body = await reader.ReadToEndAsync();

		var liveKitEvent = await webhooks.ParseAsync(body, request.Headers.Authorization.ToString());
		if (liveKitEvent == null)
		{
			logger.LogWarning("Rejected a webhook with an invalid signature");
			return TypedResults.Unauthorized();
		}

		var stored = new ProcessedWebhook { EventId = liveKitEvent.Id, Type = liveKitEvent.Event, ReceivedAt = DateTime.UtcNow };
		db.WebhookEvents.Add(stored);
		try
		{
			await db.SaveChangesAsync();
		}
		catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
		{
			return TypedResults.Ok();
		}

		try
		{
			await HandleAsync(liveKitEvent, sessions);
		}
		catch
		{
			db.WebhookEvents.Remove(stored);
			await db.SaveChangesAsync();
			throw;
		}

		return TypedResults.Ok();
	}

	private static async Task HandleAsync(WebhookEvent liveKitEvent, RecordingSessions sessions)
	{
		switch (liveKitEvent.Event)
		{
			case "room_started" when Guid.TryParse(liveKitEvent.Room?.Name, out var meetingId):
				await sessions.StartAsync(meetingId, liveKitEvent.Room.Sid);
				break;
			case "egress_started" or "egress_updated" or "egress_ended" when liveKitEvent.EgressInfo != null:
				await sessions.ApplyEgressUpdateAsync(liveKitEvent.EgressInfo);
				break;
		}
	}
}
