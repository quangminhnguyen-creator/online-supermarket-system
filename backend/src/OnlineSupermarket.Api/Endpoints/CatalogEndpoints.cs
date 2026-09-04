using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Catalog;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");

        // Lấy danh sách sản phẩm (Lọc, tìm kiếm, phân trang, ghép tồn kho theo chi nhánh)
        group.MapGet("/products", async (
            AppDbContext db,
            Guid? branchId = null,
            Guid? categoryId = null,
            Guid? brandId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? search = null,
            string? sortBy = null,
            int page = 1,
            int pageSize = 20) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var query = db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice <= maxPrice.Value);
            }

            query = sortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.BasePrice),
                "price_desc" => query.OrderByDescending(p => p.BasePrice),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAtUtc)
            };

            var totalItems = await query.LongCountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy giá bán và tồn kho theo chi nhánh nếu có branchId
            Dictionary<Guid, (decimal SellingPrice, int AvailableQuantity)> inventoryMap = new();
            if (branchId.HasValue && products.Count > 0)
            {
                var productIds = products.Select(p => p.Id).ToList();
                inventoryMap = await db.BranchInventories
                    .AsNoTracking()
                    .Where(bi => bi.BranchId == branchId.Value && productIds.Contains(bi.ProductId))
                    .ToDictionaryAsync(
                        bi => bi.ProductId,
                        bi => (bi.SellingPrice, AvailableQuantity: bi.QuantityOnHand - bi.ReservedQuantity)
                    );
            }

            var items = products.Select(p =>
            {
                decimal? sellingPrice = inventoryMap.TryGetValue(p.Id, out var inv) ? inv.SellingPrice : null;
                int? availableQty = inventoryMap.TryGetValue(p.Id, out inv) ? inv.AvailableQuantity : null;

                return new ProductSummaryResponse(
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.Sku,
                    p.BasePrice,
                    sellingPrice ?? p.BasePrice,
                    availableQty,
                    p.Unit,
                    p.ImageUrl,
                    p.CategoryId,
                    p.Category?.Name ?? string.Empty,
                    p.BrandId,
                    p.Brand?.Name ?? string.Empty
                );
            }).ToList();

            return Results.Ok(new PagedResult<ProductSummaryResponse>(items, page, pageSize, totalItems, totalPages));
        });

        // Lấy chi tiết sản phẩm theo Slug
        group.MapGet("/products/{slug}", async (AppDbContext db, string slug, Guid? branchId = null) =>
        {
            var product = await db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product is null) return Results.NotFound();

            decimal? sellingPrice = null;
            int? availableQty = null;

            if (branchId.HasValue)
            {
                var inv = await db.BranchInventories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(bi => bi.BranchId == branchId.Value && bi.ProductId == product.Id);

                if (inv is not null)
                {
                    sellingPrice = inv.SellingPrice;
                    availableQty = inv.QuantityOnHand - inv.ReservedQuantity;
                }
            }

            var response = new ProductDetailResponse(
                product.Id,
                product.Name,
                product.Slug,
                product.Sku,
                product.Description,
                product.BasePrice,
                sellingPrice ?? product.BasePrice,
                availableQty,
                product.Unit,
                product.ImageUrl,
                product.CategoryId,
                product.Category?.Name ?? string.Empty,
                product.BrandId,
                product.Brand?.Name ?? string.Empty,
                product.IsActive,
                product.CreatedAtUtc
            );

            return Results.Ok(response);
        });

        // Lấy danh mục
        group.MapGet("/categories", async (AppDbContext db) =>
        {
            var categories = await db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryResponse(c.Id, c.ParentCategoryId, c.Name, c.Slug, c.IsActive))
                .ToListAsync();

            return Results.Ok(categories);
        });

        // Lấy thương hiệu
        group.MapGet("/brands", async (AppDbContext db) =>
        {
            var brands = await db.Brands
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new BrandResponse(b.Id, b.Name, b.Slug, b.IsActive))
                .ToListAsync();

            return Results.Ok(brands);
        });
    }
}
