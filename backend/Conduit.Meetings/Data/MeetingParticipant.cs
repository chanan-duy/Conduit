namespace Conduit.Meetings.Data;

public class MeetingParticipant
{
	public required Guid MeetingId { get; init; }
	public required Guid UserId { get; init; }
	public required string Email { get; init; }
	public required string DisplayName { get; init; }
	public required DateTime AddedAt { get; init; }
}
