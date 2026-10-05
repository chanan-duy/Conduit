using Conduit.Meetings.Clients;
using Conduit.Meetings.Data;
using Conduit.Meetings.Endpoints;
using Conduit.Meetings.Hubs;
using Conduit.Meetings.Services;
using Conduit.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddConduitDefaults();
builder.AddConduitAuthentication();

builder.Services.AddDbContext<MeetingsDb>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Meetings")));

builder.Services.AddScoped<MeetingAccess>();
builder.Services.AddSingleton<LiveKitRooms>();
builder.AddInternalClient<IdentityClient>("Services:Identity");
builder.AddInternalClient<RecordingClient>("Services:Recording");
builder.Services.AddHostedService<MeetingCleanupWorker>();

builder.Services.AddSignalR(options =>
	{
		options.MaximumReceiveMessageSize = 128 * 1024;
	})
	.AddMessagePackProtocol();

var app = builder.Build();

app.UseConduitDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapMeetingsEndpoints();
app.MapInternalEndpoints();
app.MapHub<MeetingHub>("/hubs/Meeting", options => options.CloseOnAuthenticationExpiration = true);

await app.MigrateDatabaseAsync<MeetingsDb>();

app.Run();
