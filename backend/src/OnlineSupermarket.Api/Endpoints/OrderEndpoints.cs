using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Order;
using OnlineSupermarket.Domain.Orders;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var customerGroup = routes.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        customerGroup.MapGet("/", GetOrdersAsync)
            .WithName("GetOrders")
            .Produces<PaginatedOrdersDto>();

        customerGroup.MapGet("/{id:guid}", GetOrderByIdAsync)
            .WithName("GetOrderById")
            .Produces<OrderDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var adminGroup = routes.MapGroup("/api/admin/orders")
            .WithTags("Admin-Orders")
            .RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/", GetAllOrdersAsync)
            .WithName("GetAllOrders")
            .Produces<PaginatedOrdersDto>();

        adminGroup.MapGet("/{id:guid}", GetOrderByIdForAdminAsync)
            .WithName("GetOrderByIdForAdmin")
            .Produces<OrderDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        adminGroup.MapPut("/{id:guid}/status", UpdateOrderStatusAsync)
            .WithName("UpdateOrderStatus")
            .Produces<OrderDetailDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    private static async Task<IResult> GetOrdersAsync(
        ClaimsPrincipal user,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromServices] AppDbContext dbContext = null!,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var query = dbContext.Orders.Where(o => o.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var statusEnum))
            query = query.Where(o => o.Status == statusEnum);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListDto(
                o.Id, o.CreatedAtUtc, o.TotalAmount, o.Status.ToString(), o.FulfillmentType, o.Items.Count))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PaginatedOrdersDto(orders, totalCount, page, pageSize));
    }

    private static async Task<IResult> GetOrderByIdAsync(
        ClaimsPrincipal user,
        [FromRoute] Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var order = await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, cancellationToken);

        if (order == null)
            return Results.NotFound(new { message = "Order not found." });

        return Results.Ok(MapToDetailDto(order, dbContext));
    }

    private static async Task<IResult> GetAllOrdersAsync(
        [FromQuery] string? status,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromServices] AppDbContext dbContext = null!,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = dbContext.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var statusEnum))
            query = query.Where(o => o.Status == statusEnum);

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListDto(
                o.Id, o.CreatedAtUtc, o.TotalAmount, o.Status.ToString(), o.FulfillmentType, o.Items.Count))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PaginatedOrdersDto(orders, totalCount, page, pageSize));
    }

    private static async Task<IResult> GetOrderByIdForAdminAsync(
        [FromRoute] Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            return Results.NotFound(new { message = "Order not found." });

        return Results.Ok(MapToDetailDto(order, dbContext));
    }

    private static async Task<IResult> UpdateOrderStatusAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
            return Results.BadRequest(new { message = "Invalid status value." });

        var order = await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            return Results.NotFound(new { message = "Order not found." });

        var validTransitions = GetValidTransitions(order.Status);
        if (!validTransitions.Contains(newStatus))
            return Results.BadRequest(new { message = $"Invalid transition from {order.Status} to {newStatus}." });

        if (newStatus == OrderStatus.Cancelled)
        {
            await ReleaseInventoryOnCancel(order, dbContext, cancellationToken);
        }

        order.SetStatus(newStatus, request.Note);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(MapToDetailDto(order, dbContext));
    }

    private static async Task ReleaseInventoryOnCancel(
        Order order,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var inventory = await dbContext.BranchInventories
                .FirstOrDefaultAsync(bi => bi.BranchId == order.BranchId && bi.ProductId == item.ProductId, cancellationToken);

            if (inventory != null)
            {
                inventory.Release(item.Quantity);
            }
        }

        if (order.PromotionId.HasValue)
        {
            var promotion = await dbContext.Promotions
                .FirstOrDefaultAsync(p => p.Id == order.PromotionId.Value, cancellationToken);
            promotion?.ReleaseUsage();
        }
    }

    private static HashSet<OrderStatus> GetValidTransitions(OrderStatus current)
    {
        return current switch
        {
            OrderStatus.Pending => new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => new() { OrderStatus.Preparing, OrderStatus.Cancelled },
            OrderStatus.Preparing => new() { OrderStatus.Ready, OrderStatus.Shipped, OrderStatus.Cancelled },
            OrderStatus.Ready => new() { OrderStatus.Shipped, OrderStatus.Delivered },
            OrderStatus.Shipped => new() { OrderStatus.Delivered },
            OrderStatus.Delivered => new() { OrderStatus.Completed },
            OrderStatus.Completed => new(),
            OrderStatus.Cancelled => new(),
            OrderStatus.Failed => new(),
            _ => new()
        };
    }

    private static OrderDetailDto MapToDetailDto(Order order, AppDbContext dbContext)
    {
        var payment = dbContext.Payments.Local.FirstOrDefault(p => p.OrderId == order.Id)
            ?? dbContext.Payments.FirstOrDefault(p => p.OrderId == order.Id);

        var itemDtos = order.Items.Select(i => new OrderItemDto(
            i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.LineTotal)).ToList();

        var historyDtos = order.StatusHistory.Select(h => new StatusHistoryDto(
            h.FromStatus.ToString(), h.ToStatus.ToString(), h.Note, h.CreatedAtUtc)).ToList();

        PaymentDto? paymentDto = null;
        if (payment != null)
        {
            paymentDto = new PaymentDto(
                payment.Id, payment.Method.ToString(), payment.Status.ToString(),
                payment.Amount, payment.ProviderTransactionId, payment.CreatedAtUtc);
        }

        return new OrderDetailDto(
            order.Id, order.UserId, order.BranchId, order.FulfillmentType,
            order.RecipientName, order.RecipientPhone, order.DeliveryAddressSnapshot,
            order.Subtotal, order.DiscountAmount, order.ShippingFee, order.TotalAmount,
            order.PromotionCodeSnapshot, order.Status.ToString(),
            order.CreatedAtUtc, order.UpdatedAtUtc, itemDtos, historyDtos, paymentDto);
    }
}
