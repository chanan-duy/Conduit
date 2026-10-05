namespace Conduit.Recording.Data;

public class RecordingRoom
{
	public required Guid MeetingId { get; init; }
	public DateTime? DeletedAt { get; set; }
	public DateTime? CleanedAt { get; set; }
}
