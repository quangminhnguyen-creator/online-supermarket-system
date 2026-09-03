# Reviews, Inventory Intelligence, and Recommendations Design

**Date:** 2026-09-03

**Status:** Approved design

**Scope:** FR-113, FR-208, FR-209

## 1. Goal

Deliver seven new persistent data stores and the end-to-end capabilities that use them:

1. `reviews`
2. `inventory_transactions`
3. `product_view_events`
4. `recommendation_results`
5. `demand_forecasts`
6. `stock_alerts`
7. `background_job_runs`

The implementation includes domain behavior, EF Core configuration and migration, APIs, scheduled and manually triggered background work, customer and admin UI, automated tests, seed/demo data, and documentation updates.

## 2. Current-State Baseline

`AppDbContextModelSnapshot` currently contains these 17 physical tables:

1. `addresses`
2. `branch_inventories`
3. `branches`
4. `brands`
5. `cart_items`
6. `carts`
7. `categories`
8. `order_items`
9. `order_status_histories`
10. `orders`
11. `password_reset_tokens`
12. `payment_callbacks`
13. `payments`
14. `products`
15. `promotions`
16. `refresh_tokens`
17. `users`

`promotions` is already implemented and is not part of the new migration. Adding the seven stores in this design produces 24 physical tables. There are no separate `roles`, `user_roles`, `promotion_usages`, or `user_product_affinities` tables.

## 3. Architectural Approach

The design is split into three independently testable capabilities that share persistence and job infrastructure:

- Verified reviews: synchronous customer APIs backed by completed order items.
- Inventory intelligence: an append-only inventory ledger, moving-average demand forecasts, and materialized stock alerts.
- Recommendations: synchronous view-event capture and session merge, followed by scheduled materialization of global, per-user, and similar-product results.

ASP.NET Core `BackgroundService` and an in-process `Channel<JobRequest>` dispatch work. MySQL rows provide cross-instance ownership, lease recovery, and audit history. Forecast and recommendation APIs read materialized results rather than calculating them on demand. No message broker, Redis, ML.NET model, or external AI service is introduced.

## 4. Data Model

All identifiers use `Guid`; MySQL columns follow the repository's existing `char(36)` convention where explicitly configured. All timestamps are UTC `datetime(6)`. Money remains `decimal(18,2)`. Foreign keys use `Restrict` unless the current aggregate already has a stronger lifecycle rule.

### 4.1 `reviews`

| Column | Type | Rule |
|---|---|---|
| `id` | `char(36)` | Primary key |
| `user_id` | `char(36)` | FK to `users`; required |
| `order_item_id` | `char(36)` | FK to `order_items`; required; unique |
| `product_id` | `char(36)` | FK to `products`; required |
| `rating` | `tinyint` | Inclusive range 1–5 |
| `comment` | `varchar(2000)` | Trimmed; optional |
| `created_at_utc` | `datetime(6)` | Required |
| `updated_at_utc` | `datetime(6)` | Required |

Indexes support product/date pagination and a user's review history. One `OrderItem` can have one review regardless of its `Quantity`. A later order creates a different `OrderItem`, so the same customer can review the same product again after another completed purchase.

Review creation accepts only `orderItemId`, `rating`, and `comment`. The backend derives `product_id` and verifies the full chain:

```text
OrderItem.OrderId -> Order.UserId == current user
OrderItem.ProductId -> stored Review.ProductId
Order.Status == Completed
```

Customers may update their own rating/comment without a time limit in the MVP. Reviews are not deleted through the API. No `released_at` or edit-window column is added.

### 4.2 `inventory_transactions`

