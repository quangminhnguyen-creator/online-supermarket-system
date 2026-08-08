# Full Data Model Persistence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mở rộng domain model và EF Core persistence từ 5 bảng foundation thành đúng 22 bảng trong ERD đã duyệt, kèm migration MySQL và kiểm thử ràng buộc quan trọng.

**Architecture:** Mỗi module nghiệp vụ đặt entity và enum trong một feature folder của `OnlineSupermarket.Domain`; toàn bộ EF mapping nằm trong `OnlineSupermarket.Infrastructure/Persistence/Configurations`. Các task thêm model và mapping nhưng chưa thêm HTTP API, payment adapter hoặc thuật toán AI. Chỉ một người tạo migration tổng cuối cùng để tránh xung đột snapshot.

**Tech Stack:** .NET SDK 10.0.103, C# 14, ASP.NET Core 10, EF Core 10.0.9, MySql.EntityFrameworkCore 10.0.9, xUnit, MySQL 8.4.

## Global Constraints

- Nguồn sự thật là `docs/architecture/erd.md`, đúng 22 bảng.
- Năm bảng đã tồn tại: `branches`, `categories`, `brands`, `products`, `branch_inventories`.
- So sánh sản phẩm dùng `localStorage`; không tạo bảng comparison.
- Coupon nằm trong `promotions.code`; không tạo `coupons`, `promotion_products` hoặc `promotion_categories`.
- Mỗi cart và order thuộc đúng một branch; một order áp dụng tối đa một promotion.
- UUID lưu `char(36)`, tiền dùng `decimal(18,2)`, thời gian UTC dùng `datetime(6)`.
- Domain không tham chiếu EF Core; mapping chỉ dùng Fluent API trong Infrastructure.
- Foreign key mặc định `Restrict`; chỉ child thuần sở hữu được `Cascade`.
- Không lưu token thô hoặc dữ liệu thẻ; refresh token chỉ lưu hash.
- Hành vi mới phải đi theo test đỏ → code tối thiểu → test xanh → commit.
- Chỉ Thành viên A hoặc người được chỉ định chạy `dotnet ef migrations add`.

---

## File Map

```text
backend/src/OnlineSupermarket.Domain/
  Identity/                         users, addresses, refresh_tokens
  Inventory/                       branch inventory behavior, inventory_transactions
  Carts/                           carts, cart_items
  Promotions/                      promotions và coupon toàn đơn
  Orders/                          orders, order_items, order_status_histories
  Payments/                        payments, payment_callbacks
  Reviews/                         reviews
  Intelligence/                    view events, recommendations, forecasts, alerts

backend/src/OnlineSupermarket.Infrastructure/Persistence/
  AppDbContext.cs                  DbSet cho đủ 22 entity
  Configurations/                  một IEntityTypeConfiguration mỗi entity
  Migrations/                      migration ExpandFullDataModel và snapshot

backend/tests/OnlineSupermarket.Domain.Tests/
  Identity/ Inventory/ Carts/ Promotions/ Orders/ Payments/ Reviews/ Intelligence/

backend/tests/OnlineSupermarket.Api.Tests/Persistence/
  FullModelConfigurationTests.cs   kiểm tra table, key, index, precision, delete behavior
```

### Task 1: Identity persistence model

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Identity/User.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Identity/UserRole.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Identity/UserStatus.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Identity/Address.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Identity/RefreshToken.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/AddressConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Identity/IdentityEntityTests.cs`
- Test: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/FullModelConfigurationTests.cs`

**Interfaces:**
- Produces: `User.Create(string email, string passwordHash, string fullName, string? phone, UserRole role)`.
- Produces: `User.ChangeStatus(UserStatus status)`.
- Produces: `Address.Create(Guid userId, string recipientName, string recipientPhone, string province, string district, string ward, string street, bool isDefault)`.
- Produces: `RefreshToken.Issue(Guid userId, string tokenHash, DateTime expiresAtUtc)` and `Revoke(DateTime revokedAtUtc, Guid? replacedByTokenId)`.
- Enum values: `UserRole.Customer`, `UserRole.Admin`; `UserStatus.Active`, `UserStatus.Locked`, `UserStatus.Disabled`.

