using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Branch;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Infrastructure.Inventory;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class BranchEndpoints
{
    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }
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

        adminGroup.MapGet(string.Empty, ListAllBranchesAsync)
            .WithName("ListAllBranches")
            .Produces<IEnumerable<BranchDto>>();

        adminGroup.MapPost(string.Empty, CreateBranchAsync)
            .WithName("CreateBranch")
            .Produces<BranchDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        adminGroup.MapPut("/{id:guid}", UpdateBranchAsync)
            .WithName("UpdateBranch")
            .Produces<BranchDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

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
                bi.Id,
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
        ClaimsPrincipal user,
        [FromServices] AppDbContext dbContext,
        [FromServices] IInventoryMutationService mutationService,
        CancellationToken cancellationToken)
    {
        var inventory = await dbContext.BranchInventories
            .Include(bi => bi.Product)
            .FirstOrDefaultAsync(
                bi => bi.BranchId == branchId && bi.ProductId == request.ProductId,
                cancellationToken);

        if (inventory == null)
            return Results.NotFound(new { message = "Inventory not found for this product at this branch." });

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        if (request.QuantityOnHand >= 0)
        {
            var actorUserId = GetUserId(user);
            await mutationService.ApplyBatchAsync(
                [InventoryMutationCommand.ManualAdjustment(
                    inventory.Id, request.QuantityOnHand, actorUserId, request.Reason)],
                cancellationToken);
        }

        if (request.SellingPrice.HasValue)
            inventory.AdjustSellingPrice(request.SellingPrice.Value);

        if (request.ReorderLevel.HasValue)
            inventory.AdjustReorderLevel(request.ReorderLevel.Value);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = new BranchProductInventoryDto(
            inventory.Id,
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

    private static async Task<IResult> ListAllBranchesAsync(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var branches = await dbContext.Branches
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto(
                b.Id, b.Name, b.Address, b.Phone, b.Latitude, b.Longitude, b.IsActive))
            .ToListAsync(cancellationToken);

        return Results.Ok(branches);
    }

    private static async Task<IResult> CreateBranchAsync(
        [FromBody] CreateBranchRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Address))
        {
            return Results.BadRequest(new { message = "Name and address are required." });
        }

        var branch = new Branch(request.Name, request.Address, request.Phone, null, null);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new BranchDto(
            branch.Id, branch.Name, branch.Address, branch.Phone, branch.Latitude, branch.Longitude, branch.IsActive);
        return Results.Created($"/api/branches/{branch.Id}", dto);
    }

    private static async Task<IResult> UpdateBranchAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateBranchRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Address))
        {
            return Results.BadRequest(new { message = "Name and address are required." });
        }

        var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (branch is null)
        {
            return Results.NotFound(new { message = "Branch not found." });
        }

        branch.Update(request.Name, request.Address, request.Phone);
        if (request.IsActive) branch.Activate();
        else branch.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new BranchDto(
            branch.Id, branch.Name, branch.Address, branch.Phone, branch.Latitude, branch.Longitude, branch.IsActive);
        return Results.Ok(dto);
    }
}