| Column | Type | Rule |
|---|---|---|
| `id` | `char(36)` | Primary key |
| `branch_inventory_id` | `char(36)` | FK to `branch_inventories`; required |
| `transaction_type` | `varchar(30)` | `Reserve`, `Release`, `Sale`, `ManualAdjustment` |
| `quantity_on_hand_delta` | `int` | Signed delta |
| `reserved_quantity_delta` | `int` | Signed delta |
| `quantity_on_hand_after` | `int` | Non-negative snapshot |
| `reserved_quantity_after` | `int` | Non-negative snapshot |
| `reference_type` | `varchar(30)` | `Order`, `AdminAdjustment`, or `System` |
| `reference_id` | `char(36)` | Nullable source identifier |
| `operation_key` | `varchar(180)` | Nullable; unique when supplied |
| `actor_user_id` | `char(36)` | Nullable FK to `users` |
| `note` | `varchar(500)` | Optional |
| `created_at_utc` | `datetime(6)` | Required; append-only |

The entity exposes no update or delete operation. Automated order mutations use deterministic operation keys, for example `order:{orderId}:inventory:{inventoryId}:sale`, so retries cannot apply the same stock mutation twice.

Every inventory mutation and its ledger row commit in the same `Serializable` database transaction:

- Checkout reservation: `quantity_on_hand_delta = 0`, `reserved_quantity_delta = +quantity`.
- Cancellation/payment failure release: `quantity_on_hand_delta = 0`, `reserved_quantity_delta = -quantity`.
- Order becomes `Completed`: `quantity_on_hand_delta = -quantity`, `reserved_quantity_delta = -quantity`.
- Admin absolute quantity update: delta equals `newQuantity - oldQuantity`; reserved quantity does not change.

For a sale of 10 from `on_hand = 100`, `reserved = 10`, both values become 90 and 0. Available quantity therefore remains 90; the reserved units have become sold units.

### 4.3 `product_view_events`

| Column | Type | Rule |
|---|---|---|
| `id` | `char(36)` | Primary key |
| `product_id` | `char(36)` | FK to `products`; required |
| `user_id` | `char(36)` | Nullable FK to `users` |
| `anonymous_session_id` | `char(36)` | Nullable anonymous GUID |
| `branch_id` | `char(36)` | Nullable FK to `branches` |
| `viewed_at_utc` | `datetime(6)` | Required |

The endpoint records no IP address or user-agent. The frontend creates an anonymous GUID in `localStorage`. When an authenticated user claims that GUID through the merge endpoint, previously anonymous events with the same GUID and `user_id IS NULL` are assigned to that user in one atomic update. Events already assigned to a user are never reassigned.

All events remain stored. Recommendation aggregation limits refresh spam by counting at most one contribution for each user/product/UTC-day before applying recency weighting.

### 4.4 `recommendation_results`

| Column | Type | Rule |
|---|---|---|
| `id` | `char(36)` | Primary key |
| `scope` | `varchar(30)` | `Global`, `User`, or `SimilarProduct` |
| `audience_key` | `varchar(100)` | `global`, `user:{id}`, or `product:{id}` |
| `user_id` | `char(36)` | Nullable FK; required only for `User` |
| `source_product_id` | `char(36)` | Nullable FK; required only for `SimilarProduct` |
| `recommended_product_id` | `char(36)` | FK to `products`; required |
| `score` | `decimal(12,6)` | Required |
| `rank` | `int` | Positive ordering |
| `reason` | `varchar(500)` | Customer-safe explanation |
| `algorithm_version` | `varchar(50)` | Required |
| `generated_at_utc` | `datetime(6)` | Required |
| `expires_at_utc` | `datetime(6)` | Required |
| `job_run_id` | `char(36)` | FK to `background_job_runs`; required |

The unique key is `(job_run_id, audience_key, recommended_product_id)`. `audience_key` avoids nullable-composite uniqueness ambiguity in MySQL. There is no `user_product_affinities` table: affinities are temporary job calculations derived from view events and completed purchases, while only ranked output is materialized.

### 4.5 `demand_forecasts`

| Column | Type | Rule |
|---|---|---|
| `id` | `char(36)` | Primary key |
| `branch_inventory_id` | `char(36)` | FK to `branch_inventories`; required |
| `horizon_days` | `tinyint` | Exactly 7 or 14 |
| `forecast_start_date` | `date` | Required |
| `forecast_end_date` | `date` | Required |
| `predicted_quantity` | `decimal(18,2)` | Non-negative |
| `actual_data_days` | `int` | Calendar days used, 0–28 |
| `data_quality` | `varchar(20)` | `Insufficient`, `Partial`, `Sufficient` |
| `algorithm_version` | `varchar(50)` | Required |
| `generated_at_utc` | `datetime(6)` | Required |
| `job_run_id` | `char(36)` | FK to `background_job_runs`; required |

