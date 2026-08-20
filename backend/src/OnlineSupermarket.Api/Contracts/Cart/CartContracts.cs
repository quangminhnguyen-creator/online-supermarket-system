using System.Text.Json.Serialization;

namespace OnlineSupermarket.Api.Contracts.Cart;

public sealed record CartDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("branchId")] Guid BranchId,
    [property: JsonPropertyName("items")] IReadOnlyList<CartItemDto> Items,
    [property: JsonPropertyName("totalItems")] int TotalItems,
    [property: JsonPropertyName("subtotal")] decimal Subtotal);

public sealed record CartItemDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("productId")] Guid ProductId,
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("lineTotal")] decimal LineTotal,
    [property: JsonPropertyName("availableQuantity")] int AvailableQuantity);

public sealed record AddCartItemRequest(
    [property: JsonPropertyName("productId")] Guid ProductId,
    [property: JsonPropertyName("quantity")] int Quantity);

public sealed record UpdateCartItemRequest(
    [property: JsonPropertyName("quantity")] int Quantity);

public sealed record ChangeBranchRequest(
    [property: JsonPropertyName("branchId")] Guid BranchId);
