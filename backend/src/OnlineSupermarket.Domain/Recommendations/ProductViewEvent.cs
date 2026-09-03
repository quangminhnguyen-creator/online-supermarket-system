using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Recommendations;

public sealed class ProductViewEvent : Entity
{
    private ProductViewEvent()
    {
    }

    private ProductViewEvent(
        Guid id,
        Guid productId,
        Guid? userId,
        Guid? anonymousSessionId,
        Guid? branchId,
        DateTime viewedAtUtc)
        : base(id)
    {
        ProductId = productId;
        UserId = userId;
        AnonymousSessionId = anonymousSessionId;
        BranchId = branchId;
        ViewedAtUtc = viewedAtUtc;
    }

    public Guid ProductId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? AnonymousSessionId { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateTime ViewedAtUtc { get; private set; }

    public static ProductViewEvent Create(
        Guid productId,
        Guid? userId,
        Guid? anonymousSessionId,
        Guid? branchId,
        DateTime viewedAtUtc)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is invalid.", nameof(userId));
        }

        if (anonymousSessionId == Guid.Empty)
        {
            throw new ArgumentException("Anonymous session id is invalid.", nameof(anonymousSessionId));
        }

        if (viewedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("viewedAtUtc must be UTC.", nameof(viewedAtUtc));
        }

        if (userId is null == anonymousSessionId is null)
        {
            throw new ArgumentException(
                "Exactly one of userId or anonymousSessionId is required.",
                nameof(anonymousSessionId));
        }

        return new ProductViewEvent(
            Guid.NewGuid(),
            productId,
            userId,
            anonymousSessionId,
            branchId,
            viewedAtUtc);
    }
}