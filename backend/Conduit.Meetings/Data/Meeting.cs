namespace Conduit.Meetings.Data;

public class Meeting
{
	public const int MinTitleLength = 3;
	public const int MaxTitleLength = 100;

	public required Guid Id { get; init; }
	public required Guid CreatorId { get; init; }
	public required string Title { get; set; }
	public required DateTime CreatedAt { get; init; }
	public required DateTime StartDate { get; set; }
	public required DateTime EndDate { get; set; }
	public DateTime? DeletedAt { get; set; }
	public DateTime? CleanedAt { get; set; }
	public long LastMessageSequence { get; set; }

	public List<MeetingParticipant> Participants { get; init; } = [];
}
