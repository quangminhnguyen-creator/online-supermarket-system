namespace OnlineSupermarket.Api.Contracts.Admin;

public sealed record UpsertCategoryRequest(string Name, string Slug, Guid? ParentCategoryId);
public sealed record UpsertBrandRequest(string Name, string Slug);
public sealed record UpdateCatalogStatusRequest(bool IsActive);
public sealed record AdminCategoryDto(Guid Id, string Name, string Slug, Guid? ParentCategoryId, bool IsActive);
public sealed record AdminBrandDto(Guid Id, string Name, string Slug, bool IsActive);
public sealed record UpsertProductRequest(Guid CategoryId, Guid BrandId, string Sku, string Name, string Slug, string? Description, decimal BasePrice, string Unit, string? ImageUrl);
public sealed record AdminProductDto(Guid Id, Guid CategoryId, string CategoryName, Guid BrandId, string BrandName, string Sku, string Name, string Slug, string? Description, decimal BasePrice, string Unit, string? ImageUrl, bool IsActive);

