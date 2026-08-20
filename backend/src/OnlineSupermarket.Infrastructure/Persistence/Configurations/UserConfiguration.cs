using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Identity;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        builder.Property(user => user.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
        builder.Property(user => user.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(user => user.Role).HasColumnName("role").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(user => user.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(user => user.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)").IsRequired();
        builder.Property(user => user.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("datetime(6)").IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasIndex(user => new { user.Status, user.Role });
    }
}