- [ ] **Step 1: Write failing domain tests**

```csharp
[Fact]
public void IssueRefreshToken_StoresHashAndExpiry()
{
    var expiry = DateTime.UtcNow.AddDays(7);
    var token = RefreshToken.Issue(Guid.NewGuid(), "sha256-token-hash", expiry);
    Assert.Equal("sha256-token-hash", token.TokenHash);
    Assert.Equal(expiry, token.ExpiresAtUtc);
    Assert.Null(token.RevokedAtUtc);
}

[Fact]
public void CreateUser_WithBlankEmail_Throws()
{
    Assert.Throws<ArgumentException>(() =>
        User.Create(" ", "valid-password-hash", "Nguyen Van A", null, UserRole.Customer));
}
```

- [ ] **Step 2: Run the focused tests and observe failure**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests --filter "FullyQualifiedName~IdentityEntityTests"`

Expected: FAIL because Identity types do not exist.

- [ ] **Step 3: Implement entities and invariants**

Implement the exact factories above. Normalize email with `Trim().ToLowerInvariant()`. Reject blank email, password hash, full name, token hash, recipient name, phone and address components. `RefreshToken.Issue` must reject an expiry at or before creation; `RefreshToken.Revoke` must reject a second revocation.

- [ ] **Step 4: Add EF mappings and DbSets**

Map tables `users`, `addresses`, `refresh_tokens`. Add unique indexes for `users.email` and `refresh_tokens.token_hash`; add `(user_id, expires_at_utc)` index. Map `replaced_by_token_id` as an optional self-reference with `Restrict`. Store enums as strings with maximum length 20.

- [ ] **Step 5: Verify tests and commit**

Run:

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests --filter "FullyQualifiedName~Identity"
dotnet test backend/tests/OnlineSupermarket.Api.Tests --filter "FullyQualifiedName~FullModelConfigurationTests"
```

Expected: all Identity and mapping tests PASS.

Commit:

```powershell
git add backend/src/OnlineSupermarket.Domain/Identity backend/src/OnlineSupermarket.Infrastructure/Persistence backend/tests
git commit -m "feat: add identity persistence model"
```

### Task 2: Inventory transaction ledger and complete stock behavior

**Files:**
- Modify: `backend/src/OnlineSupermarket.Domain/Inventory/BranchInventory.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Inventory/InventoryTransaction.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Inventory/InventoryTransactionType.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `backend/tests/OnlineSupermarket.Domain.Tests/Inventory/BranchInventoryTests.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Inventory/InventoryTransactionTests.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/FullModelConfigurationTests.cs`

**Interfaces:**
- Produces: `BranchInventory.Release(int quantity)`, `CompleteSale(int quantity)`, `StockIn(int quantity)`, `AdjustTo(int quantityOnHand)`.
- Produces: `InventoryTransaction.Record(Guid branchInventoryId, InventoryTransactionType type, int quantityChange, int quantityAfter, Guid? performedByUserId, string? referenceType, Guid? referenceId, string? note)`.
- Enum values: `StockIn`, `Reserve`, `Release`, `Sale`, `Adjustment`.

- [ ] **Step 1: Add failing stock lifecycle tests**

```csharp
[Fact]
public void CompleteSale_DecreasesOnHandAndReservedTogether()
{
    var inventory = BranchInventory.Create(Guid.NewGuid(), Guid.NewGuid(), 25_000m, 10, 2);
    inventory.Reserve(3);
    inventory.CompleteSale(3);
    Assert.Equal(7, inventory.QuantityOnHand);
    Assert.Equal(0, inventory.ReservedQuantity);
    Assert.Equal(7, inventory.AvailableQuantity);
}

