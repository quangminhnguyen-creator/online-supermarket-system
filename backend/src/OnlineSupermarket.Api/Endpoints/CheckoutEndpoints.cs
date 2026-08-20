using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Checkout;
using OnlineSupermarket.Domain.Orders;
using OnlineSupermarket.Domain.Payments;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/checkout")
            .WithTags("Checkout")
            .RequireAuthorization();

        group.MapPost("/", CheckoutAsync)
            .WithName("Checkout")
            .Produces<CheckoutResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/payment", InitiatePaymentAsync)
            .WithName("InitiatePayment")
            .Produces<PaymentInitDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routes.MapPost("/api/checkout/payment/callback", PaymentCallbackAsync)
            .WithName("PaymentCallback")
            .AllowAnonymous()
            .WithTags("Payment-Callback");

        return routes;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    private static async Task<IResult> CheckoutAsync(
        ClaimsPrincipal user,
        [FromBody] CheckoutRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        if (request.FulfillmentType != "Pickup" && request.FulfillmentType != "Delivery")
            return Results.BadRequest(new { message = "FulfillmentType must be 'Pickup' or 'Delivery'." });

        if (request.FulfillmentType == "Delivery")
        {
            if (string.IsNullOrWhiteSpace(request.RecipientName) || string.IsNullOrWhiteSpace(request.RecipientPhone))
                return Results.BadRequest(new { message = "RecipientName and RecipientPhone are required for Delivery." });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var cart = await dbContext.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

                if (cart == null || !cart.Items.Any())
                    return Results.BadRequest(new { message = "CART_EMPTY" });

                var inventoryIds = cart.Items.Select(i => i.BranchInventoryId).ToList();
                var inventories = await dbContext.BranchInventories
                    .Where(bi => inventoryIds.Contains(bi.Id))
                    .OrderBy(bi => bi.BranchId)
                    .ThenBy(bi => bi.ProductId)
                    .ToListAsync(cancellationToken);

                var inventoryMap = inventories.ToDictionary(bi => bi.Id);

                var insufficientItems = new List<(Guid ProductId, int Requested, int Available)>();
                foreach (var item in cart.Items)
                {
                    if (!inventoryMap.TryGetValue(item.BranchInventoryId, out var inv))
                    {
                        return Results.BadRequest(new { message = $"Product {item.ProductId} not found in inventory." });
                    }
                    if (inv.AvailableQuantity < item.Quantity)
                    {
                        insufficientItems.Add((item.ProductId, item.Quantity, inv.AvailableQuantity));
                    }
                }

                if (insufficientItems.Any())
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Results.Conflict(new
                    {
                        message = "INSUFFICIENT_STOCK",
                        items = insufficientItems.Select(i => new { i.ProductId, i.Requested, i.Available })
                    });
                }

                foreach (var item in cart.Items)
                {
                    if (inventoryMap.TryGetValue(item.BranchInventoryId, out var inv))
                    {
                        inv.Reserve(item.Quantity);
                    }
                }

                var productIds = cart.Items.Select(i => i.ProductId).ToList();
                var products = await dbContext.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

                var subtotal = cart.Items.Sum(i => i.LineTotal);
                var discountAmount = 0m;
                var shippingFee = request.FulfillmentType == "Delivery" ? 15000m : 0m;
                var totalAmount = subtotal - discountAmount + shippingFee;

                var deliveryAddressSnapshot = request.FulfillmentType == "Delivery"
                    ? $"{request.RecipientName}, {request.RecipientPhone}, {request.DeliveryAddress ?? "N/A"}"
                    : "Pickup at branch";

                var orderItems = cart.Items.Select(item =>
                {
                    var product = products.GetValueOrDefault(item.ProductId);
                    return (item.ProductId, product?.Name ?? "Unknown", product?.Sku ?? "", item.UnitPrice, item.Quantity, item.LineTotal);
                }).ToList();

                var order = Order.Create(
                    userId: userId,
                    branchId: cart.BranchId,
                    fulfillmentType: request.FulfillmentType,
                    recipientName: request.RecipientName ?? "N/A",
                    recipientPhone: request.RecipientPhone ?? "N/A",
                    deliveryAddressSnapshot: deliveryAddressSnapshot,
                    deliveryAddressId: request.DeliveryAddressId,
                    items: orderItems,
                    subtotal: subtotal,
                    discountAmount: discountAmount,
                    shippingFee: shippingFee,
                    totalAmount: totalAmount);

                dbContext.Orders.Add(order);
                dbContext.CartItems.RemoveRange(cart.Items);

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var paymentInit = new PaymentInitDto(
                    PaymentId: Guid.Empty,
                    Method: "",
                    Status: "Pending",
                    CheckoutUrl: null);

                return Results.Created($"/api/orders/{order.Id}", new CheckoutResponse(
                    order.Id, subtotal, discountAmount, shippingFee, totalAmount, order.Status.ToString(), paymentInit));
            }
            catch (DbUpdateConcurrencyException)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Results.Conflict(new { message = "INSUFFICIENT_STOCK", retry = true });
                }
                await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);
            }
        }

        return Results.Conflict(new { message = "Checkout failed after retries." });
    }

    private static async Task<IResult> InitiatePaymentAsync(
        ClaimsPrincipal user,
        [FromBody] PaymentRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId, cancellationToken);

        if (order == null)
            return Results.NotFound(new { message = "Order not found." });

        if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            return Results.BadRequest(new { message = "Invalid payment method." });

        var payment = Payment.Create(order.Id, method, order.TotalAmount);
        dbContext.Payments.Add(payment);

        order.SetStatus(OrderStatus.Confirmed, $"Payment initiated: {method}");
        await dbContext.SaveChangesAsync(cancellationToken);

        string? checkoutUrl = null;
        if (method == PaymentMethod.VNPay)
            checkoutUrl = $"https://sandbox.vnpayment.vn/test?orderId={order.Id}&amount={order.TotalAmount}";
        else if (method == PaymentMethod.MoMo)
            checkoutUrl = $"https://momo.vn/test?orderId={order.Id}&amount={order.TotalAmount}";

        return Results.Ok(new PaymentInitDto(payment.Id, method.ToString(), payment.Status.ToString(), checkoutUrl));
    }

    private static async Task<IResult> PaymentCallbackAsync(
        [FromBody] PaymentCallbackRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var externalEventId = request.Data.GetValueOrDefault("transactionId") ?? Guid.NewGuid().ToString();
        var amount = decimal.TryParse(request.Data.GetValueOrDefault("amount") ?? "", out var a) ? a : 0m;
        var responseCode = request.Data.GetValueOrDefault("responseCode") ?? "00";
        var isSuccess = responseCode == "00" || responseCode == "0";

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.ProviderTransactionId == externalEventId, cancellationToken);

        if (payment == null)
        {
            if (Guid.TryParse(request.Data.GetValueOrDefault("orderId") ?? "", out var orderId))
            {
                payment = await dbContext.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
            }
        }

        if (payment == null)
            return Results.NotFound(new { message = "Payment not found." });

        var existingCallback = await dbContext.PaymentCallbacks
            .AnyAsync(pc => pc.Provider == request.Provider && pc.ExternalEventId == externalEventId, cancellationToken);

        if (existingCallback)
        {
            return Results.Ok(new { message = "Callback already processed." });
        }

        var resultStatus = isSuccess ? PaymentStatus.Completed : PaymentStatus.Failed;

        var callback = PaymentCallback.Create(
            payment.Id,
            request.Provider,
            externalEventId,
            System.Text.Json.JsonSerializer.Serialize(request.Data),
            true,
            amount,
            resultStatus);
        dbContext.PaymentCallbacks.Add(callback);

        if (isSuccess && amount == payment.Amount)
        {
            payment.MarkCompleted(externalEventId, System.Text.Json.JsonSerializer.Serialize(request.Data));
            var order = await dbContext.Orders.FindAsync([payment.OrderId], cancellationToken);
            if (order != null)
            {
                order.SetStatus(OrderStatus.Confirmed, "Payment confirmed");
            }
        }
        else
        {
            payment.MarkFailed(System.Text.Json.JsonSerializer.Serialize(request.Data));
            await ReleaseOrderInventoryAsync(payment.OrderId, dbContext, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Callback processed." });
    }

    private static async Task ReleaseOrderInventoryAsync(
        Guid orderId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null) return;

        foreach (var item in order.Items)
        {
            var inventory = await dbContext.BranchInventories
                .FirstOrDefaultAsync(bi => bi.BranchId == order.BranchId && bi.ProductId == item.ProductId, cancellationToken);

            if (inventory != null)
            {
                inventory.Release(item.Quantity);
            }
        }

        order.SetStatus(OrderStatus.Cancelled, "Payment failed - inventory released");
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
