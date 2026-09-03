# Reviews, Inventory Intelligence, and Recommendations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Điều phối việc triển khai end-to-end reviews, inventory ledger/forecast/alerts và recommendations trên nền job bền vững dùng chung.

**Architecture:** Phạm vi được chia thành bốn plan triển khai độc lập theo thứ tự phụ thuộc, sau đó một plan release hợp nhất E2E, performance smoke và tài liệu. Mỗi plan tạo phần mềm chạy được, có migration/test riêng; không tạo một migration khổng lồ hoặc một endpoint file chứa nhiều miền.

**Tech Stack:** .NET 10, C# 14, ASP.NET Core Minimal API, EF Core 10.0.9, MySQL 8.4, xUnit 2.9.3, Testcontainers.MySql 4.14.0, React 19.2.8, TypeScript 5.9.3, Vitest 4.1.10, React Testing Library 16.3.2, Playwright 1.62.1.

**Spec:** `docs/superpowers/specs/2026-09-03-reviews-inventory-intelligence-design.md`

## Global Constraints

- Baseline schema là 17 bảng; kết thúc toàn bộ roadmap phải đúng 24 bảng.
- Bảy bảng mới là `reviews`, `inventory_transactions`, `product_view_events`, `recommendation_results`, `demand_forecasts`, `stock_alerts`, `background_job_runs`.
- Không tạo `roles`, `user_roles`, `promotion_usages` hoặc `user_product_affinities`.
- Domain không phụ thuộc ASP.NET Core hoặc EF Core.
- Mọi timestamp lưu UTC `datetime(6)`; `Guid` dùng convention `char(36)` hiện có.
- Inventory mutation và ledger row phải commit/rollback cùng nhau ở isolation `Serializable`; khóa batch theo `BranchInventory.Id` tăng dần.
- Forecast chỉ hỗ trợ horizon 7 và 14 ngày; recommendation là content-based, không dùng ML.NET hoặc dịch vụ AI ngoài.
- Background job là safe-to-retry theo `jobRunId`, không tuyên bố exactly-once; inventory dùng `operation_key` để chống áp delta lặp.
- API lỗi dùng Problem Details và các mã 400/401/403/404/409 đã duyệt.
- Mỗi task theo RED -> GREEN -> REFACTOR -> COMMIT; chỉ stage file thuộc task.

---

## Plan Set và thứ tự thực hiện

### Phase 1: Shared job foundation

Plan: `docs/superpowers/plans/2026-09-03-background-job-foundation.md`

Deliverable: `background_job_runs`, durable coordinator, channel worker, lease/recovery, Admin run-history API và MySQL concurrency fixture.

Gate:

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore
```

### Phase 2A: Verified reviews

Plan: `docs/superpowers/plans/2026-09-03-verified-reviews.md`

Consumes Phase 1 only for shared conventions. Deliverable: `reviews`, verified-purchase APIs, order eligibility DTOs và customer UI.

### Phase 2B: Inventory intelligence

Plan: `docs/superpowers/plans/2026-09-03-inventory-forecast-alerts.md`

Consumes Phase 1. Deliverable: immutable ledger, atomic reserve/release/sale/manual adjustment, `demand_forecasts`, `stock_alerts`, forecast worker/API và Admin UI.

### Phase 2C: Recommendations

Plan: `docs/superpowers/plans/2026-09-03-product-recommendations.md`

Consumes Phase 1; may run in parallel with 2A after the shared migration is merged, but must not run concurrently with 2B if both branches edit `AppDbContext`, `DependencyInjection`, `Program`, `App`, or the EF snapshot. Deliverable: view capture/session merge, materialized recommendations, customer shelves và Admin diagnostics.

### Phase 3: Release validation and documentation

Plan: `docs/superpowers/plans/2026-09-03-intelligence-release-validation.md`

Consumes all previous phases. Deliverable: deterministic demo seed, portable Playwright E2E, performance smoke output, 24-table assertion, OpenAPI regeneration và synchronized README/ERD/DFD/sitemap/requirements.

---

## Integration order for shared files

Apply changes to these collision-prone files in this order:

1. `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`: JobRun -> Review -> Inventory -> Recommendation DbSets.
2. `backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs`: job coordinator/worker -> inventory services/forecast handler -> recommendation handler.
3. `backend/src/OnlineSupermarket.Api/Program.cs`: job status -> review -> inventory intelligence -> recommendation endpoint maps.
4. `frontend/src/App.tsx`: forecast and recommendation admin routes after feature components exist.
5. EF migrations: preserve timestamp order and regenerate snapshot after each capability migration.

Do not hand-edit an already generated migration from another phase to combine schemas. Rebase on the latest migration, generate the next migration, inspect SQL/model snapshot, then run the MySQL migration test.

## Final verification gate

- [ ] Run all backend tests.

```powershell
dotnet test OnlineSupermarket.slnx --no-restore
```

- [ ] Run all frontend tests and production build.

```powershell
npm --prefix frontend test -- --run
npm --prefix frontend run build
```

- [ ] Start the full stack and run Playwright flows.

```powershell
docker compose up -d --build
node frontend/src/test/run-intelligence-gui-tests.mjs
```

- [ ] Verify generated schema and OpenAPI are synchronized.

```powershell
dotnet ef migrations script --idempotent --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~MySqlSchema"
```

Expected: 24 physical tables, all tests pass, frontend build succeeds, E2E report contains three passing cross-capability flows.
