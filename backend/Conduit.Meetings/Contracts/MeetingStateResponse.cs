namespace Conduit.Meetings.Contracts;

public record MeetingStateResponse(Guid Id, bool Active, bool CanAccess);
