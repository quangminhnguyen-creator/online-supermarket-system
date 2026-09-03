# Product Views and Recommendations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ghi product views riêng tư, merge anonymous history sau login, materialize content-based recommendations và hiển thị chúng ở storefront cùng Admin diagnostics.

**Architecture:** Public view endpoint ghi event với user JWT hoặc anonymous GUID; authenticated merge chỉ claim rows chưa có owner. `RecommendationJobHandler` tính Global/User/SimilarProduct results từ views, completed purchases, category/brand và popularity, rồi commit batch theo `jobRunId`; read API tự fallback và filter availability theo branch.

**Tech Stack:** .NET 10, C# 14, EF Core 10.0.9, MySQL 8.4, ASP.NET Core Minimal API, xUnit, React 19.2.8, TypeScript 5.9.3, Vitest, React Testing Library.

**Spec:** `docs/superpowers/specs/2026-09-03-reviews-inventory-intelligence-design.md`

## Global Constraints

- Plan nền `2026-09-03-background-job-foundation.md` phải hoàn tất trước Task 4.
- Không lưu IP hoặc user-agent.
- Anonymous session là GUID từ `localStorage`; merge chỉ update rows `user_id IS NULL`.
- Không tạo `user_product_affinities`; chỉ materialize ranked `recommendation_results`.
- Scope hợp lệ: `Global`, `User`, `SimilarProduct`; `audience_key` là `global`, `user:{id}`, `product:{id}`.
- Read API trả 200 với `sourceScope`; expired/missing results fallback server-side hoặc trả list rỗng.
- Chỉ active products được materialize; branch availability filter xảy ra ở read API.
- Recommendation job lock là `global`, interval mặc định 60 phút.
- Mỗi task theo RED -> GREEN -> REFACTOR -> COMMIT; không stage thay đổi ngoài scope.

---

## File Structure

- Create `ProductViewEvent`, `RecommendationResult`, `RecommendationScope` domain files.
- Create EF configurations, DbSets, migration `AddProductRecommendations`.
- Create public view and authenticated session-merge endpoints/contracts.
- Create a relational event store so production merge remains one atomic SQL update.
- Create `RecommendationScorer`, handler, recurring schedule, and tests.
- Create customer read, Admin sample, and manual trigger endpoints/contracts.
- Create typed frontend API and anonymous identity helper.
- Modify `AuthContext` to merge once after authentication.
- Create reusable `RecommendationShelf` and mount on homepage/product detail.
- Add product-detail view capture.
- Create Admin recommendations page/route/nav.

---

### Task 1: Product view and recommendation domain models

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Recommendations/ProductViewEvent.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Recommendations/RecommendationResult.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Recommendations/RecommendationScope.cs`
- Create: `backend/tests/OnlineSupermarket.Domain.Tests/Recommendations/RecommendationEntityTests.cs`

**Interfaces:**
- Produces: `ProductViewEvent.Create(Guid productId, Guid? userId, Guid? anonymousSessionId, Guid? branchId, DateTime viewedAtUtc)`.
- Produces: `RecommendationResult.CreateGlobal`, `CreateForUser`, `CreateSimilarProduct` factories.
- Produces: exact audience keys and expiry validation.

- [ ] **Step 1: Write failing factory tests**

```csharp
[Fact]
public void ProductViewEvent_RequiresUserOrAnonymousSession()
{
    Assert.Throws<ArgumentException>(() => ProductViewEvent.Create(
        Guid.NewGuid(), null, null, null, DateTime.UtcNow));
}

[Fact]
public void UserRecommendation_BuildsStableAudienceKey()
{
    var userId = Guid.NewGuid();
    var result = RecommendationResult.CreateForUser(
        userId, Guid.NewGuid(), 0.75m, 1, "Phù hợp danh mục đã xem",
        "content-v1", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), Guid.NewGuid());

    Assert.Equal(RecommendationScope.User, result.Scope);
    Assert.Equal($"user:{userId}", result.AudienceKey);
    Assert.Null(result.SourceProductId);
}
```

Add tests for global/similar audience keys, positive rank, score range 0–1, expiry after generation, and mutually exclusive user/source product fields.

- [ ] **Step 2: Run RED**

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~RecommendationEntityTests"
```

- [ ] **Step 3: Implement factories**

