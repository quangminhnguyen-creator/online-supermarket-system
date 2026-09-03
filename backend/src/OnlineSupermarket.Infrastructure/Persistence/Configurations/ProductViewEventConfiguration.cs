using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Catalog;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Recommendations;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class ProductViewEventConfiguration : IEntityTypeConfiguration<ProductViewEvent>
{
    public void Configure(EntityTypeBuilder<ProductViewEvent> builder)
    {
        builder.ToTable("product_view_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(x => x.ProductId).HasColumnName("product_id").HasColumnType("char(36)");
        builder.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("char(36)");
        builder.Property(x => x.AnonymousSessionId).HasColumnName("anonymous_session_id").HasColumnType("char(36)");
        builder.Property(x => x.BranchId).HasColumnName("branch_id").HasColumnType("char(36)");
        builder.Property(x => x.ViewedAtUtc).HasColumnName("viewed_at_utc").HasColumnType("datetime(6)");

        builder.HasIndex(x => new { x.ProductId, x.ViewedAtUtc })
            .HasDatabaseName("ix_product_view_events_product_viewed");
        builder.HasIndex(x => new { x.UserId, x.ViewedAtUtc })
            .HasDatabaseName("ix_product_view_events_user_viewed");
        builder.HasIndex(x => new { x.AnonymousSessionId, x.ViewedAtUtc })
            .HasDatabaseName("ix_product_view_events_anonymous_viewed");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}