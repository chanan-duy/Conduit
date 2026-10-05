namespace Conduit.Meetings.Contracts;

public record ChatMessageDto(Guid Id, long Sequence, string Content, Guid CreatorId, string CreatorName, DateTime CreatedAt);
