# Intelligence Features Release Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hoàn thiện demo data, schema/OpenAPI contract, E2E, performance smoke và tài liệu cho reviews, inventory intelligence và recommendations sau khi bốn plan feature đã merge.

**Architecture:** Raw demo events được seed idempotent; materialized results vẫn do production handlers tạo qua manual/scheduled jobs. Release gates dùng real MySQL 8.4 cho schema/concurrency, Playwright cho ba flow xuyên capability, và tracked OpenAPI/architecture docs làm traceability source.

**Tech Stack:** .NET 10, EF Core 10.0.9, MySQL 8.4, xUnit, Docker Compose, React 19.2.8, Playwright 1.62.1, PowerShell 7.

**Spec:** `docs/superpowers/specs/2026-09-03-reviews-inventory-intelligence-design.md`

## Global Constraints

- Chỉ chạy plan này sau khi background foundation, reviews, inventory intelligence, và recommendations đã hoàn tất.
- Seed phải idempotent và không ghi materialized results giả.
- Schema assertion phải đếm đúng 24 physical tables trên MySQL.
- OpenAPI tracked JSON phải DeepEquals runtime Development document.
- E2E base URLs/artifact directory lấy từ environment, không chứa đường dẫn máy cá nhân.
- Performance smoke báo số đo và fail khi vượt target trên dataset chuẩn; không chạy trong test suite mặc định.
- Chỉ đổi trạng thái FR/DFD/sitemap từ Planned sang Implemented sau khi full verification xanh.
- Mỗi task theo RED -> GREEN -> REFACTOR -> COMMIT; không stage thay đổi ngoài scope.

---

## File Structure

- Modify `backend/src/OnlineSupermarket.Infrastructure/Persistence/DataSeeder.cs`: raw sale/view/review demo history.
- Modify `backend/tests/OnlineSupermarket.Infrastructure.Tests/DataSeederTests.cs`: idempotency and consistency.
- Extend MySQL schema tests to assert exact 24-table set.
- Modify `backend/tests/OnlineSupermarket.Api.Tests/OpenApiContractTests.cs`: new operations/security/status codes.
- Regenerate `docs/api/openapi.json` from running Development API.
- Create `frontend/src/test/run-intelligence-gui-tests.mjs`: portable Playwright E2E.
- Create `scripts/run-intelligence-performance-smoke.ps1`: explicit performance run.
- Update README, requirements, ERD, DFD, sitemap, and progress/test-flow documents.

---

### Task 1: Deterministic demo history and idempotency

**Files:**
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/DataSeeder.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Program.cs`
- Modify: `backend/tests/OnlineSupermarket.Infrastructure.Tests/DataSeederTests.cs`

**Interfaces:**
- Produces: completed-order review eligibility for `user1@test.com`.
- Produces: 28 days of deterministic `Sale` ledger rows for selected branch inventories.
- Produces: deterministic anonymous/authenticated product views across categories/brands.
- Does not create `demand_forecasts`, `stock_alerts`, `recommendation_results`, or successful job rows directly.
- Produces: `DataSeeder.SeedIntelligenceDemoDataAsync(AppDbContext, IInventoryMutationService)` called only by Development startup after `SeedAllAsync`.

- [ ] **Step 1: Write failing seed consistency tests**

```csharp
[Fact]
public async Task SeedAll_CreatesRawIntelligenceHistoryButNoMaterializedResults()
{
    await DataSeeder.SeedAllAsync(_context, _hasher);
    await DataSeeder.SeedIntelligenceDemoDataAsync(_context, _inventoryMutationService);

    Assert.NotEmpty(await _context.InventoryTransactions
        .Where(x => x.TransactionType == InventoryTransactionType.Sale).ToListAsync());
    Assert.NotEmpty(await _context.ProductViewEvents.ToListAsync());
    Assert.Empty(await _context.DemandForecasts.ToListAsync());
    Assert.Empty(await _context.StockAlerts.ToListAsync());
    Assert.Empty(await _context.RecommendationResults.ToListAsync());
}

