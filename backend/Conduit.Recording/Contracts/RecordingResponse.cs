namespace Conduit.Recording.Contracts;

public record RecordingResponse(Guid Id, string Status, DateTime CreatedAt, DateTime UpdatedAt, string? Error);
