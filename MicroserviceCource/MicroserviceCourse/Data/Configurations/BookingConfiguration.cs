using MicroserviceCourse.Model.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroserviceCourse.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings",
            t => { t.HasCheckConstraint("CK_bookings_CreatedBeforeProcessed", "\"CreatedAt\" < \"ProcessedAt\""); });

        builder.HasKey(b => b.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        
        builder.HasIndex(b => b.EventId);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(b => b.ProcessedAt);

        builder.HasOne(b => b.Event)
            .WithMany(b => b.Bookings)
            .HasForeignKey(b => b.EventId);
    }
}