using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Orders;

public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusHistory> _statusHistory = [];

    private Order() { }

    public Guid UserId { get; private set; }
    public Guid BranchId { get; private set; }
    public string FulfillmentType { get; private set; } = string.Empty;
    public Guid? DeliveryAddressId { get; private set; }
    public string RecipientName { get; private set; } = string.Empty;
    public string RecipientPhone { get; private set; } = string.Empty;
    public string DeliveryAddressSnapshot { get; private set; } = string.Empty;
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid? PromotionId { get; private set; }
    public string? PromotionCodeSnapshot { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyList<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public static Order Create(
        Guid userId,
        Guid branchId,
        string fulfillmentType,
        string recipientName,
        string recipientPhone,
        string deliveryAddressSnapshot,
        Guid? deliveryAddressId,
        IReadOnlyList<(Guid ProductId, string ProductName, string Sku, decimal UnitPrice, int Quantity, decimal LineTotal)> items,
        decimal subtotal,
        decimal discountAmount,
        decimal shippingFee,
        decimal totalAmount,
        Guid? promotionId = null,
        string? promotionCodeSnapshot = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BranchId = branchId,
            FulfillmentType = Guard.Required(fulfillmentType, nameof(fulfillmentType)),
            RecipientName = Guard.Required(recipientName, nameof(recipientName)),
            RecipientPhone = Guard.Required(recipientPhone, nameof(recipientPhone)),
            DeliveryAddressSnapshot = deliveryAddressSnapshot,
            DeliveryAddressId = deliveryAddressId,
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            ShippingFee = shippingFee,
            TotalAmount = totalAmount,
            PromotionId = promotionId,
            PromotionCodeSnapshot = promotionCodeSnapshot,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        foreach (var item in items)
        {
            order._items.Add(OrderItem.Create(order.Id, item.ProductId, item.ProductName, item.Sku, item.UnitPrice, item.Quantity, item.LineTotal));
        }

        order._statusHistory.Add(OrderStatusHistory.Create(order.Id, null, OrderStatus.Pending, "Order created"));

        return order;
    }

    public void SetStatus(OrderStatus newStatus, string? note = null)
    {
        _statusHistory.Add(OrderStatusHistory.Create(Id, Status, newStatus, note));
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
