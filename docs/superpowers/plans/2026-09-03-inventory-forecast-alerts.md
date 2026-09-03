# Inventory Ledger, Demand Forecast, and Stock Alerts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ghi ledger nguyên tử cho mọi inventory mutation, chuyển reservation thành sale đúng một lần khi order hoàn tất, rồi materialize forecast 7/14 ngày và stock alerts cho Admin.

**Architecture:** `InventoryMutationService` là đường duy nhất cho on-hand/reserved mutations và yêu cầu caller đang ở trong transaction `Serializable`; service load inventory theo `Id` tăng dần và thêm immutable ledger rows. `ForecastJobHandler` đọc `Sale` transactions, tính moving average 28 ngày, ghi hai horizon và alert batch theo `jobRunId`; API/UI chỉ đọc latest successful materialization.

**Tech Stack:** .NET 10, C# 14, EF Core 10.0.9, MySQL 8.4, ASP.NET Core Minimal API, xUnit, React 19.2.8, TypeScript 5.9.3, Vitest, React Testing Library.

**Spec:** `docs/superpowers/specs/2026-09-03-reviews-inventory-intelligence-design.md`

## Global Constraints

- Plan nền `2026-09-03-background-job-foundation.md` phải hoàn tất trước Task 6.
- `inventory_transactions` append-only; API không có update/delete.
- `operation_key` chống apply lại order reserve/release/sale; unique violation là no-op có kiểm chứng, không apply delta lần hai.
- Mọi batch sort `BranchInventory.Id` tăng dần trước khi query/mutate.
- Mọi endpoint inventory mutation dùng transaction `Serializable`; retry tạo transaction mới.
- Demand chỉ lấy `Sale`; không lấy reservation, release hoặc manual adjustment.
- Forecast observation dùng tối đa 28 complete UTC days; horizon chỉ 7/14.
- Alert dùng 14-day forecast và available quantity snapshot.
- Mỗi task theo RED -> GREEN -> REFACTOR -> COMMIT; không stage thay đổi ngoài scope.

---

## File Structure

- Create `backend/src/OnlineSupermarket.Domain/Inventory/InventoryTransaction.cs` and enums: immutable ledger.
- Modify `backend/src/OnlineSupermarket.Domain/Inventory/BranchInventory.cs`: complete-sale mutation.
- Create `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs`.
- Create `backend/src/OnlineSupermarket.Infrastructure/Inventory/InventoryMutationCommand.cs`.
- Create `backend/src/OnlineSupermarket.Infrastructure/Inventory/InventoryMutationService.cs`.
- Modify checkout/order/branch endpoints to use the service and serializable transaction scope.
- Create `backend/src/OnlineSupermarket.Domain/Intelligence/DemandForecast.cs`, `StockAlert.cs`, and calculation enums.
- Create `backend/src/OnlineSupermarket.Infrastructure/Intelligence/DemandForecastCalculator.cs`.
- Create EF configurations and migration `AddInventoryIntelligence`.
- Create `ForecastJobHandler` and `ForecastRecurringSchedule`.
- Create Admin inventory intelligence contracts/endpoints.
- Create `frontend/src/api/inventoryIntelligenceApi.ts`.
- Create `AdminInventoryTransactions.tsx` and enhance `AdminInventoryPage.tsx`.
- Create `AdminForecastPage.tsx`, tests, route, nav, and scoped CSS.

---

### Task 1: Inventory transaction domain and sale mutation

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Inventory/InventoryTransaction.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Inventory/InventoryTransactionType.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Inventory/InventoryReferenceType.cs`
- Modify: `backend/src/OnlineSupermarket.Domain/Inventory/BranchInventory.cs`
- Modify: `backend/tests/OnlineSupermarket.Domain.Tests/Inventory/BranchInventoryTests.cs`
- Create: `backend/tests/OnlineSupermarket.Domain.Tests/Inventory/InventoryTransactionTests.cs`

**Interfaces:**
- Produces: `BranchInventory.CompleteSale(int quantity): void`.
- Produces: `InventoryTransaction.Create(..., DateTime createdAtUtc)` with exact post-mutation snapshots.
- Produces: enum values `Reserve`, `Release`, `Sale`, `ManualAdjustment` and `Order`, `AdminAdjustment`, `System`.

- [ ] **Step 1: Write failing sale and ledger tests**

```csharp
[Fact]
public void CompleteSale_DecrementsOnHandAndReservedTogether()
{
    var inventory = BranchInventory.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, 100, 20);
    inventory.Reserve(10);

    inventory.CompleteSale(10);

    Assert.Equal(90, inventory.QuantityOnHand);
    Assert.Equal(0, inventory.ReservedQuantity);
    Assert.Equal(90, inventory.AvailableQuantity);
}

