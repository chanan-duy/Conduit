namespace Conduit.Recording.Data;

public class RecordingSession
{
	public required Guid Id { get; init; }
	public required Guid MeetingId { get; init; }
	public required string RoomSid { get; init; }
	public string? EgressId { get; set; }
	public required SessionStatus Status { get; set; }
	public required string FileKey { get; init; }
	public string? Error { get; set; }
	public required DateTime CreatedAt { get; init; }
	public required DateTime UpdatedAt { get; set; }

	public bool IsActive => Status is SessionStatus.Starting or SessionStatus.Recording;
}