[Fact]
public void Release_MoreThanReserved_Throws()
{
    var inventory = BranchInventory.Create(Guid.NewGuid(), Guid.NewGuid(), 25_000m, 10, 2);
    inventory.Reserve(2);
    Assert.Throws<InvalidOperationException>(() => inventory.Release(3));
}
```

- [ ] **Step 2: Run tests and observe failure**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests --filter "FullyQualifiedName~Inventory"`

Expected: FAIL because the lifecycle methods and transaction entity do not exist.

- [ ] **Step 3: Implement stock invariants and immutable ledger entity**

All quantities passed to reserve/release/sale/stock-in must be positive. `CompleteSale` requires both sufficient reserved and on-hand quantity. `AdjustTo` rejects negative values and any value below `ReservedQuantity`. `InventoryTransaction` exposes no mutation method after creation.

- [ ] **Step 4: Map and verify inventory ledger**

Map `inventory_transactions`, enum as varchar(30), signed `quantity_change`, `(branch_inventory_id, created_at_utc)` and `(reference_type, reference_id)` indexes. Both optional user and inventory foreign keys use `Restrict`.

Run: `dotnet test OnlineSupermarket.slnx --no-restore`

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add backend/src/OnlineSupermarket.Domain/Inventory backend/src/OnlineSupermarket.Infrastructure/Persistence backend/tests
git commit -m "feat: add inventory transaction ledger"
```

### Task 3: Branch-bound cart model

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Carts/Cart.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Carts/CartItem.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/CartConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/CartItemConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Carts/CartEntityTests.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/FullModelConfigurationTests.cs`

**Interfaces:**
- Produces: `Cart.Create(Guid userId, Guid branchId)`.
- Produces: `CartItem.Create(Guid cartId, Guid productId, int quantity)` and `ChangeQuantity(int quantity)`.

- [ ] **Step 1: Write failing cart tests**

```csharp
[Fact]
public void ChangeQuantity_ToZero_Throws()
{
    var item = CartItem.Create(Guid.NewGuid(), Guid.NewGuid(), 1);
    Assert.Throws<ArgumentOutOfRangeException>(() => item.ChangeQuantity(0));
}
```

- [ ] **Step 2: Run the focused test and observe failure**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests --filter "FullyQualifiedName~CartEntityTests"`

Expected: FAIL because Cart types do not exist.

- [ ] **Step 3: Implement cart entities and mappings**

Reject empty user, branch, cart and product IDs and non-positive quantity. Map `carts` with unique `(user_id, branch_id)`. Map `cart_items` with unique `(cart_id, product_id)` and cascade delete only from cart to items; product deletion remains `Restrict`.

- [ ] **Step 4: Verify and commit**

Run: `dotnet test OnlineSupermarket.slnx --no-restore`

Expected: all tests PASS.

```powershell
git add backend/src/OnlineSupermarket.Domain/Carts backend/src/OnlineSupermarket.Infrastructure/Persistence backend/tests
git commit -m "feat: add branch-bound cart model"
```

### Task 4: Whole-order promotion and coupon rules

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Promotions/Promotion.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Promotions/DiscountType.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/PromotionConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Promotions/PromotionTests.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/FullModelConfigurationTests.cs`

**Interfaces:**
- Produces: `Promotion.Create(string name, string? code, DiscountType discountType, decimal discountValue, decimal minOrderAmount, decimal? maxDiscountAmount, int? usageLimit, int? perUserLimit, DateTime startAtUtc, DateTime endAtUtc)`.
- Produces: `decimal CalculateDiscount(decimal subtotal, DateTime nowUtc)`.
- Enum values: `Percentage`, `FixedAmount`.

- [ ] **Step 1: Write failing promotion tests**

