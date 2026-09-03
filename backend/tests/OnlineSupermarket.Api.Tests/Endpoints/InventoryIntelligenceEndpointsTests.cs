using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Branch;
using OnlineSupermarket.Api.Contracts.Inventory;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Inventory;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class InventoryIntelligenceEndpointsTests
{
    private sealed record InventorySeed(
        HttpClient AdminClient,
        Guid AdminUserId,
        Guid BranchId,
        Guid ProductId,
        Guid InventoryId);

    private static async Task<InventorySeed> SeedInventoryAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var admin = User.Create($"admin_intel_{Guid.NewGuid():N}@test.com", "hash", "Admin", null, UserRole.Admin);
        var branch = new Branch("Intel Branch", "1 Test Street", "0900000000", 10m, 106m);
        var category = new OnlineSupermarket.Domain.Catalog.Category("SciFi", "scifi");
        var brand = new OnlineSupermarket.Domain.Catalog.Brand("Ace", "ace");
        db.Categories.Add(category);
        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        var product = new OnlineSupermarket.Domain.Catalog.Product(
            category.Id, brand.Id, $"SKU-{Guid.NewGuid():N}", "Product A", $"slug-{Guid.NewGuid():N}",
            "desc", 100_000m, "cái", null);
        var productId = product.Id;
        var inventory = BranchInventory.Create(branch.Id, productId, 100_000m, 10, 5);

        db.Users.Add(admin);
        db.Branches.Add(branch);
        db.Products.Add(product);
        db.BranchInventories.Add(inventory);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(admin));

        return new InventorySeed(client, admin.Id, branch.Id, productId, inventory.Id);
    }

    private static async Task<HttpClient> CreateCustomerClientAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var customer = User.Create($"customer_intel_{Guid.NewGuid():N}@test.com", "hash", "Customer", null);
        db.Users.Add(customer);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(customer));
        return client;
    }

    private static async Task SeedLedgerAsync(TestApiFactory factory, Guid inventoryId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (var i = 0; i < count; i++)
        {
            db.InventoryTransactions.Add(InventoryTransaction.Create(
                inventoryId,
                InventoryTransactionType.ManualAdjustment,
                i + 1,
                0,
                10 + i + 1,
                0,
                InventoryReferenceType.AdminAdjustment,
                null,
                null,
                null,
                $"adjustment {i + 1}",
                DateTime.UtcNow.AddSeconds(i)));
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTransactions_AsCustomer_ReturnsForbidden()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedInventoryAsync(factory);
        var customer = await CreateCustomerClientAsync(factory);

        var response = await customer.GetAsync(
            $"/api/admin/inventory/{seed.InventoryId}/transactions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_WithoutAuth_ReturnsUnauthorized()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedInventoryAsync(factory);
        var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync(
            $"/api/admin/inventory/{seed.InventoryId}/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ManualAdjustment_LogsLedgerRowWithActorAndReason()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedInventoryAsync(factory);

        var response = await seed.AdminClient.PutAsJsonAsync(
            $"/api/admin/branches/{seed.BranchId}/inventory",
            new { productId = seed.ProductId, quantityOnHand = 25, reason = "restock" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<BranchProductInventoryDto>();
        Assert.NotNull(dto);
        Assert.Equal(25, dto.QuantityOnHand);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ledger = await db.InventoryTransactions.SingleAsync();
        Assert.Equal(InventoryTransactionType.ManualAdjustment, ledger.TransactionType);
        Assert.Equal(15, ledger.QuantityOnHandDelta);
        Assert.Equal(0, ledger.ReservedQuantityDelta);
        Assert.Equal(25, ledger.QuantityOnHandAfter);
        Assert.Equal(InventoryReferenceType.AdminAdjustment, ledger.ReferenceType);
        Assert.Equal(seed.AdminUserId, ledger.ActorUserId);
        Assert.Equal("restock", ledger.Note);
    }

    [Fact]
    public async Task GetTransactions_IsPaginatedAndOrdered()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedInventoryAsync(factory);
        await SeedLedgerAsync(factory, seed.InventoryId, count: 3);

        var pageOne = await seed.AdminClient.GetAsync(
            $"/api/admin/inventory/{seed.InventoryId}/transactions?page=1&pageSize=2");
        var pageTwo = await seed.AdminClient.GetAsync(
            $"/api/admin/inventory/{seed.InventoryId}/transactions?page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, pageOne.StatusCode);
        var first = await pageOne.Content.ReadFromJsonAsync<PaginatedInventoryTransactionsDto>();
        Assert.NotNull(first);
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.Data.Count);
        Assert.True(first.Data[0].CreatedAtUtc >= first.Data[1].CreatedAtUtc);

        var second = await pageTwo.Content.ReadFromJsonAsync<PaginatedInventoryTransactionsDto>();
        Assert.NotNull(second);
        Assert.Single(second.Data);
    }
}