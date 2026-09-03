using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Catalog;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Orders;
using OnlineSupermarket.Domain.Reviews;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", t =>
        {
            t.HasCheckConstraint("ck_reviews_rating", "rating >= 1 AND rating <= 5");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasColumnType("char(36)")
            .ValueGeneratedNever();

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .HasColumnType("char(36)")
            .IsRequired();

        builder.Property(r => r.OrderItemId)
            .HasColumnName("order_item_id")
            .HasColumnType("char(36)")
            .IsRequired();

        builder.Property(r => r.ProductId)
            .HasColumnName("product_id")
            .HasColumnType("char(36)")
            .IsRequired();

        builder.Property(r => r.Rating)
            .HasColumnName("rating")
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasColumnName("comment")
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(r => r.OrderItemId)
            .IsUnique()
            .HasDatabaseName("ix_reviews_order_item_id");

        builder.HasIndex(r => new { r.ProductId, r.CreatedAtUtc })
            .HasDatabaseName("ix_reviews_product_created");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("ix_reviews_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrderItem>()
            .WithMany()
            .HasForeignKey(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
