using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Inventory;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(x => x.BranchInventoryId).HasColumnName("branch_inventory_id").HasColumnType("char(36)");
        builder.Property(x => x.TransactionType).HasColumnName("transaction_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.QuantityOnHandDelta).HasColumnName("quantity_on_hand_delta");
        builder.Property(x => x.ReservedQuantityDelta).HasColumnName("reserved_quantity_delta");
        builder.Property(x => x.QuantityOnHandAfter).HasColumnName("quantity_on_hand_after");
        builder.Property(x => x.ReservedQuantityAfter).HasColumnName("reserved_quantity_after");
        builder.Property(x => x.ReferenceType).HasColumnName("reference_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ReferenceId).HasColumnName("reference_id").HasColumnType("char(36)");
        builder.Property(x => x.OperationKey).HasColumnName("operation_key").HasMaxLength(180);
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id").HasColumnType("char(36)");
        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)");
        builder.HasIndex(x => x.OperationKey).IsUnique().HasDatabaseName("ix_inventory_transactions_operation_key");
        builder.HasIndex(x => new { x.BranchInventoryId, x.CreatedAtUtc })
            .HasDatabaseName("ix_inventory_transactions_inventory_created");
        builder.HasOne<BranchInventory>()
            .WithMany()
            .HasForeignKey(x => x.BranchInventoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}