using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Orders;

public sealed class OrderItem : Entity
{
    private OrderItem() { }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    public static OrderItem Create(Guid orderId, Guid productId, string productName, string sku, decimal unitPrice, int quantity, decimal lineTotal)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            Sku = sku,
            UnitPrice = unitPrice,
            Quantity = quantity,
            LineTotal = lineTotal
        };
    }
}
