using OnlineSupermarket.Domain.Promotions;

namespace OnlineSupermarket.Domain.Tests.Promotions;

public sealed class PromotionTests
{
    [Fact]
    public void Create_NormalizesCodeAndDefaultsActive()
    {
        var promo = Promotion.Create(" welcome10 ", DiscountType.Percentage, 10m);

        Assert.Equal("WELCOME10", promo.Code);
        Assert.True(promo.IsActive);
        Assert.Equal(0, promo.UsageCount);
        Assert.False(promo.IsExhausted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveValue_Throws(decimal value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Promotion.Create("CODE", DiscountType.FixedAmount, value));
    }

    [Fact]
    public void Create_PercentageOver100_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Promotion.Create("BIG", DiscountType.Percentage, 150m));
    }

    [Fact]
    public void Create_NegativeMinOrder_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Promotion.Create("CODE", DiscountType.FixedAmount, 1000m, minOrderAmount: -1m));
    }

    [Fact]
    public void Create_UsageLimitBelowOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Promotion.Create("CODE", DiscountType.FixedAmount, 1000m, usageLimit: 0));
    }

    [Fact]
    public void CalculateDiscount_Percentage_RoundsToWholeDong()
    {
        var promo = Promotion.Create("P10", DiscountType.Percentage, 10m);

        Assert.Equal(12_345m, promo.CalculateDiscount(123_450m));
    }

    [Fact]
    public void CalculateDiscount_Fixed_ReturnsValue()
    {
        var promo = Promotion.Create("F50", DiscountType.FixedAmount, 50_000m);

        Assert.Equal(50_000m, promo.CalculateDiscount(200_000m));
    }

    [Fact]
    public void CalculateDiscount_NeverExceedsSubtotal()
    {
        var promo = Promotion.Create("F50", DiscountType.FixedAmount, 50_000m);

        Assert.Equal(30_000m, promo.CalculateDiscount(30_000m));
    }

    [Fact]
    public void IncrementUsage_UpToLimit_ThenThrows()
    {
        var promo = Promotion.Create("LIMITED", DiscountType.FixedAmount, 1000m, usageLimit: 2);

        promo.IncrementUsage();
        promo.IncrementUsage();

        Assert.Equal(2, promo.UsageCount);
        Assert.True(promo.IsExhausted);
        Assert.Throws<InvalidOperationException>(() => promo.IncrementUsage());
    }

    [Fact]
    public void ReleaseUsage_DecrementsAndFloorsAtZero()
    {
        var promo = Promotion.Create("LIMITED", DiscountType.FixedAmount, 1000m, usageLimit: 5);
        promo.IncrementUsage();

        promo.ReleaseUsage();
        promo.ReleaseUsage();

        Assert.Equal(0, promo.UsageCount);
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var promo = Promotion.Create("TOGGLE", DiscountType.Percentage, 5m);

        promo.Deactivate();
        Assert.False(promo.IsActive);

        promo.Activate();
        Assert.True(promo.IsActive);
    }

    [Fact]
    public void Update_ChangesValuesWithValidation()
    {
        var promo = Promotion.Create("UPD", DiscountType.Percentage, 5m);

        promo.Update(15m, 100_000m, 50);

        Assert.Equal(15m, promo.DiscountValue);
        Assert.Equal(100_000m, promo.MinOrderAmount);
        Assert.Equal(50, promo.UsageLimit);
        Assert.Throws<ArgumentOutOfRangeException>(() => promo.Update(200m, 0m, null));
    }

    [Fact]
    public void CheckEligibility_WhenAllConditionsMet_IsEligible()
    {
        var promo = Promotion.Create("OK", DiscountType.FixedAmount, 50_000m, minOrderAmount: 100_000m);

        Assert.Equal(PromotionEligibility.Eligible, promo.CheckEligibility(100_000m));
    }

    [Fact]
    public void CheckEligibility_WhenInactive_ReturnsInactive()
    {
        var promo = Promotion.Create("OFF", DiscountType.Percentage, 10m);
        promo.Deactivate();

        Assert.Equal(PromotionEligibility.Inactive, promo.CheckEligibility(1_000_000m));
    }

    [Fact]
    public void CheckEligibility_WhenExhausted_ReturnsExhausted()
    {
        var promo = Promotion.Create("USED", DiscountType.Percentage, 10m, usageLimit: 1);
        promo.IncrementUsage();

        Assert.Equal(PromotionEligibility.Exhausted, promo.CheckEligibility(1_000_000m));
    }

    [Fact]
    public void CheckEligibility_WhenBelowMinOrder_ReturnsMinOrderNotMet()
    {
        var promo = Promotion.Create("BIG", DiscountType.FixedAmount, 50_000m, minOrderAmount: 500_000m);

        Assert.Equal(PromotionEligibility.MinOrderNotMet, promo.CheckEligibility(499_999m));
    }
}
