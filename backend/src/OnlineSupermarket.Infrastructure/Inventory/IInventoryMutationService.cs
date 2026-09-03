namespace OnlineSupermarket.Infrastructure.Inventory;

public interface IInventoryMutationService
{
    Task ApplyBatchAsync(
        IReadOnlyCollection<InventoryMutationCommand> commands,
        CancellationToken cancellationToken);
}