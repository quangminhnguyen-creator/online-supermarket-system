# Background Job Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây nền job bền vững dùng chung cho forecast và recommendations, gồm audit row, distributed lease, channel dispatch, startup recovery và Admin status API.

**Architecture:** `background_job_runs` là durable source of truth; `Channel<JobRequest>` chỉ đánh thức worker trong process. `JobRunCoordinator` claim/release bằng unique `(job_name, lock_key)` và token-guarded updates, còn `IntelligenceWorker` dispatch qua `IBackgroundJobHandler` để các capability cài handler riêng.

**Tech Stack:** .NET 10, C# 14, EF Core 10.0.9, MySQL 8.4, ASP.NET Core Minimal API, xUnit 2.9.3, Moq 4.20.72, Testcontainers.MySql 4.14.0.

**Spec:** `docs/superpowers/specs/2026-09-03-reviews-inventory-intelligence-design.md`

## Global Constraints

- Chỉ plan này tạo `background_job_runs`; chưa tạo sáu bảng feature.
- Status hợp lệ: `Queued`, `Running`, `Succeeded`, `Failed`.
- Forecast lock key là `branch:{branchId}`; recommendation lock key là `global`.
- Mọi state update sau claim phải match cả `id` và `lock_token`.
- `error_summary` phải sanitized và giới hạn 2.000 ký tự.
- Startup requeue `Queued`; chỉ expired `Running` mới chuyển `Failed` và nhả lock.
- `Infrastructure:DisableBackgroundServices=true` phải giữ API tests hiện tại deterministic.
- Mỗi task theo RED -> GREEN -> REFACTOR -> COMMIT; không stage thay đổi ngoài scope.

---

## File Structure

- Create `backend/src/OnlineSupermarket.Domain/Jobs/BackgroundJobRun.cs`: aggregate và state machine.
- Create `backend/src/OnlineSupermarket.Domain/Jobs/BackgroundJobName.cs`: `Forecast`, `Recommendations`.
- Create `backend/src/OnlineSupermarket.Domain/Jobs/BackgroundJobStatus.cs`: lifecycle enum.
- Create `backend/src/OnlineSupermarket.Domain/Jobs/BackgroundJobTrigger.cs`: scheduled/manual enum.
- Create `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/BackgroundJobRunConfiguration.cs`: MySQL mapping/indexes.
- Modify `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`: `BackgroundJobRuns` DbSet.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/JobRequest.cs`: channel message.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/IBackgroundJobQueue.cs`: queue boundary.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/BackgroundJobQueue.cs`: bounded channel.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/IJobRunCoordinator.cs`: durable lifecycle boundary.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/JobRunCoordinator.cs`: atomic claims/recovery.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/IBackgroundJobHandler.cs`: feature-handler contract.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/IRecurringJobSchedule.cs`: feature schedule contract.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/JobErrorSanitizer.cs`: deterministic secret redaction.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/IntelligenceWorker.cs`: recovery and dispatch loop.
- Create `backend/src/OnlineSupermarket.Infrastructure/Jobs/IntelligenceJobsOptions.cs`: schedule/lease configuration.
- Modify `backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs`: registrations and hosted worker.
- Create `backend/src/OnlineSupermarket.Api/Contracts/Jobs/JobContracts.cs`: Admin DTOs.
- Create `backend/src/OnlineSupermarket.Api/Endpoints/JobEndpoints.cs`: run history/status.
- Modify `backend/src/OnlineSupermarket.Api/Program.cs`: map status endpoints.
- Create `backend/tests/OnlineSupermarket.Domain.Tests/Jobs/BackgroundJobRunTests.cs`: domain lifecycle tests.
- Create `backend/tests/OnlineSupermarket.Infrastructure.Tests/Jobs/JobRunCoordinatorTests.cs`: coordinator tests.
- Create `backend/tests/OnlineSupermarket.Infrastructure.Tests/Jobs/IntelligenceWorkerTests.cs`: orchestration tests.
- Create `backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlFixture.cs`: disposable MySQL 8.4.
- Create `backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlJobLockTests.cs`: real concurrency.
- Modify `backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj`: Testcontainers package.
- Create `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/JobEndpointsTests.cs`: auth/history contract.

---

### Task 1: Domain state machine cho job run

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Jobs/BackgroundJobRun.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Jobs/BackgroundJobName.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Jobs/BackgroundJobStatus.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Jobs/BackgroundJobTrigger.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Jobs/BackgroundJobRunTests.cs`

