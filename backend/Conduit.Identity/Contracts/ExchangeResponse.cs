namespace Conduit.Identity.Contracts;

public record ExchangeResponse(string AuthToken, long ExpiresIn);
