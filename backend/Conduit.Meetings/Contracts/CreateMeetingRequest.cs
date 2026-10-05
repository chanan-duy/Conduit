namespace Conduit.Meetings.Contracts;

public record CreateMeetingRequest(string Title, DateTime StartDate, DateTime EndDate);