```csharp
[Fact]
public void PercentageCoupon_RespectsMaximumDiscount()
{
    var promotion = Promotion.Create("Giảm 20%", "SALE20", DiscountType.Percentage,
        20m, 100_000m, 50_000m, 100, 1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
    Assert.Equal(50_000m, promotion.CalculateDiscount(500_000m, DateTime.UtcNow));
}

[Fact]
public void Create_WithEndBeforeStart_Throws()
{
    Assert.Throws<ArgumentException>(() => Promotion.Create("Sai thời gian", null,
        DiscountType.FixedAmount, 10_000m, 0m, null, null, null,
        DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-1)));
}
```

- [ ] **Step 2: Run tests and observe failure**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests --filter "FullyQualifiedName~PromotionTests"`

Expected: FAIL because Promotion types do not exist.

- [ ] **Step 3: Implement exact coupon constraints**

Normalize non-null code with `Trim().ToUpperInvariant()`. Percentage must be greater than 0 and at most 100; fixed amount must be positive. Money and limits cannot be negative. Return zero when inactive, outside the effective window or below minimum subtotal. Clamp discount to subtotal and optional maximum.

- [ ] **Step 4: Map, verify and commit**

Map `promotions.code` as unique nullable varchar(50), all money as decimal(18,2), enum as varchar(20), and index `(is_active, start_at_utc, end_at_utc)`.

Run: `dotnet test OnlineSupermarket.slnx --no-restore`

Expected: all tests PASS.

```powershell
git add backend/src/OnlineSupermarket.Domain/Promotions backend/src/OnlineSupermarket.Infrastructure/Persistence backend/tests
git commit -m "feat: add whole-order coupon model"
```

### Task 5: Order aggregate and status history

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Orders/Order.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Orders/OrderItem.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Orders/OrderStatusHistory.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Orders/OrderStatus.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Orders/PaymentStatus.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Orders/FulfillmentType.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/OrderItemConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/OrderStatusHistoryConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Orders/OrderTests.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/FullModelConfigurationTests.cs`

**Interfaces:**
- Produces: `Order.CreatePendingPayment(string orderNumber, Guid userId, Guid branchId, FulfillmentType fulfillmentType, string recipientName, string recipientPhone, string? deliveryAddressSnapshot, Guid? promotionId, string? promotionCodeSnapshot, decimal subtotal, decimal discountAmount, decimal shippingFee)`.
- Produces: `Confirm(PaymentStatus paymentStatus, Guid? changedByUserId, string? note)`, `StartPreparing(Guid? changedByUserId, string? note)`, `MarkReadyForPickup(Guid? changedByUserId, string? note)`, `StartShipping(Guid? changedByUserId, string? note)`, `Complete(Guid? changedByUserId, string? note)`, `Cancel(Guid? changedByUserId, string? note)` and `MarkPaymentFailed(string? note)`.
- Produces: `OrderItem.Create(Guid orderId, Guid productId, string sku, string name, string unit, decimal unitPrice, int quantity, decimal discountAmount)`.
- Status values: `PendingPayment`, `Confirmed`, `Preparing`, `ReadyForPickup`, `Shipping`, `Completed`, `Cancelled`, `PaymentFailed`.
- Payment status values: `Pending`, `PendingCollection`, `Paid`, `Failed`, `Cancelled`.
- Fulfillment values: `Delivery`, `Pickup`.

- [ ] **Step 1: Write failing transition and money tests**

```csharp
[Fact]
public void DeliveryOrder_TotalEqualsSubtotalMinusDiscountPlusShipping()
{
    var order = Order.CreatePendingPayment("ORD-20260808-0001", Guid.NewGuid(), Guid.NewGuid(),
        FulfillmentType.Delivery, "Nguyen Van A", "0900000000", "1 Nguyen Hue, HCM",
        null, null, 200_000m, 20_000m, 30_000m);
    Assert.Equal(210_000m, order.TotalAmount);
}

[Fact]
public void PendingOrder_CannotJumpDirectlyToCompleted()
{
    var order = Order.CreatePendingPayment("ORD-20260808-0002", Guid.NewGuid(), Guid.NewGuid(),
        FulfillmentType.Pickup, "Nguyen Van B", "0911111111", null,
        null, null, 100_000m, 0m, 0m);
    Assert.Throws<InvalidOperationException>(() => order.Complete(null, null));
}
```

