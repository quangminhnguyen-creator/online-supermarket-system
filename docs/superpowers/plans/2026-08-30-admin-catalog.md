# Admin Catalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng luồng quản trị danh mục, thương hiệu và sản phẩm được bảo vệ bằng role Admin, từ Minimal API đến giao diện React.

**Architecture:** Mở rộng trực tiếp domain entity và Minimal API hiện có; validation nội tại nằm trong entity, validation uniqueness/relationship nằm trong `AdminCatalogEndpoints`. Frontend dùng một admin API client, một route guard/layout và ba trang quản trị độc lập.

**Tech Stack:** .NET 9, ASP.NET Core Minimal API, EF Core/MySQL, xUnit, React 19, React Router, TypeScript, Vitest, Testing Library.

**Spec:** `docs/superpowers/specs/2026-08-30-admin-catalog-design.md`

## Global Constraints

- Không thêm repository/application layer; endpoint tiếp tục dùng trực tiếp `AppDbContext`.
- Toàn bộ `/api/admin/catalog` phải gọi `.RequireAuthorization("AdminOnly")`.
- Không xóa vật lý; status mutation chỉ đổi `IsActive`.
- Ảnh sản phẩm chỉ nhận `imageUrl`; không thêm binary upload hoặc storage dependency.
- Public catalog chỉ trả category, brand và product active.
- Không cho deactivate category/brand đang được product active tham chiếu; không cho deactivate category cha còn child active.
- Không cho cập nhật parent category tạo self-reference hoặc cycle.
- Không sửa hoặc commit các thay đổi có sẵn trong `PLAN.md`, `docs/project-progress-4-members.html`, `docs/tasks/plan-order-history.html`, `docs/tasks/plan-catalog-category-hierarchy.html`.

## File Map

- Modify `backend/src/OnlineSupermarket.Domain/Catalog/Category.cs`: update, activate/deactivate behavior.
- Modify `backend/src/OnlineSupermarket.Domain/Catalog/Brand.cs`: update, activate/deactivate behavior.
- Modify `backend/src/OnlineSupermarket.Domain/Catalog/Product.cs`: atomic catalog update, activate/deactivate behavior.
- Modify `backend/tests/OnlineSupermarket.Domain.Tests/Catalog/CatalogEntityTests.cs`: domain behavior coverage.
- Create `backend/src/OnlineSupermarket.Api/Contracts/Admin/AdminCatalogContracts.cs`: request/response records shared by Admin Catalog endpoints.
- Create `backend/src/OnlineSupermarket.Api/Endpoints/AdminCatalogEndpoints.cs`: authorization, validation, CRUD/status handlers.
- Modify `backend/src/OnlineSupermarket.Api/Program.cs`: map Admin Catalog endpoints.
- Create `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/AdminCatalogEndpointsTests.cs`: authenticated endpoint integration tests.
- Modify `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/CatalogEndpointsTests.cs`: public inactive filtering regression tests.
- Modify `backend/tests/OnlineSupermarket.Api.Tests/OpenApiContractTests.cs`: Admin Catalog contract assertions.
- Modify `frontend/src/api/httpClient.ts`: add JSON PATCH helper.
- Create `frontend/src/api/adminCatalogApi.ts`: typed Admin Catalog API boundary.
- Create `frontend/src/api/adminCatalogApi.test.ts`: URL, token and payload tests.
- Create `frontend/src/features/admin/AdminRoute.tsx`: guest/customer/admin state gate.
- Create `frontend/src/features/admin/AdminLayout.tsx`: Admin Catalog navigation and outlet.
- Create `frontend/src/features/admin/AdminCatalog.css`: scoped admin layout, table, form and state styles.
- Create `frontend/src/features/admin/categories/AdminCategoriesPage.tsx`: category list/form/status workflow.
- Create `frontend/src/features/admin/categories/AdminCategoriesPage.test.tsx`: category UI tests.
- Create `frontend/src/features/admin/brands/AdminBrandsPage.tsx`: brand list/form/status workflow.
- Create `frontend/src/features/admin/brands/AdminBrandsPage.test.tsx`: brand UI tests.
- Create `frontend/src/features/admin/products/AdminProductsPage.tsx`: paginated product list/form/status workflow.
- Create `frontend/src/features/admin/products/AdminProductsPage.test.tsx`: product UI tests.
- Modify `frontend/src/App.tsx`: register nested Admin routes.
- Modify `frontend/src/App.test.tsx`: route wiring coverage.
- Modify `docs/api/openapi.json`: document Admin Catalog paths and schemas.
- Modify `docs/requirements/functional-requirements.md`: mark FR-201/FR-202 complete only after full verification.

