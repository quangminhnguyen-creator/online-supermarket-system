using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Inventory;

public sealed class InventoryTransaction : Entity
{
    private InventoryTransaction()
    {
    }

    private InventoryTransaction(
        Guid branchInventoryId,
        InventoryTransactionType transactionType,
        int quantityOnHandDelta,
        int reservedQuantityDelta,
        int quantityOnHandAfter,
        int reservedQuantityAfter,
        InventoryReferenceType referenceType,
        Guid? referenceId,
        string? operationKey,
        Guid? actorUserId,
        string? note,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        BranchInventoryId = branchInventoryId;
        TransactionType = transactionType;
        QuantityOnHandDelta = quantityOnHandDelta;
        ReservedQuantityDelta = reservedQuantityDelta;
        QuantityOnHandAfter = quantityOnHandAfter;
        ReservedQuantityAfter = reservedQuantityAfter;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        OperationKey = operationKey;
        ActorUserId = actorUserId;
        Note = note;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid BranchInventoryId { get; private set; }
    public InventoryTransactionType TransactionType { get; private set; }
    public int QuantityOnHandDelta { get; private set; }
    public int ReservedQuantityDelta { get; private set; }
    public int QuantityOnHandAfter { get; private set; }
    public int ReservedQuantityAfter { get; private set; }
    public InventoryReferenceType ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? OperationKey { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static InventoryTransaction Create(
        Guid branchInventoryId,
        InventoryTransactionType transactionType,
        int quantityOnHandDelta,
        int reservedQuantityDelta,
        int quantityOnHandAfter,
        int reservedQuantityAfter,
        InventoryReferenceType referenceType,
        Guid? referenceId,
        string? operationKey,
        Guid? actorUserId,
        string? note,
        DateTime createdAtUtc)
    {
        if (quantityOnHandAfter < 0 || reservedQuantityAfter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityOnHandAfter),
                "Transaction snapshots cannot be negative.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("createdAtUtc must be UTC.", nameof(createdAtUtc));
        }

        var trimmedKey = operationKey?.Trim();
        return new InventoryTransaction(
            branchInventoryId,
            transactionType,
            quantityOnHandDelta,
            reservedQuantityDelta,
            quantityOnHandAfter,
            reservedQuantityAfter,
            referenceType,
            referenceId,
            string.IsNullOrEmpty(trimmedKey) ? null : trimmedKey,
            actorUserId,
            note?.Trim(),
            createdAtUtc);
    }
}