**Interfaces:**
- Produces: `BackgroundJobRun.Queue(BackgroundJobName, Guid?, BackgroundJobTrigger, Guid?, string, Guid, DateTime, DateTime): BackgroundJobRun`.
- Produces: `Start(Guid lockToken, DateTime nowUtc, DateTime leaseExpiresAtUtc): void`.
- Produces: `RenewLease(Guid lockToken, DateTime leaseExpiresAtUtc): void`.
- Produces: `Succeed(Guid lockToken, DateTime completedAtUtc): void`.
- Produces: `Fail(Guid lockToken, string errorSummary, DateTime completedAtUtc): void`.

- [ ] **Step 1: Viết failing lifecycle tests**

```csharp
[Fact]
public void BackgroundJobRun_FollowsQueuedRunningSucceededLifecycle()
{
    var token = Guid.NewGuid();
    var now = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);
    var run = BackgroundJobRun.Queue(
        BackgroundJobName.Forecast, Guid.NewGuid(), BackgroundJobTrigger.Manual,
        Guid.NewGuid(), "branch:abc", token, now, now.AddMinutes(5));

    run.Start(token, now.AddSeconds(1), now.AddMinutes(6));
    run.RenewLease(token, now.AddMinutes(7));
    run.Succeed(token, now.AddMinutes(2));

    Assert.Equal(BackgroundJobStatus.Succeeded, run.Status);
    Assert.Null(run.LockKey);
    Assert.Null(run.LockToken);
    Assert.Null(run.LockExpiresAtUtc);
}

[Fact]
public void BackgroundJobRun_WithWrongToken_RejectsMutation()
{
    var now = DateTime.UtcNow;
    var run = BackgroundJobRun.Queue(
        BackgroundJobName.Recommendations, null, BackgroundJobTrigger.Scheduled,
        null, "global", Guid.NewGuid(), now, now.AddMinutes(5));

    Assert.Throws<InvalidOperationException>(() =>
        run.Start(Guid.NewGuid(), now, now.AddMinutes(5)));
}

[Fact]
public void Fail_TruncatesErrorSummaryToTwoThousandCharacters()
{
    var token = Guid.NewGuid();
    var now = DateTime.UtcNow;
    var run = BackgroundJobRun.Queue(
        BackgroundJobName.Recommendations, null, BackgroundJobTrigger.Scheduled,
        null, "global", token, now, now.AddMinutes(5));

    run.Start(token, now, now.AddMinutes(5));
    run.Fail(token, new string('x', 2_100), now.AddSeconds(1));

    Assert.Equal(2_000, run.ErrorSummary!.Length);
}
```

- [ ] **Step 2: Chạy test xác nhận RED**

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~BackgroundJobRunTests"
```

Expected: FAIL vì namespace `OnlineSupermarket.Domain.Jobs` chưa tồn tại.

- [ ] **Step 3: Cài state machine tối thiểu**

```csharp
public void Start(Guid lockToken, DateTime nowUtc, DateTime leaseExpiresAtUtc)
{
    EnsureOwner(lockToken);
    if (Status != BackgroundJobStatus.Queued)
        throw new InvalidOperationException("Only queued jobs can start.");
    Status = BackgroundJobStatus.Running;
    StartedAtUtc = nowUtc;
    LockExpiresAtUtc = leaseExpiresAtUtc;
}

public void Succeed(Guid lockToken, DateTime completedAtUtc)
{
    EnsureOwner(lockToken);
    if (Status != BackgroundJobStatus.Running)
        throw new InvalidOperationException("Only running jobs can succeed.");
    Status = BackgroundJobStatus.Succeeded;
    CompletedAtUtc = completedAtUtc;
    ClearLock();
}
```

Implement `Queue`, `RenewLease`, `Fail`, `EnsureOwner`, and `ClearLock` with the exact signatures above. Validate non-empty lock key/token, UTC timestamps, and `branchId` required for `Forecast` but null for `Recommendations`.

- [ ] **Step 4: Chạy GREEN và commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~BackgroundJobRunTests"
git add backend/src/OnlineSupermarket.Domain/Jobs backend/tests/OnlineSupermarket.Domain.Tests/Jobs/BackgroundJobRunTests.cs
git commit -m "feat(jobs): add durable job run state machine"
```

