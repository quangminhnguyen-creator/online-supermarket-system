using System.Text.Json.Serialization;

namespace OnlineSupermarket.Api.Contracts.Catalog;

public sealed record ProductListRequest(
    [property: JsonPropertyName("categoryId")] Guid? CategoryId = null,
    [property: JsonPropertyName("brandId")] Guid? BrandId = null,
    [property: JsonPropertyName("minPrice")] decimal? MinPrice = null,
    [property: JsonPropertyName("maxPrice")] decimal? MaxPrice = null,
    [property: JsonPropertyName("branchId")] Guid? BranchId = null,
    [property: JsonPropertyName("search")] string? Search = null,
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("pageSize")] int PageSize = 20);

public sealed record ProductSummaryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("basePrice")] decimal BasePrice,
    [property: JsonPropertyName("imageUrl")] string? ImageUrl,
    [property: JsonPropertyName("categoryId")] Guid CategoryId,
    [property: JsonPropertyName("categoryName")] string CategoryName,
    [property: JsonPropertyName("categorySlug")] string CategorySlug,
    [property: JsonPropertyName("brandName")] string BrandName);

public sealed record ProductDetailDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("basePrice")] decimal BasePrice,
    [property: JsonPropertyName("unit")] string Unit,
    [property: JsonPropertyName("imageUrl")] string? ImageUrl,
    [property: JsonPropertyName("categoryId")] Guid CategoryId,
    [property: JsonPropertyName("categoryName")] string CategoryName,
    [property: JsonPropertyName("categorySlug")] string CategorySlug,
    [property: JsonPropertyName("brandId")] Guid BrandId,
    [property: JsonPropertyName("brandName")] string BrandName,
    [property: JsonPropertyName("branchInventory")] BranchInventoryDto? BranchInventory);

public sealed record BranchInventoryDto(
    [property: JsonPropertyName("branchId")] Guid BranchId,
    [property: JsonPropertyName("sellingPrice")] decimal SellingPrice,
    [property: JsonPropertyName("availableQuantity")] int AvailableQuantity,
    [property: JsonPropertyName("onHand")] int QuantityOnHand);

public sealed record PaginatedResponse<T>(
    [property: JsonPropertyName("data")] IReadOnlyList<T> Data,
    [property: JsonPropertyName("meta")] PaginationMeta Meta);

public sealed record PaginationMeta(
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("totalPages")] int TotalPages);

public sealed record CategoryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("parentCategoryId")] Guid? ParentCategoryId,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record BrandDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("isActive")] bool IsActive);