The unique key is `(job_run_id, branch_inventory_id, horizon_days)`. Each run writes separate 7-day and 14-day rows.

### 4.6 `stock_alerts`

| Column | Type | Rule |
|---|---|---|
| `id` | `char(36)` | Primary key |
| `branch_inventory_id` | `char(36)` | FK to `branch_inventories`; required |
| `demand_forecast_id` | `char(36)` | FK to the 14-day forecast; required |
| `available_quantity_snapshot` | `int` | Required |
| `reorder_level_snapshot` | `int` | Required |
| `predicted_quantity_snapshot` | `decimal(18,2)` | Required |
| `recommended_reorder_quantity` | `int` | Non-negative |
| `severity` | `varchar(20)` | `Low`, `Medium`, `High` |
| `created_at_utc` | `datetime(6)` | Required |
| `job_run_id` | `char(36)` | FK to `background_job_runs`; required |

The unique key is `(job_run_id, branch_inventory_id)`. An alert is created when:

```text
available_quantity - ceil(predicted_14_day_quantity) <= reorder_level
```

The reorder recommendation is:

```text
max(0, ceil(predicted_14_day_quantity) + reorder_level - available_quantity)
```

This detects depletion below safety stock even when a literal stockout is not yet predicted.

### 4.7 `background_job_runs`

| Column | Type | Rule |
|---|---|---|
| `id` | `char(36)` | Primary key |
| `job_name` | `varchar(100)` | `Forecast` or `Recommendations` |
| `branch_id` | `char(36)` | Nullable FK; set for branch forecast |
| `status` | `varchar(20)` | `Queued`, `Running`, `Succeeded`, `Failed` |
| `trigger` | `varchar(20)` | `Scheduled` or `Manual` |
| `requested_by_user_id` | `char(36)` | Nullable FK; set for manual trigger |
| `started_at_utc` | `datetime(6)` | Nullable until running |
| `completed_at_utc` | `datetime(6)` | Nullable until terminal |
| `error_summary` | `varchar(2000)` | Nullable; sanitized and truncated |
| `lock_key` | `varchar(150)` | Nullable in terminal state |
| `lock_token` | `char(36)` | Nullable ownership token |
| `lock_expires_at_utc` | `datetime(6)` | Nullable lease expiry |
| `created_at_utc` | `datetime(6)` | Required |

MySQL uses a normal unique index on `(job_name, lock_key)`. A filtered `WHERE lock_key IS NOT NULL` index is not used; MySQL's unique-index behavior already permits multiple `NULL` values.

Forecast locks use `branch:{branchId}` and allow different branches to run in parallel. Recommendations use `global` and permit only one system-wide run.

## 5. Business Algorithms

### 5.1 Forecast

The MVP uses a simple moving average over at most the previous 28 complete UTC calendar days. Demand comes only from `Sale` inventory transactions; reservations, releases, and admin adjustments do not count as demand.

- The observation window starts at the later of 28 days ago or the first available sale date.
- Calendar days with no sale count as zero after the first available sale date.
- `predicted_quantity = average_daily_sales * horizon_days`.
- `actual_data_days < 7` is `Insufficient`; 7–13 is `Partial`; 14–28 is `Sufficient`.
- No history produces a zero forecast with `actual_data_days = 0` and `Insufficient` quality.
- Both horizon rows are committed together with their branch's alert batch.

### 5.2 Stock Alerts

Alerts always use the corresponding run's 14-day forecast and a snapshot of current available stock and reorder level. Severity is deterministic:

- `High`: predicted demand exceeds available stock.
- `Medium`: projected remainder is at or below half the reorder level.
- `Low`: projected remainder is above half the reorder level but at or below the reorder level.

