using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Catalog;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class ProductViewEventEndpointsTests
{
    private sealed record CaptureSeed(
        HttpClient Client,
        Guid UserId,
        Guid BranchId,
        Guid ProductId);

    private static async Task<CaptureSeed> SeedCatalogAsync(
        TestApiFactory factory,
        bool authenticate = false)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var branch = new Branch("View Branch", "1 Test Street", "0900000000", 10m, 106m);
        var category = new Category("ViewCats", "view-cats");
        var brand = new Brand("ViewBrand", "view-brand");
        db.Branches.Add(branch);
        db.Categories.Add(category);
        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        var product = new Product(
            category.Id, brand.Id, $"SKU-{Guid.NewGuid():N}", "View Product", $"view-slug-{Guid.NewGuid():N}",
            "desc", 50_000m, "cái", null);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var client = factory.CreateClient();

        if (authenticate)
        {
            var user = User.Create($"view_{Guid.NewGuid():N}@test.com", "hash", "Viewer", null);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(user));
        }

        return new CaptureSeed(client, userId, branch.Id, product.Id);
    }

    [Fact]
    public async Task RecordView_AsGuest_PersistsAnonymousOwnerWithApprovedFields()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedCatalogAsync(factory);
        var sessionId = Guid.NewGuid();

        var response = await seed.Client.PostAsJsonAsync(
            $"/api/products/{seed.ProductId}/view-events",
            new { anonymousSessionId = sessionId, branchId = seed.BranchId });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.ProductViewEvents.SingleAsync();
        Assert.Null(stored.UserId);
        Assert.Equal(sessionId, stored.AnonymousSessionId);
        Assert.Equal(seed.ProductId, stored.ProductId);
        Assert.Equal(seed.BranchId, stored.BranchId);
    }

    [Fact]
    public async Task RecordView_AsAuthenticatedUser_PrefersJwtOwnerAndIgnoresBody()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedCatalogAsync(factory, authenticate: true);
        var sessionId = Guid.NewGuid();

        var response = await seed.Client.PostAsJsonAsync(
            $"/api/products/{seed.ProductId}/view-events",
            new { anonymousSessionId = sessionId });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.ProductViewEvents.SingleAsync();
        Assert.Equal(seed.UserId, stored.UserId);
        Assert.Null(stored.AnonymousSessionId);
    }

    [Fact]
    public async Task RecordView_WithEmptyAnonymousSessionId_ReturnsBadRequest()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedCatalogAsync(factory);

        var response = await seed.Client.PostAsJsonAsync(
            $"/api/products/{seed.ProductId}/view-events",
            new { anonymousSessionId = Guid.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordView_MissingProduct_ReturnsNotFound()
    {
        using var factory = new TestApiFactory();
        var seed = await SeedCatalogAsync(factory);

        var response = await seed.Client.PostAsJsonAsync(
            $"/api/products/{Guid.NewGuid()}/view-events",
            new { anonymousSessionId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}