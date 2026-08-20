using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Cart;
using OnlineSupermarket.Domain.Shopping;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/cart")
            .WithTags("Cart")
            .RequireAuthorization();

        group.MapGet("/", GetCartAsync)
            .WithName("GetCart")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/items", AddCartItemAsync)
            .WithName("AddCartItem")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/items/{itemId:guid}", UpdateCartItemAsync)
            .WithName("UpdateCartItem")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/items/{itemId:guid}", RemoveCartItemAsync)
            .WithName("RemoveCartItem")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/change-branch", ChangeBranchAsync)
            .WithName("ChangeBranch")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/", ClearCartAsync)
            .WithName("ClearCart")
            .Produces(StatusCodes.Status204NoContent);

        return routes;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    private static async Task<IResult> GetCartAsync(
        ClaimsPrincipal user,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var cart = await GetOrCreateCartAsync(userId, dbContext, cancellationToken);
        return Results.Ok(await MapToDtoAsync(cart, dbContext, cancellationToken));
    }

    private static async Task<IResult> AddCartItemAsync(
        ClaimsPrincipal user,
        [FromBody] AddCartItemRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        if (request.Quantity <= 0)
            return Results.BadRequest(new { message = "Quantity must be greater than 0." });

        var cart = await GetOrCreateCartAsync(userId, dbContext, cancellationToken);

        var inventory = await dbContext.BranchInventories
            .FirstOrDefaultAsync(bi => bi.BranchId == cart.BranchId && bi.ProductId == request.ProductId, cancellationToken);

        if (inventory == null)
            return Results.NotFound(new { message = "Product not available at this branch." });

        var existingItem = await dbContext.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == request.ProductId, cancellationToken);

        var totalRequested = (existingItem?.Quantity ?? 0) + request.Quantity;
        if (totalRequested > inventory.AvailableQuantity)
        {
            return Results.Conflict(new { message = "INSUFFICIENT_STOCK", availableQuantity = inventory.AvailableQuantity });
        }

        if (existingItem != null)
        {
            existingItem.UpdateQuantity(totalRequested);
        }
        else
        {
            var newItem = CartItem.Create(cart.Id, request.ProductId, inventory.Id, inventory.SellingPrice, request.Quantity);
            dbContext.CartItems.Add(newItem);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(await MapToDtoAsync(cart, dbContext, cancellationToken));
    }

    private static async Task<IResult> UpdateCartItemAsync(
        ClaimsPrincipal user,
        [FromRoute] Guid itemId,
        [FromBody] UpdateCartItemRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var cartItem = await dbContext.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == itemId, cancellationToken);

        if (cartItem == null)
            return Results.NotFound(new { message = "Cart item not found." });

        var cart = await dbContext.Carts
            .FirstOrDefaultAsync(c => c.Id == cartItem.CartId && c.UserId == userId, cancellationToken);

        if (cart == null)
            return Results.NotFound(new { message = "Cart item not found." });

        if (request.Quantity <= 0)
        {
            dbContext.CartItems.Remove(cartItem);
        }
        else
        {
            var inventory = await dbContext.BranchInventories
                .FirstOrDefaultAsync(bi => bi.Id == cartItem.BranchInventoryId, cancellationToken);

            if (inventory != null && request.Quantity > inventory.AvailableQuantity)
            {
                return Results.Conflict(new { message = "INSUFFICIENT_STOCK", availableQuantity = inventory.AvailableQuantity });
            }

            cartItem.UpdateQuantity(request.Quantity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(await MapToDtoAsync(cart, dbContext, cancellationToken));
    }

    private static async Task<IResult> RemoveCartItemAsync(
        ClaimsPrincipal user,
        [FromRoute] Guid itemId,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var cartItem = await dbContext.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == itemId, cancellationToken);

        if (cartItem == null)
            return Results.NotFound(new { message = "Cart item not found." });

        var cart = await dbContext.Carts
            .FirstOrDefaultAsync(c => c.Id == cartItem.CartId && c.UserId == userId, cancellationToken);

        if (cart == null)
            return Results.NotFound(new { message = "Cart item not found." });

        dbContext.CartItems.Remove(cartItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(await MapToDtoAsync(cart, dbContext, cancellationToken));
    }

    private static async Task<IResult> ChangeBranchAsync(
        ClaimsPrincipal user,
        [FromBody] ChangeBranchRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var branchExists = await dbContext.Branches
            .AnyAsync(b => b.Id == request.BranchId && b.IsActive, cancellationToken);

        if (!branchExists)
            return Results.NotFound(new { message = "Branch not found." });

        var cart = await dbContext.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
            return Results.NotFound(new { message = "Cart not found." });

        cart.ChangeBranch(request.BranchId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(await MapToDtoAsync(cart, dbContext, cancellationToken));
    }

    private static async Task<IResult> ClearCartAsync(
        ClaimsPrincipal user,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var cart = await dbContext.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
            return Results.NoContent();

        cart.Clear();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<Cart> GetOrCreateCartAsync(
        Guid userId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var cart = await dbContext.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            var defaultBranch = await dbContext.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .FirstAsync(cancellationToken);

            cart = new Cart(userId, defaultBranch.Id);
            dbContext.Carts.Add(cart);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return cart;
    }

    private static async Task<CartDto> MapToDtoAsync(Cart cart, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = await dbContext.CartItems
            .Where(ci => ci.CartId == cart.Id)
            .ToListAsync(cancellationToken);

        var productIds = items.Select(i => i.ProductId).ToList();
        var products = await dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var inventoryMap = await dbContext.BranchInventories
            .Where(bi => bi.BranchId == cart.BranchId && productIds.Contains(bi.ProductId))
            .ToDictionaryAsync(bi => bi.ProductId, cancellationToken);

        var itemDtos = items.Select(ci =>
        {
            var available = inventoryMap.TryGetValue(ci.ProductId, out var inv) ? inv.AvailableQuantity : 0;
            var productName = products.TryGetValue(ci.ProductId, out var p) ? p.Name : "Unknown";
            var sku = products.TryGetValue(ci.ProductId, out var p2) ? p2.Sku : "";
            return new CartItemDto(ci.Id, ci.ProductId, productName, sku, ci.UnitPrice, ci.Quantity, ci.LineTotal, available);
        }).ToList();

        var subtotal = itemDtos.Sum(i => i.LineTotal);

        return new CartDto(cart.Id, cart.UserId, cart.BranchId, itemDtos, cart.TotalItems, subtotal);
    }
}
