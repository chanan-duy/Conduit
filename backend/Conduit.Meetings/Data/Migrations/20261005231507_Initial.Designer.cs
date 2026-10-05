using System;
using Conduit.Meetings.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Conduit.Meetings.Data.Migrations
{
    [DbContext(typeof(MeetingsDb))]
    [Migration("20261005231507_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Conduit.Meetings.Data.ChatMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("AuthorId")
                        .HasColumnType("uuid");

                    b.Property<string>("AuthorName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(10000)
                        .HasColumnType("character varying(10000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("MeetingId")
                        .HasColumnType("uuid");

                    b.Property<long>("Sequence")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("MeetingId", "Sequence")
                        .IsUnique();

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("Conduit.Meetings.Data.Meeting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CleanedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("CreatorId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("LastMessageSequence")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.HasIndex("CreatorId");

                    b.HasIndex("DeletedAt")
                        .HasFilter("\"DeletedAt\" IS NOT NULL AND \"CleanedAt\" IS NULL");

                    b.ToTable("Meetings", t =>
                        {
                            t.HasCheckConstraint("CK_Meetings_EndAfterStart", "\"EndDate\" > \"StartDate\"");
                        });
                });

            modelBuilder.Entity("Conduit.Meetings.Data.MeetingParticipant", b =>
                {
                    b.Property<Guid>("MeetingId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("AddedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("MeetingId", "UserId");

                    b.HasIndex("UserId", "MeetingId");

                    b.ToTable("Participants");
                });

            modelBuilder.Entity("Conduit.Meetings.Data.ChatMessage", b =>
                {
                    b.HasOne("Conduit.Meetings.Data.Meeting", null)
                        .WithMany()
                        .HasForeignKey("MeetingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Conduit.Meetings.Data.MeetingParticipant", b =>
                {
                    b.HasOne("Conduit.Meetings.Data.Meeting", null)
                        .WithMany("Participants")
                        .HasForeignKey("MeetingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Conduit.Meetings.Data.Meeting", b =>
                {
                    b.Navigation("Participants");
                });
#pragma warning restore 612, 618
        }
    }
}
