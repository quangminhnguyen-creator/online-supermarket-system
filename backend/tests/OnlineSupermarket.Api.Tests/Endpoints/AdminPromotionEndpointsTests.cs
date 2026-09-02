using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Promotion;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class AdminPromotionEndpointsTests
{
    private static async Task<HttpClient> CreateClientAsync(TestApiFactory factory, UserRole role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = User.Create($"{role}_{Guid.NewGuid():N}@test.com", "hash", role.ToString(), null, role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(user));
        return client;
    }

    [Fact]
    public async Task List_AsAdmin_ReturnsSeededPromotions()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.GetAsync("/api/admin/promotions?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PaginatedPromotionsDto>();
        Assert.NotNull(body);
        Assert.Contains(body.Promotions, p => p.Code == "WELCOME10");
    }

    [Fact]
    public async Task List_AsCustomer_ReturnsForbidden()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Customer);

        var response = await client.GetAsync("/api/admin/promotions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAndNormalizesCode()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.PostAsJsonAsync("/api/admin/promotions", new
        {
            code = "newyear",
            discountType = "Percentage",
            discountValue = 15m,
            minOrderAmount = 0m,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PromotionDto>();
        Assert.NotNull(dto);
        Assert.Equal("NEWYEAR", dto.Code);
        Assert.True(dto.IsActive);
        Assert.Equal(0, dto.UsageCount);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ReturnsConflict()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        // WELCOME10 is seeded on startup.
        var response = await client.PostAsJsonAsync("/api/admin/promotions", new
        {
            code = "welcome10",
            discountType = "Percentage",
            discountValue = 5m,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidDiscountType_ReturnsBadRequest()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.PostAsJsonAsync("/api/admin/promotions", new
        {
            code = "BADTYPE",
            discountType = "NotAType",
            discountValue = 10m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithPercentageOver100_ReturnsBadRequest()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.PostAsJsonAsync("/api/admin/promotions", new
        {
            code = "TOOBIG",
            discountType = "Percentage",
            discountValue = 150m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesValuesAndDeactivates()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var createResponse = await client.PostAsJsonAsync("/api/admin/promotions", new
        {
            code = "UPDME",
            discountType = "Percentage",
            discountValue = 5m,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PromotionDto>();
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync($"/api/admin/promotions/{created.Id}", new
        {
            discountValue = 20m,
            minOrderAmount = 100_000m,
            usageLimit = (int?)50,
            isActive = false,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PromotionDto>();
        Assert.NotNull(dto);
        Assert.Equal(20m, dto.DiscountValue);
        Assert.Equal(100_000m, dto.MinOrderAmount);
        Assert.Equal(50, dto.UsageLimit);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.PutAsJsonAsync($"/api/admin/promotions/{Guid.NewGuid()}", new
        {
            discountValue = 10m,
            minOrderAmount = 0m,
            usageLimit = (int?)null,
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