---

### Task 2: Mapping và migration `background_job_runs`

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/BackgroundJobRunConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Generate: EF migration named `AddBackgroundJobRuns` and its designer.
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs`

**Interfaces:**
- Produces: `AppDbContext.BackgroundJobRuns`.
- Produces: unique index `ix_background_job_runs_job_lock` on `(job_name, lock_key)`.
- Produces: query index `ix_background_job_runs_job_created` on `(job_name, created_at_utc)`.

- [ ] **Step 1: Viết failing EF metadata test**

```csharp
[Fact]
public void BackgroundJobRun_HasExpectedTableAndLockIndex()
{
    using var context = CreateContext();
    var entity = context.Model.FindEntityType(typeof(BackgroundJobRun));

    Assert.NotNull(entity);
    Assert.Equal("background_job_runs", entity!.GetTableName());
    var index = entity.GetIndexes().Single(i =>
        i.Properties.Select(p => p.Name).SequenceEqual(
            new[] { nameof(BackgroundJobRun.JobName), nameof(BackgroundJobRun.LockKey) }));
    Assert.True(index.IsUnique);
}
```

- [ ] **Step 2: Chạy test xác nhận RED**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ModelConfigurationTests.BackgroundJobRun"
```

Expected: FAIL vì entity chưa được add vào EF model.

- [ ] **Step 3: Thêm DbSet và configuration**

```csharp
public DbSet<BackgroundJobRun> BackgroundJobRuns => Set<BackgroundJobRun>();
```

```csharp
builder.ToTable("background_job_runs");
builder.HasKey(x => x.Id);
builder.Property(x => x.JobName).HasColumnName("job_name").HasConversion<string>().HasMaxLength(100);
builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
builder.Property(x => x.Trigger).HasColumnName("trigger").HasConversion<string>().HasMaxLength(20);
builder.Property(x => x.ErrorSummary).HasColumnName("error_summary").HasMaxLength(2000);
builder.Property(x => x.LockKey).HasColumnName("lock_key").HasMaxLength(150);
builder.HasIndex(x => new { x.JobName, x.LockKey })
    .IsUnique().HasDatabaseName("ix_background_job_runs_job_lock");
builder.HasIndex(x => new { x.JobName, x.CreatedAtUtc })
    .HasDatabaseName("ix_background_job_runs_job_created");
```

Map every column in spec §4.7, including `char(36)` GUIDs and `datetime(6)` timestamps, and use `Restrict` for optional branch/requester FKs.

- [ ] **Step 4: Tạo migration và kiểm tra snapshot**

```powershell
dotnet ef migrations add AddBackgroundJobRuns --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
rg -n "background_job_runs|ix_background_job_runs_job_lock" backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations
```

Expected: migration creates exactly one new table and both named indexes.

- [ ] **Step 5: Chạy GREEN và commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ModelConfigurationTests"
git add backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/BackgroundJobRunConfiguration.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddBackgroundJobRuns.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddBackgroundJobRuns.Designer.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs
git commit -m "feat(jobs): persist background job runs"
```

---

### Task 3: Durable coordinator và bounded channel

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/JobRequest.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/IBackgroundJobQueue.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/BackgroundJobQueue.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/IJobRunCoordinator.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/JobRunCoordinator.cs`
- Test: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Jobs/JobRunCoordinatorTests.cs`

**Interfaces:**
- Produces: `JobRequest(Guid JobRunId, BackgroundJobName JobName, Guid? BranchId, Guid LockToken)`.
- Produces: `JobQueueResult(bool Accepted, Guid? JobRunId, string? ConflictCode)`.
- Produces: `TryQueueAsync`, `TryStartAsync`, `RenewLeaseAsync`, `SucceedAsync`, `FailAsync`, `RecoverAsync` on `IJobRunCoordinator`.
- Produces: `EnqueueAsync` and `DequeueAsync` on `IBackgroundJobQueue`.

`JobRunCoordinatorTests` must use the Infrastructure test project's existing
SQLite provider with one open in-memory connection and `EnsureCreated`; EF
InMemory cannot enforce the active-lock unique index. Inject a fake queue to
assert enqueue occurs only after a successful database save.

- [ ] **Step 1: Viết failing coordinator tests**

```csharp
[Fact]
public async Task TryQueueAsync_WithActiveLock_ReturnsConflict()
{
    await _coordinator.TryQueueAsync(
        BackgroundJobName.Forecast, _branchId, BackgroundJobTrigger.Manual,
        _adminId, CancellationToken.None);

    var second = await _coordinator.TryQueueAsync(
        BackgroundJobName.Forecast, _branchId, BackgroundJobTrigger.Manual,
        _adminId, CancellationToken.None);

    Assert.False(second.Accepted);
    Assert.Equal("JOB_ALREADY_RUNNING", second.ConflictCode);
}

