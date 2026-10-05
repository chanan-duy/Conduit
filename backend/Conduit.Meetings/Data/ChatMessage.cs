namespace Conduit.Meetings.Data;

public class ChatMessage
{
	public const int MaxContentLength = 10_000;

	public required Guid Id { get; init; }
	public required Guid MeetingId { get; init; }
	public required long Sequence { get; init; }
	public required Guid AuthorId { get; init; }
	public required string AuthorName { get; init; }
	public required string Content { get; init; }
	public required DateTime CreatedAt { get; init; }
}
