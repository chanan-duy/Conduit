using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Conduit.Recording.Services;

public class LiveKitWebhooks
{
	private static readonly JsonParser Parser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

	private readonly JsonWebTokenHandler _handler = new();
	private readonly TokenValidationParameters _validation;

	public LiveKitWebhooks(IConfiguration config)
	{
		var apiKey = config["LiveKit:ApiKey"] ?? throw new InvalidOperationException("'LiveKit:ApiKey' is not configured.");
		var apiSecret = config["LiveKit:ApiSecret"] ?? throw new InvalidOperationException("'LiveKit:ApiSecret' is not configured.");

		_validation = new TokenValidationParameters
		{
			ValidIssuer = apiKey,
			ValidateAudience = false,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiSecret)),
		};
	}

	public async Task<WebhookEvent?> ParseAsync(string body, string authorization)
	{
		var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..] : authorization;
		var result = await _handler.ValidateTokenAsync(token, _validation);
		if (!result.IsValid || !result.Claims.TryGetValue("sha256", out var signedHash))
		{
			return null;
		}

		var bodyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
		return signedHash as string == bodyHash ? Parser.Parse<WebhookEvent>(body) : null;
	}
}
