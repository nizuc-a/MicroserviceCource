using MicroserviceCourse.Model.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroserviceCourse.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events",
            t => { t.HasCheckConstraint("CK_events_StartBeforeEnd", "\"StartAt\" < \"EndAt\""); });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasIndex(e => e.StartAt);
        builder.HasIndex(e => e.EndAt);

        builder.Property(e => e.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.StartAt)
            .IsRequired();

        builder.Property(e => e.EndAt)
            .IsRequired();

        builder.Property(e => e.TotalSeats)
            .IsRequired();

        builder.Property(e => e.AvailableSeats)
            .IsRequired();
    }
}