---

### Task 1: Catalog domain behavior

**Files:**
- Modify: `backend/src/OnlineSupermarket.Domain/Catalog/Category.cs`
- Modify: `backend/src/OnlineSupermarket.Domain/Catalog/Brand.cs`
- Modify: `backend/src/OnlineSupermarket.Domain/Catalog/Product.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Catalog/CatalogEntityTests.cs`

**Interfaces:**
- Produces: `Category.Update(string name, string slug, Guid? parentCategoryId)`, `Activate()`, `Deactivate()`.
- Produces: `Brand.Update(string name, string slug)`, `Activate()`, `Deactivate()`.
- Produces: `Product.Update(Guid categoryId, Guid brandId, string sku, string name, string slug, string? description, decimal basePrice, string unit, string? imageUrl)`, `Activate()`, `Deactivate()`.

- [ ] **Step 1: Write failing domain tests**

Add focused tests proving trim/assignment, negative price rejection, empty IDs, and status transitions:

```csharp
[Fact]
public void Category_UpdateAndDeactivate_ChangesMutableState()
{
    var category = new Category("Cũ", "cu");
    var parentId = Guid.NewGuid();
    category.Update(" Mới ", " moi ", parentId);
    category.Deactivate();
    Assert.Equal("Mới", category.Name);
    Assert.Equal("moi", category.Slug);
    Assert.Equal(parentId, category.ParentCategoryId);
    Assert.False(category.IsActive);
    category.Activate();
    Assert.True(category.IsActive);
}

[Fact]
public void Product_Update_WithNegativePrice_Throws()
{
    var product = CreateProduct();
    Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(
        Guid.NewGuid(), Guid.NewGuid(), "SKU-2", "Tên", "ten", null, -1m, "cái", null));
}
```

Add equivalent Brand update/status and Product successful update/status tests. Use a private `CreateProduct()` factory in the test class to keep inputs consistent.

- [ ] **Step 2: Run tests and verify red state**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --filter FullyQualifiedName~CatalogEntityTests`

Expected: compile failure because the new behavior methods do not exist.

- [ ] **Step 3: Implement minimal entity behavior**

Use `Guard.Required` for strings, trim optional values, validate both IDs and price before assigning any field so a failed update is atomic:

```csharp
public void Deactivate() => IsActive = false;
public void Activate() => IsActive = true;
```

`Product.Update` must validate first into local variables, then assign all properties; it replaces the narrower `ChangeCategory` use for Admin mutation but does not remove `ChangeCategory` because existing callers/tests depend on it.

- [ ] **Step 4: Run domain tests**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --filter FullyQualifiedName~CatalogEntityTests`

Expected: all `CatalogEntityTests` pass.

- [ ] **Step 5: Commit domain behavior**

```powershell
git add backend/src/OnlineSupermarket.Domain/Catalog backend/tests/OnlineSupermarket.Domain.Tests/Catalog/CatalogEntityTests.cs
git commit -m "feat(catalog): add admin mutation behavior"
```

### Task 2: Admin Category and Brand APIs

