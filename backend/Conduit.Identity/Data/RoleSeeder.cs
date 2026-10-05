using System.Security.Claims;
using Conduit.Shared;
using Microsoft.AspNetCore.Identity;

namespace Conduit.Identity.Data;

public static class RoleSeeder
{
	private static readonly Dictionary<string, string[]> RolePermissions = new()
	{
		[ConduitRoles.User] =
		[
			ConduitPermissions.MeetingsView,
			ConduitPermissions.MeetingsManage,
			ConduitPermissions.MeetingsJoin,
			ConduitPermissions.ChatSubscribe,
			ConduitPermissions.ChatHistory,
			ConduitPermissions.ChatSend,
			ConduitPermissions.RecordingsView,
		],
		[ConduitRoles.RecorderBot] =
		[
			ConduitPermissions.ChatSubscribe,
			ConduitPermissions.ChatHistory,
		],
	};

	public static async Task SeedRolesAsync(this WebApplication app)
	{
		await using var scope = app.Services.CreateAsyncScope();
		var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

		foreach (var (roleName, permissions) in RolePermissions)
		{
			var role = await roleManager.FindByNameAsync(roleName);
			if (role == null)
			{
				role = new IdentityRole<Guid>(roleName) { Id = Guid.CreateVersion7() };
				await roleManager.CreateAsync(role);
			}

			var existing = await roleManager.GetClaimsAsync(role);
			foreach (var permission in permissions)
			{
				if (!existing.Any(c => c.Type == ConduitClaims.Permissions && c.Value == permission))
				{
					await roleManager.AddClaimAsync(role, new Claim(ConduitClaims.Permissions, permission));
				}
			}
		}
	}
}
