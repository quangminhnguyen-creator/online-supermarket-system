using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Admin;
using OnlineSupermarket.Api.Tests.Auth;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Catalog;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class AdminCatalogEndpointsTests : IClassFixture<AuthTestApiFactory>
{
    private readonly AuthTestApiFactory _factory;

    public AdminCatalogEndpointsTests(AuthTestApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var email = $"{Guid.NewGuid()}@example.com";
        var user = User.Create(email, passwordHasher.HashPassword("Password123!"), "Test User", null, role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(user);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("/api/admin/catalog/categories")]
    [InlineData("/api/admin/catalog/brands")]
    public async Task AdminCatalog_WithoutToken_ReturnsUnauthorized(string path)
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/catalog/categories")]
    [InlineData("/api/admin/catalog/brands")]
    public async Task AdminCatalog_WithCustomerToken_ReturnsForbidden(string path)
    {
        using var client = await CreateAuthenticatedClientAsync(UserRole.Customer);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateSlugIgnoringCase_ReturnsConflict()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = new Category("Tivi", "tivi");
        dbContext.Categories.Add(existing);
        await dbContext.SaveChangesAsync();

        using var client = await CreateAuthenticatedClientAsync(UserRole.Admin);
        var response = await client.PostAsJsonAsync("/api/admin/catalog/categories",
            new UpsertCategoryRequest("Tên khác", "TIVI", null));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreated()
    {
        using var client = await CreateAuthenticatedClientAsync(UserRole.Admin);
        var response = await client.PostAsJsonAsync("/api/admin/catalog/categories",
            new UpsertCategoryRequest("Tivi mới", "tivi-moi", null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_WithSelfParent_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = new Category("Tivi", "tivi-2");
        dbContext.Categories.Add(existing);
        await dbContext.SaveChangesAsync();

        using var client = await CreateAuthenticatedClientAsync(UserRole.Admin);
        var response = await client.PutAsJsonAsync($"/api/admin/catalog/categories/{existing.Id}",
            new UpsertCategoryRequest("Tivi", "tivi-2", existing.Id));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
