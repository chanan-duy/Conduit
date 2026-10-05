using Conduit.Recording.Clients;
using Conduit.Recording.Data;
using Conduit.Recording.Endpoints;
using Conduit.Recording.Services;
using Conduit.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddConduitDefaults();
builder.AddConduitAuthentication();

builder.Services.AddDbContext<RecordingDb>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Recording")));

builder.Services.AddSingleton<LiveKitWebhooks>();
builder.Services.AddSingleton<EgressClient>();
builder.Services.AddSingleton<RecordingFiles>();
builder.Services.AddScoped<RecordingSessions>();
builder.AddInternalClient<MeetingsClient>("Services:Meetings");
builder.AddInternalClient<IdentityClient>("Services:Identity");

var app = builder.Build();

app.UseConduitDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapWebhookEndpoints();
app.MapRecordingsEndpoints();
app.MapInternalEndpoints();

await app.MigrateDatabaseAsync<RecordingDb>();

app.Run();