[Fact]
public async Task CompleteAsync_WithWrongToken_DoesNotReleaseOwnerLock()
{
    var queued = await QueueForecastAsync();

    var updated = await _coordinator.SucceedAsync(
        queued.JobRunId!.Value, Guid.NewGuid(), CancellationToken.None);

    Assert.False(updated);
    Assert.NotNull((await LoadRunAsync(queued.JobRunId.Value)).LockKey);
}
```

- [ ] **Step 2: Chạy test xác nhận RED**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~JobRunCoordinatorTests"
```

Expected: FAIL vì coordinator/queue contracts chưa tồn tại.

- [ ] **Step 3: Cài queue và coordinator**

```csharp
public sealed record JobRequest(
    Guid JobRunId,
    BackgroundJobName JobName,
    Guid? BranchId,
    Guid LockToken);

public interface IBackgroundJobQueue
{
    ValueTask EnqueueAsync(JobRequest request, CancellationToken cancellationToken);
    ValueTask<JobRequest> DequeueAsync(CancellationToken cancellationToken);
}

public sealed record JobQueueResult(
    bool Accepted,
    Guid? JobRunId,
    string? ConflictCode);

public interface IJobRunCoordinator
{
    Task<JobQueueResult> TryQueueAsync(
        BackgroundJobName jobName,
        Guid? branchId,
        BackgroundJobTrigger trigger,
        Guid? requestedByUserId,
        CancellationToken cancellationToken);
    Task<bool> TryStartAsync(Guid runId, Guid lockToken, CancellationToken cancellationToken);
    Task<bool> RenewLeaseAsync(Guid runId, Guid lockToken, CancellationToken cancellationToken);
    Task<bool> SucceedAsync(Guid runId, Guid lockToken, CancellationToken cancellationToken);
    Task<bool> FailAsync(
        Guid runId,
        Guid lockToken,
        string errorSummary,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<JobRequest>> RecoverAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
```

Use `Channel.CreateBounded<JobRequest>(new BoundedChannelOptions(256) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true })`. `TryQueueAsync` creates the row, saves it, then enqueues only after the transaction commits. A focused helper recognizes SQLite constraint code 19 in coordinator tests and MySQL duplicate-key code 1062 naming `ix_background_job_runs_job_lock` in production, then returns `JobQueueResult(false, null, "JOB_ALREADY_RUNNING")`; do not catch unrelated `DbUpdateException` values.

- [ ] **Step 4: Thêm recovery and token-guarded updates**

Use `ExecuteUpdateAsync` predicates shaped as:

```csharp
db.BackgroundJobRuns.Where(x =>
    x.Id == runId &&
    x.LockToken == lockToken &&
    x.Status == BackgroundJobStatus.Running)
```

`RecoverAsync(nowUtc)` returns queued `JobRequest` rows and atomically fails only `Running` rows with `LockExpiresAtUtc < nowUtc`. Verify returned requests keep the persisted token.