[Fact]
public async Task SeedAll_RunningTwice_DoesNotDuplicateRawEventsOrStockDeltas()
{
    await DataSeeder.SeedAllAsync(_context, _hasher);
    await DataSeeder.SeedIntelligenceDemoDataAsync(_context, _inventoryMutationService);
    var first = await SnapshotIntelligenceSeedAsync();
    await DataSeeder.SeedAllAsync(_context, _hasher);
    await DataSeeder.SeedIntelligenceDemoDataAsync(_context, _inventoryMutationService);
    var second = await SnapshotIntelligenceSeedAsync();
    Assert.Equal(first, second);
}
```

- [ ] **Step 2: Implement seed methods in dependency order**

Keep the existing `SeedAllAsync` signature stable. In Development `Program.cs`,
resolve the scoped inventory mutation service and call this after `SeedAllAsync`:

```csharp
await DataSeeder.SeedIntelligenceDemoDataAsync(
    context,
    scope.ServiceProvider.GetRequiredService<IInventoryMutationService>());
```

Inside the new method, use fixed operation keys and stable lookups by
SKU/email/order item. For historical completed orders, open `Serializable`,
apply consistent reserve then sale commands through `IInventoryMutationService`,
and commit inventory plus ledger together. Pass explicit historical UTC
timestamps into seeded transactions. Sort inventory IDs before mutation. Seed at
least one eligible unreviewed completed item and one existing review.

- [ ] **Step 3: Run seed tests and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~DataSeederTests"
git add backend/src/OnlineSupermarket.Infrastructure/Persistence/DataSeeder.cs backend/src/OnlineSupermarket.Api/Program.cs backend/tests/OnlineSupermarket.Infrastructure.Tests/DataSeederTests.cs
git commit -m "feat(seed): add review and intelligence demo history"
```

---

### Task 2: Exact MySQL schema gate

**Files:**
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlSchemaTests.cs`

**Interfaces:**
- Consumes: shared `MySqlFixture` and every migration.
- Produces: exact unordered table-set assertion and migration idempotency test.

- [ ] **Step 1: Write exact 24-table assertion**

```csharp
[Fact]
public async Task LatestMigration_CreatesExactlyTwentyFourTables()
{
    var expected = new[]
    {
        "addresses", "background_job_runs", "branch_inventories", "branches", "brands",
        "cart_items", "carts", "categories", "demand_forecasts", "inventory_transactions",
        "order_items", "order_status_histories", "orders", "password_reset_tokens",
        "payment_callbacks", "payments", "product_view_events", "products", "promotions",
        "recommendation_results", "refresh_tokens", "reviews", "stock_alerts", "users",
    };

    var actual = await ReadBaseTablesFromInformationSchemaAsync();
    Assert.Equal(expected.Order(), actual.Order());
}
```

- [ ] **Step 2: Add idempotent migration application test**

Create an empty test database, call `Database.MigrateAsync()` twice, and assert the same table set and a single row per migration in `__EFMigrationsHistory`.

- [ ] **Step 3: Run MySQL gate and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~MySqlSchemaTests"
git add backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlSchemaTests.cs
git commit -m "test(schema): enforce exact 24 table model"
```

---

### Task 3: OpenAPI operation coverage and tracked document

**Files:**
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/OpenApiContractTests.cs`
- Modify: `docs/api/openapi.json`

**Interfaces:**
- Produces: operation IDs and documented 202/400/401/403/404/409 responses for all new endpoints.
- Preserves: OpenAPI available only in Development.

- [ ] **Step 1: Extend failing operation contract test**

```csharp
[Theory]
[InlineData("/api/reviews", "CreateReview")]
[InlineData("/api/products/{productId}/reviews", "GetProductReviews")]
[InlineData("/api/admin/forecast", "AdminGetForecast")]
[InlineData("/api/admin/stock-alerts", "AdminGetStockAlerts")]
[InlineData("/api/recommendations", "GetRecommendations")]
[InlineData("/api/admin/jobs/recommendations/runs", "AdminRunRecommendations")]
public async Task OpenApi_ContainsIntelligenceOperation(string path, string operationId)
{
    using var client = _factory.CreateClient();
    var document = await client.GetStringAsync("/openapi/v1.json");
    Assert.Contains(path, document, StringComparison.Ordinal);
    Assert.Contains(operationId, document, StringComparison.Ordinal);
}
```

Include every route listed in spec §6 and verify bearer security on customer/Admin mutations while public reads remain callable without authorization.

- [ ] **Step 2: Run contract test to prove tracked JSON is stale**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~OpenApiContractTests"
```

