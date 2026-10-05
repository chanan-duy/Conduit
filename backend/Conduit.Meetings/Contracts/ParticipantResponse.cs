namespace Conduit.Meetings.Contracts;

public record ParticipantResponse(Guid UserId, string Email, string DisplayName);
