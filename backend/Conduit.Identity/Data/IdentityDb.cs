using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Identity.Data;

public class IdentityDb(DbContextOptions<IdentityDb> options) : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
	public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
	public DbSet<BotGrant> BotGrants => Set<BotGrant>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.Entity<AppUser>(user =>
		{
			user.Property(x => x.Email).HasMaxLength(254);
			user.Property(x => x.DisplayName).HasMaxLength(AppUser.MaxDisplayNameLength);
		});

		builder.Entity<RefreshToken>(token =>
		{
			token.HasKey(x => x.TokenHash);
			token.HasIndex(x => x.UserId);
			token.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<BotGrant>().HasKey(x => x.TokenHash);
	}
}
