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

    [Fact]
    public async Task PublicCatalog_OmitsInactiveEntities()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var inactiveCategory = new OnlineSupermarket.Domain.Catalog.Category("Inactive Cat", "inactive-cat");
        inactiveCategory.Deactivate();
        
        var inactiveBrand = new OnlineSupermarket.Domain.Catalog.Brand("Inactive Brand", "inactive-brand");
        inactiveBrand.Deactivate();

        var activeCategory = new OnlineSupermarket.Domain.Catalog.Category("Active Cat", "active-cat");
        var activeBrand = new OnlineSupermarket.Domain.Catalog.Brand("Active Brand", "active-brand");
        
        dbContext.Categories.AddRange(inactiveCategory, activeCategory);
        dbContext.Brands.AddRange(inactiveBrand, activeBrand);
        await dbContext.SaveChangesAsync();

        var inactiveProduct = new OnlineSupermarket.Domain.Catalog.Product(activeCategory.Id, activeBrand.Id, "INAC-PROD", "Inactive Product", "inactive-product", null, 1000m, "cái", null);
        inactiveProduct.Deactivate();
        
        var productWithInactiveCategory = new OnlineSupermarket.Domain.Catalog.Product(inactiveCategory.Id, activeBrand.Id, "INAC-CAT-PROD", "Prod Inac Cat", "prod-inac-cat", null, 1000m, "cái", null);
        var productWithInactiveBrand = new OnlineSupermarket.Domain.Catalog.Product(activeCategory.Id, inactiveBrand.Id, "INAC-BRAND-PROD", "Prod Inac Brand", "prod-inac-brand", null, 1000m, "cái", null);
        
        dbContext.Products.AddRange(inactiveProduct, productWithInactiveCategory, productWithInactiveBrand);
        await dbContext.SaveChangesAsync();

        var client = _factory.CreateClient();

        var categoriesResponse = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        Assert.DoesNotContain(categoriesResponse!, c => c.Slug == "inactive-cat");

        var brandsResponse = await client.GetFromJsonAsync<IEnumerable<BrandDto>>("/api/brands");
        Assert.DoesNotContain(brandsResponse!, b => b.Slug == "inactive-brand");

        var productsResponse = await client.GetFromJsonAsync<PaginatedResponse<ProductSummaryDto>>("/api/products");
        Assert.DoesNotContain(productsResponse!.Data, p => p.Sku == "INAC-PROD");
        Assert.DoesNotContain(productsResponse.Data, p => p.Sku == "INAC-CAT-PROD");
        Assert.DoesNotContain(productsResponse.Data, p => p.Sku == "INAC-BRAND-PROD");

        var inactiveProdResponse = await client.GetAsync($"/api/products/{inactiveProduct.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, inactiveProdResponse.StatusCode);

        var inacCatProdResponse = await client.GetAsync($"/api/products/{productWithInactiveCategory.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, inacCatProdResponse.StatusCode);

        var inacBrandProdResponse = await client.GetAsync($"/api/products/{productWithInactiveBrand.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, inacBrandProdResponse.StatusCode);
    }
}