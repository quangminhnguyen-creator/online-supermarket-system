using OnlineSupermarket.Domain.Inventory;

namespace OnlineSupermarket.Infrastructure.Inventory;

public sealed record InventoryMutationCommand(
    Guid BranchInventoryId,
    InventoryTransactionType TransactionType,
    int Quantity,
    int? AbsoluteQuantityOnHand,
    InventoryReferenceType ReferenceType,
    Guid? ReferenceId,
    string? OperationKey,
    Guid? ActorUserId,
    string? Note,
    DateTime? OccurredAtUtc)
{
    private static string OrderKey(Guid orderId, Guid inventoryId, string type)
        => $"order:{orderId}:inventory:{inventoryId}:{type}";

    public static InventoryMutationCommand Reserve(
        Guid branchInventoryId,
        int quantity,
        Guid orderId,
        Guid? actorUserId = null,
        string? note = null,
        DateTime? occurredAtUtc = null)
        => new(
            branchInventoryId,
            InventoryTransactionType.Reserve,
            quantity,
            null,
            InventoryReferenceType.Order,
            orderId,
            OrderKey(orderId, branchInventoryId, "reserve"),
            actorUserId,
            note,
            occurredAtUtc);

    public static InventoryMutationCommand Release(
        Guid branchInventoryId,
        int quantity,
        Guid orderId,
        Guid? actorUserId = null,
        string? note = null,
        DateTime? occurredAtUtc = null)
        => new(
            branchInventoryId,
            InventoryTransactionType.Release,
            quantity,
            null,
            InventoryReferenceType.Order,
            orderId,
            OrderKey(orderId, branchInventoryId, "release"),
            actorUserId,
            note,
            occurredAtUtc);

    public static InventoryMutationCommand Sale(
        Guid branchInventoryId,
        int quantity,
        Guid orderId,
        Guid? actorUserId = null,
        string? note = null,
        DateTime? occurredAtUtc = null)
        => new(
            branchInventoryId,
            InventoryTransactionType.Sale,
            quantity,
            null,
            InventoryReferenceType.Order,
            orderId,
            OrderKey(orderId, branchInventoryId, "sale"),
            actorUserId,
            note,
            occurredAtUtc);

    public static InventoryMutationCommand ManualAdjustment(
        Guid branchInventoryId,
        int absoluteQuantityOnHand,
        Guid? actorUserId = null,
        string? note = null,
        DateTime? occurredAtUtc = null)
        => new(
            branchInventoryId,
            InventoryTransactionType.ManualAdjustment,
            0,
            absoluteQuantityOnHand,
            InventoryReferenceType.AdminAdjustment,
            null,
            null,
            actorUserId,
            note,
            occurredAtUtc);
}