**Files:**
- Create: `backend/src/OnlineSupermarket.Api/Contracts/Admin/AdminCatalogContracts.cs`
- Create: `backend/src/OnlineSupermarket.Api/Endpoints/AdminCatalogEndpoints.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Program.cs`
- Create: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/AdminCatalogEndpointsTests.cs`

**Interfaces:**
- Consumes: Task 1 Category/Brand behavior.
- Produces: `MapAdminCatalogEndpoints(IEndpointRouteBuilder)`.
- Produces: `AdminCategoryDto`, `AdminBrandDto`, `UpsertCategoryRequest`, `UpsertBrandRequest`, `UpdateCatalogStatusRequest`.

- [ ] **Step 1: Write authorization and Category/Brand failing tests**

Create test helpers that seed a `User` with `UserRole.Admin` or `UserRole.Customer`, resolve `ITokenService`, and set `AuthenticationHeaderValue("Bearer", token)`. Cover:

```csharp
[Theory]
[InlineData("/api/admin/catalog/categories")]
[InlineData("/api/admin/catalog/brands")]
public async Task AdminCatalog_WithoutToken_ReturnsUnauthorized(string path)
{
    using var client = _factory.CreateClient();
    Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
}

[Fact]
public async Task CreateCategory_WithDuplicateSlugIgnoringCase_ReturnsConflict()
{
    using var client = await CreateAuthenticatedClientAsync(UserRole.Admin);
    var response = await client.PostAsJsonAsync("/api/admin/catalog/categories",
        new UpsertCategoryRequest("Tên khác", "TIVI", null));
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}
```

Also cover Customer `403`, Admin list including inactive, successful create/update/status, self-parent `400`, cycle `400`, missing parent `400`, category-with-active-child `409`, category-with-active-product `409`, brand-with-active-product `409`, and restore success.

- [ ] **Step 2: Run endpoint tests and verify red state**

Run: `dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --filter FullyQualifiedName~AdminCatalogEndpointsTests`

Expected: tests fail with `404` because routes are not mapped.

- [ ] **Step 3: Define contracts**

Create records with exact signatures:

```csharp
public sealed record UpsertCategoryRequest(string Name, string Slug, Guid? ParentCategoryId);
public sealed record UpsertBrandRequest(string Name, string Slug);
public sealed record UpdateCatalogStatusRequest(bool IsActive);
public sealed record AdminCategoryDto(Guid Id, string Name, string Slug, Guid? ParentCategoryId, bool IsActive);
public sealed record AdminBrandDto(Guid Id, string Name, string Slug, bool IsActive);
```

- [ ] **Step 4: Implement Category and Brand handlers**

Create one group and protect it once:

```csharp
var group = routes.MapGroup("/api/admin/catalog")
    .WithTags("Admin/Catalog")
    .RequireAuthorization("AdminOnly");
```

For uniqueness, exclude the resource being updated and compare normalized lower-case values. For cycle detection, walk `ParentCategoryId` upward with `HashSet<Guid>` until null; return `400` if the edited category ID is encountered or an already visited node repeats. Before deactivate, query active children/products and return `Results.Conflict(new { message = "..." })`.

Map all eight Category/Brand endpoints and call `app.MapAdminCatalogEndpoints()` in `Program.cs` after public Catalog mapping.

- [ ] **Step 5: Run focused endpoint tests**

Run: `dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --filter FullyQualifiedName~AdminCatalogEndpointsTests`

Expected: Category/Brand, `401` and `403` tests pass.

- [ ] **Step 6: Commit Category and Brand APIs**

```powershell
git add backend/src/OnlineSupermarket.Api/Contracts/Admin backend/src/OnlineSupermarket.Api/Endpoints/AdminCatalogEndpoints.cs backend/src/OnlineSupermarket.Api/Program.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/AdminCatalogEndpointsTests.cs
git commit -m "feat(admin): add category and brand management APIs"
```

### Task 3: Admin Product API

**Files:**
- Modify: `backend/src/OnlineSupermarket.Api/Contracts/Admin/AdminCatalogContracts.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Endpoints/AdminCatalogEndpoints.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/AdminCatalogEndpointsTests.cs`

**Interfaces:**
- Consumes: Task 1 `Product.Update/Activate/Deactivate`.
- Produces: `UpsertProductRequest`, `AdminProductDto`, `AdminProductListResponse` and five product handlers.

- [ ] **Step 1: Write failing Product API tests**

Add tests for paginated/filterable list, create, update, deactivate, restore, duplicate SKU ignoring case, duplicate slug ignoring case, missing/inactive category, missing/inactive brand, negative price and not-found mutation.

Representative success assertion:

```csharp
var response = await client.PostAsJsonAsync("/api/admin/catalog/products", new UpsertProductRequest(
    category.Id, brand.Id, "ADM-001", "Sản phẩm Admin", "san-pham-admin",
    "Mô tả", 1_250_000m, "cái", "https://img.example/item.jpg"));
