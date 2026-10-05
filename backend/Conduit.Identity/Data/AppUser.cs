using Microsoft.AspNetCore.Identity;

namespace Conduit.Identity.Data;

public class AppUser : IdentityUser<Guid>
{
	public const int MaxDisplayNameLength = 254;

	public string DisplayName { get; set; } = string.Empty;
}