Historical alerts remain queryable through their job run. The default admin view selects the latest successful run for each branch.

### 5.3 Recommendations

The content-based job uses active products, category and brand metadata, deduplicated daily view contributions, and completed purchases. Recent activity has more weight; completed purchases carry more weight than views.

- `User`: rank unseen active products using the user's category/brand affinity plus normalized global popularity.
- `SimilarProduct`: rank active products by same-category, same-brand, and popularity signals; exclude the source product.
- `Global`: rank active products by recent unique views and completed sales for cold-start users.
- Products unavailable in a requested branch are filtered when serving the API, not when generating global results.
- Results expire after the configurable recommendation interval plus a grace period.

The customer API applies fallback internally:

```text
Homepage: User -> Global -> empty list
Product detail: SimilarProduct -> Global -> empty list
```

It always returns `200 OK` with `sourceScope` set to `User`, `SimilarProduct`, or `Global`; no available result returns an empty list. `410 Gone` is not used because expired materialized rows are an internal cache concern, not a separately addressable resource.

## 6. API Contracts

### 6.1 Customer/Public APIs

| Method | Route | Authorization | Behavior |
|---|---|---|---|
| `GET` | `/api/products/{productId}/reviews` | Public | Paginated reviews and rating aggregate |
| `GET` | `/api/products/{productId}/review-eligibility` | Customer | Eligible completed order item and existing review |
| `POST` | `/api/reviews` | Customer | Create verified review |
| `PUT` | `/api/reviews/{reviewId}` | Owner | Update rating/comment |
| `POST` | `/api/products/{productId}/views` | Public | Record authenticated or anonymous view |
| `POST` | `/api/recommendations/session/merge` | Customer | Claim unowned events for anonymous GUID |
| `GET` | `/api/recommendations` | Public/optional JWT | Homepage user/global results |
| `GET` | `/api/products/{productId}/recommendations` | Public | Similar/global product results |

The frontend supplies optional `branchId` for recommendation availability filtering and `anonymousSessionId` for guest view capture. Authentication identity always comes from the validated JWT, never from request user IDs.

Order detail responses add `canReview` and nullable `reviewId` to every item.
On a product page, eligibility selects the most recent completed, unreviewed
`OrderItem` for that product. If none remains, the response exposes the user's
most recently updated review for editing; order detail always targets its exact
item and never performs this selection.

### 6.2 Admin APIs

| Method | Route | Behavior |
|---|---|---|
| `GET` | `/api/admin/inventory/{inventoryId}/transactions` | Paginated immutable ledger |
| `GET` | `/api/admin/forecast?branchId=&horizonDays=7|14` | Latest successful forecasts |
| `GET` | `/api/admin/stock-alerts?branchId=&severity=` | Latest active alert snapshots |
| `GET` | `/api/admin/recommendations/results?scope=&limit=` | Last run and sample results |
| `GET` | `/api/admin/jobs/{jobName}/runs` | Paginated audit history |
| `POST` | `/api/admin/jobs/forecast/runs` | Queue a branch forecast |
| `POST` | `/api/admin/jobs/recommendations/runs` | Queue a global recommendation run |

The forecast trigger body requires `branchId`; the recommendation trigger has
no target body because its lock is global. Manual job triggers return
`202 Accepted` with `jobRunId` and a status URL. A currently owned lock returns
`409 Conflict`. Forecast queries accept only 7 or 14; all other values return
`400 Bad Request`.

### 6.3 Error Semantics

- `400`: invalid rating/comment, anonymous GUID, horizon, filter, or state transition input.
- `401`: missing/invalid authentication for customer-owned operations.
- `403`: authenticated principal lacks Admin role or attempts to edit another user's review.
- `404`: product, order item, review, inventory, or owned resource is unavailable.
- `409`: duplicate `order_item_id` review, active job lock, repeated operation key, or stock/order concurrency conflict.

Problem Details remains the error response format used by the current minimal APIs.

## 7. Background Processing

`IntelligenceWorker : BackgroundService` consumes `Channel<JobRequest>`. The channel provides immediate in-process wake-up; `background_job_runs` is the durable audit/ownership source.