Assert.Equal(HttpStatusCode.Created, response.StatusCode);
var dto = await response.Content.ReadFromJsonAsync<AdminProductDto>();
Assert.Equal("ADM-001", dto!.Sku);
Assert.True(dto.IsActive);
```

- [ ] **Step 2: Run Product tests and verify red state**

Run: `dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --filter "FullyQualifiedName~AdminCatalogEndpointsTests&Name~Product"`

Expected: product routes return `404`.

- [ ] **Step 3: Add Product contracts**

```csharp
public sealed record UpsertProductRequest(
    Guid CategoryId, Guid BrandId, string Sku, string Name, string Slug,
    string? Description, decimal BasePrice, string Unit, string? ImageUrl);

public sealed record AdminProductDto(
    Guid Id, Guid CategoryId, string CategoryName, Guid BrandId, string BrandName,
    string Sku, string Name, string Slug, string? Description, decimal BasePrice,
    string Unit, string? ImageUrl, bool IsActive);
```

Reuse public `PaginationMeta` in `PaginatedResponse<AdminProductDto>` instead of creating a second pagination envelope.

- [ ] **Step 4: Implement Product handlers**

Map:

```text
GET   /api/admin/catalog/products
GET   /api/admin/catalog/products/{id}
POST  /api/admin/catalog/products
PUT   /api/admin/catalog/products/{id}
PATCH /api/admin/catalog/products/{id}/status
```

Clamp page/pageSize exactly like `CatalogEndpoints`. Include Category and Brand in DTO queries. Normalize query `search`; allow nullable `isActive`. Validate active Category/Brand before entity construction/update. Map domain `ArgumentException`/`ArgumentOutOfRangeException` to `400` and database uniqueness conflicts detected before save to `409`.

- [ ] **Step 5: Run Product API tests**

Run: `dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --filter FullyQualifiedName~AdminCatalogEndpointsTests`

Expected: all Admin Catalog endpoint tests pass.

- [ ] **Step 6: Commit Product API**

```powershell
git add backend/src/OnlineSupermarket.Api/Contracts/Admin/AdminCatalogContracts.cs backend/src/OnlineSupermarket.Api/Endpoints/AdminCatalogEndpoints.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/AdminCatalogEndpointsTests.cs
git commit -m "feat(admin): add product management API"
```

### Task 4: Public catalog regressions and OpenAPI

**Files:**
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/CatalogEndpointsTests.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/OpenApiContractTests.cs`
- Modify: `docs/api/openapi.json`

**Interfaces:**
- Consumes: Admin Catalog routes and status behavior from Tasks 2–3.
- Produces: documented API contract and regression proof that inactive records stay private.

- [ ] **Step 1: Add failing public filtering regression tests**

Seed inactive category, brand and product through domain methods, then assert public `/api/categories`, `/api/brands`, `/api/products`, and `/api/products/{id}` omit them. Include a product whose category or brand becomes inactive; public product queries must require `p.Category.IsActive && p.Brand.IsActive` in addition to `p.IsActive`.

- [ ] **Step 2: Run Catalog endpoint tests**

Run: `dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --filter FullyQualifiedName~CatalogEndpointsTests`