[Fact]
public void Transaction_CapturesSignedDeltasAndAfterState()
{
    var transaction = InventoryTransaction.Create(
        Guid.NewGuid(), InventoryTransactionType.Sale,
        -10, -10, 90, 0, InventoryReferenceType.Order,
        Guid.NewGuid(), "order:o:inventory:i:sale", null, null, DateTime.UtcNow);

    Assert.Equal(-10, transaction.QuantityOnHandDelta);
    Assert.Equal(-10, transaction.ReservedQuantityDelta);
    Assert.Equal(90, transaction.QuantityOnHandAfter);
}
```

Add tests that sale quantity must be positive, cannot exceed both on-hand and reserved, snapshots cannot be negative, and `operationKey` is trimmed.

- [ ] **Step 2: Run RED**

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~BranchInventoryTests|FullyQualifiedName~InventoryTransactionTests"
```

- [ ] **Step 3: Implement domain behavior**

```csharp
public void CompleteSale(int quantity)
{
    if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
    if (quantity > ReservedQuantity || quantity > QuantityOnHand)
        throw new InvalidOperationException("Sale exceeds reserved inventory.");
    QuantityOnHand -= quantity;
    ReservedQuantity -= quantity;
    UpdatedAtUtc = DateTime.UtcNow;
}
```

`InventoryTransaction` has a private EF constructor, only `Create`, no mutation methods, validates the two after-state snapshots, and requires an explicit UTC `createdAtUtc`. Runtime services pass `TimeProvider.GetUtcNow()`; deterministic seed data may pass historical UTC timestamps.

- [ ] **Step 4: Run GREEN and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~BranchInventoryTests|FullyQualifiedName~InventoryTransactionTests"
git add backend/src/OnlineSupermarket.Domain/Inventory backend/tests/OnlineSupermarket.Domain.Tests/Inventory
git commit -m "feat(inventory): add immutable stock transaction model"
```

---

### Task 2: Persist inventory ledger

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Generate: EF migration named `AddInventoryTransactions` and its designer.
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs`

**Interfaces:**
- Produces: `AppDbContext.InventoryTransactions`.
- Produces: unique nullable `operation_key`, index `(branch_inventory_id, created_at_utc)`, and Restrict FKs.

- [ ] **Step 1: Write failing EF model test**

```csharp
[Fact]
public void InventoryTransaction_HasLedgerIndexesAndNoCascadeDelete()
{
    using var context = CreateContext();
    var entity = context.Model.FindEntityType(typeof(InventoryTransaction))!;

    Assert.Equal("inventory_transactions", entity.GetTableName());
    Assert.True(entity.GetIndexes().Single(i =>
        i.Properties.Single().Name == nameof(InventoryTransaction.OperationKey)).IsUnique);
    Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
}
```

- [ ] **Step 2: Add config/DbSet and generate migration**

```csharp
builder.ToTable("inventory_transactions");
builder.Property(x => x.TransactionType).HasColumnName("transaction_type").HasConversion<string>().HasMaxLength(30);
builder.Property(x => x.ReferenceType).HasColumnName("reference_type").HasConversion<string>().HasMaxLength(30);
builder.Property(x => x.OperationKey).HasColumnName("operation_key").HasMaxLength(180);
builder.HasIndex(x => x.OperationKey).IsUnique().HasDatabaseName("ix_inventory_transactions_operation_key");
builder.HasIndex(x => new { x.BranchInventoryId, x.CreatedAtUtc })
    .HasDatabaseName("ix_inventory_transactions_inventory_created");
```

```powershell
dotnet ef migrations add AddInventoryTransactions --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
```

