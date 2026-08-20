namespace OnlineSupermarket.Domain.Payments;

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded,
    PendingCollection
}