Expected: the inactive-parent product test fails against current query behavior.

- [ ] **Step 3: Tighten public filters**

Modify product list/detail predicates so active product, category and brand are all required. Keep existing branch/category hierarchy behavior unchanged.

- [ ] **Step 4: Add OpenAPI contract assertions and document paths**

Extend tests to assert all 13 Admin Catalog operations exist across nine URL paths and contain bearer security. Add request/response schemas and response codes `200/201/400/401/403/404/409` to `docs/api/openapi.json`; keep JSON valid and preserve existing components.

- [ ] **Step 5: Run backend suite**

Run: `dotnet test OnlineSupermarket.slnx`

Expected: all backend/domain/infrastructure tests pass.

- [ ] **Step 6: Commit public filters and contract**

```powershell
git add backend/src/OnlineSupermarket.Api/Endpoints/CatalogEndpoints.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/CatalogEndpointsTests.cs backend/tests/OnlineSupermarket.Api.Tests/OpenApiContractTests.cs docs/api/openapi.json
git commit -m "test(catalog): protect inactive data and document admin API"
```

### Task 5: Typed frontend Admin Catalog client

**Files:**
- Modify: `frontend/src/api/httpClient.ts`
- Create: `frontend/src/api/adminCatalogApi.ts`
- Create: `frontend/src/api/adminCatalogApi.test.ts`

**Interfaces:**
- Produces: `patchJson<T>(path, body, options)`.
- Produces: `adminCatalogApi` with typed category, brand and product list/create/update/status methods.

- [ ] **Step 1: Write failing API client tests**

Mock `fetch` and assert bearer token, encoded filters, verbs and JSON payload:

```ts
await adminCatalogApi.updateProductStatus('product-1', false, 'jwt-token')
expect(fetch).toHaveBeenCalledWith('/api/admin/catalog/products/product-1/status', expect.objectContaining({
  method: 'PATCH',
  headers: expect.objectContaining({ Authorization: 'Bearer jwt-token' }),
  body: JSON.stringify({ isActive: false }),
}))
```

Cover at least one GET query, one POST, one PUT and one PATCH call.

- [ ] **Step 2: Run API client test and verify red state**

Run: `npm test -- --run src/api/adminCatalogApi.test.ts` from `frontend`.

Expected: import failure because client/module does not exist.

- [ ] **Step 3: Implement `patchJson` and typed client**

Use these public types: `AdminCategory`, `AdminBrand`, `AdminProduct`, `UpsertCategoryInput`, `UpsertBrandInput`, `UpsertProductInput`, `UpdateStatusInput`, `AdminProductListParams`. Reuse `PaginatedResponse<T>` from `catalogApi.ts`. Each mutation receives `token: string`; list methods also receive the token and optional `AbortSignal`.

- [ ] **Step 4: Run API client tests**

Run: `npm test -- --run src/api/adminCatalogApi.test.ts` from `frontend`.

Expected: all API client tests pass.

- [ ] **Step 5: Commit client boundary**

```powershell
git add frontend/src/api/httpClient.ts frontend/src/api/adminCatalogApi.ts frontend/src/api/adminCatalogApi.test.ts
git commit -m "feat(frontend): add admin catalog API client"
```

### Task 6: Admin route guard and layout

**Files:**
- Create: `frontend/src/features/admin/AdminRoute.tsx`
- Create: `frontend/src/features/admin/AdminLayout.tsx`
- Create: `frontend/src/features/admin/AdminCatalog.css`
- Create: `frontend/src/features/admin/AdminRoute.test.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/App.test.tsx`

**Interfaces:**
- Consumes: `useAuth()` fields `user`, `accessToken`, `isAuthenticated`, `isLoading`.
- Produces: `<AdminRoute />` rendering `<Outlet />` only for `user.role === 'Admin'`.
- Produces: `<AdminLayout />` with links to the three Admin Catalog pages and `<Outlet />`.

- [ ] **Step 1: Write failing route guard tests**