- [ ] **Step 2: Run tests and observe failure**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests --filter "FullyQualifiedName~OrderTests"`

Expected: FAIL because Order types do not exist.

- [ ] **Step 3: Implement aggregate invariants**

Require delivery snapshot for `Delivery`; require no shipping fee for `Pickup`. Enforce `total = subtotal - discount + shipping` and non-negative total. Permit only these transitions: `PendingPayment -> Confirmed|PaymentFailed|Cancelled`, `Confirmed -> Preparing|Cancelled`, `Preparing -> ReadyForPickup|Shipping`, `ReadyForPickup|Shipping -> Completed`. Every transition creates an `OrderStatusHistory` with optional actor and note.

- [ ] **Step 4: Map order tables**

Map order number unique; order money decimal(18,2); enums varchar(30); order-to-items and order-to-history cascade; user, branch, promotion and product foreign keys restrict. Make `order_items.order_id` indexed and `reviews.order_item_id` available for a later unique relationship.

- [ ] **Step 5: Verify and commit**

Run: `dotnet test OnlineSupermarket.slnx --no-restore`

Expected: all tests PASS.

```powershell
git add backend/src/OnlineSupermarket.Domain/Orders backend/src/OnlineSupermarket.Infrastructure/Persistence backend/tests
git commit -m "feat: add order aggregate model"
```

### Task 6: Payment callback idempotency and verified reviews

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Payments/Payment.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Payments/PaymentCallback.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Payments/PaymentMethod.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Payments/PaymentProvider.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Payments/PaymentState.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Reviews/Review.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Reviews/ReviewStatus.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/PaymentCallbackConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Payments/PaymentTests.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Reviews/ReviewTests.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/FullModelConfigurationTests.cs`

**Interfaces:**
- Produces: `Payment.Create(Guid orderId, PaymentMethod method, PaymentProvider provider, string requestId, decimal amount)` and `MarkPaid(string providerTransactionId, DateTime paidAtUtc)`.
- Produces: `PaymentCallback.Record(Guid? paymentId, PaymentProvider provider, string externalEventId, string payloadHash, string responseCode, bool isSignatureValid, DateTime processedAtUtc)`.
- Produces: `Review.Create(Guid userId, Guid productId, Guid orderItemId, int rating, string? comment)`.

- [ ] **Step 1: Write failing payment and review tests**

```csharp
[Fact]
public void Payment_CannotBeMarkedPaidTwice()
{
    var payment = Payment.Create(Guid.NewGuid(), PaymentMethod.VNPay,
        PaymentProvider.VNPay, "request-001", 150_000m);
    payment.MarkPaid("provider-001", DateTime.UtcNow);
    Assert.Throws<InvalidOperationException>(() =>
        payment.MarkPaid("provider-002", DateTime.UtcNow));
}

[Theory]
[InlineData(0)]
[InlineData(6)]
public void Review_RejectsRatingOutsideOneToFive(int rating)
{
    Assert.Throws<ArgumentOutOfRangeException>(() =>
        Review.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), rating, null));
}
```

