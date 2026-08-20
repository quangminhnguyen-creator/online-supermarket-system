using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Payments;

public sealed class Payment : Entity
{
    private Payment() { }

    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public string? ProviderResponse { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public static Payment Create(Guid orderId, PaymentMethod method, decimal amount)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Method = method,
            Amount = amount,
            Status = method == PaymentMethod.COD ? PaymentStatus.PendingCollection : PaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkCompleted(string providerTransactionId, string? response = null)
    {
        Status = PaymentStatus.Completed;
        ProviderTransactionId = providerTransactionId;
        ProviderResponse = response;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string? response = null)
    {
        Status = PaymentStatus.Failed;
        ProviderResponse = response;
    }
}
