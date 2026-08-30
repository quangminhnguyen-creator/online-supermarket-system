using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Catalog;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Catalog");

        group.MapGet("/products", GetProductsAsync)
            .WithName("GetProducts")
            .Produces<PaginatedResponse<ProductSummaryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/products/{id:guid}", GetProductByIdAsync)
            .WithName("GetProductById")
            .Produces<ProductDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/categories", GetCategoriesAsync)
            .WithName("GetCategories")
            .Produces<IEnumerable<CategoryDto>>();

        group.MapGet("/brands", GetBrandsAsync)
            .WithName("GetBrands")
            .Produces<IEnumerable<BrandDto>>();

        return routes;
    }

    private static async Task<IResult> GetProductsAsync(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? brandId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] Guid? branchId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] AppDbContext dbContext = null!,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            var includedCategoryIds = await dbContext.Categories
                .Where(c => c.IsActive &&
                    (c.Id == categoryId.Value || c.ParentCategoryId == categoryId.Value))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(p => includedCategoryIds.Contains(p.CategoryId));
        }

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.BasePrice >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.BasePrice <= maxPrice.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Sku,
                p.BasePrice,
                p.ImageUrl,
                p.CategoryId,
                p.Category!.Name,
                p.Category.Slug,
                p.Brand!.Name))
            .ToListAsync(cancellationToken);

        var response = new PaginatedResponse<ProductSummaryDto>(
            products,
            new PaginationMeta(totalCount, page, pageSize, totalPages));

        return Results.Ok(response);
    }

    private static async Task<IResult> GetProductByIdAsync(
        [FromRoute] Guid id,
        [FromQuery] Guid? branchId,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);

        if (product == null)
            return Results.NotFound(new { message = "Product not found." });

        BranchInventoryDto? inventory = null;
        if (branchId.HasValue)
        {
            var inv = await dbContext.BranchInventories
                .FirstOrDefaultAsync(bi => bi.BranchId == branchId.Value && bi.ProductId == id, cancellationToken);

            if (inv != null)
            {
                inventory = new BranchInventoryDto(
                    inv.BranchId,
                    inv.SellingPrice,
                    inv.AvailableQuantity,
                    inv.QuantityOnHand);
            }
        }

        var dto = new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Sku,
            product.Description,
            product.BasePrice,
            product.Unit,
            product.ImageUrl,
            product.CategoryId,
            product.Category!.Name,
            product.Category.Slug,
            product.BrandId,
            product.Brand!.Name,
            inventory);

        return Results.Ok(dto);
    }

    private static async Task<IResult> GetCategoriesAsync(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.ParentCategoryId, c.IsActive))
            .ToListAsync(cancellationToken);

        return Results.Ok(categories);
    }

    private static async Task<IResult> GetBrandsAsync(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var brands = await dbContext.Brands
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto(b.Id, b.Name, b.Slug, b.IsActive))
            .ToListAsync(cancellationToken);

        return Results.Ok(brands);
    }
}
