using System.Security.Claims;

namespace Conduit.Shared;

public static class PrincipalExtensions
{
	public static Guid GetUserId(this ClaimsPrincipal principal)
	{
		return Guid.TryParse(principal.FindFirstValue(ConduitClaims.Subject), out var userId) ? userId : Guid.Empty;
	}

	public static string GetName(this ClaimsPrincipal principal)
	{
		return principal.FindFirstValue(ConduitClaims.Name) ?? string.Empty;
	}

	public static bool HasPermission(this ClaimsPrincipal principal, string permission)
	{
		return principal.HasClaim(ConduitClaims.Permissions, permission);
	}

	public static bool IsRecorderBotFor(this ClaimsPrincipal principal, Guid meetingId)
	{
		return principal.IsInRole(ConduitRoles.RecorderBot) &&
		       principal.HasClaim(ConduitClaims.MeetingId, meetingId.ToString());
	}
}
