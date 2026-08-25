using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Catalog;
using OnlineSupermarket.Domain.Inventory;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class BranchInventoryConfiguration : IEntityTypeConfiguration<BranchInventory>
{
    public void Configure(EntityTypeBuilder<BranchInventory> builder)
    {
        builder.ToTable("branch_inventories");
        builder.HasKey(inventory => inventory.Id);
        builder.Property(inventory => inventory.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(inventory => inventory.BranchId).HasColumnName("branch_id").HasColumnType("char(36)");
        builder.Property(inventory => inventory.ProductId).HasColumnName("product_id").HasColumnType("char(36)");
        builder.Property(inventory => inventory.SellingPrice).HasColumnName("selling_price").HasPrecision(18, 2);
        builder.Property(inventory => inventory.QuantityOnHand).HasColumnName("quantity_on_hand");
        builder.Property(inventory => inventory.ReservedQuantity).HasColumnName("reserved_quantity");
        builder.Ignore(inventory => inventory.AvailableQuantity);
        builder.Property(inventory => inventory.ReorderLevel).HasColumnName("reorder_level");
        builder.Property(inventory => inventory.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("datetime(6)");
        builder.HasIndex(inventory => new { inventory.BranchId, inventory.ProductId }).IsUnique();
        builder.HasOne(inventory => inventory.Branch).WithMany().HasForeignKey(inventory => inventory.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(inventory => inventory.Product).WithMany().HasForeignKey(inventory => inventory.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