Expected: operation presence passes against runtime document; tracked-document equality fails.

- [ ] **Step 3: Regenerate from the Development API**

```powershell
docker compose up -d --build mysql api
curl.exe --fail --silent --show-error http://localhost:8080/openapi/v1.json --output docs/api/openapi.json
```

- [ ] **Step 4: Verify equality and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~OpenApiContractTests"
git add backend/tests/OnlineSupermarket.Api.Tests/OpenApiContractTests.cs docs/api/openapi.json
git commit -m "docs(api): publish review and intelligence contracts"
```

---

### Task 4: Portable Playwright cross-capability flows

**Files:**
- Create: `frontend/src/test/run-intelligence-gui-tests.mjs`

**Interfaces:**
- Consumes environment: `E2E_BASE_URL`, `E2E_API_URL`, `E2E_ARTIFACT_DIR`.
- Produces JSON report and screenshots for three approved E2E flows.

- [ ] **Step 1: Add portable configuration and preflight**

```javascript
const BASE_URL = process.env.E2E_BASE_URL ?? 'http://localhost:5173'
const API_URL = process.env.E2E_API_URL ?? 'http://localhost:8080/api'
const ARTIFACT_DIR = process.env.E2E_ARTIFACT_DIR ?? 'artifacts/intelligence-e2e'

