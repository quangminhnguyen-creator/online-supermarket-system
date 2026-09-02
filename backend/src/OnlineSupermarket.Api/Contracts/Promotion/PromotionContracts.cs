using System.Text.Json.Serialization;

namespace OnlineSupermarket.Api.Contracts.Promotion;

public sealed record PromotionDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("discountType")] string DiscountType,
    [property: JsonPropertyName("discountValue")] decimal DiscountValue,
    [property: JsonPropertyName("minOrderAmount")] decimal MinOrderAmount,
    [property: JsonPropertyName("usageLimit")] int? UsageLimit,
    [property: JsonPropertyName("usageCount")] int UsageCount,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("updatedAtUtc")] DateTime UpdatedAtUtc);

public sealed record CreatePromotionRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("discountType")] string DiscountType,
    [property: JsonPropertyName("discountValue")] decimal DiscountValue,
    [property: JsonPropertyName("minOrderAmount")] decimal MinOrderAmount = 0m,
    [property: JsonPropertyName("usageLimit")] int? UsageLimit = null);

public sealed record UpdatePromotionRequest(
    [property: JsonPropertyName("discountValue")] decimal DiscountValue,
    [property: JsonPropertyName("minOrderAmount")] decimal MinOrderAmount,
    [property: JsonPropertyName("usageLimit")] int? UsageLimit,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record PaginatedPromotionsDto(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("promotions")] IReadOnlyList<PromotionDto> Promotions);
