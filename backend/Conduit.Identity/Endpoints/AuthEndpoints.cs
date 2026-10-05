using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Conduit.Identity.Contracts;
using Conduit.Identity.Data;
using Conduit.Identity.Tokens;
using Conduit.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Identity.Endpoints;

public static class AuthEndpoints
{
	private static readonly EmailAddressAttribute EmailValidator = new();

	public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

		group.MapPost("/register", Register);
		group.MapPost("/login", Login);
		group.MapPost("/refresh", Refresh);
		group.MapPost("/logout", Logout);
		group.MapGet("/profile", GetProfile).RequireAuthorization();
		group.MapPatch("/profile", UpdateProfile).RequireAuthorization();
		group.MapPost("/exchange-token", ExchangeToken);
	}

	private static async Task<Results<Ok, ValidationProblem>> Register(RegisterRequest request, UserManager<AppUser> userManager)
	{
		var email = request.Email.Trim();
		if (email.Length > 254 || !EmailValidator.IsValid(email))
		{
			return Invalid("email", "Enter a valid email address.");
		}

		var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim();
		if (displayName.Length > AppUser.MaxDisplayNameLength)
		{
			return Invalid("displayName", $"Name must be at most {AppUser.MaxDisplayNameLength} characters.");
		}

		var user = new AppUser
		{
			Id = Guid.CreateVersion7(),
			UserName = email,
			Email = email,
			DisplayName = displayName,
		};

		var result = await userManager.CreateAsync(user, request.Password);
		if (!result.Succeeded)
		{
			return TypedResults.ValidationProblem(result.Errors
				.GroupBy(e => e.Code)
				.ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
		}

		await userManager.AddToRoleAsync(user, ConduitRoles.User);

		return TypedResults.Ok();
	}

	private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> Login(
		LoginRequest request,
		UserManager<AppUser> userManager,
		SignInManager<AppUser> signInManager,
		TokenIssuer tokenIssuer
	)
	{
		var user = await userManager.FindByEmailAsync(request.Email.Trim());
		if (user == null)
		{
			return TypedResults.Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
		}

		var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
		if (!result.Succeeded)
		{
			var detail = result.IsLockedOut ? "Too many attempts, try again later." : "Invalid email or password.";
			return TypedResults.Problem(detail, statusCode: StatusCodes.Status401Unauthorized);
		}

		return TypedResults.Ok(await tokenIssuer.IssueForUserAsync(user));
	}

	private static async Task<Results<Ok<TokenResponse>, UnauthorizedHttpResult>> Refresh(
		RefreshRequest request,
		IdentityDb db,
		UserManager<AppUser> userManager,
		TokenIssuer tokenIssuer
	)
	{
		var hash = TokenIssuer.Hash(request.RefreshToken);
		var now = DateTime.UtcNow;

		await using var transaction = await db.Database.BeginTransactionAsync();

		var revoked = await db.RefreshTokens
			.Where(t => t.TokenHash == hash && t.RevokedAt == null && t.ExpiresAt > now)
			.ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
		if (revoked == 0)
		{
			return TypedResults.Unauthorized();
		}

		var userId = await db.RefreshTokens.Where(t => t.TokenHash == hash).Select(t => t.UserId).SingleAsync();
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user == null)
		{
			return TypedResults.Unauthorized();
		}

		var response = await tokenIssuer.IssueForUserAsync(user);
		await transaction.CommitAsync();

		return TypedResults.Ok(response);
	}

	private static async Task<NoContent> Logout(RefreshRequest request, IdentityDb db)
	{
		var hash = TokenIssuer.Hash(request.RefreshToken);
		var now = DateTime.UtcNow;

		await db.RefreshTokens
			.Where(t => t.TokenHash == hash && t.RevokedAt == null)
			.ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));

		return TypedResults.NoContent();
	}

	private static async Task<Results<Ok<ProfileResponse>, NotFound>> GetProfile(ClaimsPrincipal principal, UserManager<AppUser> userManager)
	{
		var user = await userManager.FindByIdAsync(principal.GetUserId().ToString());
		if (user == null)
		{
			return TypedResults.NotFound();
		}

		return TypedResults.Ok(new ProfileResponse(user.Id, user.Email ?? string.Empty, user.DisplayName));
	}

	private static async Task<Results<Ok<ProfileResponse>, NotFound, ValidationProblem>> UpdateProfile(
		UpdateProfileRequest request,
		ClaimsPrincipal principal,
		UserManager<AppUser> userManager
	)
	{
		var displayName = request.DisplayName.Trim();
		if (displayName.Length is 0 or > AppUser.MaxDisplayNameLength)
		{
			return Invalid("displayName", $"Name must be from 1 to {AppUser.MaxDisplayNameLength} characters.");
		}

		var user = await userManager.FindByIdAsync(principal.GetUserId().ToString());
		if (user == null)
		{
			return TypedResults.NotFound();
		}

		user.DisplayName = displayName;
		await userManager.UpdateAsync(user);

		return TypedResults.Ok(new ProfileResponse(user.Id, user.Email ?? string.Empty, user.DisplayName));
	}

	private static async Task<Results<Ok<ExchangeResponse>, UnauthorizedHttpResult>> ExchangeToken(
		ExchangeRequest request,
		IdentityDb db,
		TokenIssuer tokenIssuer
	)
	{
		var hash = TokenIssuer.Hash(request.ExchangeToken);
		var now = DateTime.UtcNow;

		var consumed = await db.BotGrants
			.Where(g => g.TokenHash == hash && g.MeetingId == request.MeetingId && g.ConsumedAt == null && g.ExpiresAt > now)
			.ExecuteUpdateAsync(s => s.SetProperty(g => g.ConsumedAt, now));
		if (consumed == 0)
		{
			return TypedResults.Unauthorized();
		}

		var grant = await db.BotGrants.AsNoTracking().SingleAsync(g => g.TokenHash == hash);
		var token = await tokenIssuer.IssueForBotAsync(grant);

		return TypedResults.Ok(new ExchangeResponse(token, (long)TokenIssuer.BotTokenLifetime.TotalSeconds));
	}

	private static ValidationProblem Invalid(string field, string message)
	{
		return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
	}
}