Mock `useAuth` for loading, guest, Customer and Admin states. Assert loading text, guest redirect to `/`, Customer redirect to `/`, and Admin child rendering. Use `<MemoryRouter initialEntries={['/admin/catalog/categories']}>` with a sentinel home route.

- [ ] **Step 2: Run route tests and verify red state**

Run: `npm test -- --run src/features/admin/AdminRoute.test.tsx src/App.test.tsx` from `frontend`.

Expected: component import/route assertions fail.

- [ ] **Step 3: Implement route guard, layout and route wiring**

Add nested routes:

```tsx
<Route element={<AdminRoute />}>
  <Route path="/admin" element={<AdminLayout />}>
    <Route index element={<Navigate to="catalog/categories" replace />} />
    <Route path="catalog/categories" element={<AdminCategoriesPage />} />
    <Route path="catalog/brands" element={<AdminBrandsPage />} />
    <Route path="catalog/products" element={<AdminProductsPage />} />
  </Route>
</Route>
```

Initially create page modules that render accessible headings only; Tasks 7–8 replace those minimal bodies. Style admin navigation, content container, responsive table wrapper, form grid, buttons, alerts and visually hidden labels in `AdminCatalog.css`.

- [ ] **Step 4: Run route tests and frontend build**

Run: `npm test -- --run src/features/admin/AdminRoute.test.tsx src/App.test.tsx` and `npm run build` from `frontend`.

Expected: tests and TypeScript build pass.

- [ ] **Step 5: Commit Admin shell**

```powershell
git add frontend/src/App.tsx frontend/src/App.test.tsx frontend/src/features/admin
git commit -m "feat(frontend): add protected admin catalog shell"
```

### Task 7: Category and Brand management pages

**Files:**
- Modify: `frontend/src/features/admin/categories/AdminCategoriesPage.tsx`
- Create: `frontend/src/features/admin/categories/AdminCategoriesPage.test.tsx`
- Modify: `frontend/src/features/admin/brands/AdminBrandsPage.tsx`
- Create: `frontend/src/features/admin/brands/AdminBrandsPage.test.tsx`
- Modify: `frontend/src/features/admin/AdminCatalog.css`

**Interfaces:**
- Consumes: Task 5 `adminCatalogApi`, Task 6 route/auth shell.
- Produces: complete Category and Brand list/create/edit/status workflows.

- [ ] **Step 1: Write failing page tests**

Mock `adminCatalogApi` and `useAuth`. For each page cover: loading list, empty state, rendered active/inactive rows, create form payload, edit form population/update payload, server Problem Details message, confirmation before deactivate, restore without destructive wording, and reload after mutation.

Category-specific assertion:

```ts
expect(adminCatalogApi.createCategory).toHaveBeenCalledWith(
  { name: 'Điện gia dụng', slug: 'dien-gia-dung', parentCategoryId: null },
  'jwt-token'
)
```

- [ ] **Step 2: Run page tests and verify red state**

Run: `npm test -- --run src/features/admin/categories/AdminCategoriesPage.test.tsx src/features/admin/brands/AdminBrandsPage.test.tsx` from `frontend`.

Expected: behavior assertions fail against heading-only pages.

- [ ] **Step 3: Implement Category page**

Use controlled fields `name`, `slug`, `parentCategoryId`; exclude the edited category and its descendants from parent options. Load with `includeInactive=true`, use `AbortController`, clear stale rows on failed load, disable form/actions while saving, and render errors with `role="alert"`.

- [ ] **Step 4: Implement Brand page**

Use controlled `name` and `slug`, identical loading/error/status semantics, and accessible row action names containing the brand name.

- [ ] **Step 5: Run focused UI tests and build**

Run: `npm test -- --run src/features/admin/categories/AdminCategoriesPage.test.tsx src/features/admin/brands/AdminBrandsPage.test.tsx` and `npm run build` from `frontend`.

Expected: all focused tests and build pass.

- [ ] **Step 6: Commit Category and Brand pages**

