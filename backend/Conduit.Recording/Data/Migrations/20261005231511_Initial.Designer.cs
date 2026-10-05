using System;
using Conduit.Recording.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Conduit.Recording.Data.Migrations
{
    [DbContext(typeof(RecordingDb))]
    [Migration("20261005231511_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Conduit.Recording.Data.ProcessedWebhook", b =>
                {
                    b.Property<string>("EventId")
                        .HasColumnType("text");

                    b.Property<DateTime>("ReceivedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("EventId");

                    b.ToTable("WebhookEvents");
                });

            modelBuilder.Entity("Conduit.Recording.Data.RecordingRoom", b =>
                {
                    b.Property<Guid>("MeetingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CleanedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("MeetingId");

                    b.ToTable("Rooms");
                });

            modelBuilder.Entity("Conduit.Recording.Data.RecordingSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("EgressId")
                        .HasColumnType("text");

                    b.Property<string>("Error")
                        .HasColumnType("text");

                    b.Property<string>("FileKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("MeetingId")
                        .HasColumnType("uuid");

                    b.Property<string>("RoomSid")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("EgressId")
                        .IsUnique();

                    b.HasIndex("RoomSid")
                        .IsUnique();

                    b.HasIndex("MeetingId", "CreatedAt");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("Conduit.Recording.Data.RecordingSession", b =>
                {
                    b.HasOne("Conduit.Recording.Data.RecordingRoom", null)
                        .WithMany()
                        .HasForeignKey("MeetingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
