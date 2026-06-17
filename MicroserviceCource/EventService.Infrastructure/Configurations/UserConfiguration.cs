using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Configurations;

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

        builder.HasIndex(b => b.Login);
        
        builder.Property(b => b.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();
        
        builder.Property(b => b.Role)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();
        
        builder.HasMany(u=> u.Bookings)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}