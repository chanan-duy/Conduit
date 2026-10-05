using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Conduit.Shared;

public static class InternalApi
{
	public const string KeyHeader = "X-Internal-Key";

	public static RouteGroupBuilder MapInternalGroup(this IEndpointRouteBuilder app)
	{
		var key = Encoding.UTF8.GetBytes(GetKey(app.ServiceProvider.GetRequiredService<IConfiguration>()));

		var group = app.MapGroup("/internal").ExcludeFromDescription();
		group.AddEndpointFilter(async (context, next) =>
		{
			var provided = Encoding.UTF8.GetBytes(context.HttpContext.Request.Headers[KeyHeader].ToString());
			if (!CryptographicOperations.FixedTimeEquals(provided, key))
			{
				return Results.Unauthorized();
			}

			return await next(context);
		});

		return group;
	}

	public static IHttpClientBuilder AddInternalClient<TClient>(this WebApplicationBuilder builder, string baseAddressKey)
		where TClient : class
	{
		var baseAddress = builder.Configuration[baseAddressKey] ??
		                  throw new InvalidOperationException($"'{baseAddressKey}' is not configured.");
		var key = GetKey(builder.Configuration);

		return builder.Services.AddHttpClient<TClient>(client =>
		{
			client.BaseAddress = new Uri(baseAddress);
			client.Timeout = TimeSpan.FromSeconds(3);
			client.DefaultRequestHeaders.Add(KeyHeader, key);
		});
	}

	private static string GetKey(IConfiguration configuration)
	{
		return configuration["Internal:Key"] ?? throw new InvalidOperationException("'Internal:Key' is not configured.");
	}
}
