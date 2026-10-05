using System.Net;

namespace Conduit.Meetings.Clients;

public class RecordingClient(HttpClient http)
{
	public async Task<bool> TryCleanUpMeetingAsync(Guid meetingId)
	{
		using var response = await http.DeleteAsync($"internal/meetings/{meetingId}");
		response.EnsureSuccessStatusCode();
		return response.StatusCode == HttpStatusCode.NoContent;
	}
}
