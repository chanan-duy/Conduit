namespace Conduit.Identity.Data;

public class BotGrant
{
	public required string TokenHash { get; init; }
	public required Guid MeetingId { get; init; }
	public required Guid SessionId { get; init; }
	public required DateTime ExpiresAt { get; init; }
	public DateTime? ConsumedAt { get; set; }
}
