using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Inventory;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Persistence;

public sealed class InventoryTransactionConfigurationTests
{
    [Fact]
    public void InventoryTransaction_HasLedgerIndexesAndNoCascadeDelete()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(InventoryTransaction))!;

        Assert.Equal("inventory_transactions", entity.GetTableName());
        Assert.True(entity.GetIndexes().Single(i =>
            i.Properties.Count == 1 &&
            i.Properties.Single().Name == nameof(InventoryTransaction.OperationKey)).IsUnique);
        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Count == 2 &&
            i.Properties.Select(p => p.Name).SequenceEqual([
                nameof(InventoryTransaction.BranchInventoryId),
                nameof(InventoryTransaction.CreatedAtUtc),
            ]));
        Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    [Fact]
    public void InventoryTransaction_MapsEnumsAsStringsAndSnapshots()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(InventoryTransaction))!;

        var transactionType = entity.FindProperty(nameof(InventoryTransaction.TransactionType))!;
        Assert.Equal("transaction_type", transactionType.GetColumnName());

        Assert.Equal("operation_key", entity.FindProperty(nameof(InventoryTransaction.OperationKey))!.GetColumnName());
        Assert.Equal("created_at_utc", entity.FindProperty(nameof(InventoryTransaction.CreatedAtUtc))!.GetColumnName());
        Assert.Equal("quantity_on_hand_after", entity.FindProperty(nameof(InventoryTransaction.QuantityOnHandAfter))!.GetColumnName());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}