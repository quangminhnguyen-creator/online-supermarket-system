using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Inventory;

public sealed class BranchInventory : Entity
{
    private BranchInventory()
    {
    }

    private BranchInventory(
        Guid branchId,
        Guid productId,
        decimal sellingPrice,
        int quantityOnHand,
        int reorderLevel)
        : base(Guid.NewGuid())
    {
        BranchId = branchId;
        ProductId = productId;
        SellingPrice = sellingPrice;
        QuantityOnHand = quantityOnHand;
        ReorderLevel = reorderLevel;
    }

    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal SellingPrice { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => QuantityOnHand - ReservedQuantity;
    public int ReorderLevel { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public static BranchInventory Create(
        Guid branchId,
        Guid productId,
        decimal sellingPrice,
        int quantityOnHand,
        int reorderLevel)
    {
        if (sellingPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sellingPrice));
        }

        if (quantityOnHand < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityOnHand));
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderLevel));
        }

        return new BranchInventory(
            branchId, productId, sellingPrice, quantityOnHand, reorderLevel);
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException("Insufficient available inventory.");
        }

        ReservedQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
