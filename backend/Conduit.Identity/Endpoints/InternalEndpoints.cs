using Conduit.Identity.Contracts;
using Conduit.Identity.Data;
using Conduit.Identity.Tokens;
using Conduit.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace Conduit.Identity.Endpoints;

public static class InternalEndpoints
{
	public static void MapInternalEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapInternalGroup();

		group.MapGet("/users/{id:guid}", async Task<Results<Ok<UserResponse>, NotFound>> (Guid id, UserManager<AppUser> userManager) =>
		{
			var user = await userManager.FindByIdAsync(id.ToString());
			return user == null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(user));
		});

		group.MapGet("/users", async Task<Results<Ok<UserResponse>, NotFound>> (string email, UserManager<AppUser> userManager) =>
		{
			var user = await userManager.FindByEmailAsync(email.Trim());
			return user == null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(user));
		});

		group.MapPost("/recording-bots", async (RecordingBotRequest request, TokenIssuer tokenIssuer) =>
		{
			var exchangeToken = await tokenIssuer.CreateBotGrantAsync(request.MeetingId, request.SessionId);
			return TypedResults.Ok(new RecordingBotResponse(exchangeToken, (long)TokenIssuer.BotGrantLifetime.TotalSeconds));
		});
	}

	private static UserResponse ToResponse(AppUser user)
	{
		return new UserResponse(user.Id, user.Email ?? string.Empty, user.DisplayName);
	}
}
