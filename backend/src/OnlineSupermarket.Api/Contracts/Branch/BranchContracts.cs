using System.Text.Json.Serialization;

namespace OnlineSupermarket.Api.Contracts.Branch;

public sealed record BranchDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("latitude")] decimal? Latitude,
    [property: JsonPropertyName("longitude")] decimal? Longitude,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record BranchInventoryListDto(
    [property: JsonPropertyName("branchId")] Guid BranchId,
    [property: JsonPropertyName("products")] IReadOnlyList<BranchProductInventoryDto> Products);

public sealed record BranchProductInventoryDto(
    [property: JsonPropertyName("productId")] Guid ProductId,
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("sellingPrice")] decimal SellingPrice,
    [property: JsonPropertyName("quantityOnHand")] int QuantityOnHand,
    [property: JsonPropertyName("reservedQuantity")] int ReservedQuantity,
    [property: JsonPropertyName("availableQuantity")] int AvailableQuantity,
    [property: JsonPropertyName("reorderLevel")] int ReorderLevel);

public sealed record InventoryAdjustmentRequest(
    [property: JsonPropertyName("productId")] Guid ProductId,
    [property: JsonPropertyName("quantityOnHand")] int QuantityOnHand,
    [property: JsonPropertyName("sellingPrice")] decimal? SellingPrice = null,
    [property: JsonPropertyName("reorderLevel")] int? ReorderLevel = null,
    [property: JsonPropertyName("reason")] string? Reason = null);
