using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Configuration;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.Topic)
            .HasColumnName("topic")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Key)
            .HasColumnName("key")
            .HasMaxLength(512);

        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(m => m.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(m => m.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(m => m.Error)
            .HasColumnName("error");

        builder.Property(m => m.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.HasIndex(m => m.ProcessedAt);
        builder.HasIndex(m => m.OccurredAt);
    }
}
