using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Promotions;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(p => p.DiscountType).HasColumnName("discount_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.DiscountValue).HasColumnName("discount_value").HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.MinOrderAmount).HasColumnName("min_order_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.UsageLimit).HasColumnName("usage_limit");
        builder.Property(p => p.UsageCount).HasColumnName("usage_count").IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)");
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("datetime(6)");
        builder.HasIndex(p => p.Code).HasDatabaseName("ix_promotions_code").IsUnique();
    }
}
