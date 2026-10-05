namespace Conduit.Identity.Contracts;

public record TokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);
