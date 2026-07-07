using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Configurations;

public class UserConfiguration: IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        builder.HasKey(x => x.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        
        builder.Property(b => b.Login)
            .HasColumnName("login")
            .IsRequired();

        builder.HasIndex(b => b.Login)
            .IsUnique();
        
        builder.Property(b => b.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();
        
        builder.Property(b => b.Role)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();
        
        builder.Property(e => e.BookingIds)
            .HasColumnName("booking_ids")
            .HasColumnType("uuid[]")
            .IsRequired();
    }
}