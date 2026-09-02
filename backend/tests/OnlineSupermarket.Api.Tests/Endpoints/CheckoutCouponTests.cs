using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Checkout;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Inventory;
using OnlineSupermarket.Domain.Promotions;
using OnlineSupermarket.Domain.Shopping;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class CheckoutCouponTests
{
    private sealed record Scenario(HttpClient Client, Guid UserId, Guid PromotionId, decimal Subtotal);

    private static async Task<Scenario> SeedAsync(
        TestApiFactory factory,
        Promotion promotion,
        decimal unitPrice = 100_000m,
        int quantity = 2,
        int inventoryQty = 100)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = User.Create($"coupon_{Guid.NewGuid():N}@test.com", "hash", "Coupon User", null);
        var branch = new Branch("Coupon Branch", "123 Test Street", "0900000000", 10m, 106m);
        var productId = Guid.NewGuid();
        var inventory = BranchInventory.Create(branch.Id, productId, unitPrice, inventoryQty, 5);
        var cart = new Cart(user.Id, branch.Id);
        cart.AddItem(productId, inventory.Id, unitPrice, quantity);

        db.Users.Add(user);
        db.Branches.Add(branch);
        db.BranchInventories.Add(inventory);
        db.Carts.Add(cart);
        db.Promotions.Add(promotion);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(user));

        return new Scenario(client, user.Id, promotion.Id, unitPrice * quantity);
    }

    [Fact]
    public async Task ValidateCoupon_WithEligibleCode_ReturnsDiscount()
    {
        using var factory = new TestApiFactory();
        var scenario = await SeedAsync(factory, Promotion.Create("PCT10", DiscountType.Percentage, 10m));

        var response = await scenario.Client.PostAsJsonAsync(
            "/api/checkout/validate-coupon", new CouponValidationRequest("pct10"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CouponValidationResponse>();
        Assert.NotNull(body);
        Assert.True(body.Valid);
        Assert.Equal(scenario.Subtotal * 0.1m, body.DiscountAmount);
    }

    [Fact]
    public async Task ValidateCoupon_WithUnknownCode_ReturnsInvalid()
    {
        using var factory = new TestApiFactory();
        var scenario = await SeedAsync(factory, Promotion.Create("REAL", DiscountType.Percentage, 10m));

        var response = await scenario.Client.PostAsJsonAsync(
            "/api/checkout/validate-coupon", new CouponValidationRequest("DOES-NOT-EXIST"));

        var body = await response.Content.ReadFromJsonAsync<CouponValidationResponse>();
        Assert.NotNull(body);
        Assert.False(body.Valid);
        Assert.Equal("INVALID_CODE", body.Reason);
    }

    [Fact]
    public async Task ValidateCoupon_BelowMinOrder_ReturnsMinOrderNotMet()
    {
        using var factory = new TestApiFactory();
        // subtotal = 200,000 < min 500,000
        var scenario = await SeedAsync(
            factory, Promotion.Create("MIN500", DiscountType.FixedAmount, 50_000m, minOrderAmount: 500_000m));

        var response = await scenario.Client.PostAsJsonAsync(
            "/api/checkout/validate-coupon", new CouponValidationRequest("MIN500"));

        var body = await response.Content.ReadFromJsonAsync<CouponValidationResponse>();
        Assert.NotNull(body);
        Assert.False(body.Valid);
        Assert.Equal("MIN_ORDER_NOT_MET", body.Reason);
    }

    [Fact]
    public async Task Checkout_WithValidCoupon_AppliesDiscountAndIncrementsUsage()
    {
        using var factory = new TestApiFactory();
        var scenario = await SeedAsync(factory, Promotion.Create("CHK10", DiscountType.Percentage, 10m));

        var response = await scenario.Client.PostAsJsonAsync(
            "/api/checkout", new CheckoutRequest("Pickup", CouponCode: "chk10"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(body);
        Assert.Equal(scenario.Subtotal * 0.1m, body.DiscountAmount);
        Assert.Equal(scenario.Subtotal - scenario.Subtotal * 0.1m, body.TotalAmount);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var promo = await db.Promotions.FirstAsync(p => p.Id == scenario.PromotionId);
        Assert.Equal(1, promo.UsageCount);

        var order = await db.Orders.FirstAsync(o => o.UserId == scenario.UserId);
        Assert.Equal("CHK10", order.PromotionCodeSnapshot);
        Assert.Equal(scenario.PromotionId, order.PromotionId);
    }

    [Fact]
    public async Task Checkout_WithUnknownCoupon_ReturnsBadRequest()
    {
        using var factory = new TestApiFactory();
        var scenario = await SeedAsync(factory, Promotion.Create("REAL2", DiscountType.Percentage, 10m));

        var response = await scenario.Client.PostAsJsonAsync(
            "/api/checkout", new CheckoutRequest("Pickup", CouponCode: "GHOST"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_WithExhaustedCoupon_ReturnsBadRequest()
    {
        using var factory = new TestApiFactory();
        var exhausted = Promotion.Create("ONCE", DiscountType.Percentage, 10m, usageLimit: 1);
        exhausted.IncrementUsage(); // already at the limit
        var scenario = await SeedAsync(factory, exhausted);

        var response = await scenario.Client.PostAsJsonAsync(
            "/api/checkout", new CheckoutRequest("Pickup", CouponCode: "ONCE"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
