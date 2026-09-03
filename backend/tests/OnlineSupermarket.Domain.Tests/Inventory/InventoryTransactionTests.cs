using OnlineSupermarket.Domain.Inventory;

namespace OnlineSupermarket.Domain.Tests.Inventory;

public sealed class InventoryTransactionTests
{
    [Fact]
    public void Transaction_CapturesSignedDeltasAndAfterState()
    {
        var transaction = InventoryTransaction.Create(
            Guid.NewGuid(), InventoryTransactionType.Sale,
            -10, -10, 90, 0, InventoryReferenceType.Order,
            Guid.NewGuid(), "order:o:inventory:i:sale", null, null, DateTime.UtcNow);

        Assert.Equal(-10, transaction.QuantityOnHandDelta);
        Assert.Equal(-10, transaction.ReservedQuantityDelta);
        Assert.Equal(90, transaction.QuantityOnHandAfter);
        Assert.Equal(0, transaction.ReservedQuantityAfter);
        Assert.Equal(InventoryReferenceType.Order, transaction.ReferenceType);
        Assert.Equal("order:o:inventory:i:sale", transaction.OperationKey);
    }

    [Fact]
    public void Transaction_TrimsOperationKey()
    {
        var transaction = InventoryTransaction.Create(
            Guid.NewGuid(), InventoryTransactionType.Release,
            0, -1, 10, 0, InventoryReferenceType.Order,
            Guid.NewGuid(), "  order:o:inventory:i:release  ", null, null, DateTime.UtcNow);

        Assert.Equal("order:o:inventory:i:release", transaction.OperationKey);
    }

    [Fact]
    public void Transaction_Create_WithNegativeSnapshot_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InventoryTransaction.Create(
                Guid.NewGuid(), InventoryTransactionType.Sale,
                -10, -10, -1, 0, InventoryReferenceType.Order,
                Guid.NewGuid(), "order:o:inventory:i:sale", null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Transaction_Create_WithNonUtcTimestamp_Throws()
    {
        var localNow = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            InventoryTransaction.Create(
                Guid.NewGuid(), InventoryTransactionType.Reserve,
                0, 1, 10, 1, InventoryReferenceType.Order,
                null, "order:o:inventory:i:reserve", null, null, localNow));
    }

    [Fact]
    public void Transaction_IsImmutable_AfterCreation()
    {
        var transaction = InventoryTransaction.Create(
            Guid.NewGuid(), InventoryTransactionType.ManualAdjustment,
            5, 0, 15, 1, InventoryReferenceType.AdminAdjustment,
            null, "admin:adjustment", Guid.NewGuid(), "restock", DateTime.UtcNow);

        Assert.Equal(5, transaction.QuantityOnHandDelta);
        Assert.Equal(0, transaction.ReservedQuantityDelta);
        Assert.Equal(15, transaction.QuantityOnHandAfter);
        Assert.NotNull(transaction.ActorUserId);
        Assert.Equal("restock", transaction.Note);
    }
}