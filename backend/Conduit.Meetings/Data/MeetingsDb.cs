using Microsoft.EntityFrameworkCore;

namespace Conduit.Meetings.Data;

public class MeetingsDb(DbContextOptions<MeetingsDb> options) : DbContext(options)
{
	public DbSet<Meeting> Meetings => Set<Meeting>();
	public DbSet<MeetingParticipant> Participants => Set<MeetingParticipant>();
	public DbSet<ChatMessage> Messages => Set<ChatMessage>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.Entity<Meeting>(meeting =>
		{
			meeting.Property(x => x.Title).HasMaxLength(Meeting.MaxTitleLength);
			meeting.HasIndex(x => x.CreatorId);
			meeting.HasIndex(x => x.DeletedAt).HasFilter("\"DeletedAt\" IS NOT NULL AND \"CleanedAt\" IS NULL");
			meeting.ToTable(t => t.HasCheckConstraint("CK_Meetings_EndAfterStart", "\"EndDate\" > \"StartDate\""));
		});

		builder.Entity<MeetingParticipant>(participant =>
		{
			participant.HasKey(x => new { x.MeetingId, x.UserId });
			participant.HasIndex(x => new { x.UserId, x.MeetingId });
			participant.HasOne<Meeting>().WithMany(x => x.Participants).HasForeignKey(x => x.MeetingId);
		});

		builder.Entity<ChatMessage>(message =>
		{
			message.Property(x => x.Content).HasMaxLength(ChatMessage.MaxContentLength);
			message.HasIndex(x => new { x.MeetingId, x.Sequence }).IsUnique();
			message.HasOne<Meeting>().WithMany().HasForeignKey(x => x.MeetingId);
		});
	}
}