- [ ] **Step 2: Run focused tests and observe failure**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests --filter "FullyQualifiedName~PaymentTests|FullyQualifiedName~ReviewTests"`

Expected: FAIL because Payment and Review types do not exist.

- [ ] **Step 3: Implement state rules and mappings**

Support methods `COD`, `VNPay`, `MoMo`; providers `Internal`, `VNPay`, `MoMo`; payment states `Pending`, `Paid`, `Failed`, `Cancelled`. Enforce positive amount, immutable request ID, and one paid transition. Map unique request ID, unique nullable provider transaction ID, unique callback `(provider, external_event_id)`, and unique review `order_item_id` with rating check 1–5.

- [ ] **Step 4: Verify and commit**

Run: `dotnet test OnlineSupermarket.slnx --no-restore`

Expected: all tests PASS.

```powershell
git add backend/src/OnlineSupermarket.Domain/Payments backend/src/OnlineSupermarket.Domain/Reviews backend/src/OnlineSupermarket.Infrastructure/Persistence backend/tests
git commit -m "feat: add payment and review persistence"
```

### Task 7: AI event and result persistence

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/ProductViewEvent.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/RecommendationResult.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/DemandForecast.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/StockAlert.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/ForecastMethod.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/StockAlertLevel.cs`
- Create: `backend/src/OnlineSupermarket.Domain/Intelligence/StockAlertStatus.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/ProductViewEventConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/RecommendationResultConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/DemandForecastConfiguration.cs`
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/StockAlertConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Test: `backend/tests/OnlineSupermarket.Domain.Tests/Intelligence/IntelligenceEntityTests.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/FullModelConfigurationTests.cs`

**Interfaces:**
- Produces: `ProductViewEvent.Record(Guid? userId, string? anonymousSessionId, Guid productId, Guid branchId, DateTime viewedAtUtc)`.
- Produces: `RecommendationResult.Create(Guid userId, Guid productId, Guid branchId, decimal score, string reason, DateTime generatedAtUtc, DateTime expiresAtUtc)`.
- Produces: `DemandForecast.Create(Guid branchId, Guid productId, DateOnly forecastDate, decimal predictedQuantity, ForecastMethod method, DateTime generatedAtUtc)`.
- Produces: `StockAlert.Create(Guid branchId, Guid productId, Guid? demandForecastId, StockAlertLevel level, int availableQuantity, decimal predictedDemand, int recommendedQuantity)` and `Resolve(DateTime resolvedAtUtc)`.

- [ ] **Step 1: Write failing AI persistence tests**

```csharp
[Fact]
public void ViewEvent_RequiresUserOrAnonymousSession()
{
    Assert.Throws<ArgumentException>(() => ProductViewEvent.Record(
        null, null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
}

[Fact]
public void Forecast_RejectsNegativePrediction()
{
    Assert.Throws<ArgumentOutOfRangeException>(() => DemandForecast.Create(
        Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow),
        -1m, ForecastMethod.MovingAverage, DateTime.UtcNow));
}
```

- [ ] **Step 2: Run tests and observe failure**

Run: `dotnet test backend/tests/OnlineSupermarket.Domain.Tests --filter "FullyQualifiedName~IntelligenceEntityTests"`

Expected: FAIL because Intelligence types do not exist.

- [ ] **Step 3: Implement entities and mappings**

Require exactly one guest/customer identity for view events. Require non-negative scores, predictions and quantities; expiry after generation; one-way alert resolution. Map the four tables exactly as `product_view_events`, `recommendation_results`, `demand_forecasts` and `stock_alerts`. Map forecast method and alert enums as strings. Add unique `(branch_id, product_id, forecast_date)` and the indexes specified in the ERD.

- [ ] **Step 4: Verify and commit**

Run: `dotnet test OnlineSupermarket.slnx --no-restore`

Expected: all tests PASS.

```powershell
git add backend/src/OnlineSupermarket.Domain/Intelligence backend/src/OnlineSupermarket.Infrastructure/Persistence backend/tests
git commit -m "feat: add AI persistence model"
```

### Task 8: Generate the consolidated MySQL migration

**Files:**
- Generate: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_ExpandFullDataModel.cs`
- Generate: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_ExpandFullDataModel.Designer.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/FullModelConfigurationTests.cs`