```powershell
git add frontend/src/features/admin/categories frontend/src/features/admin/brands frontend/src/features/admin/AdminCatalog.css
git commit -m "feat(frontend): manage admin categories and brands"
```

### Task 8: Product management page

**Files:**
- Modify: `frontend/src/features/admin/products/AdminProductsPage.tsx`
- Create: `frontend/src/features/admin/products/AdminProductsPage.test.tsx`
- Modify: `frontend/src/features/admin/AdminCatalog.css`

**Interfaces:**
- Consumes: Task 5 Admin Product API and Category/Brand list methods.
- Produces: filterable, paginated Product list plus create/edit/status workflow.

- [ ] **Step 1: Write failing Product page tests**

Cover initial parallel load of products/categories/brands, search/category/brand/status filters, pagination, empty/error states, create payload conversion of price to number, edit population, field validation, confirmation before deactivate, restore, and refresh after mutation.

```ts
expect(adminCatalogApi.createProduct).toHaveBeenCalledWith(expect.objectContaining({
  categoryId: 'category-1',
  brandId: 'brand-1',
  sku: 'TV-001',
  basePrice: 12990000,
  imageUrl: 'https://img.example/tv.jpg',
}), 'jwt-token')
```

- [ ] **Step 2: Run Product page test and verify red state**

Run: `npm test -- --run src/features/admin/products/AdminProductsPage.test.tsx` from `frontend`.

Expected: behavior assertions fail against heading-only page.

- [ ] **Step 3: Implement Product list and filters**

Keep `page`, `search`, `categoryId`, `brandId`, `isActive` in component state; reset `page` to 1 when a filter changes. Request `pageSize=20`, abort superseded requests, and render backend pagination meta.

- [ ] **Step 4: Implement Product form and status actions**

Require category, brand, SKU, name, slug, non-negative numeric price and unit before mutation. Convert blank description/image URL to `null`. Category/brand selects only offer active records. Display returned `ApiError.data.message` with a generic Vietnamese fallback.

- [ ] **Step 5: Run Product tests and frontend suite**

Run: `npm test -- --run src/features/admin/products/AdminProductsPage.test.tsx`, then `npm test -- --run` and `npm run build` from `frontend`.

Expected: focused and full frontend tests pass; TypeScript/Vite build succeeds.

- [ ] **Step 6: Commit Product page**

```powershell
git add frontend/src/features/admin/products frontend/src/features/admin/AdminCatalog.css
git commit -m "feat(frontend): manage admin products"
```

### Task 9: Final verification and requirement status

**Files:**
- Modify: `docs/requirements/functional-requirements.md`

**Interfaces:**
- Consumes: all previous tasks.
- Produces: verified `FR-201` and `FR-202` completion record.

- [ ] **Step 1: Run formatting and diff safety checks**

Run: `git diff --check` and `git status --short`.

Expected: no whitespace errors; pre-existing user files remain uncommitted and absent from feature commits.

- [ ] **Step 2: Run complete backend verification**

Run: `dotnet test OnlineSupermarket.slnx`

Expected: zero failed tests.

- [ ] **Step 3: Run complete frontend verification**

Run from `frontend`: `npm test -- --run` followed by `npm run build`.

Expected: zero failed tests and successful production build.

- [ ] **Step 4: Update functional requirement registry**

Change only the FR-201 and FR-202 rows from `DRAFT / PLANNED_CYCLE_4` to `✅ IMPLEMENTED / DONE`. Keep FR-202 acceptance wording explicit that v1 stores an image URL and binary upload remains outside this delivery.

- [ ] **Step 5: Commit verified requirement status**

```powershell
git add docs/requirements/functional-requirements.md
git commit -m "docs: mark admin catalog requirements complete"
```

- [ ] **Step 6: Record final evidence**

Run: `git log -9 --oneline` and `git status --short`.

Expected: feature commits are visible; only the user's pre-existing unrelated changes remain in the working tree.
