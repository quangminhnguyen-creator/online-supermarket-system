using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Promotions;

public sealed class Promotion : Entity
{
    private Promotion()
    {
    }

    private Promotion(
        string code,
        DiscountType discountType,
        decimal discountValue,
        decimal minOrderAmount,
        int? usageLimit)
        : base(Guid.NewGuid())
    {
        Code = code;
        DiscountType = discountType;
        DiscountValue = discountValue;
        MinOrderAmount = minOrderAmount;
        UsageLimit = usageLimit;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal MinOrderAmount { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsExhausted => UsageLimit.HasValue && UsageCount >= UsageLimit.Value;

    /// <summary>
    /// Pure eligibility check for a given order subtotal. The caller maps the result
    /// to an API reason and only applies the discount when the result is <see cref="PromotionEligibility.Eligible"/>.
    /// </summary>
    public PromotionEligibility CheckEligibility(decimal subtotal)
    {
        if (!IsActive) return PromotionEligibility.Inactive;
        if (IsExhausted) return PromotionEligibility.Exhausted;
        if (subtotal < MinOrderAmount) return PromotionEligibility.MinOrderNotMet;
        return PromotionEligibility.Eligible;
    }

    public static Promotion Create(
        string code,
        DiscountType discountType,
        decimal discountValue,
        decimal minOrderAmount = 0m,
        int? usageLimit = null)
    {
        var normalizedCode = Guard.Required(code, nameof(code)).ToUpperInvariant();
        ValidateAmounts(discountType, discountValue, minOrderAmount, usageLimit);

        return new Promotion(normalizedCode, discountType, discountValue, minOrderAmount, usageLimit);
    }

    /// <summary>
    /// Pure discount computation for a given subtotal. Never exceeds the subtotal.
    /// The engine (and checkout) still decides whether the promotion may be applied at all.
    /// </summary>
    public decimal CalculateDiscount(decimal subtotal)
    {
        if (subtotal <= 0) return 0m;

        var discount = DiscountType == DiscountType.Percentage
            ? Math.Round(subtotal * DiscountValue / 100m, 0, MidpointRounding.AwayFromZero)
            : DiscountValue;

        return Math.Min(discount, subtotal);
    }

    public void IncrementUsage()
    {
        if (IsExhausted)
        {
            throw new InvalidOperationException("Promotion usage limit reached.");
        }

        UsageCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReleaseUsage()
    {
        UsageCount = Math.Max(0, UsageCount - 1);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(decimal discountValue, decimal minOrderAmount, int? usageLimit)
    {
        ValidateAmounts(DiscountType, discountValue, minOrderAmount, usageLimit);

        DiscountValue = discountValue;
        MinOrderAmount = minOrderAmount;
        UsageLimit = usageLimit;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateAmounts(
        DiscountType discountType,
        decimal discountValue,
        decimal minOrderAmount,
        int? usageLimit)
    {
        if (discountValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Discount value must be positive.");
        }

        if (discountType == DiscountType.Percentage && discountValue > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Percentage discount cannot exceed 100.");
        }

        if (minOrderAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minOrderAmount), "Minimum order amount cannot be negative.");
        }

        if (usageLimit.HasValue && usageLimit.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usageLimit), "Usage limit must be at least 1.");
        }
    }
}
