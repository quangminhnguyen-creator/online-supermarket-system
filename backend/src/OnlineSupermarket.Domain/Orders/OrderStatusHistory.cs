using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Orders;

public sealed class OrderStatusHistory : Entity
{
    private OrderStatusHistory() { }

    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static OrderStatusHistory Create(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string? note)
    {
        return new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = fromStatus ?? (OrderStatus)0,
            ToStatus = toStatus,
            Note = note,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
