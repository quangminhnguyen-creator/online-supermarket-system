namespace OnlineSupermarket.Api.Contracts.Catalog;

public record ProductSummaryResponse(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    decimal BasePrice,
    decimal? SellingPrice,
    int? AvailableQuantity,
    string Unit,
    string ImageUrl,
    Guid CategoryId,
    string CategoryName,
    Guid BrandId,
    string BrandName
);

public record ProductDetailResponse(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    string Description,
    decimal BasePrice,
    decimal? SellingPrice,
    int? AvailableQuantity,
    string Unit,
    string ImageUrl,
    Guid CategoryId,
    string CategoryName,
    Guid BrandId,
    string BrandName,
    bool IsActive,
    DateTime CreatedAtUtc
);

public record CategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Slug,
    bool IsActive
);

public record BrandResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages
);
