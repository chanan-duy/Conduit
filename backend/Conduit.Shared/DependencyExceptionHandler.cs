using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Conduit.Shared;

internal sealed class DependencyExceptionHandler(IProblemDetailsService problemDetails, ILogger<DependencyExceptionHandler> logger)
	: IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
		if (!IsDependencyFailure(exception))
		{
			return false;
		}

		logger.LogWarning("Dependency unavailable: {Message}", exception.Message);

		httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
		return await problemDetails.TryWriteAsync(new ProblemDetailsContext
		{
			HttpContext = httpContext,
			Exception = exception,
			ProblemDetails =
			{
				Title = "Dependency unavailable",
				Status = StatusCodes.Status503ServiceUnavailable,
			},
		});
	}

	private static bool IsDependencyFailure(Exception exception)
	{
		return exception switch
		{
			HttpRequestException or TaskCanceledException or TimeoutException => true,
			NpgsqlException npgsql => npgsql.IsTransient,
			_ => exception.InnerException is { } inner && IsDependencyFailure(inner),
		};
	}
}
