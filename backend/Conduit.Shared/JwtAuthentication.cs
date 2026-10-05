using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Conduit.Shared;

public static class JwtAuthentication
{
	public static void AddConduitAuthentication(this WebApplicationBuilder builder, Action<JwtBearerOptions>? configure = null)
	{
		builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.MapInboundClaims = false;
				options.RequireHttpsMetadata = false;
				options.MetadataAddress = builder.Configuration["Jwt:MetadataAddress"] ?? string.Empty;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidIssuer = ConduitTokens.Issuer,
					ValidAudience = ConduitTokens.Audience,
					NameClaimType = ConduitClaims.Name,
					RoleClaimType = ConduitClaims.Role,
					ClockSkew = TimeSpan.FromSeconds(30),
				};
				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						var accessToken = context.Request.Query["access_token"];
						if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
						{
							context.Token = accessToken;
						}

						return Task.CompletedTask;
					},
				};

				configure?.Invoke(options);
			});

		builder.Services.AddAuthorization();
	}

	public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission) where TBuilder : IEndpointConventionBuilder
	{
		return builder.RequireAuthorization(policy => policy.RequireClaim(ConduitClaims.Permissions, permission));
	}
}
