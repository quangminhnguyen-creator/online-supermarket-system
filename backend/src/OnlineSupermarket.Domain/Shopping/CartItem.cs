using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Shopping;

public sealed class CartItem : Entity
{
    private CartItem() { }

    private CartItem(Guid cartId, Guid productId, Guid branchInventoryId, decimal unitPrice, int quantity)
        : base(Guid.NewGuid())
    {
        CartId = cartId;
        ProductId = productId;
        BranchInventoryId = branchInventoryId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid BranchInventoryId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    // Navigation property (EF Core managed)
    public Cart? Cart { get; private set; }

    public static CartItem Create(Guid cartId, Guid productId, Guid branchInventoryId, decimal unitPrice, int quantity)
    {
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        return new CartItem(cartId, productId, branchInventoryId, unitPrice, quantity);
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }
}
