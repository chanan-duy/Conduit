using Conduit.Recording.Services;
using Conduit.Shared;

namespace Conduit.Recording.Endpoints;

public static class InternalEndpoints
{
	public static void MapInternalEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapInternalGroup();

		group.MapDelete("/meetings/{id:guid}", async (Guid id, RecordingSessions sessions) =>
			await sessions.TryCleanUpMeetingAsync(id) ? Results.NoContent() : Results.Accepted());
	}
}
