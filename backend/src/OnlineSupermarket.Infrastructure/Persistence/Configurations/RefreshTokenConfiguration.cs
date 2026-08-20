using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Identity;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(token => token.UserId).HasColumnName("user_id").HasColumnType("char(36)").IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("datetime(6)").IsRequired();
        builder.Property(token => token.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("datetime(6)");
        builder.Property(token => token.ReplacedByTokenId).HasColumnName("replaced_by_token_id").HasColumnType("char(36)");
        builder.Property(token => token.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)").IsRequired();

        builder.Ignore(token => token.IsActive);
        builder.Ignore(token => token.IsRevoked);
        builder.Ignore(token => token.IsExpired);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(token => token.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
