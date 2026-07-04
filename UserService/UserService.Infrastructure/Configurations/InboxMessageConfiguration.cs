using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Entities;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.Topic)
            .HasColumnName("topic")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Partition)
            .HasColumnName("partition")
            .IsRequired();

        builder.Property(m => m.Offset)
            .HasColumnName("offset")
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

        builder.Property(m => m.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(m => m.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(m => m.Error)
            .HasColumnName("error");

        builder.Property(m => m.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.HasIndex(m => new { m.Topic, m.Partition, m.Offset })
            .IsUnique();

        builder.HasIndex(m => m.ProcessedAt);
    }
}
