using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VendorManagement.Domain.Entities;

namespace VendorManagement.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();

        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.SapVendorId).HasMaxLength(20);
        builder.HasIndex(u => u.SapVendorId);

        builder.Property(u => u.LastLoginIp).HasMaxLength(45);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasDefaultValue(Domain.Enums.UserStatus.PendingVerification);

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(u => u.DeletedAt == null); // soft delete
    }
}