- [ ] **Step 3: Verify mapping and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ModelConfigurationTests"
rg -n "inventory_transactions|operation_key" backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations
git add backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddInventoryTransactions.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddInventoryTransactions.Designer.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs
git commit -m "feat(inventory): persist stock transaction ledger"
```

---

### Task 3: Central inventory mutation service

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Inventory/InventoryMutationCommand.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Inventory/IInventoryMutationService.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Inventory/InventoryMutationService.cs`
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Inventory/InventoryMutationServiceTests.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Produces: command factories `Reserve`, `Release`, `Sale`, `ManualAdjustment`.
- Produces: `ApplyBatchAsync(IReadOnlyCollection<InventoryMutationCommand>, CancellationToken): Task`.
- Requires: `AppDbContext.Database.CurrentTransaction` is non-null.

- [ ] **Step 1: Write failing sorted/atomic/idempotent tests**

```csharp
[Fact]
public async Task ApplyBatchAsync_LoadsByAscendingInventoryId_AndAddsLedgerRows()
{
    await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    var commands = new[]
    {
        InventoryMutationCommand.Reserve(_highId, 2, _orderId, _userId),
        InventoryMutationCommand.Reserve(_lowId, 1, _orderId, _userId),
    };

    await _service.ApplyBatchAsync(commands, CancellationToken.None);
    await _db.SaveChangesAsync();

    var ledgerOrder = _db.ChangeTracker.Entries<InventoryTransaction>()
        .Select(entry => entry.Entity.BranchInventoryId)
        .ToArray();
    Assert.Equal(new[] { _lowId, _highId }, ledgerOrder);
    Assert.Equal(2, await _db.InventoryTransactions.CountAsync());
}

[Fact]
public async Task ApplyBatchAsync_WithoutTransaction_Throws()
{
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _service.ApplyBatchAsync(
            new[] { InventoryMutationCommand.Reserve(_lowId, 1, _orderId, _userId) },
            CancellationToken.None));
}
```

The assertion observes EF's tracked insert order; no test-only property is added to
the production service.

- [ ] **Step 2: Implement exact command shape**

```csharp
public sealed record InventoryMutationCommand(
    Guid BranchInventoryId,
    InventoryTransactionType TransactionType,
    int Quantity,
    int? AbsoluteQuantityOnHand,
    InventoryReferenceType ReferenceType,
    Guid? ReferenceId,
    string? OperationKey,
    Guid? ActorUserId,
    string? Note,
    DateTime? OccurredAtUtc);
```

Load all distinct IDs with `OrderBy(x => x.Id)`, fail if any are missing,
preflight every command, then mutate and add ledger rows. Use
`OccurredAtUtc ?? _timeProvider.GetUtcNow().UtcDateTime` for the immutable
transaction timestamp. For an existing `operation_key`, verify the stored
type/reference/inventory match and skip that command; a mismatched replay throws
conflict.