**Interfaces:**
- Produces: 22 EF entity types and 22 MySQL tables after applying both migrations.
- Consumes: all domain entities and configurations from Tasks 1–7.

- [ ] **Step 1: Add the complete model-count test**

```csharp
[Fact]
public void Model_ContainsExactlyTwentyTwoApplicationTables()
{
    using var context = CreateContext();
    var tables = context.Model.GetEntityTypes()
        .Select(entity => entity.GetTableName())
        .Where(name => name is not null)
        .Distinct()
        .ToArray();
    Assert.Equal(22, tables.Length);
}
```

- [ ] **Step 2: Run the model test before migration**

Run: `dotnet test backend/tests/OnlineSupermarket.Api.Tests --filter Model_ContainsExactlyTwentyTwoApplicationTables`

Expected: PASS only when all model configurations from Tasks 1–7 are registered; otherwise stop and fix missing mappings before generating migration.

- [ ] **Step 3: Generate the migration once**

Run:

```powershell
dotnet tool restore
dotnet ef migrations add ExpandFullDataModel --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api --output-dir Persistence/Migrations
```

Expected: one migration adds exactly the 17 missing tables and required foreign keys/indexes; it must not drop or recreate the five foundation tables.

- [ ] **Step 4: Inspect and validate generated operations**

Run:

```powershell
Select-String -Path backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_ExpandFullDataModel.cs -Pattern 'CreateTable'
Select-String -Path backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_ExpandFullDataModel.cs -Pattern 'DropTable'
```

Expected: 17 `CreateTable` operations in `Up`; corresponding `DropTable` calls appear only in `Down`.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test OnlineSupermarket.slnx --no-restore
dotnet build OnlineSupermarket.slnx --no-restore
git diff --check
```

Expected: all tests PASS; build has 0 warnings and 0 errors; diff check is clean.

```powershell
git add backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations backend/tests/OnlineSupermarket.Api.Tests/Persistence
git commit -m "feat: add full data model migration"
```

### Task 9: Align documentation and perform final schema verification

**Files:**
- Modify: `docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md`
- Modify: `README.md`
- Verify: `docs/architecture/erd.md`

**Interfaces:**
- Documents: comparison remains client-side; coupon remains in `promotions`; exactly 22 tables.
- Documents: canonical order statuses and one-branch-per-order rule.

- [ ] **Step 1: Update the stale design spec**

Remove `PromotionProducts`, `PromotionCategories`, `Coupons` and database-backed comparison from the spec. Keep comparison as a Guest/Customer feature backed by `localStorage`. Replace the order flow with `PendingPayment -> Confirmed -> Preparing -> ReadyForPickup/Shipping -> Completed`, plus `PaymentFailed` and `Cancelled` branches.

- [ ] **Step 2: Add schema commands to README**

Document:

```powershell
dotnet tool restore
dotnet ef database update --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
```

- [ ] **Step 3: Run the final verification gate**

Run:

```powershell
dotnet test OnlineSupermarket.slnx --no-restore
dotnet build OnlineSupermarket.slnx --no-restore
dotnet list OnlineSupermarket.slnx package --vulnerable --include-transitive
git diff --check
git status --short
```

Expected: all tests PASS; build has 0 warnings/errors; no vulnerable NuGet packages; only intended documentation changes remain before commit.

- [ ] **Step 4: Commit**

```powershell
git add docs README.md
git commit -m "docs: align specification with full ERD"
```

## Completion Gate

- EF model contains exactly 22 application tables.
- Migration adds exactly 17 tables without rebuilding the five foundation tables.
- Every FK, unique key, money precision and required index in `docs/architecture/erd.md` is covered by a model test.
- Inventory reserve/release/sale invariants pass.
- Coupon calculation and order state transition tests pass.
- Callback idempotency constraints and verified-purchase review uniqueness are mapped.
- Backend tests and build pass with zero failures, warnings and errors.
- ERD, design spec, migration and AppDbContext agree on table names and scope.
