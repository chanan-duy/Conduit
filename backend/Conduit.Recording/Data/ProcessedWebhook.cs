namespace Conduit.Recording.Data;

public class ProcessedWebhook
{
	public required string EventId { get; init; }
	public required string Type { get; init; }
	public required DateTime ReceivedAt { get; init; }
}
