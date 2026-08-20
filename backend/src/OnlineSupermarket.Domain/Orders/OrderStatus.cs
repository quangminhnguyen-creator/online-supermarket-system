namespace OnlineSupermarket.Domain.Orders;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Preparing,
    Ready,
    Shipped,
    Delivered,
    Completed,
    Cancelled,
    Failed
}
