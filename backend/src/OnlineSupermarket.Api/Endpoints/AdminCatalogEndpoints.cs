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
        });

        group.MapPost("/categories", async (UpsertCategoryRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (await dbContext.Categories.AnyAsync(c => c.Slug.ToLower() == request.Slug.ToLower(), cancellationToken))
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

            var category = new Category(request.Name, request.Slug, request.ParentCategoryId);
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/admin/catalog/categories/{category.Id}", 
                new AdminCategoryDto(category.Id, category.Name, category.Slug, category.ParentCategoryId, category.IsActive));
        });

        group.MapPut("/categories/{id:guid}", async (Guid id, UpsertCategoryRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var category = await dbContext.Categories.FindAsync(new object[] { id }, cancellationToken);
            if (category == null) return Results.NotFound();

            if (await dbContext.Categories.AnyAsync(c => c.Id != id && c.Slug.ToLower() == request.Slug.ToLower(), cancellationToken))
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

            category.Update(request.Name, request.Slug, request.ParentCategoryId);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new AdminCategoryDto(category.Id, category.Name, category.Slug, category.ParentCategoryId, category.IsActive));
        });

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
        });

        // Brands
        group.MapGet("/brands", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var brands = await dbContext.Brands
                .OrderBy(b => b.Name)
                .Select(b => new AdminBrandDto(b.Id, b.Name, b.Slug, b.IsActive))
                .ToListAsync(cancellationToken);
            return Results.Ok(brands);
        });

        group.MapPost("/brands", async (UpsertBrandRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (await dbContext.Brands.AnyAsync(b => b.Slug.ToLower() == request.Slug.ToLower(), cancellationToken))
            {
                return Results.Conflict(new { message = "Brand slug already exists." });
            }

            var brand = new Brand(request.Name, request.Slug);
            dbContext.Brands.Add(brand);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/admin/catalog/brands/{brand.Id}", 
                new AdminBrandDto(brand.Id, brand.Name, brand.Slug, brand.IsActive));
        });

        group.MapPut("/brands/{id:guid}", async (Guid id, UpsertBrandRequest request, AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var brand = await dbContext.Brands.FindAsync(new object[] { id }, cancellationToken);
            if (brand == null) return Results.NotFound();

            if (await dbContext.Brands.AnyAsync(b => b.Id != id && b.Slug.ToLower() == request.Slug.ToLower(), cancellationToken))
            {
                return Results.Conflict(new { message = "Brand slug already exists." });
            }

            brand.Update(request.Name, request.Slug);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new AdminBrandDto(brand.Id, brand.Name, brand.Slug, brand.IsActive));
        });

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
        });
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
