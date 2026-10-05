using System.Net;

namespace Conduit.Meetings.Clients;

public class IdentityClient(HttpClient http)
{
	public async Task<IdentityUser?> FindByEmailAsync(string email)
	{
		using var response = await http.GetAsync($"internal/users?email={Uri.EscapeDataString(email)}");
		if (response.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}

		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<IdentityUser>();
	}
}
