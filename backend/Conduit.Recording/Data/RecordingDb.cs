using Microsoft.EntityFrameworkCore;

namespace Conduit.Recording.Data;

public class RecordingDb(DbContextOptions<RecordingDb> options) : DbContext(options)
{
	public DbSet<RecordingRoom> Rooms => Set<RecordingRoom>();
	public DbSet<RecordingSession> Sessions => Set<RecordingSession>();
	public DbSet<ProcessedWebhook> WebhookEvents => Set<ProcessedWebhook>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.Entity<RecordingRoom>().HasKey(x => x.MeetingId);

		builder.Entity<RecordingSession>(session =>
		{
			session.Property(x => x.Status).HasConversion<string>();
			session.HasIndex(x => x.RoomSid).IsUnique();
			session.HasIndex(x => x.EgressId).IsUnique();
			session.HasIndex(x => new { x.MeetingId, x.CreatedAt });
			session.HasOne<RecordingRoom>().WithMany().HasForeignKey(x => x.MeetingId);
		});

		builder.Entity<ProcessedWebhook>().HasKey(x => x.EventId);
	}
}