- [ ] **Step 5: Chạy GREEN và commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~JobRunCoordinatorTests"
git add backend/src/OnlineSupermarket.Infrastructure/Jobs backend/tests/OnlineSupermarket.Infrastructure.Tests/Jobs/JobRunCoordinatorTests.cs
git commit -m "feat(jobs): add durable queue coordinator"
```

---

### Task 4: Worker dispatch, lease renewal và failure isolation

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/IBackgroundJobHandler.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/IRecurringJobSchedule.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/JobErrorSanitizer.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/IntelligenceWorker.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Jobs/IntelligenceJobsOptions.cs`
- Test: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Jobs/IntelligenceWorkerTests.cs`

**Interfaces:**
- Produces: `IBackgroundJobHandler.JobName`.
- Produces: `IBackgroundJobHandler.ExecuteAsync(Guid jobRunId, Guid? branchId, CancellationToken): Task`.
- Produces: `IRecurringJobSchedule.GetDueJobsAsync(DateTime nowUtc, CancellationToken): Task<IReadOnlyList<RecurringJobRequest>>`.
- Produces: configuration section `IntelligenceJobs` with lease, forecast time, and recommendation interval.

- [ ] **Step 1: Viết failing dispatch/recovery tests**

```csharp
[Fact]
public async Task RunOneAsync_DispatchesMatchingHandlerAndCompletesRun()
{
    var request = new JobRequest(_runId, BackgroundJobName.Forecast, _branchId, _token);

    await _worker.RunOneAsync(request, CancellationToken.None);

    _forecastHandler.Verify(x => x.ExecuteAsync(_runId, _branchId, It.IsAny<CancellationToken>()), Times.Once);
    _coordinator.Verify(x => x.SucceedAsync(_runId, _token, It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task RunOneAsync_WhenHandlerThrows_FailsRunAndContinues()
{
    _forecastHandler.Setup(x => x.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("database details must not leak"));

    await _worker.RunOneAsync(
        new JobRequest(_runId, BackgroundJobName.Forecast, _branchId, _token),
        CancellationToken.None);

    _coordinator.Verify(x => x.FailAsync(
        _runId, _token, It.Is<string>(s => s.Length <= 2_000),
        It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task DispatchAsync_NeverExceedsConfiguredConcurrency()
{
    var probe = new ConcurrencyProbeHandler(blockUntilReleased: true);
    var worker = CreateWorker(probe, maxConcurrentJobs: 2);

    var dispatch = worker.DispatchAsync(CreateRequests(4), CancellationToken.None);
    await probe.WaitUntilStartedAsync(expected: 2);

    Assert.Equal(2, probe.MaximumObservedConcurrency);
    probe.ReleaseAll();
    await dispatch;
}

[Theory]
[InlineData("Password=real-secret", "Password=[REDACTED]")]
[InlineData("Pwd=real-secret", "Pwd=[REDACTED]")]
[InlineData("SecretKey=real-secret", "SecretKey=[REDACTED]")]
[InlineData("token=real-secret", "token=[REDACTED]")]
public void JobErrorSanitizer_RedactsSecrets(string raw, string expected)
{
    Assert.Equal(expected, JobErrorSanitizer.Sanitize(raw));
}
```

- [ ] **Step 2: Chạy test xác nhận RED**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~IntelligenceWorkerTests"
```

- [ ] **Step 3: Implement worker loop**

```csharp
public interface IBackgroundJobHandler
{
    BackgroundJobName JobName { get; }
    Task ExecuteAsync(Guid jobRunId, Guid? branchId, CancellationToken cancellationToken);
}

public sealed record RecurringJobRequest(
    BackgroundJobName JobName,
    Guid? BranchId);

public interface IRecurringJobSchedule
{
    Task<IReadOnlyList<RecurringJobRequest>> GetDueJobsAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
```

`IntelligenceWorker` is singleton because it is hosted; it injects only the
singleton queue plus `IServiceScopeFactory`. On startup it creates a scope for
`RecoverAsync`, enqueues recovered requests, then creates a fresh scope once per
minute to resolve scoped `IRecurringJobSchedule` and `IJobRunCoordinator`
instances. Each due request calls `TryQueueAsync` with trigger `Scheduled`.
Consume the channel with one reader, dispatch at most `MaxConcurrentJobs` through
a `SemaphoreSlim`, and await all tracked executions during graceful shutdown.
For each request create a fresh scope, resolve the matching scoped
handler/coordinator, then perform token-guarded start,
periodic lease renewal at one-third of lease duration, handler dispatch,
succeed/fail update, and cancellation-safe cleanup. Expose `internal RunOneAsync`
and `internal QueueDueJobsAsync` for deterministic unit tests through the
existing `InternalsVisibleTo`.

`JobErrorSanitizer.Sanitize` replaces CR/LF with spaces, redacts values following
case-insensitive `Password=`, `Pwd=`, `SecretKey=`, and `token=` keys up to the
next semicolon or whitespace, then truncates to 2,000 characters. Persist only
`ExceptionType: sanitized message`; never persist stack traces or serialized
inner exceptions.

- [ ] **Step 4: Chạy GREEN và commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~IntelligenceWorkerTests"
git add backend/src/OnlineSupermarket.Infrastructure/Jobs backend/tests/OnlineSupermarket.Infrastructure.Tests/Jobs/IntelligenceWorkerTests.cs
git commit -m "feat(jobs): dispatch leased intelligence jobs"
```

---

### Task 5: Dependency injection và configuration contract

**Files:**
- Modify: `backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/OnlineSupermarket.Api/appsettings.json`
- Modify: `backend/src/OnlineSupermarket.Api/appsettings.Development.json`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Configuration/ConfigurationContractTests.cs`

**Interfaces:**
- Consumes: queue, coordinator, worker, and options from Tasks 3–4.
- Produces: singleton queue, scoped coordinator, and hosted worker when background services are enabled.

- [ ] **Step 1: Viết failing configuration test**

```csharp
[Fact]
public void IntelligenceJobs_DefaultsAreValid()
{
    var options = _configuration.GetSection(IntelligenceJobsOptions.SectionName)
        .Get<IntelligenceJobsOptions>();

    Assert.NotNull(options);
    Assert.True(options!.LeaseMinutes >= 3);
    Assert.Equal(60, options.RecommendationIntervalMinutes);
    Assert.Equal(4, options.MaxConcurrentJobs);
    Assert.InRange(options.ForecastHourUtc, 0, 23);
}
```

- [ ] **Step 2: Add exact defaults and registrations**

```json
"IntelligenceJobs": {
  "LeaseMinutes": 10,
  "MaxConcurrentJobs": 4,
  "RecommendationIntervalMinutes": 60,
  "ForecastHourUtc": 1
}
```

```csharp
services.Configure<IntelligenceJobsOptions>(configuration.GetSection(IntelligenceJobsOptions.SectionName));
services.AddSingleton(TimeProvider.System);
services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
services.AddScoped<IJobRunCoordinator, JobRunCoordinator>();
if (!configuration.GetValue<bool>("Infrastructure:DisableBackgroundServices"))
{
    services.AddHostedService<IntelligenceWorker>();
}
```

Keep `RefreshTokenCleanupService` under the same existing disable flag.

- [ ] **Step 3: Chạy tests và commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ConfigurationContractTests"
git add backend/src/OnlineSupermarket.Infrastructure/DependencyInjection.cs backend/src/OnlineSupermarket.Api/appsettings.json backend/src/OnlineSupermarket.Api/appsettings.Development.json backend/tests/OnlineSupermarket.Api.Tests/Configuration/ConfigurationContractTests.cs
git commit -m "feat(jobs): configure intelligence worker"
```

---

### Task 6: Admin run history và status API

**Files:**
- Create: `backend/src/OnlineSupermarket.Api/Contracts/Jobs/JobContracts.cs`
- Create: `backend/src/OnlineSupermarket.Api/Endpoints/JobEndpoints.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Program.cs`
- Test: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/JobEndpointsTests.cs`

**Interfaces:**
- Produces: `GET /api/admin/jobs/{jobName}/runs?page=1&pageSize=20`.
- Produces: `GET /api/admin/jobs/runs/{runId}` for the `Location` returned by later trigger endpoints.
- Produces: `JobRunDto` and `PaginatedJobRunsDto`.

- [ ] **Step 1: Viết failing authorization and response tests**

```csharp
[Fact]
public async Task GetRuns_AsCustomer_ReturnsForbidden()
{
    using var client = await CreateAuthenticatedClientAsync(UserRole.Customer);
    var response = await client.GetAsync("/api/admin/jobs/Forecast/runs");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task GetRuns_AsAdmin_ReturnsNewestFirst()
{
    await SeedRunsAsync(BackgroundJobName.Forecast);
    using var client = await CreateAuthenticatedClientAsync(UserRole.Admin);

    var response = await client.GetFromJsonAsync<PaginatedJobRunsDto>(
        "/api/admin/jobs/Forecast/runs?page=1&pageSize=20");

    Assert.NotNull(response);
    Assert.Equal(2, response!.TotalCount);
    Assert.True(response.Data[0].CreatedAtUtc >= response.Data[1].CreatedAtUtc);
}
```

- [ ] **Step 2: Implement contracts and endpoints**

```csharp
public sealed record JobRunDto(
    Guid Id, string JobName, Guid? BranchId, string Status, string Trigger,
    DateTime? StartedAtUtc, DateTime? CompletedAtUtc, string? ErrorSummary,
    DateTime CreatedAtUtc);
```

Validate `jobName` with `Enum.TryParse<BackgroundJobName>(ignoreCase: true)`, clamp `pageSize` to 1–100, order by `CreatedAtUtc` descending, and require `AdminOnly` for the entire group.

- [ ] **Step 3: Map, test, and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~JobEndpointsTests"
git add backend/src/OnlineSupermarket.Api/Contracts/Jobs backend/src/OnlineSupermarket.Api/Endpoints/JobEndpoints.cs backend/src/OnlineSupermarket.Api/Program.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/JobEndpointsTests.cs
git commit -m "feat(jobs): expose admin job run status"
```

---

### Task 7: Real MySQL schema and concurrency fixture

**Files:**
- Modify: `backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj`
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlFixture.cs`
- Create: `backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlJobLockTests.cs`

**Interfaces:**
- Produces: collection fixture backed by `mysql:8.4`.
- Produces: reusable `CreateContext()` returning independent real connections.

- [ ] **Step 1: Add package and failing concurrent-claim test**

```xml
<PackageReference Include="Testcontainers.MySql" Version="4.14.0" />
```

```csharp
[Fact]
public async Task ConcurrentClaims_ForSameBranch_AllowExactlyOneOwner()
{
    var attempts = Enumerable.Range(0, 8).Select(async _ =>
    {
        await using var context = _fixture.CreateContext();
        var coordinator = CreateCoordinator(context);
        return await coordinator.TryQueueAsync(
            BackgroundJobName.Forecast, _branchId, BackgroundJobTrigger.Manual,
            _adminId, CancellationToken.None);
    });

    var results = await Task.WhenAll(attempts);

    Assert.Single(results.Where(x => x.Accepted));
    Assert.Equal(7, results.Count(x => x.ConflictCode == "JOB_ALREADY_RUNNING"));
}
```

- [ ] **Step 2: Implement MySQL fixture**

```csharp
private readonly MySqlContainer _container = new MySqlBuilder()
    .WithImage("mysql:8.4")
    .WithDatabase("online_supermarket_tests")
    .WithUsername("test")
    .WithPassword("test-password")
    .Build();
```

Start once per non-parallel collection, apply `Database.MigrateAsync`, provide a new `AppDbContext` per concurrent task, and dispose the container after the collection. Add a schema assertion for `background_job_runs` and its unique index.

- [ ] **Step 3: Run real database tests**

```powershell
dotnet test backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~MySqlJobLockTests"
```

Expected: exactly one owner for the same key; claims for two different branches both succeed.

Add a second real-connection test that seeds one expired `Running` row, invokes
recovery plus replacement claim concurrently from two coordinators, then asserts
the expired row is `Failed` and exactly one non-terminal row owns the branch key.

- [ ] **Step 4: Run foundation regression and commit**

```powershell
dotnet test OnlineSupermarket.slnx --no-restore
git add backend/tests/OnlineSupermarket.Infrastructure.Tests/OnlineSupermarket.Infrastructure.Tests.csproj backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlFixture.cs backend/tests/OnlineSupermarket.Infrastructure.Tests/Persistence/MySqlJobLockTests.cs
git commit -m "test(jobs): verify mysql job lock concurrency"
```
