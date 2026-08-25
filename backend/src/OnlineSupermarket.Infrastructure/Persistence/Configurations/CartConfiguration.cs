using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Shopping;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.BranchId)
            .HasColumnName("branch_id")
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("datetime(6)");

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("datetime(6)");

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("ix_carts_user_id");

        builder.HasIndex(c => new { c.UserId, c.BranchId })
            .HasDatabaseName("ix_carts_user_branch")
            .IsUnique();
    }
}

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Id)
            .HasColumnName("id");

        builder.Property(ci => ci.CartId)
            .HasColumnName("cart_id")
            .IsRequired();

        builder.Property(ci => ci.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(ci => ci.BranchInventoryId)
            .HasColumnName("branch_inventory_id")
            .IsRequired();

        builder.Property(ci => ci.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(ci => ci.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(ci => ci.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("datetime(6)");

        builder.HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ci => ci.CartId)
            .HasDatabaseName("ix_cart_items_cart_id");
    }
}
