using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Payments;

public sealed class PaymentCallback : Entity
{
    private PaymentCallback() { }

    public Guid PaymentId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalEventId { get; private set; } = string.Empty;
    public string RawResponse { get; private set; } = string.Empty;
    public bool IsValidSignature { get; private set; }
    public decimal? Amount { get; private set; }
    public PaymentStatus ResultStatus { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }

    public static PaymentCallback Create(
        Guid paymentId,
        string provider,
        string externalEventId,
        string rawResponse,
        bool isValidSignature,
        decimal? amount,
        PaymentStatus resultStatus)
    {
        return new PaymentCallback
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            Provider = provider,
            ExternalEventId = externalEventId,
            RawResponse = rawResponse,
            IsValidSignature = isValidSignature,
            Amount = amount,
            ResultStatus = resultStatus,
            ReceivedAtUtc = DateTime.UtcNow
        };
    }
}
