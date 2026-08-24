using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Identity;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(t => t.UserId).HasColumnName("user_id").HasColumnType("char(36)").IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(500).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("datetime(6)").IsRequired();
        builder.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)").IsRequired();
        builder.Property(t => t.IsUsed).HasColumnName("is_used").HasDefaultValue(false).IsRequired();

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.ExpiresAtUtc);
    }
}
