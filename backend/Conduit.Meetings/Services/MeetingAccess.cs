using System.Security.Claims;
using Conduit.Meetings.Data;
using Conduit.Shared;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Meetings.Services;

public class MeetingAccess(MeetingsDb db)
{
	public async Task<MeetingView?> FindAsync(ClaimsPrincipal user, Guid meetingId)
	{
		var userId = user.GetUserId();

		var meeting = await db.Meetings
			.AsNoTracking()
			.Where(m => m.Id == meetingId && m.DeletedAt == null)
			.Select(m => new
			{
				m.Id,
				m.CreatorId,
				m.Title,
				m.StartDate,
				m.EndDate,
				IsParticipant = m.Participants.Any(p => p.UserId == userId),
			})
			.FirstOrDefaultAsync();

		if (meeting == null)
		{
			return null;
		}

		var role = MeetingRole.None;
		if (meeting.CreatorId == userId)
		{
			role = MeetingRole.Owner;
		}
		else if (meeting.IsParticipant)
		{
			role = MeetingRole.Participant;
		}
		else if (user.IsRecorderBotFor(meetingId))
		{
			role = MeetingRole.RecorderBot;
		}

		return new MeetingView(meeting.Id, meeting.CreatorId, meeting.Title, meeting.StartDate, meeting.EndDate, role);
	}
}
