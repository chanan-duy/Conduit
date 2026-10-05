namespace Conduit.Meetings.Services;

public record MeetingView(Guid Id, Guid CreatorId, string Title, DateTime StartDate, DateTime EndDate, MeetingRole Role);
