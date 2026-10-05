using Conduit.Identity.Data;
using Conduit.Identity.Endpoints;
using Conduit.Identity.Tokens;
using Conduit.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddConduitDefaults();

builder.Services.AddDbContext<IdentityDb>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));

builder.Services.AddIdentityCore<AppUser>(options =>
	{
		options.User.RequireUniqueEmail = true;
		options.Lockout.MaxFailedAccessAttempts = 10;
	})
	.AddRoles<IdentityRole<Guid>>()
	.AddEntityFrameworkStores<IdentityDb>()
	.AddSignInManager();

var signingKey = SigningKey.LoadOrCreate(builder.Configuration["Jwt:KeyPath"] ?? "keys/signing-key.pem");
builder.Services.AddSingleton(signingKey);
builder.Services.AddScoped<TokenIssuer>();

builder.AddConduitAuthentication(options => options.TokenValidationParameters.IssuerSigningKey = signingKey.Key);

var app = builder.Build();

app.UseConduitDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/.well-known/openid-configuration", (HttpRequest request) => TypedResults.Ok(new
{
	issuer = ConduitTokens.Issuer,
	jwks_uri = $"{request.Scheme}://{request.Host}/.well-known/jwks.json",
})).ExcludeFromDescription();
app.MapGet("/.well-known/jwks.json", () => TypedResults.Ok(new { keys = new[] { signingKey.PublicJwk } })).ExcludeFromDescription();

app.MapAuthEndpoints();
app.MapInternalEndpoints();

await app.MigrateDatabaseAsync<IdentityDb>();
await app.SeedRolesAsync();

app.Run();
