namespace Conduit.Meetings.Contracts;

public record MeetingResponse(Guid Id, string Title, DateTime StartDate, DateTime EndDate, bool IsOwner);
