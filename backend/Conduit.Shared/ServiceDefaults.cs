using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Npgsql;

namespace Conduit.Shared;

public static class ServiceDefaults
{
	public static void AddConduitDefaults(this WebApplicationBuilder builder)
	{
		builder.Logging.AddSimpleConsole(options =>
		{
			options.SingleLine = true;
			options.TimestampFormat = "HH:mm:ss ";
		});

		builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
		{
			document.Servers = [new OpenApiServer { Url = "/" }];
			document.Components ??= new OpenApiComponents();
			document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
			document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT",
			};
			document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
			return Task.CompletedTask;
		}));
		builder.Services.AddProblemDetails();
		builder.Services.AddExceptionHandler<DependencyExceptionHandler>();
	}

	public static void UseConduitDefaults(this WebApplication app)
	{
		app.UseExceptionHandler();
		app.UseStatusCodePages();
		app.MapOpenApi();
		app.MapGet("/health", () => Results.Ok()).ExcludeFromDescription();
	}

	public static async Task MigrateDatabaseAsync<TContext>(this WebApplication app) where TContext : DbContext
	{
		for (var attempt = 1;; attempt++)
		{
			try
			{
				await using var scope = app.Services.CreateAsyncScope();
				await scope.ServiceProvider.GetRequiredService<TContext>().Database.MigrateAsync();
				return;
			}
			catch (NpgsqlException e) when (attempt < 30)
			{
				app.Logger.LogWarning("Database is not available yet ({Message}), retrying", e.Message);
				await Task.Delay(TimeSpan.FromSeconds(2));
			}
		}
	}
}
