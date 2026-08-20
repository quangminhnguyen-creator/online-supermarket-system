using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Shopping;

public sealed class Cart : Entity
{
    private readonly List<CartItem> _items = [];

    private Cart() { }

    public Cart(Guid userId, Guid branchId)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        BranchId = branchId;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid BranchId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public int TotalItems => _items.Sum(i => i.Quantity);

    public CartItem AddItem(Guid productId, Guid branchInventoryId, decimal unitPrice, int quantity)
    {
        var existing = _items.SingleOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            existing.UpdateQuantity(existing.Quantity + quantity);
        }
        else
        {
            var item = CartItem.Create(Id, productId, branchInventoryId, unitPrice, quantity);
            _items.Add(item);
        }
        UpdatedAtUtc = DateTime.UtcNow;
        return existing ?? _items.Last();
    }

    public void UpdateItemQuantity(Guid itemId, int quantity)
    {
        var item = _items.SingleOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Cart item not found.");
        if (quantity <= 0)
        {
            _items.Remove(item);
        }
        else
        {
            item.UpdateQuantity(quantity);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.SingleOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Cart item not found.");
        _items.Remove(item);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Clear()
    {
        _items.Clear();
        Touch();
    }

    public void ChangeBranch(Guid newBranchId)
    {
        BranchId = newBranchId;
        _items.Clear();
        Touch();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
