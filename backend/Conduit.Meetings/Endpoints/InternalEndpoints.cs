using Conduit.Meetings.Contracts;
using Conduit.Meetings.Data;
using Conduit.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Meetings.Endpoints;

public static class InternalEndpoints
{
	public static void MapInternalEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapInternalGroup();

		group.MapGet("/meetings/{id:guid}/state", async Task<Results<Ok<MeetingStateResponse>, NotFound>> (
			Guid id,
			Guid? userId,
			MeetingsDb db
		) =>
		{
			var meeting = await db.Meetings
				.AsNoTracking()
				.Where(m => m.Id == id)
				.Select(m => new
				{
					m.DeletedAt,
					CanAccess = userId != null && (m.CreatorId == userId || m.Participants.Any(p => p.UserId == userId)),
				})
				.FirstOrDefaultAsync();

			if (meeting == null)
			{
				return TypedResults.NotFound();
			}

			var active = meeting.DeletedAt == null;
			return TypedResults.Ok(new MeetingStateResponse(id, active, active && meeting.CanAccess));
		});
	}
}