```csharp
private static string UserAudienceKey(Guid userId) => $"user:{userId}";
private static string ProductAudienceKey(Guid productId) => $"product:{productId}";

public static RecommendationResult CreateGlobal(
    Guid recommendedProductId, decimal score, int rank, string reason,
    string algorithmVersion, DateTime generatedAtUtc, DateTime expiresAtUtc, Guid jobRunId)
    => Create(RecommendationScope.Global, "global", null, null,
        recommendedProductId, score, rank, reason, algorithmVersion,
        generatedAtUtc, expiresAtUtc, jobRunId);
```

- [ ] **Step 4: Run GREEN and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~RecommendationEntityTests"
git add backend/src/OnlineSupermarket.Domain/Recommendations backend/tests/OnlineSupermarket.Domain.Tests/Recommendations
git commit -m "feat(recommendations): add view and result domain models"
```

---

### Task 2: Persistence and migration

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/ProductViewEventConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/RecommendationResultConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Generate: EF migration named `AddProductRecommendations` and its designer.
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs`

**Interfaces:**
- Produces: `AppDbContext.ProductViewEvents`, `AppDbContext.RecommendationResults`.
- Produces: unique `(job_run_id, audience_key, recommended_product_id)`.
- Produces: view indexes `(user_id, viewed_at_utc)`, `(anonymous_session_id, viewed_at_utc)`, `(product_id, viewed_at_utc)`.

- [ ] **Step 1: Write failing EF model tests**

```csharp
[Fact]
public void RecommendationResult_HasAudienceScopedUniqueIndex()
{
    using var context = CreateContext();
    var entity = context.Model.FindEntityType(typeof(RecommendationResult))!;
    var index = entity.GetIndexes().Single(x => x.Properties.Select(p => p.Name)
        .SequenceEqual(new[] { "JobRunId", "AudienceKey", "RecommendedProductId" }));
    Assert.True(index.IsUnique);
}
```

- [ ] **Step 2: Map exact schema and checks**

```csharp
builder.ToTable("recommendation_results", table =>
{
    table.HasCheckConstraint("ck_recommendation_rank", "rank > 0");
    table.HasCheckConstraint("ck_recommendation_score", "score >= 0 AND score <= 1");
});
builder.Property(x => x.Score).HasColumnName("score").HasPrecision(12, 6);
builder.HasIndex(x => new { x.JobRunId, x.AudienceKey, x.RecommendedProductId })
    .IsUnique().HasDatabaseName("ix_recommendation_results_run_audience_product");
```

Map all FKs Restrict and every field from spec §4.3–4.4.

- [ ] **Step 3: Generate migration and inspect**

```powershell
dotnet ef migrations add AddProductRecommendations --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
rg -n "product_view_events|recommendation_results|audience_key" backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations
```

- [ ] **Step 4: Test and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ModelConfigurationTests"
git add backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/ProductViewEventConfiguration.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/RecommendationResultConfiguration.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddProductRecommendations.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddProductRecommendations.Designer.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs
git commit -m "feat(recommendations): persist view events and ranked results"
```

---

### Task 3: View capture and anonymous-session merge APIs

**Files:**
- Create: `backend/src/OnlineSupermarket.Api/Contracts/Recommendation/RecommendationContracts.cs`
- Create: `backend/src/OnlineSupermarket.Api/Endpoints/RecommendationEventEndpoints.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Recommendations/IProductViewEventStore.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Recommendations/ProductViewEventStore.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Program.cs`
- Create: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/RecommendationEventEndpointsTests.cs`
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlProductViewEventStoreTests.cs`

**Interfaces:**
- Produces: `POST /api/products/{productId}/views`.
- Produces: `POST /api/recommendations/session/merge`.
- Produces: `RecordProductViewRequest(Guid AnonymousSessionId, Guid? BranchId)` and `MergeRecommendationSessionRequest(Guid AnonymousSessionId)`.
- Produces: `IProductViewEventStore.MergeAnonymousSessionAsync(Guid, Guid, CancellationToken): Task<int>`.

- [ ] **Step 1: Write failing privacy/ownership tests**

```csharp
[Fact]
public async Task RecordView_AsGuest_PersistsOnlyApprovedFields()
{
    var response = await _client.PostAsJsonAsync($"/api/products/{_productId}/views",
        new { anonymousSessionId = _sessionId, branchId = _branchId });

    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    var stored = Assert.Single(_factory.Context.ProductViewEvents);
    Assert.Null(stored.UserId);
    Assert.Equal(_sessionId, stored.AnonymousSessionId);
}

