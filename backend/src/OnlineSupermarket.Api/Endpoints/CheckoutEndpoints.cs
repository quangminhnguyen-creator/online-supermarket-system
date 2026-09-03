using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Checkout;
using OnlineSupermarket.Domain.Orders;
using OnlineSupermarket.Domain.Payments;
using OnlineSupermarket.Domain.Promotions;
using OnlineSupermarket.Infrastructure.Inventory;
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

        group.MapPost("/validate-coupon", ValidateCouponAsync)
            .WithName("ValidateCoupon")
            .Produces<CouponValidationResponse>();

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

    private static string CouponReason(PromotionEligibility eligibility) => eligibility switch
    {
        PromotionEligibility.Inactive => "COUPON_INACTIVE",
        PromotionEligibility.Exhausted => "COUPON_EXHAUSTED",
        PromotionEligibility.MinOrderNotMet => "MIN_ORDER_NOT_MET",
        _ => "OK"
    };

    private static string CouponMessage(string reason) => reason switch
    {
        "INVALID_CODE" => "Mã giảm giá không tồn tại.",
        "COUPON_INACTIVE" => "Mã giảm giá đã ngừng áp dụng.",
        "COUPON_EXHAUSTED" => "Mã giảm giá đã hết lượt sử dụng.",
        "MIN_ORDER_NOT_MET" => "Đơn hàng chưa đạt giá trị tối thiểu để áp mã.",
        _ => "Mã giảm giá hợp lệ."
    };

    private static async Task<IResult> ValidateCouponAsync(
        ClaimsPrincipal user,
        [FromBody] CouponValidationRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);

        var cart = await dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        var subtotal = cart?.Items.Sum(i => i.LineTotal) ?? 0m;

        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        var promotion = string.IsNullOrEmpty(code)
            ? null
            : await dbContext.Promotions.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

        if (promotion is null)
        {
            return Results.Ok(new CouponValidationResponse(false, 0m, "INVALID_CODE", CouponMessage("INVALID_CODE")));
        }

        var eligibility = promotion.CheckEligibility(subtotal);
        if (eligibility != PromotionEligibility.Eligible)
        {
            var reason = CouponReason(eligibility);
            return Results.Ok(new CouponValidationResponse(false, 0m, reason, CouponMessage(reason)));
        }

        var discount = promotion.CalculateDiscount(subtotal);
        return Results.Ok(new CouponValidationResponse(true, discount, null, CouponMessage("OK")));
    }

    private static async Task<IResult> CheckoutAsync(
        ClaimsPrincipal user,
        [FromBody] CheckoutRequest request,
        [FromServices] AppDbContext dbContext,
        [FromServices] IInventoryMutationService mutationService,
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

        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);

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
                    .OrderBy(bi => bi.Id)
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
                    return Results.Conflict(new
                    {
                        message = "INSUFFICIENT_STOCK",
                        items = insufficientItems.Select(i => new { i.ProductId, i.Requested, i.Available })
                    });
                }

                var productIds = cart.Items.Select(i => i.ProductId).ToList();
                var products = await dbContext.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

                var subtotal = cart.Items.Sum(i => i.LineTotal);

                // Apply an optional coupon. Re-validated here inside the transaction so a
                // code that passed the preview but was exhausted meanwhile is rejected.
                decimal discountAmount = 0m;
                Promotion? appliedPromotion = null;
                if (!string.IsNullOrWhiteSpace(request.CouponCode))
                {
                    var code = request.CouponCode.Trim().ToUpperInvariant();
                    var promotion = await dbContext.Promotions
                        .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

                    if (promotion is null)
                    {
                        return Results.BadRequest(new { message = "INVALID_COUPON" });
                    }

                    var eligibility = promotion.CheckEligibility(subtotal);
                    if (eligibility != PromotionEligibility.Eligible)
                    {
                        return Results.BadRequest(new { message = CouponReason(eligibility) });
                    }

                    discountAmount = promotion.CalculateDiscount(subtotal);
                    promotion.IncrementUsage();
                    appliedPromotion = promotion;
                }

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
                    totalAmount: totalAmount,
                    promotionId: appliedPromotion?.Id,
                    promotionCodeSnapshot: appliedPromotion?.Code);

                var reserveCommands = cart.Items
                    .Select(item => InventoryMutationCommand.Reserve(
                        item.BranchInventoryId, item.Quantity, order.Id, userId))
                    .ToArray();

                await mutationService.ApplyBatchAsync(reserveCommands, cancellationToken);

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
            .Include(o => o.StatusHistory)
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
            var order = await dbContext.Orders
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);
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
            .Include(o => o.StatusHistory)
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

        await ReleasePromotionUsageAsync(order, dbContext, cancellationToken);

        order.SetStatus(OrderStatus.Cancelled, "Payment failed - inventory released");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task ReleasePromotionUsageAsync(
        Order order,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!order.PromotionId.HasValue) return;

        var promotion = await dbContext.Promotions
            .FirstOrDefaultAsync(p => p.Id == order.PromotionId.Value, cancellationToken);

        promotion?.ReleaseUsage();
    }
}
