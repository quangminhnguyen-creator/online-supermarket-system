namespace OnlineSupermarket.Api.Contracts.Admin;

public sealed record UpsertCategoryRequest(string Name, string Slug, Guid? ParentCategoryId);
public sealed record UpsertBrandRequest(string Name, string Slug);
public sealed record UpdateCatalogStatusRequest(bool IsActive);
public sealed record AdminCategoryDto(Guid Id, string Name, string Slug, Guid? ParentCategoryId, bool IsActive);
public sealed record AdminBrandDto(Guid Id, string Name, string Slug, bool IsActive);
