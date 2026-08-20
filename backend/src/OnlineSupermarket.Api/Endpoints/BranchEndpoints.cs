using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Branch;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class BranchEndpoints
{
    public static IEndpointRouteBuilder MapBranchEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Branches");

        group.MapGet("/branches", GetBranchesAsync)
            .WithName("GetBranches")
            .Produces<IEnumerable<BranchDto>>();

        group.MapGet("/branches/{id:guid}", GetBranchByIdAsync)
            .WithName("GetBranchById")
            .Produces<BranchDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/branches/{id:guid}/inventory", GetBranchInventoryAsync)
            .WithName("GetBranchInventory")
            .Produces<BranchInventoryListDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var adminGroup = routes.MapGroup("/api/admin/branches").WithTags("Admin-Branches").RequireAuthorization("AdminOnly");

        adminGroup.MapPut("/{branchId:guid}/inventory", UpdateInventoryAsync)
            .WithName("UpdateBranchInventory")
            .Produces<BranchProductInventoryDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> GetBranchesAsync(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var branches = await dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto(
                b.Id, b.Name, b.Address, b.Phone,
                b.Latitude, b.Longitude, b.IsActive))
            .ToListAsync(cancellationToken);

        return Results.Ok(branches);
    }

    private static async Task<IResult> GetBranchByIdAsync(
        [FromRoute] Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var branch = await dbContext.Branches
            .Where(b => b.Id == id)
            .Select(b => new BranchDto(
                b.Id, b.Name, b.Address, b.Phone,
                b.Latitude, b.Longitude, b.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (branch == null)
            return Results.NotFound(new { message = "Branch not found." });

        return Results.Ok(branch);
    }

    private static async Task<IResult> GetBranchInventoryAsync(
        [FromRoute] Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var branchExists = await dbContext.Branches
            .AnyAsync(b => b.Id == id && b.IsActive, cancellationToken);

        if (!branchExists)
            return Results.NotFound(new { message = "Branch not found." });

        var inventory = await dbContext.BranchInventories
            .Include(bi => bi.Product)
            .Where(bi => bi.BranchId == id)
            .OrderBy(bi => bi.Product != null ? bi.Product.Name : string.Empty)
            .Select(bi => new BranchProductInventoryDto(
                bi.ProductId,
                bi.Product != null ? bi.Product.Name : string.Empty,
                bi.Product != null ? bi.Product.Sku : string.Empty,
                bi.SellingPrice,
                bi.QuantityOnHand,
                bi.ReservedQuantity,
                bi.AvailableQuantity,
                bi.ReorderLevel))
            .ToListAsync(cancellationToken);

        var response = new BranchInventoryListDto(id, inventory);
        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateInventoryAsync(
        [FromRoute] Guid branchId,
        [FromBody] InventoryAdjustmentRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var inventory = await dbContext.BranchInventories
            .Include(bi => bi.Product)
            .FirstOrDefaultAsync(
                bi => bi.BranchId == branchId && bi.ProductId == request.ProductId,
                cancellationToken);

        if (inventory == null)
            return Results.NotFound(new { message = "Inventory not found for this product at this branch." });

        if (request.SellingPrice.HasValue)
            inventory.AdjustSellingPrice(request.SellingPrice.Value);

        if (request.QuantityOnHand >= 0)
            inventory.AdjustQuantity(request.QuantityOnHand);

        if (request.ReorderLevel.HasValue)
            inventory.AdjustReorderLevel(request.ReorderLevel.Value);

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new BranchProductInventoryDto(
            inventory.ProductId,
            inventory.Product!.Name,
            inventory.Product.Sku!,
            inventory.SellingPrice,
            inventory.QuantityOnHand,
            inventory.ReservedQuantity,
            inventory.AvailableQuantity,
            inventory.ReorderLevel);

        return Results.Ok(dto);
    }
}