const health = await fetch(`${API_URL}/health`)
if (!health.ok) throw new Error(`API preflight failed: ${health.status}`)
```

Use `path.resolve(ARTIFACT_DIR)` and create only that directory. Never reuse the machine-specific artifact constant from the older checkout runner.

- [ ] **Step 2: Implement Completed order -> review flow**

Login as seeded customer, open completed order, click its review action, submit rating/comment on product detail, assert aggregate/list update, then edit and assert one review row remains.

```javascript
await page.getByRole('link', { name: /Viết đánh giá/ }).click()
await page.getByLabel('5 sao').check()
await page.getByLabel('Nhận xét').fill('Đánh giá E2E')
await page.getByRole('button', { name: 'Gửi đánh giá' }).click()
await expect(page.getByText('Đánh giá E2E')).toBeVisible()
```

- [ ] **Step 3: Implement inventory -> forecast -> alert flow**

Login as Admin, lower one inventory quantity through existing modal, trigger branch forecast, poll job status until `Succeeded`, open `/admin/forecast`, assert 14-day row, then verify the matching alert and transaction history on `/admin/inventory`.

- [ ] **Step 4: Implement anonymous -> login -> personalized flow**

Clear storage, view products from one category, capture anonymous GUID, login, assert merge endpoint succeeds, trigger recommendation job as Admin through API helper, return as customer, and assert homepage `sourceScope`-backed shelf contains a category-relevant product.

- [ ] **Step 5: Run and commit**

```powershell
docker compose up -d --build
node frontend/src/test/run-intelligence-gui-tests.mjs
git add frontend/src/test/run-intelligence-gui-tests.mjs
git commit -m "test(e2e): cover review forecast and recommendation flows"
```

Expected: report contains 3/3 passed; screenshots are artifacts and are not committed unless repository policy explicitly tracks them.

---

### Task 5: Explicit performance smoke runner

**Files:**
- Create: `scripts/run-intelligence-performance-smoke.ps1`
- Modify: `README.md`

**Interfaces:**
- Consumes running Docker Compose stack and deterministic seed/load helpers.
- Checks: review GET <200 ms at 1,000 rows; review POST <100 ms; recommendation GET <300 ms; 100-branch forecast <5 minutes.

- [ ] **Step 1: Implement measured request helper**

```powershell
function Measure-ApiRequest {
    param([string]$Name, [scriptblock]$Request, [double]$MaximumMilliseconds)
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    & $Request | Out-Null
    $watch.Stop()
    [pscustomobject]@{
        Name = $Name
        ElapsedMilliseconds = $watch.Elapsed.TotalMilliseconds
        TargetMilliseconds = $MaximumMilliseconds
        Passed = $watch.Elapsed.TotalMilliseconds -lt $MaximumMilliseconds
    }
}
```

Warm each endpoint once before measuring, print environment/dataset size, collect results, emit JSON under `artifacts/performance`, and exit 1 if any `Passed` is false. Run this script explicitly; do not attach it to default `dotnet test`.

- [ ] **Step 2: Run against standardized local stack**

```powershell
docker compose up -d --build
pwsh scripts/run-intelligence-performance-smoke.ps1
```

- [ ] **Step 3: Document command and commit**

```powershell
git add scripts/run-intelligence-performance-smoke.ps1 README.md
git commit -m "test(perf): add intelligence performance smoke checks"
```

---

### Task 6: Synchronize architecture and requirement documentation

**Files:**
- Modify: `README.md`
- Modify: `docs/requirements/functional-requirements.md`
- Modify: `docs/architecture/erd.md`
- Modify: `docs/architecture/dfd.md`
- Modify: `docs/architecture/sitemap.md`
- Modify: `docs/project-spec.html`
- Modify: `docs/project-progress-4-members.html`
- Modify: `docs/progress-member-1-3.html`
- Modify: `docs/all-test-flows.html`

**Interfaces:**
- Produces: consistent 24-table count, endpoint/route inventory, P11/P13/P14 data flows, and implementation evidence.

- [ ] **Step 1: Update sources of truth from implementation**

In ERD, copy table names/columns/indexes/FKs from the final EF snapshot. In sitemap, copy routes from `frontend/src/App.tsx`. In DFD, move P11/P13/P14 from planned to implemented and diagram event/job/result flows. In requirements, mark FR-113/208/209 implemented only with test evidence.

- [ ] **Step 2: Run consistency searches**

```powershell
rg -n "22 bảng|25 bảng|UserProductAffinities|user_product_affinities|apiapi|FR-113|FR-208|FR-209" README.md docs
rg -n "reviews|inventory_transactions|product_view_events|recommendation_results|demand_forecasts|stock_alerts|background_job_runs" README.md docs/architecture docs/requirements
```

Expected: no stale 22/25-table claim, no affinity table claim, no `/apiapi/`; every new store appears in ERD/DFD/README.

- [ ] **Step 3: Run doc/API checks and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~OpenApiContractTests|FullyQualifiedName~ModelConfigurationTests"
node --test frontend/src/test/admin-task-board.node-test.mjs
git add README.md docs/requirements/functional-requirements.md docs/architecture/erd.md docs/architecture/dfd.md docs/architecture/sitemap.md docs/project-spec.html docs/project-progress-4-members.html docs/progress-member-1-3.html docs/all-test-flows.html
git commit -m "docs: complete review and intelligence traceability"
```

---

### Task 7: Final regression gate

**Files:**
- Verify only; fix failures in the owning task's files and rerun its focused test before repeating this gate.

**Interfaces:**
- Consumes: the merged deliverables and verification commands from Tasks 1–6 plus all four capability plans linked by the roadmap.
- Produces: a release decision backed by backend, frontend, real-MySQL, E2E, performance, schema, OpenAPI, and working-tree evidence; it creates no new production interface.

- [ ] **Step 1: Backend full suite**

```powershell
dotnet test OnlineSupermarket.slnx --no-restore
```

- [ ] **Step 2: Frontend full suite and build**

```powershell
npm --prefix frontend test -- --run
npm --prefix frontend run build
```

- [ ] **Step 3: Real MySQL gates**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~MySql"
```

- [ ] **Step 4: Full-stack E2E and performance**

```powershell
docker compose up -d --build
node frontend/src/test/run-intelligence-gui-tests.mjs
pwsh scripts/run-intelligence-performance-smoke.ps1
```

- [ ] **Step 5: Review working tree and commit only verified fixes**

```powershell
git status --short
git diff --check
git log -10 --oneline
```

Expected: every command passes; schema is 24 tables; E2E is 3/3; tracked OpenAPI matches runtime; no unrelated file is staged.
