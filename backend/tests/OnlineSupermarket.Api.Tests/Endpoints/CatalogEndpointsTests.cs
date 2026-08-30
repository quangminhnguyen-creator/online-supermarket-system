using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Catalog;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class CatalogEndpointsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public CatalogEndpointsTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProducts_WithParentCategory_ReturnsProductsFromDirectChildren()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tvAndMonitor = await dbContext.Categories.SingleAsync(c => c.Slug == "tv-man-hinh");

        var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<PaginatedResponse<ProductSummaryDto>>(
            $"/api/products?categoryId={tvAndMonitor.Id}&pageSize=100");

        Assert.Contains(response!.Data, p => p.Sku == "TV-SAM-001");
        Assert.Contains(response.Data, p => p.Sku == "MH-SAM-001");
        Assert.DoesNotContain(response.Data, p => p.Sku == "AT-JBL-001");
    }

    [Fact]
    public async Task GetProducts_WithLeafCategory_FiltersToExactLeafAndExposesCategorySlug()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tv = await dbContext.Categories.SingleAsync(c => c.Slug == "tivi");

        var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<PaginatedResponse<ProductSummaryDto>>(
            $"/api/products?categoryId={tv.Id}&pageSize=100");

        Assert.All(response!.Data, p => Assert.Equal("tivi", p.CategorySlug));
        Assert.DoesNotContain(response.Data, p => p.Sku == "MH-SAM-001");

        var detail = await client.GetFromJsonAsync<ProductDetailDto>(
            $"/api/products/{response.Data[0].Id}");
        Assert.Equal("tivi", detail!.CategorySlug);
    }
}