[Fact]
public async Task Merge_ClaimsOnlyUnownedRowsAndIsSafeToRepeat()
{
    _eventStore.SetupSequence(x => x.MergeAnonymousSessionAsync(
            _sessionId, _userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(2)
        .ReturnsAsync(0);
    using var client = await CreateCustomerClientAsync(_userId);

    var first = await client.PostAsJsonAsync("/api/recommendations/session/merge",
        new { anonymousSessionId = _sessionId });
    var second = await client.PostAsJsonAsync("/api/recommendations/session/merge",
        new { anonymousSessionId = _sessionId });

    Assert.Equal(2, (await first.Content.ReadFromJsonAsync<MergeSessionResponse>())!.MergedCount);
    Assert.Equal(0, (await second.Content.ReadFromJsonAsync<MergeSessionResponse>())!.MergedCount);
}
```

Add invalid/empty GUID 400, product/branch 404, authenticated identity from JWT, merge 401, and no reassignment of another user's rows.

- [ ] **Step 2: Implement endpoints**

Use optional authentication on view capture. Ignore any client user ID. The
Infrastructure store uses one relational `ExecuteUpdateAsync` predicate:

```csharp
db.ProductViewEvents.Where(x =>
    x.AnonymousSessionId == request.AnonymousSessionId && x.UserId == null)
```

Set `UserId` to the JWT user and return affected count.

Register `IProductViewEventStore` as scoped. In API tests replace only this store
with a mock because the existing `TestApiFactory` uses EF InMemory, which does not
execute relational bulk updates. In `MySqlProductViewEventStoreTests`, seed
unowned rows plus a row owned by another user, run two simultaneous merge calls
through independent contexts, and assert every row is assigned at most once and
the pre-owned row is unchanged.

- [ ] **Step 3: Test and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~RecommendationEventEndpointsTests"
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~MySqlProductViewEventStoreTests"
git add backend/src/OnlineSupermarket.Api/Contracts/Recommendation backend/src/OnlineSupermarket.Api/Endpoints/RecommendationEventEndpoints.cs backend/src/OnlineSupermarket.Api/Program.cs backend/src/OnlineSupermarket.Infrastructure/Recommendations/IProductViewEventStore.cs backend/src/OnlineSupermarket.Infrastructure/Recommendations/ProductViewEventStore.cs backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/RecommendationEventEndpointsTests.cs backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlProductViewEventStoreTests.cs
git commit -m "feat(recommendations): capture and merge product views"
```

---

### Task 4: Deterministic content-based scorer

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Recommendations/RecommendationScorer.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Recommendations/RecommendationInputs.cs`
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Recommendations/RecommendationScorerTests.cs`

**Interfaces:**
- Produces: `ScoreGlobal`, `ScoreUser`, `ScoreSimilarProducts` returning ordered `ScoredProduct`.
- Produces: at most 20 Global/User and 8 SimilarProduct results per audience.

- [ ] **Step 1: Write failing ranking tests**

```csharp
[Fact]
public void ScoreUser_CapsRepeatedViewsPerProductPerUtcDay()
{
    var oneView = BuildInput(viewCountSameDay: 1);
    var fiftyViews = BuildInput(viewCountSameDay: 50);

    var first = RecommendationScorer.ScoreUser(oneView, _userId);
    var repeated = RecommendationScorer.ScoreUser(fiftyViews, _userId);

    Assert.Equal(first.Select(x => x.Score), repeated.Select(x => x.Score));
}

[Fact]
public void ScoreSimilar_ExcludesSourceAndInactiveProducts()
{
    var ranked = RecommendationScorer.ScoreSimilarProducts(_input, _sourceProductId);

    Assert.DoesNotContain(ranked, x => x.ProductId == _sourceProductId);
    Assert.DoesNotContain(ranked, x => x.ProductId == _inactiveProductId);
    Assert.True(ranked.SequenceEqual(ranked.OrderByDescending(x => x.Score).ThenBy(x => x.ProductId)));
}
```

Add cold-start global ranking, completed-purchase weight, recency decay, unseen-product exclusion, stable tie-breaker, and customer-safe reason tests.

- [ ] **Step 2: Implement exact scoring weights**

```text
Global = 0.40 * normalized unique 28-day views
       + 0.60 * normalized 28-day sold quantity

User = 0.55 * normalized category affinity
     + 0.25 * normalized brand affinity
     + 0.20 * Global score

Similar = 0.60 * same category
        + 0.30 * same brand
        + 0.10 * Global score
```

One view per user/product/day contributes `1 / (1 + ageDays)`. Each completed purchased unit contributes three times the corresponding view weight. Clamp scores to 0–1; order by score descending then product ID ascending.

- [ ] **Step 3: Run tests and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~RecommendationScorerTests"
git add backend/src/OnlineSupermarket.Infrastructure/Recommendations backend/tests/OnlineSupermarket.Infrastructure.Tests/Recommendations
git commit -m "feat(recommendations): rank content based suggestions"
```

---

### Task 5: Recommendation job handler and hourly schedule

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Recommendations/RecommendationJobHandler.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Recommendations/RecommendationRecurringSchedule.cs`
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Recommendations/RecommendationJobHandlerTests.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Implements: `IBackgroundJobHandler` with `JobName == Recommendations`.
- Implements: recurring schedule with global lock every configured 60 minutes.
- Produces: atomic Global/User/SimilarProduct batch for one run.

- [ ] **Step 1: Write failing batch tests**

```csharp
[Fact]
public async Task ExecuteAsync_MaterializesAllThreeScopesInOneRun()
{
    await SeedViewsPurchasesAndCatalogAsync();

    await _handler.ExecuteAsync(_jobRunId, null, CancellationToken.None);

    var rows = await _db.RecommendationResults.Where(x => x.JobRunId == _jobRunId).ToListAsync();
    Assert.Contains(rows, x => x.Scope == RecommendationScope.Global);
    Assert.Contains(rows, x => x.Scope == RecommendationScope.User && x.UserId == _userId);
    Assert.Contains(rows, x => x.Scope == RecommendationScope.SimilarProduct && x.SourceProductId != null);
}
```

Add rollback, inactive exclusion, expiry, stable ranks, no-data global-empty, and new-run-preserves-history tests.

- [ ] **Step 2: Implement query/materialization**

Load active product/category/brand projections plus previous 28 days of views and completed order items. Calculate in memory, create result rows with algorithm `content-v1`, `generatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime`, and expiry `RecommendationIntervalMinutes + 15` minutes. Save in one transaction without deleting prior runs. `RecommendationRecurringSchedule` is due only when there is no active global run and the newest queued/started/completed global run was created at least `RecommendationIntervalMinutes` ago.

- [ ] **Step 3: Register and verify**

```csharp
services.AddScoped<IBackgroundJobHandler, RecommendationJobHandler>();
services.AddScoped<IRecurringJobSchedule, RecommendationRecurringSchedule>();
```

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~RecommendationJobHandlerTests"
git add backend/src/OnlineSupermarket.Infrastructure/Recommendations backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs backend/tests/OnlineSupermarket.Infrastructure.Tests/Recommendations
git commit -m "feat(recommendations): materialize hourly results"
```

---

### Task 6: Customer/Admin read APIs and manual trigger

**Files:**
- Modify: `backend/src/OnlineSupermarket.Api/Contracts/Recommendation/RecommendationContracts.cs`
- Create: `backend/src/OnlineSupermarket.Api/Endpoints/RecommendationEndpoints.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Program.cs`
- Create: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/RecommendationEndpointsTests.cs`

**Interfaces:**
- Produces: `GET /api/recommendations?branchId=&limit=`.
- Produces: `GET /api/products/{productId}/recommendations?branchId=&limit=`.
- Produces: `GET /api/admin/recommendations/results?scope=&limit=`.
- Produces: `POST /api/admin/jobs/recommendations/runs`.

- [ ] **Step 1: Write failing fallback/availability tests**

```csharp
[Fact]
public async Task Homepage_WhenUserRowsExpired_FallsBackToGlobalWith200()
{
    await SeedExpiredUserAndFreshGlobalResultsAsync();
    using var client = await CreateCustomerClientAsync(_userId);

    var response = await client.GetAsync("/api/recommendations?branchId=" + _branchId);
    var body = await response.Content.ReadFromJsonAsync<RecommendationResponse>();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("Global", body!.SourceScope);
    Assert.All(body.Items, x => Assert.True(x.AvailableQuantity > 0));
}

[Fact]
public async Task ProductRecommendations_WhenNoMaterialization_ReturnsEmpty200()
{
    var response = await _client.GetFromJsonAsync<RecommendationResponse>(
        $"/api/products/{_productId}/recommendations");
    Assert.NotNull(response);
    Assert.Empty(response!.Items);
}
```

Add User-first, Similar-first, branch filter then Global fallback, limit 1–20 validation, Admin auth, sample scope filter, trigger 202/status location, and active-lock 409.

- [ ] **Step 2: Implement response contracts and latest-success query**

```csharp
public sealed record RecommendationItemDto(
    Guid ProductId, string Name, string Slug, string? ImageUrl,
    decimal Price, int? AvailableQuantity, decimal Score, string Reason);

public sealed record RecommendationResponse(
    string? SourceScope, DateTime? GeneratedAtUtc,
    IReadOnlyList<RecommendationItemDto> Items);
```

Use only non-expired rows from the latest `Succeeded` recommendation run. Filter active products and branch availability before applying requested limit. If the first scope becomes empty after filter, execute the documented fallback.

- [ ] **Step 3: Run tests and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~RecommendationEndpointsTests"
git add backend/src/OnlineSupermarket.Api/Contracts/Recommendation backend/src/OnlineSupermarket.Api/Endpoints/RecommendationEndpoints.cs backend/src/OnlineSupermarket.Api/Program.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/RecommendationEndpointsTests.cs
git commit -m "feat(recommendations): expose materialized recommendation api"
```

---

### Task 7: Frontend anonymous identity, typed API, and login merge

**Files:**
- Create: `frontend/src/features/recommendations/recommendationIdentity.ts`
- Create: `frontend/src/features/recommendations/recommendationIdentity.test.ts`
- Create: `frontend/src/api/recommendationApi.ts`
- Create: `frontend/src/api/recommendationApi.test.ts`
- Modify: `frontend/src/features/auth/AuthContext.tsx`
- Modify: `frontend/src/features/auth/Auth.test.tsx`

**Interfaces:**
- Produces: `getOrCreateAnonymousSessionId`, `rotateAnonymousSessionId`.
- Produces: typed view/merge/read/admin/trigger clients.
- Changes: successful login/register/session restore attempts merge without failing authentication.

- [ ] **Step 1: Write failing identity/merge tests**

```typescript
it('creates and reuses a valid anonymous guid', () => {
  const first = getOrCreateAnonymousSessionId()
  const second = getOrCreateAnonymousSessionId()
  expect(second).toBe(first)
  expect(first).toMatch(/^[0-9a-f-]{36}$/i)
})

it('keeps login successful when session merge fails', async () => {
  recommendationApi.mergeSession.mockRejectedValue(new Error('offline'))
  await user.login(validCredentials)
  expect(screen.getByText('Tài khoản')).toBeInTheDocument()
})
```

- [ ] **Step 2: Implement API and merge lifecycle**

After tokens/user are stored, call merge with the current anonymous GUID. On success rotate to a fresh GUID; on failure retain the old GUID so the next authenticated initialization retries. Do not block login rendering on merge.

- [ ] **Step 3: Test and commit**

```powershell
npm --prefix frontend test -- --run src/features/recommendations/recommendationIdentity.test.ts src/api/recommendationApi.test.ts src/features/auth/Auth.test.tsx
git add frontend/src/features/recommendations/recommendationIdentity.ts frontend/src/features/recommendations/recommendationIdentity.test.ts frontend/src/api/recommendationApi.ts frontend/src/api/recommendationApi.test.ts frontend/src/features/auth/AuthContext.tsx frontend/src/features/auth/Auth.test.tsx
git commit -m "feat(frontend): merge anonymous recommendation history"
```

---

### Task 8: Homepage/product shelves and product view capture

**Files:**
- Create: `frontend/src/features/recommendations/RecommendationShelf.tsx`
- Create: `frontend/src/features/recommendations/RecommendationShelf.test.tsx`
- Create: `frontend/src/features/recommendations/RecommendationShelf.css`
- Modify: `frontend/src/features/products/ProductBrowsePage.tsx`
- Modify: `frontend/src/features/products/ProductBrowse.test.tsx`
- Modify: `frontend/src/features/products/ProductDetailPage.tsx`
- Modify: `frontend/src/features/products/ProductDetail.test.tsx`

**Interfaces:**
- Produces: reusable shelf with placement `home` or `product`.
- Produces: one view request per mounted product ID; StrictMode rerender is deduped client-side.

- [ ] **Step 1: Write failing shelf/view tests**

```typescript
it('renders fallback scope without exposing it as an error', async () => {
  recommendationApi.getHomepage.mockResolvedValue({
    sourceScope: 'Global', generatedAtUtc: now, items: [product],
  })
  render(<RecommendationShelf placement="home" />)
  expect(await screen.findByRole('region', { name: 'Gợi ý cho bạn' })).toBeInTheDocument()
})

it('records one product view despite StrictMode effect replay', async () => {
  render(<StrictMode><ProductDetailPage /></StrictMode>)
  await screen.findByRole('heading', { name: product.name })
  expect(recommendationApi.recordView).toHaveBeenCalledTimes(1)
})
```

Cover empty/error preserving catalog content, retry, branch-aware request, product similar heading, and abort on route change.

- [ ] **Step 2: Implement shelf and capture**

Mount home shelf above normal `ProductGrid`; mount similar shelf below product content. Reuse `ProductCard`/`ProductGrid` rendering rather than duplicate product markup. A module-level/session key `viewed:{productId}` or request guard prevents StrictMode duplication for the current page session.

- [ ] **Step 3: Test/build and commit**

```powershell
npm --prefix frontend test -- --run src/features/recommendations/RecommendationShelf.test.tsx src/features/products/ProductBrowse.test.tsx src/features/products/ProductDetail.test.tsx
npm --prefix frontend run build
git add frontend/src/features/recommendations frontend/src/features/products/ProductBrowsePage.tsx frontend/src/features/products/ProductBrowse.test.tsx frontend/src/features/products/ProductDetailPage.tsx frontend/src/features/products/ProductDetail.test.tsx
git commit -m "feat(frontend): show and feed product recommendations"
```

---

### Task 9: Admin recommendations diagnostics

**Files:**
- Create: `frontend/src/features/admin/AdminRecommendationsPage.tsx`
- Create: `frontend/src/features/admin/AdminRecommendationsPage.test.tsx`
- Create: `frontend/src/features/admin/AdminRecommendationsPage.css`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/App.test.tsx`
- Modify: `frontend/src/features/admin/AdminLayout.tsx`

**Interfaces:**
- Produces: `/admin/recommendations` route with last status, run history, sample scope/results, and manual rerun.

- [ ] **Step 1: Write failing diagnostics tests**

```typescript
it('filters sample results by scope and triggers a global run', async () => {
  renderAdminRecommendations()
  await user.selectOptions(await screen.findByLabelText('Phạm vi kết quả'), 'SimilarProduct')
  expect(recommendationApi.getAdminResults).toHaveBeenLastCalledWith(
    expect.objectContaining({ scope: 'SimilarProduct' }))

  await user.click(screen.getByRole('button', { name: 'Chạy lại gợi ý' }))
  expect(recommendationApi.triggerRecommendations).toHaveBeenCalledTimes(1)
})
```

Add loading/empty/error, 409-running, terminal status polling, and unmount-abort tests.

- [ ] **Step 2: Implement route/nav/page**

Use existing Admin layout/table patterns. Show algorithm version, score/reason, audience/source identifiers safe for Admin, generation/expiry, and bounded sample limit. Poll accepted status URL until terminal with cleanup on unmount.

- [ ] **Step 3: Test/build and commit**

```powershell
npm --prefix frontend test -- --run src/features/admin/AdminRecommendationsPage.test.tsx src/App.test.tsx
npm --prefix frontend run build
git add frontend/src/features/admin/AdminRecommendationsPage.tsx frontend/src/features/admin/AdminRecommendationsPage.test.tsx frontend/src/features/admin/AdminRecommendationsPage.css frontend/src/features/admin/AdminLayout.tsx frontend/src/App.tsx frontend/src/App.test.tsx
git commit -m "feat(frontend): add recommendation job diagnostics"
```
