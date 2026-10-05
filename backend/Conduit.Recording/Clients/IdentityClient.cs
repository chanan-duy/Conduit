namespace Conduit.Recording.Clients;

public class IdentityClient(HttpClient http)
{
	public async Task<string> CreateBotGrantAsync(Guid meetingId, Guid sessionId)
	{
		using var response = await http.PostAsJsonAsync("internal/recording-bots", new { meetingId, sessionId });
		response.EnsureSuccessStatusCode();
		var grant = await response.Content.ReadFromJsonAsync<RecordingBotGrant>();
		return grant!.ExchangeToken;
	}
}
