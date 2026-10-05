using System.Net;

namespace Conduit.Recording.Clients;

public class MeetingsClient(HttpClient http)
{
	public async Task<MeetingState?> GetStateAsync(Guid meetingId, Guid? userId = null)
	{
		var query = userId == null ? string.Empty : $"?userId={userId}";
		using var response = await http.GetAsync($"internal/meetings/{meetingId}/state{query}");
		if (response.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}

		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<MeetingState>();
	}
}
