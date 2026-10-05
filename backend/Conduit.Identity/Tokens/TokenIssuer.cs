using System.Security.Claims;
using System.Security.Cryptography;
using Conduit.Identity.Contracts;
using Conduit.Identity.Data;
using Conduit.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Conduit.Identity.Tokens;

public class TokenIssuer(
	SigningKey signingKey,
	IdentityDb db,
	UserManager<AppUser> userManager,
	RoleManager<IdentityRole<Guid>> roleManager
)
{
	public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(10);
	public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);
	public static readonly TimeSpan BotTokenLifetime = TimeSpan.FromHours(2);
	public static readonly TimeSpan BotGrantLifetime = TimeSpan.FromMinutes(2);

	private readonly JsonWebTokenHandler _handler = new();

	public async Task<TokenResponse> IssueForUserAsync(AppUser user)
	{
		var claims = new List<Claim>
		{
			new(ConduitClaims.Subject, user.Id.ToString()),
			new(ConduitClaims.Name, user.DisplayName),
		};

		foreach (var roleName in await userManager.GetRolesAsync(user))
		{
			claims.Add(new Claim(ConduitClaims.Role, roleName));
			claims.AddRange(await GetPermissionsAsync(roleName));
		}

		var refreshToken = CreateSecret();
		db.RefreshTokens.Add(new RefreshToken
		{
			TokenHash = Hash(refreshToken),
			UserId = user.Id,
			ExpiresAt = DateTime.UtcNow + RefreshTokenLifetime,
		});
		await db.SaveChangesAsync();

		return new TokenResponse("Bearer", CreateJwt(claims, AccessTokenLifetime), (long)AccessTokenLifetime.TotalSeconds, refreshToken);
	}

	public async Task<string> IssueForBotAsync(BotGrant grant)
	{
		var claims = new List<Claim>
		{
			new(ConduitClaims.Subject, grant.SessionId.ToString()),
			new(ConduitClaims.Name, "Recording bot"),
			new(ConduitClaims.Role, ConduitRoles.RecorderBot),
			new(ConduitClaims.MeetingId, grant.MeetingId.ToString()),
			new(ConduitClaims.SessionId, grant.SessionId.ToString()),
		};
		claims.AddRange(await GetPermissionsAsync(ConduitRoles.RecorderBot));

		return CreateJwt(claims, BotTokenLifetime);
	}

	public async Task<string> CreateBotGrantAsync(Guid meetingId, Guid sessionId)
	{
		var exchangeToken = CreateSecret();
		db.BotGrants.Add(new BotGrant
		{
			TokenHash = Hash(exchangeToken),
			MeetingId = meetingId,
			SessionId = sessionId,
			ExpiresAt = DateTime.UtcNow + BotGrantLifetime,
		});
		await db.SaveChangesAsync();

		return exchangeToken;
	}

	public static string Hash(string secret)
	{
		return Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret)));
	}

	private async Task<IEnumerable<Claim>> GetPermissionsAsync(string roleName)
	{
		var role = await roleManager.FindByNameAsync(roleName);
		if (role == null)
		{
			return [];
		}

		var roleClaims = await roleManager.GetClaimsAsync(role);
		return roleClaims.Where(c => c.Type == ConduitClaims.Permissions);
	}

	private string CreateJwt(IEnumerable<Claim> claims, TimeSpan lifetime)
	{
		var now = DateTime.UtcNow;
		return _handler.CreateToken(new SecurityTokenDescriptor
		{
			Issuer = ConduitTokens.Issuer,
			Audience = ConduitTokens.Audience,
			Subject = new ClaimsIdentity(claims),
			IssuedAt = now,
			NotBefore = now,
			Expires = now + lifetime,
			SigningCredentials = signingKey.Credentials,
		});
	}

	private static string CreateSecret()
	{
		return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
	}
}