- [ ] **Step 3: Run tests, register scoped service, and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~InventoryMutationServiceTests"
git add backend/src/OnlineSupermarket.Infrastructure/Inventory backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs backend/tests/OnlineSupermarket.Infrastructure.Tests/Inventory
git commit -m "feat(inventory): centralize atomic inventory mutations"
```

---

### Task 4: Route every existing mutation through the ledger

**Files:**
- Modify: `backend/src/OnlineSupermarket.Api/Endpoints/CheckoutEndpoints.cs:105`
- Modify: `backend/src/OnlineSupermarket.Api/Endpoints/CheckoutEndpoints.cs:306`
- Modify: `backend/src/OnlineSupermarket.Api/Endpoints/OrderEndpoints.cs:159`
- Modify: `backend/src/OnlineSupermarket.Api/Endpoints/BranchEndpoints.cs:119`
- Modify: `backend/src/OnlineSupermarket.Api/Contracts/Branch/BranchContracts.cs`
- Create: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/InventoryMutationEndpointTests.cs`
- Add: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlInventoryTransactionTests.cs`

**Interfaces:**
- Consumes: `IInventoryMutationService.ApplyBatchAsync`.
- Produces: reserve/release/sale/manual-adjustment rows in the same transaction as source state.
- Changes: `BranchProductInventoryDto` adds `InventoryId` so Admin can request the exact ledger.

- [ ] **Step 1: Write failing integration assertions for all four mutation types**

```csharp
[Fact]
public async Task CompletingOrder_ConvertsReservationToSaleExactlyOnce()
{
    var fixture = await SeedDeliveredReservedOrderAsync(quantity: 3);
    using var admin = await CreateAdminClientAsync();

    var first = await admin.PutAsJsonAsync($"/api/admin/orders/{fixture.OrderId}/status",
        new { status = "Completed" });
    var second = await admin.PutAsJsonAsync($"/api/admin/orders/{fixture.OrderId}/status",
        new { status = "Completed" });

    Assert.Equal(HttpStatusCode.OK, first.StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    var sale = Assert.Single(await LoadTransactionsAsync(InventoryTransactionType.Sale));
    Assert.Equal(-3, sale.QuantityOnHandDelta);
    Assert.Equal(-3, sale.ReservedQuantityDelta);
}
```

Add assertions for checkout `Reserve`, cancellation/payment failure `Release`, and admin quantity edit `ManualAdjustment` with actor/reason.

- [ ] **Step 2: Refactor transaction scopes and lock order**

For checkout, start a fresh serializable transaction inside each retry attempt. Build the order in memory to obtain `order.Id`, then call reserve commands with deterministic operation keys before the shared `SaveChangesAsync`. Query inventories by `Id` order, not `(BranchId, ProductId)`.

For order status, payment callback, and admin inventory update, explicitly begin `Serializable`, call the mutation service, update order/payment/price/reorder state, save once, and commit. When transitioning `Delivered -> Completed`, use `Sale`; cancellation and payment failure use `Release`.

Add `InventoryId` as the first field of `BranchProductInventoryDto`, project
`bi.Id` in `BranchEndpoints`, and update backend/frontend fixtures that construct
this DTO.

- [ ] **Step 3: Run endpoint tests**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~Checkout|FullyQualifiedName~OrderEndpoints|FullyQualifiedName~AdminBranchEndpoints"
```

- [ ] **Step 4: Prove rollback and concurrency on MySQL**

```csharp
[Fact]
public async Task LedgerInsertFailure_RollsBackInventoryMutation()
{
    await SeedExistingOperationKeyAsync("duplicate-key");
    var before = await LoadInventoryAsync();

    await Assert.ThrowsAsync<DbUpdateException>(() =>
        ApplyAndCommitReserveAsync(before.Id, "duplicate-key"));

    var after = await LoadInventoryAsync();
    Assert.Equal(before.ReservedQuantity, after.ReservedQuantity);
}
```

Run with two real connections against the same inventory and assert no negative availability and one row per operation key.

- [ ] **Step 5: Commit endpoint integration**

```powershell
git add backend/src/OnlineSupermarket.Api/Endpoints/CheckoutEndpoints.cs backend/src/OnlineSupermarket.Api/Endpoints/OrderEndpoints.cs backend/src/OnlineSupermarket.Api/Endpoints/BranchEndpoints.cs backend/src/OnlineSupermarket.Api/Contracts/Branch/BranchContracts.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/InventoryMutationEndpointTests.cs backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlInventoryTransactionTests.cs
git commit -m "feat(inventory): log all stock mutations atomically"
```

---

### Task 5: Forecast and alert domain calculations

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/DemandForecast.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/ForecastDataQuality.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/StockAlert.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/StockAlertSeverity.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Intelligence/DemandForecastCalculator.cs`
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Intelligence/DemandForecastCalculatorTests.cs`

**Interfaces:**
- Produces: `Calculate(IReadOnlyDictionary<DateOnly,int> dailySales, DateOnly observationEnd, int horizonDays): ForecastCalculation`.
- Produces: `CreateAlert(DemandForecast forecast, int availableQuantity, int reorderLevel): StockAlert?`.

- [ ] **Step 1: Write failing forecast boundary tests**

```csharp
[Theory]
[InlineData(7, 14.0)]
[InlineData(14, 28.0)]
public void Calculate_UsesDailyAverageAcrossCompleteCalendarDays(int horizon, double expected)
{
    var sales = Enumerable.Range(0, 7).ToDictionary(
        offset => new DateOnly(2026, 9, 2).AddDays(-offset), _ => 2);

    var result = DemandForecastCalculator.Calculate(
        sales, new DateOnly(2026, 9, 2), horizon);

    Assert.Equal((decimal)expected, result.PredictedQuantity);
    Assert.Equal(ForecastDataQuality.Partial, result.DataQuality);
}

[Fact]
public void CreateAlert_WhenProjectedRemainderTouchesReorderLevel_ReturnsLowAlert()
{
    var alert = DemandForecastCalculator.CreateAlert(
        CreateForecast(predicted: 10), availableQuantity: 20, reorderLevel: 10);

    Assert.NotNull(alert);
    Assert.Equal(0, alert!.RecommendedReorderQuantity);
    Assert.Equal(StockAlertSeverity.Low, alert.Severity);
}
```

Add no-history, missing zero-sale calendar days, 28-day cap, invalid horizon, medium boundary, high stockout, and no-alert cases.

- [ ] **Step 2: Implement deterministic algorithm**

Use `DateOnly` for calendar grouping and `decimal` for quantities. `actual_data_days` counts from first sale day through observation end, capped at 28, including zero-sale days. Return zero/Insufficient for no history. Build alerts only from a 14-day forecast.

- [ ] **Step 3: Run tests and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~DemandForecastCalculatorTests"
git add backend/src/OnlineSupermarket.Domain/Intelligence backend/src/OnlineSupermarket.Infrastructure/Intelligence/DemandForecastCalculator.cs backend/tests/OnlineSupermarket.Infrastructure.Tests/Intelligence
git commit -m "feat(forecast): calculate demand and stock alerts"
```

---

### Task 6: Persist forecasts and alerts

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/DemandForecastConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/StockAlertConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Generate: EF migration named `AddDemandForecastsAndStockAlerts` and its designer.
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs`

**Interfaces:**
- Produces: `AppDbContext.DemandForecasts`, `AppDbContext.StockAlerts`.
- Produces: unique run/inventory/horizon and run/inventory indexes from spec.

- [ ] **Step 1: Write failing metadata tests**

```csharp
[Fact]
public void ForecastAndAlert_HaveRunScopedUniqueKeys()
{
    using var context = CreateContext();
    var forecast = context.Model.FindEntityType(typeof(DemandForecast))!;
    var alert = context.Model.FindEntityType(typeof(StockAlert))!;

    Assert.Contains(forecast.GetIndexes(), x => x.IsUnique && x.Properties.Count == 3);
    Assert.Contains(alert.GetIndexes(), x => x.IsUnique && x.Properties.Count == 2);
}
```

- [ ] **Step 2: Map exact columns and generate migration**

```powershell
dotnet ef migrations add AddDemandForecastsAndStockAlerts --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
rg -n "demand_forecasts|stock_alerts|horizon_days|recommended_reorder_quantity" backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations
```

Add check constraints `horizon_days IN (7, 14)` and non-negative predicted/reorder quantities.

- [ ] **Step 3: Run tests and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ModelConfigurationTests"
git add backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/DemandForecastConfiguration.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/StockAlertConfiguration.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddDemandForecastsAndStockAlerts.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddDemandForecastsAndStockAlerts.Designer.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs
git commit -m "feat(forecast): persist forecasts and stock alerts"
```

---

### Task 7: Forecast handler, schedule, and materialized batch

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Intelligence/ForecastJobHandler.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Intelligence/ForecastRecurringSchedule.cs`
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Intelligence/ForecastJobHandlerTests.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Implements: `IBackgroundJobHandler` with `JobName == Forecast`.
- Implements: shared recurring schedule contract, one due job per active branch daily.
- Produces: one atomic batch containing 7-day, 14-day, and zero-or-one alert per inventory.

- [ ] **Step 1: Write failing materialization test**

```csharp
[Fact]
public async Task ExecuteAsync_WritesTwoForecastsAndAlertPerAtRiskInventory()
{
    var inventory = await SeedInventoryWithDailySalesAsync(days: 14, unitsPerDay: 2, available: 20, reorder: 5);

    await _handler.ExecuteAsync(_jobRunId, inventory.BranchId, CancellationToken.None);

    var forecasts = await _db.DemandForecasts.Where(x => x.JobRunId == _jobRunId).ToListAsync();
    Assert.Equal(new[] { 7, 14 }, forecasts.Select(x => x.HorizonDays).Order().ToArray());
    Assert.Single(await _db.StockAlerts.Where(x => x.JobRunId == _jobRunId).ToListAsync());
}
```

Add tests for no-history rows, branch isolation, ignoring non-Sale transactions, rollback on failure, and rerun with a new run ID.

- [ ] **Step 2: Implement handler query and atomic write**

Query the previous 28 complete UTC days once for the branch, group sales by inventory/date in memory, calculate both horizons, build 14-day alerts, and commit all new rows in one transaction. Never delete prior successful runs. `ForecastRecurringSchedule` returns a branch only when UTC time is at or after `ForecastHourUtc` and that branch has no active run or successful run created on the current UTC date.

- [ ] **Step 3: Register handler/schedule and test**

```csharp
services.AddScoped<IBackgroundJobHandler, ForecastJobHandler>();
services.AddScoped<IRecurringJobSchedule, ForecastRecurringSchedule>();
```

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~ForecastJobHandlerTests"
```

- [ ] **Step 4: Commit**

```powershell
git add backend/src/OnlineSupermarket.Infrastructure/Intelligence backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs backend/tests/OnlineSupermarket.Infrastructure.Tests/Intelligence
git commit -m "feat(forecast): materialize scheduled branch forecasts"
```

---

### Task 8: Admin forecast, alert, and ledger APIs

**Files:**
- Create: `backend/src/OnlineSupermarket.Api/Contracts/Inventory/InventoryIntelligenceContracts.cs`
- Create: `backend/src/OnlineSupermarket.Api/Endpoints/InventoryIntelligenceEndpoints.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Program.cs`
- Create: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/InventoryIntelligenceEndpointsTests.cs`

**Interfaces:**
- Produces all four Admin routes from spec §6.2: transactions, forecast, alerts, and forecast trigger.
- Consumes: `IJobRunCoordinator.TryQueueAsync` for trigger.

- [ ] **Step 1: Write failing contract tests**

```csharp
[Theory]
[InlineData(1)]
[InlineData(13)]
[InlineData(30)]
public async Task GetForecast_WithUnsupportedHorizon_ReturnsBadRequest(int horizon)
{
    using var admin = await CreateAdminClientAsync();
    var response = await admin.GetAsync($"/api/admin/forecast?branchId={_branchId}&horizonDays={horizon}");
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task TriggerForecast_WhenAccepted_ReturnsStatusLocation()
{
    using var admin = await CreateAdminClientAsync();
    var response = await admin.PostAsJsonAsync("/api/admin/jobs/forecast/runs", new { branchId = _branchId });

    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    Assert.StartsWith("/api/admin/jobs/runs/", response.Headers.Location!.OriginalString);
}
```

Add 401/403, missing branch 404, active lock 409, latest-success-only, severity filter, transaction pagination, and actor/reference projection tests.

- [ ] **Step 2: Implement DTOs and endpoints**

```csharp
public sealed record ForecastDto(
    Guid Id, Guid BranchInventoryId, Guid ProductId, string ProductName,
    int HorizonDays, decimal PredictedQuantity, int ActualDataDays,
    string DataQuality, DateOnly ForecastStartDate, DateOnly ForecastEndDate,
    DateTime GeneratedAtUtc, Guid JobRunId);

public sealed record StockAlertDto(
    Guid Id, Guid BranchInventoryId, Guid ProductId, string ProductName,
    int AvailableQuantity, int ReorderLevel, decimal PredictedQuantity,
    int RecommendedReorderQuantity, string Severity, DateTime CreatedAtUtc);
```

Resolve latest `Succeeded` run per branch and never return rows from a failed/running batch.

- [ ] **Step 3: Test and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~InventoryIntelligenceEndpointsTests"
git add backend/src/OnlineSupermarket.Api/Contracts/Inventory backend/src/OnlineSupermarket.Api/Endpoints/InventoryIntelligenceEndpoints.cs backend/src/OnlineSupermarket.Api/Program.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/InventoryIntelligenceEndpointsTests.cs
git commit -m "feat(forecast): expose admin inventory intelligence api"
```

---

### Task 9: Admin inventory alerts and ledger UI

**Files:**
- Create: `frontend/src/api/inventoryIntelligenceApi.ts`
- Create: `frontend/src/api/inventoryIntelligenceApi.test.ts`
- Modify: `frontend/src/api/branchApi.ts`
- Modify: `frontend/src/api/branchApi.test.ts`
- Create: `frontend/src/features/admin/AdminInventoryTransactions.tsx`
- Create: `frontend/src/features/admin/AdminInventoryTransactions.test.tsx`
- Modify: `frontend/src/features/admin/AdminInventoryPage.tsx`
- Modify: `frontend/src/features/admin/AdminInventoryPage.test.tsx`
- Modify: `frontend/src/features/admin/AdminInventoryPage.css`

**Interfaces:**
- Produces typed `getStockAlerts` and `getTransactions` clients.
- Produces alert filter/badge and transaction history drawer/dialog from inventory rows.

- [ ] **Step 1: Write failing alert/history UI tests**

```typescript
it('shows materialized alert severity and recommended reorder quantity', async () => {
  branchApi.getBranchInventory.mockResolvedValue(inventoryResponse)
  inventoryIntelligenceApi.getStockAlerts.mockResolvedValue({ data: [highAlert], totalCount: 1 })

  renderAdminInventory()

  expect(await screen.findByText('Nguy cơ hết hàng')).toBeInTheDocument()
  expect(screen.getByText('Đề xuất nhập 15')).toBeInTheDocument()
})

it('opens immutable transaction history for one inventory row', async () => {
  renderAdminInventory()
  await user.click(await screen.findByRole('button', { name: 'Lịch sử kho Sản phẩm A' }))
  expect(await screen.findByRole('dialog', { name: 'Lịch sử giao dịch kho' })).toBeInTheDocument()
})
```

- [ ] **Step 2: Implement API and UI states**

Keep existing computed low-stock badge but label materialized forecast alerts separately. Add severity filter, loading/empty/error/retry states, and accessible dialog focus return.

- [ ] **Step 3: Test/build and commit**

```powershell
npm --prefix frontend test -- --run src/api/inventoryIntelligenceApi.test.ts src/features/admin/AdminInventoryPage.test.tsx src/features/admin/AdminInventoryTransactions.test.tsx
npm --prefix frontend run build
git add frontend/src/api/inventoryIntelligenceApi.ts frontend/src/api/inventoryIntelligenceApi.test.ts frontend/src/api/branchApi.ts frontend/src/api/branchApi.test.ts frontend/src/features/admin/AdminInventoryTransactions.tsx frontend/src/features/admin/AdminInventoryTransactions.test.tsx frontend/src/features/admin/AdminInventoryPage.tsx frontend/src/features/admin/AdminInventoryPage.test.tsx frontend/src/features/admin/AdminInventoryPage.css
git commit -m "feat(frontend): show stock alerts and inventory ledger"
```

---

### Task 10: Admin forecast page, route, and manual refresh

**Files:**
- Create: `frontend/src/features/admin/AdminForecastPage.tsx`
- Create: `frontend/src/features/admin/AdminForecastPage.test.tsx`
- Create: `frontend/src/features/admin/AdminForecastPage.css`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/App.test.tsx`
- Modify: `frontend/src/features/admin/AdminLayout.tsx`

**Interfaces:**
- Produces: route `/admin/forecast`.
- Consumes: forecast, job history/status, and trigger API methods.

- [ ] **Step 1: Write failing route/page tests**

```typescript
it('switches between strict 7 and 14 day forecasts', async () => {
  renderForecastPage()
  await screen.findByRole('table', { name: 'Dự báo nhu cầu' })
  await user.click(screen.getByRole('button', { name: '14 ngày' }))
  expect(inventoryIntelligenceApi.getForecast).toHaveBeenLastCalledWith(
    expect.objectContaining({ branchId: 'b-1', horizonDays: 14 }))
})

it('shows 409 without losing the current result table', async () => {
  inventoryIntelligenceApi.triggerForecast.mockRejectedValue(new ApiError(409, { message: 'JOB_ALREADY_RUNNING' }))
  renderForecastPage()
  await user.click(await screen.findByRole('button', { name: 'Chạy lại dự báo' }))
  expect(screen.getByRole('status')).toHaveTextContent('Dự báo đang chạy')
  expect(screen.getByRole('table', { name: 'Dự báo nhu cầu' })).toBeInTheDocument()
})
```

- [ ] **Step 2: Implement route/nav/page**

Show branch selector, 7/14 controls, latest status/timestamps, data-quality labels, results, run history, and manual trigger. After `202`, poll the returned status URL with bounded backoff and stop at terminal state/unmount.

- [ ] **Step 3: Run tests/build and commit**

```powershell
npm --prefix frontend test -- --run src/features/admin/AdminForecastPage.test.tsx src/App.test.tsx
npm --prefix frontend run build
git add frontend/src/features/admin/AdminForecastPage.tsx frontend/src/features/admin/AdminForecastPage.test.tsx frontend/src/features/admin/AdminForecastPage.css frontend/src/features/admin/AdminLayout.tsx frontend/src/App.tsx frontend/src/App.test.tsx
git commit -m "feat(frontend): add admin demand forecast workspace"
```
