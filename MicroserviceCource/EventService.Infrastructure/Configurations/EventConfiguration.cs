using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events",
            t => { t.HasCheckConstraint("CK_events_StartBeforeEnd", "\"start_at\" < \"end_at\""); });

        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.HasIndex(e => e.StartAt);
        builder.HasIndex(e => e.EndAt);

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(e => e.StartAt)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(e => e.EndAt)
            .HasColumnName("end_at")
            .IsRequired();

        builder.Property(e => e.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(e => e.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();

        builder.Property(e => e.BookingIds)
            .HasColumnName("booking_ids")
            .HasColumnType("uuid[]")
            .IsRequired();
    }
}