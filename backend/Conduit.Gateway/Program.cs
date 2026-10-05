using System.Diagnostics;
using Scalar.AspNetCore;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
	options.SingleLine = true;
	options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.Use(async (context, next) =>
{
	var started = Stopwatch.GetTimestamp();
	await next();

	var cluster = context.Features.Get<IReverseProxyFeature>()?.Route.Config.ClusterId ?? "gateway";
	if (cluster != "web")
	{
		app.Logger.LogInformation("{Method} {Path} -> {Cluster} {Status} in {Elapsed:0} ms",
			context.Request.Method, context.Request.Path, cluster, context.Response.StatusCode,
			Stopwatch.GetElapsedTime(started).TotalMilliseconds);
	}
});

app.Use(async (context, next) =>
{
	if (context.Request.Path.StartsWithSegments("/internal"))
	{
		context.Response.StatusCode = StatusCodes.Status404NotFound;
		return;
	}

	await next();
});

app.MapScalarApiReference("/docs", options => options
	.WithTitle("Conduit API")
	.DisableAgent()
	.DisableMcp()
	.DisableTelemetry()
	.HideDeveloperTools()
	.HideClientButton()
	.AddDocument("identity", "Identity", "/openapi/identity.json")
	.AddDocument("meetings", "Meetings", "/openapi/meetings.json")
	.AddDocument("recording", "Recording", "/openapi/recording.json"));

app.MapReverseProxy();

app.Run();