### 7.1 Scheduling

- Forecast: daily, one queued run per active branch.
- Recommendations: hourly, one global run.
- Intervals and lease duration are configuration-backed with production-safe defaults.
- `Infrastructure:DisableBackgroundServices=true` continues to disable workers in ordinary API tests.

### 7.2 Claim and Recovery

1. Begin a database transaction.
2. Atomically mark expired `Running` records as `Failed`, clear their lock fields, and set a sanitized recovery error.
3. Insert a `Queued` run with its lock key/token. The unique index rejects a concurrent owner.
4. Commit, enqueue the run ID, and return `202` for manual requests.
5. The worker changes `Queued -> Running` only when run ID and lock token match.
6. Lease renewal and terminal updates include run ID and lock token in their predicates.
7. Success or failure clears lock key/token/expiry and sets completion time.

At startup the worker re-enqueues non-expired `Queued` rows. Expired `Running` rows become failed before a replacement run may claim the lock. Exceptions are recorded per run and do not stop the worker loop.

### 7.3 Retry Semantics

Jobs are safe to retry, not exactly-once distributed executions. Each explicit retry creates a new `jobRunId`. A run calculates into a transaction and commits the complete materialized batch or rolls back all rows. Unique batch keys prevent duplicate rows within a run. Inventory operation keys separately guarantee that replayed order transitions cannot apply stock deltas twice.

## 8. Frontend Design

### 8.1 Reviews

- `/orders/history/:id`: completed, unreviewed order items show a review action; reviewed items link to edit.
- `/product/:id`: show rating aggregate, paginated review list, and authenticated eligibility-aware create/edit form.
- Form states cover unauthenticated, ineligible, eligible, submitting, saved, conflict, and validation failure.

### 8.2 Recommendations

- `/` (`ProductBrowsePage`): a recommendation shelf above the normal catalog grid.
- `/product/:id`: a similar-products shelf and a fire-once view event per mounted product route.
- Guest identity is a GUID in `localStorage`; `AuthContext` calls session merge once after successful login/session restoration.
- Empty recommendation results do not hide the normal catalog or product content.

### 8.3 Forecast and Alerts

- `/admin/inventory`: add stock-alert badges/filter and a latest-alert panel without replacing existing inventory editing.
- `/admin/forecast`: add 7/14-day forecast table, branch selector, data-quality state, last-run status, run history, and manual refresh.
- `/admin/recommendations`: add last-run status, run history, sample results, and manual refresh.
- `AdminLayout` gains Forecast and Recommendations navigation items; existing `AdminRoute` remains the authorization boundary.

All screens include loading, empty, error, stale-result, and retry states and preserve the project's current responsive/accessibility patterns.

## 9. Testing Strategy

### 9.1 Domain and Service Tests

- Review rating 1–5 boundaries, 2,000-character comment limit, ownership-independent entity invariants, and updates.
- Inventory delta/snapshot invariants, non-negative stock, append-only transaction behavior, and deterministic operation keys.
- Job state transitions `Queued -> Running -> Succeeded|Failed`, token ownership, lease expiry, and illegal transitions.
- Moving-average 7/14 calculations with 0, 1, 6, 7, 13, 14, and 28 observation days.
- Alert threshold/reorder quantity/severity boundary cases.
- Recommendation ordering, same-product exclusion, inactive-product exclusion, event daily cap, recency, and fallback.

### 9.2 API Integration Tests

The existing `WebApplicationFactory` plus EF Core InMemory database remains appropriate for endpoint contract, authorization, and application-flow tests. Add cases for:

- Verified purchase chain, wrong owner, wrong product derivation, non-completed order, duplicate review `409`, update ownership, eligibility, and pagination.
- View capture for guest/user, invalid anonymous GUID, merge ownership rules, and repeated merge safety.
- Strict 7/14 horizon, admin filters, sample results, and status history.
- Manual trigger `202`, active lock `409`, and authorization `401/403`.

### 9.3 MySQL Persistence and Concurrency Tests

