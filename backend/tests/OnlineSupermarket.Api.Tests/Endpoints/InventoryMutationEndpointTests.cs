using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Checkout;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Inventory;
using OnlineSupermarket.Domain.Shopping;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class InventoryMutationEndpointTests
{
    private sealed record CheckoutSeed(HttpClient Client, Guid UserId, Guid InventoryId);

    private static async Task<CheckoutSeed> SeedCheckoutAsync(
        TestApiFactory factory,
        int inventoryQty,
        int cartQty)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = User.Create($"customer_{Guid.NewGuid():N}@test.com", "hash", "Customer", null);
        var branch = new Branch("Ledger Branch", "1 Test Street", "0900000000", 10m, 106m);
        var productId = Guid.NewGuid();
        var inventory = BranchInventory.Create(branch.Id, productId, 100_000m, inventoryQty, 5);
        var cart = new Cart(user.Id, branch.Id);
        cart.AddItem(productId, inventory.Id, 100_000m, cartQty);

        db.Users.Add(user);
        db.Branches.Add(branch);
        db.BranchInventories.Add(inventory);
        db.Carts.Add(cart);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(user));

        return new CheckoutSeed(client, user.Id, inventory.Id);
    }

    [Fact]
    public async Task Checkout_LogsReserveTransactionWithOrderReference()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedCheckoutAsync(factory, inventoryQty: 100, cartQty: 2);

        var response = await seed.Client.PostAsJsonAsync("/api/checkout", new CheckoutRequest("Pickup"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(body);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ledger = await db.InventoryTransactions.SingleAsync();
        Assert.Equal(InventoryTransactionType.Reserve, ledger.TransactionType);
        Assert.Equal(0, ledger.QuantityOnHandDelta);
        Assert.Equal(2, ledger.ReservedQuantityDelta);
        Assert.Equal(100, ledger.QuantityOnHandAfter);
        Assert.Equal(2, ledger.ReservedQuantityAfter);
        Assert.Equal(InventoryReferenceType.Order, ledger.ReferenceType);
        Assert.Equal(body.OrderId, ledger.ReferenceId);
        Assert.Equal(
            $"order:{body.OrderId}:inventory:{seed.InventoryId}:reserve",
            ledger.OperationKey);
    }

    [Fact]
    public async Task Checkout_WithInsufficientStock_RollsBackOrderAndLedger()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedCheckoutAsync(factory, inventoryQty: 2, cartQty: 5);

        var response = await seed.Client.PostAsJsonAsync("/api/checkout", new CheckoutRequest("Pickup"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Orders.AnyAsync(o => o.UserId == seed.UserId));
        Assert.Equal(0, await db.InventoryTransactions.CountAsync());

        var inventory = await db.BranchInventories.SingleAsync(bi => bi.Id == seed.InventoryId);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(2, inventory.QuantityOnHand);
    }
}