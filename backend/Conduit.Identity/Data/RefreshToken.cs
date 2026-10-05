namespace Conduit.Identity.Data;

public class RefreshToken
{
	public required string TokenHash { get; init; }
	public required Guid UserId { get; init; }
	public required DateTime ExpiresAt { get; init; }
	public DateTime? RevokedAt { get; set; }
}
