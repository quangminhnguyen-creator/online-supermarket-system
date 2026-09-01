using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Admin;
using OnlineSupermarket.Domain.Catalog;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class AdminCatalogEndpoints
{
    public static void MapAdminCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/catalog")
            .WithTags("Admin/Catalog")
            .RequireAuthorization("AdminOnly");

        // Categories
        group.MapGet("/categories", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var categories = await dbContext.Categories
                .OrderBy(c => c.Name)
                .Select(c => new AdminCategoryDto(c.Id, c.Name, c.Slug, c.ParentCategoryId, c.IsActive))
                .ToListAsync(cancellationToken);
            return Results.Ok(categories);
        })
        .WithName("AdminGetCategories")
        .Produces<IEnumerable<AdminCategoryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/categories", async (UpsertCategoryRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Category name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Results.BadRequest(new { message = "Category slug is required." });
            }

            var normalizedSlug = request.Slug.Trim().ToLower();
            if (await dbContext.Categories.AnyAsync(c => c.Slug.ToLower() == normalizedSlug, cancellationToken))
            {
                return Results.Conflict(new { message = "Category slug already exists." });
            }

            if (request.ParentCategoryId.HasValue)
            {
                var parent = await dbContext.Categories.FindAsync(new object[] { request.ParentCategoryId.Value }, cancellationToken);
                if (parent == null)
                {
                    return Results.BadRequest(new { message = "Parent category not found." });
                }
            }

            try
            {
                var category = new Category(request.Name, request.Slug, request.ParentCategoryId);
                dbContext.Categories.Add(category);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created($"/api/admin/catalog/categories/{category.Id}",
                    new AdminCategoryDto(category.Id, category.Name, category.Slug, category.ParentCategoryId, category.IsActive));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminCreateCategory")
        .Produces<AdminCategoryDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/categories/{id:guid}", async (Guid id, UpsertCategoryRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Category name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Results.BadRequest(new { message = "Category slug is required." });
            }

            var category = await dbContext.Categories.FindAsync(new object[] { id }, cancellationToken);
            if (category == null) return Results.NotFound();

            var normalizedSlug = request.Slug.Trim().ToLower();
            if (await dbContext.Categories.AnyAsync(c => c.Id != id && c.Slug.ToLower() == normalizedSlug, cancellationToken))
            {
                return Results.Conflict(new { message = "Category slug already exists." });
            }

            if (request.ParentCategoryId.HasValue)
            {
                if (request.ParentCategoryId.Value == id)
                {
                    return Results.BadRequest(new { message = "Category cannot be its own parent." });
                }

                var parent = await dbContext.Categories.FindAsync(new object[] { request.ParentCategoryId.Value }, cancellationToken);
                if (parent == null)
                {
                    return Results.BadRequest(new { message = "Parent category not found." });
                }

                if (await WouldCreateCategoryCycleAsync(dbContext, id, request.ParentCategoryId, cancellationToken))
                {
                    return Results.BadRequest(new { message = "Would create a category cycle." });
                }
            }

            try
            {
                category.Update(request.Name, request.Slug, request.ParentCategoryId);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(new AdminCategoryDto(category.Id, category.Name, category.Slug, category.ParentCategoryId, category.IsActive));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminUpdateCategory")
        .Produces<AdminCategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/categories/{id:guid}/status", async (Guid id, UpdateCatalogStatusRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var category = await dbContext.Categories.FindAsync(new object[] { id }, cancellationToken);
            if (category == null) return Results.NotFound();

            if (!request.IsActive)
            {
                if (await dbContext.Categories.AnyAsync(c => c.ParentCategoryId == id && c.IsActive, cancellationToken))
                {
                    return Results.Conflict(new { message = "Cannot deactivate category with active child categories." });
                }

                if (await dbContext.Products.AnyAsync(p => p.CategoryId == id && p.IsActive, cancellationToken))
                {
                    return Results.Conflict(new { message = "Cannot deactivate category with active products." });
                }

                category.Deactivate();
            }
            else
            {
                category.Activate();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(new AdminCategoryDto(category.Id, category.Name, category.Slug, category.ParentCategoryId, category.IsActive));
        })
        .WithName("AdminUpdateCategoryStatus")
        .Produces<AdminCategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // Brands
        group.MapGet("/brands", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var brands = await dbContext.Brands
                .OrderBy(b => b.Name)
                .Select(b => new AdminBrandDto(b.Id, b.Name, b.Slug, b.IsActive))
                .ToListAsync(cancellationToken);
            return Results.Ok(brands);
        })
        .WithName("AdminGetBrands")
        .Produces<IEnumerable<AdminBrandDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/brands", async (UpsertBrandRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Brand name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Results.BadRequest(new { message = "Brand slug is required." });
            }

            var normalizedSlug = request.Slug.Trim().ToLower();
            if (await dbContext.Brands.AnyAsync(b => b.Slug.ToLower() == normalizedSlug, cancellationToken))
            {
                return Results.Conflict(new { message = "Brand slug already exists." });
            }

            try
            {
                var brand = new Brand(request.Name, request.Slug);
                dbContext.Brands.Add(brand);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created($"/api/admin/catalog/brands/{brand.Id}",
                    new AdminBrandDto(brand.Id, brand.Name, brand.Slug, brand.IsActive));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminCreateBrand")
        .Produces<AdminBrandDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/brands/{id:guid}", async (Guid id, UpsertBrandRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Brand name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Results.BadRequest(new { message = "Brand slug is required." });
            }

            var brand = await dbContext.Brands.FindAsync(new object[] { id }, cancellationToken);
            if (brand == null) return Results.NotFound();

            var normalizedSlug = request.Slug.Trim().ToLower();
            if (await dbContext.Brands.AnyAsync(b => b.Id != id && b.Slug.ToLower() == normalizedSlug, cancellationToken))
            {
                return Results.Conflict(new { message = "Brand slug already exists." });
            }

            try
            {
                brand.Update(request.Name, request.Slug);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(new AdminBrandDto(brand.Id, brand.Name, brand.Slug, brand.IsActive));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminUpdateBrand")
        .Produces<AdminBrandDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/brands/{id:guid}/status", async (Guid id, UpdateCatalogStatusRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var brand = await dbContext.Brands.FindAsync(new object[] { id }, cancellationToken);
            if (brand == null) return Results.NotFound();

            if (!request.IsActive)
            {
                if (await dbContext.Products.AnyAsync(p => p.BrandId == id && p.IsActive, cancellationToken))
                {
                    return Results.Conflict(new { message = "Cannot deactivate brand with active products." });
                }

                brand.Deactivate();
            }
            else
            {
                brand.Activate();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(new AdminBrandDto(brand.Id, brand.Name, brand.Slug, brand.IsActive));
        })
        .WithName("AdminUpdateBrandStatus")
        .Produces<AdminBrandDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // Products
        group.MapGet("/products", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] int page = 1,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize = 20,
            [Microsoft.AspNetCore.Mvc.FromQuery] string? search = null,
            [Microsoft.AspNetCore.Mvc.FromQuery] Guid? categoryId = null,
            [Microsoft.AspNetCore.Mvc.FromQuery] Guid? brandId = null,
            [Microsoft.AspNetCore.Mvc.FromQuery] bool? isActive = null,
            [Microsoft.AspNetCore.Mvc.FromServices] AppDbContext dbContext = null!,
            CancellationToken cancellationToken = default) =>
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = dbContext.Products.Include(p => p.Category).Include(p => p.Brand).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchTerm) || p.Sku.ToLower().Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProductDto(
                    p.Id, p.CategoryId, p.Category!.Name, p.BrandId, p.Brand!.Name,
                    p.Sku, p.Name, p.Slug, p.Description, p.BasePrice, p.Unit, p.ImageUrl, p.IsActive))
                .ToListAsync(cancellationToken);

            var response = new OnlineSupermarket.Api.Contracts.Catalog.PaginatedResponse<AdminProductDto>(
                items,
                new OnlineSupermarket.Api.Contracts.Catalog.PaginationMeta(totalCount, page, pageSize, totalPages));

            return Results.Ok(response);
        })
        .WithName("AdminGetProducts")
        .Produces<OnlineSupermarket.Api.Contracts.Catalog.PaginatedResponse<AdminProductDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/products", async (UpsertProductRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Product name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Results.BadRequest(new { message = "Product slug is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Sku))
            {
                return Results.BadRequest(new { message = "Product sku is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Unit))
            {
                return Results.BadRequest(new { message = "Product unit is required." });
            }

            if (request.CategoryId == Guid.Empty)
            {
                return Results.BadRequest(new { message = "Category ID is required." });
            }

            if (request.BrandId == Guid.Empty)
            {
                return Results.BadRequest(new { message = "Brand ID is required." });
            }

            if (request.BasePrice < 0)
            {
                return Results.BadRequest(new { message = "Base price cannot be negative." });
            }

            var normalizedSku = request.Sku.Trim().ToLower();
            var normalizedSlug = request.Slug.Trim().ToLower();

            if (await dbContext.Products.AnyAsync(p => p.Sku.ToLower() == normalizedSku, cancellationToken))
            {
                return Results.Conflict(new { message = "Product sku already exists." });
            }

            if (await dbContext.Products.AnyAsync(p => p.Slug.ToLower() == normalizedSlug, cancellationToken))
            {
                return Results.Conflict(new { message = "Product slug already exists." });
            }

            var category = await dbContext.Categories.FindAsync(new object[] { request.CategoryId }, cancellationToken);
            var brand = await dbContext.Brands.FindAsync(new object[] { request.BrandId }, cancellationToken);

            if (category == null || !category.IsActive)
            {
                return Results.Conflict(new { message = "Active category is required." });
            }

            if (brand == null || !brand.IsActive)
            {
                return Results.Conflict(new { message = "Active brand is required." });
            }

            try
            {
                var product = new Product(request.CategoryId, request.BrandId, request.Sku, request.Name, request.Slug, request.Description, request.BasePrice, request.Unit, request.ImageUrl);
                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created($"/api/admin/catalog/products/{product.Id}", 
                    new AdminProductDto(product.Id, product.CategoryId, category.Name, product.BrandId, brand.Name, product.Sku, product.Name, product.Slug, product.Description, product.BasePrice, product.Unit, product.ImageUrl, product.IsActive));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminCreateProduct")
        .Produces<AdminProductDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/products/{id:guid}", async (Guid id, UpsertProductRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Product name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Results.BadRequest(new { message = "Product slug is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Sku))
            {
                return Results.BadRequest(new { message = "Product sku is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Unit))
            {
                return Results.BadRequest(new { message = "Product unit is required." });
            }

            if (request.CategoryId == Guid.Empty)
            {
                return Results.BadRequest(new { message = "Category ID is required." });
            }

            if (request.BrandId == Guid.Empty)
            {
                return Results.BadRequest(new { message = "Brand ID is required." });
            }

            if (request.BasePrice < 0)
            {
                return Results.BadRequest(new { message = "Base price cannot be negative." });
            }

            var product = await dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
                
            if (product == null) return Results.NotFound();

            var normalizedSku = request.Sku.Trim().ToLower();
            var normalizedSlug = request.Slug.Trim().ToLower();

            if (await dbContext.Products.AnyAsync(p => p.Id != id && p.Sku.ToLower() == normalizedSku, cancellationToken))
            {
                return Results.Conflict(new { message = "Product sku already exists." });
            }

            if (await dbContext.Products.AnyAsync(p => p.Id != id && p.Slug.ToLower() == normalizedSlug, cancellationToken))
            {
                return Results.Conflict(new { message = "Product slug already exists." });
            }

            var category = product.Category;
            if (product.CategoryId != request.CategoryId)
            {
                category = await dbContext.Categories.FindAsync(new object[] { request.CategoryId }, cancellationToken);
                if (category == null || !category.IsActive)
                    return Results.Conflict(new { message = "Active category is required." });
            }

            var brand = product.Brand;
            if (product.BrandId != request.BrandId)
            {
                brand = await dbContext.Brands.FindAsync(new object[] { request.BrandId }, cancellationToken);
                if (brand == null || !brand.IsActive)
                    return Results.Conflict(new { message = "Active brand is required." });
            }

            try
            {
                product.Update(request.CategoryId, request.BrandId, request.Sku, request.Name, request.Slug, request.Description, request.BasePrice, request.Unit, request.ImageUrl);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(new AdminProductDto(product.Id, product.CategoryId, category!.Name, product.BrandId, brand!.Name, product.Sku, product.Name, product.Slug, product.Description, product.BasePrice, product.Unit, product.ImageUrl, product.IsActive));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminUpdateProduct")
        .Produces<AdminProductDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/products/{id:guid}/status", async (Guid id, UpdateCatalogStatusRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var product = await dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
                
            if (product == null) return Results.NotFound();

            if (request.IsActive)
            {
                if (!product.Category!.IsActive || !product.Brand!.IsActive)
                {
                    return Results.Conflict(new { message = "Cannot restore product because its category or brand is inactive." });
                }
                product.Activate();
            }
            else
            {
                product.Deactivate();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(new AdminProductDto(product.Id, product.CategoryId, product.Category!.Name, product.BrandId, product.Brand!.Name, product.Sku, product.Name, product.Slug, product.Description, product.BasePrice, product.Unit, product.ImageUrl, product.IsActive));
        })
        .WithName("AdminUpdateProductStatus")
        .Produces<AdminProductDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<bool> WouldCreateCategoryCycleAsync(AppDbContext dbContext, Guid categoryId, Guid? newParentId, CancellationToken cancellationToken)
    {
        if (!newParentId.HasValue) return false;

        var visited = new HashSet<Guid> { categoryId };
        var currentParentId = newParentId;

        while (currentParentId.HasValue)
        {
            if (!visited.Add(currentParentId.Value))
            {
                return true; // Cycle detected
            }

            var parent = await dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == currentParentId.Value, cancellationToken);

            currentParentId = parent?.ParentCategoryId;
        }

        return false;
    }
}
