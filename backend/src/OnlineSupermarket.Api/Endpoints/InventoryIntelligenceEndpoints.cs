using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Inventory;
using OnlineSupermarket.Domain.Inventory;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class InventoryIntelligenceEndpoints
{
    public static IEndpointRouteBuilder MapInventoryIntelligenceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/inventory")
            .WithTags("Admin-Inventory-Intelligence")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/{inventoryId:guid}/transactions", GetTransactionsAsync)
            .WithName("GetInventoryTransactions")
            .Produces<PaginatedInventoryTransactionsDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> GetTransactionsAsync(
        [FromRoute] Guid inventoryId,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var exists = await dbContext.BranchInventories
            .AnyAsync(bi => bi.Id == inventoryId, cancellationToken);

        if (!exists)
            return Results.NotFound(new { message = "Inventory not found." });

        var query = dbContext.InventoryTransactions
            .Where(t => t.BranchInventoryId == inventoryId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new InventoryTransactionDto(
                t.Id,
                t.BranchInventoryId,
                t.TransactionType.ToString(),
                t.QuantityOnHandDelta,
                t.ReservedQuantityDelta,
                t.QuantityOnHandAfter,
                t.ReservedQuantityAfter,
                t.ReferenceType.ToString(),
                t.ReferenceId,
                t.ActorUserId,
                t.Note,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PaginatedInventoryTransactionsDto(items, totalCount, page, pageSize));
    }
}