EF InMemory does not enforce relational constraints, transaction isolation, or MySQL unique-null semantics. Add a MySQL 8.4 integration fixture using a disposable test database/container:

- Apply the full migration and assert all 24 physical tables.
- Assert FK, review/order-item unique, operation-key unique, and job lock indexes.
- Execute inventory mutation plus ledger insert under `Serializable`; verify full rollback on ledger failure.
- Use real concurrent tasks/connections to prove only one lock claim and only one automated inventory operation succeeds.
- Mock `IJobLockService` only in worker orchestration unit tests; the persistence suite owns real race-condition coverage.

Mutation tests run in transactions and roll back where possible. Migration/DDL tests use an isolated database and drop it during fixture disposal. Seed data is deterministic and scoped to each test collection.

### 9.4 Worker Tests

- Scheduled and manual dispatch.
- Startup requeue of `Queued` work.
- Claim conflict, lease renewal, stale `Running` recovery, and token mismatch.
- Failure isolation and sanitized/truncated error storage.
- Full result-batch rollback on calculation/write failure.
- Safe rerun with a new run ID and no duplicate result rows.

### 9.5 Frontend and E2E Tests

- Component/API-client tests use the repository's Vitest and React Testing Library setup.
- Browser flows use the existing Playwright dependency against the Docker Compose development stack; base UI/API URLs and artifact paths must be environment variables rather than machine-specific constants.
- E2E flows cover completed order to create/edit review, inventory mutation to forecast/alert, and anonymous views to login merge to personalized recommendations.

### 9.6 Performance Smoke Checks

Performance checks use deterministic local seed sizes and report results without becoming hardware-sensitive CI gates:

- Product review query with 1,000 rows: target below 200 ms.
- Review creation: target below 100 ms excluding network latency.
- Materialized recommendation query: target below 300 ms.
- Forecast batch for 100 branches: target below 5 minutes.

Any regression beyond a target requires query-plan/index review before release even though the timing check is not a universal CI pass/fail gate.

## 10. Documentation and Traceability

Implementation must update these sources without claiming features are implemented before tests pass:

- `docs/requirements/functional-requirements.md`: FR-113, FR-208, FR-209 status and acceptance criteria.
- `docs/architecture/erd.md`: seven tables, fields, indexes, and physical relationships.
- `docs/architecture/dfd.md`: promote P11, P13, and P14 into implemented data flows.
- `docs/architecture/sitemap.md`: customer/admin routes and placement.
- `docs/api/openapi.json`: generated contracts for every new endpoint.
- `README.md`: 24-table inventory, endpoints, worker configuration, and run/test instructions.
- Relevant progress/test-flow documents: implementation evidence and E2E coverage.

## 11. Delivery Boundaries and Order

The implementation should be executed as separate reviewable plans while preserving this shared contract:

1. Shared persistence and job-run foundation.
2. Verified reviews end to end.
3. Inventory ledger, forecast, alerts, and admin UI.
4. View events, session merge, recommendations, and customer/admin UI.
5. Cross-capability E2E, performance smoke checks, OpenAPI, and architecture-document synchronization.

Each capability must remain deployable with empty materialized tables. Customer catalog/order behavior and existing admin inventory editing must continue working when background services are disabled.

## 12. Definition of Done

- The EF snapshot and migration contain exactly 24 physical tables: 17 existing plus seven new.
- Reviews enforce completed, owned `OrderItem` verification and one review per order item.
- Every inventory mutation has an atomic, immutable ledger row; completed orders convert reservations into sales once.
- Scheduled and manual jobs expose durable status, reject duplicate active locks, recover stale leases, and commit materialized batches atomically.
- Forecast and alert APIs expose deterministic 7/14-day results and safety-stock-aware alerts.
- Recommendations support global, user, and similar-product scopes with server-side fallback and anonymous-session merge.
- Approved customer/admin UI placement, accessibility states, automated tests, seed/demo data, OpenAPI, ERD, DFD, sitemap, requirements, and README are synchronized.
- Existing backend and frontend regression suites remain green.
