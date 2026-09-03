# Verified Reviews Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cho phép khách hàng tạo và sửa review 1–5 sao cho đúng `OrderItem` thuộc đơn `Completed`, đồng thời hiển thị aggregate/list/form ở product detail và action ở order detail.

**Architecture:** `Review` giữ invariant rating/comment; endpoint tự suy ra product/user từ order item và database unique `order_item_id` là hàng rào race cuối cùng. API product trả aggregate/list và eligibility riêng; order detail enrich từng item để UI không tự suy luận business rule.

**Tech Stack:** .NET 10, C# 14, EF Core 10.0.9, MySQL 8.4, ASP.NET Core Minimal API, xUnit, React 19.2.8, TypeScript 5.9.3, Vitest 4.1.10, React Testing Library 16.3.2.

**Spec:** `docs/superpowers/specs/2026-09-03-reviews-inventory-intelligence-design.md`

## Global Constraints

- Unique review key là `order_item_id`, không phải `(user_id, product_id)`.
- Một `OrderItem` có `Quantity > 1` vẫn chỉ được một review; order khác tạo eligibility khác.
- Request không nhận `userId` hoặc `productId`; backend lấy JWT user và suy ra product từ `OrderItem`.
- Chỉ `OrderStatus.Completed` đủ điều kiện; `Delivered` chưa đủ.
- Rating 1–5; comment trim, nullable, tối đa 2.000 ký tự.
- Owner được sửa không giới hạn thời gian; không có delete/release endpoint trong MVP.
- Public list không trả email hoặc dữ liệu định danh ngoài display name.
- Mỗi task theo RED -> GREEN -> REFACTOR -> COMMIT; không stage thay đổi ngoài scope.

---

## File Structure

- Create `backend/src/OnlineSupermarket.Domain/Reviews/Review.cs`: review aggregate.
- Create `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs`: table/FK/index mapping.
- Modify `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`: `Reviews` DbSet.
- Generate migration `AddReviews` and update model snapshot.
- Create `backend/src/OnlineSupermarket.Api/Contracts/Review/ReviewContracts.cs`: request/response records.
- Create `backend/src/OnlineSupermarket.Api/Endpoints/ReviewEndpoints.cs`: public/customer handlers.
- Modify `backend/src/OnlineSupermarket.Api/Contracts/Order/OrderContracts.cs`: item id/eligibility fields.
- Modify `backend/src/OnlineSupermarket.Api/Endpoints/OrderEndpoints.cs`: batch-project review state.
- Modify `backend/src/OnlineSupermarket.Api/Program.cs`: `MapReviewEndpoints`.
- Create `frontend/src/api/reviewApi.ts`: typed clients.
- Create `frontend/src/api/reviewApi.test.ts`: contract tests.
- Create `frontend/src/features/reviews/ProductReviews.tsx`: aggregate/list state.
- Create `frontend/src/features/reviews/ReviewForm.tsx`: create/edit state.
- Create `frontend/src/features/reviews/ProductReviews.css`: scoped styling.
- Modify `frontend/src/features/products/ProductDetailPage.tsx`: mount review block.
- Modify `frontend/src/features/orders/OrderDetailPage.tsx`: per-item actions.
- Modify `frontend/src/api/orderApi.ts`: DTO fields.
- Create/modify corresponding xUnit and Vitest test files listed per task.

---

### Task 1: Review domain invariants

**Files:**
- Create: `backend/src/OnlineSupermarket.Domain/Reviews/Review.cs`
- Create: `backend/tests/OnlineSupermarket.Domain.Tests/Reviews/ReviewTests.cs`

**Interfaces:**
- Produces: `Review.Create(Guid userId, Guid orderItemId, Guid productId, int rating, string? comment): Review`.
- Produces: `Review.Update(int rating, string? comment): void`.
- Produces: `Id`, `UserId`, `OrderItemId`, `ProductId`, `Rating`, `Comment`, `CreatedAtUtc`, `UpdatedAtUtc`.

- [ ] **Step 1: Viết failing boundary tests**

```csharp
[Theory]
[InlineData(0)]
[InlineData(6)]
public void Create_WithRatingOutsideOneToFive_Throws(int rating)
{
    Assert.Throws<ArgumentOutOfRangeException>(() => Review.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), rating, "Ổn"));
}

[Fact]
public void Create_TrimsComment_AndUpdateRefreshesTimestamp()
{
    var review = Review.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4, "  Hàng tốt  ");
    var created = review.CreatedAtUtc;

    review.Update(5, "  Rất tốt  ");

    Assert.Equal("Rất tốt", review.Comment);
    Assert.True(review.UpdatedAtUtc >= created);
}

[Fact]
public void Create_WithCommentOverTwoThousandCharacters_Throws()
{
    Assert.Throws<ArgumentException>(() => Review.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, new string('x', 2_001)));
}
```

- [ ] **Step 2: Chạy test xác nhận RED**

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~ReviewTests"
```

- [ ] **Step 3: Implement entity tối thiểu**

```csharp
private static string? NormalizeComment(string? comment)
{
    var value = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    if (value?.Length > 2_000)
        throw new ArgumentException("Comment cannot exceed 2000 characters.", nameof(comment));
    return value;
}

private static void EnsureRating(int rating)
{
    if (rating is < 1 or > 5)
        throw new ArgumentOutOfRangeException(nameof(rating));
}
```

Use private EF constructor, `Guid.NewGuid()`, and UTC timestamps following existing entities.

- [ ] **Step 4: Chạy GREEN và commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Domain.Tests/OnlineSupermarket.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~ReviewTests"
git add backend/src/OnlineSupermarket.Domain/Reviews backend/tests/OnlineSupermarket.Domain.Tests/Reviews
git commit -m "feat(reviews): add review domain model"
```

---

### Task 2: Persistence, unique constraint và migration

**Files:**
- Create: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs`
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs`
- Generate: EF migration named `AddReviews` and its designer.
- Modify: `backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs`

**Interfaces:**
- Produces: `AppDbContext.Reviews`.
- Produces: unique `ix_reviews_order_item_id`.
- Produces: query indexes `(product_id, created_at_utc)` and `user_id`.

- [ ] **Step 1: Viết failing model test**

```csharp
[Fact]
public void Review_HasUniqueOrderItemAndRestrictRelationships()
{
    using var context = CreateContext();
    var entity = context.Model.FindEntityType(typeof(Review))!;

    Assert.Equal("reviews", entity.GetTableName());
    Assert.True(entity.GetIndexes().Single(i =>
        i.Properties.Single().Name == nameof(Review.OrderItemId)).IsUnique);
    Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
}
```

- [ ] **Step 2: Add DbSet/configuration**

```csharp
public DbSet<Review> Reviews => Set<Review>();
```

```csharp
builder.ToTable("reviews", table =>
    table.HasCheckConstraint("ck_reviews_rating", "rating >= 1 AND rating <= 5"));
builder.HasKey(x => x.Id);
builder.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(2000);
builder.HasIndex(x => x.OrderItemId).IsUnique().HasDatabaseName("ix_reviews_order_item_id");
builder.HasIndex(x => new { x.ProductId, x.CreatedAtUtc }).HasDatabaseName("ix_reviews_product_created");
```

Map the three Restrict FKs to `User`, `OrderItem`, and `Product`.

- [ ] **Step 3: Generate migration and inspect it**

```powershell
dotnet ef migrations add AddReviews --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
rg -n "CreateTable|reviews|ck_reviews_rating|ix_reviews_order_item_id" backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations
```

Expected: one new table; unique index and rating check exist; no other table is recreated.

- [ ] **Step 4: Run GREEN and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ModelConfigurationTests"
git add backend/src/OnlineSupermarket.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/AppDbContext.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddReviews.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/*_AddReviews.Designer.cs backend/src/OnlineSupermarket.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs backend/tests/OnlineSupermarket.Api.Tests/Persistence/ModelConfigurationTests.cs
git commit -m "feat(reviews): persist verified reviews"
```

---

### Task 3: Review API contract and verified-purchase enforcement

**Files:**
- Create: `backend/src/OnlineSupermarket.Api/Contracts/Review/ReviewContracts.cs`
- Create: `backend/src/OnlineSupermarket.Api/Endpoints/ReviewEndpoints.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Program.cs`
- Create: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/ReviewEndpointsTests.cs`

**Interfaces:**
- Produces: `GET /api/products/{productId}/reviews?page=1&pageSize=10`.
- Produces: `GET /api/products/{productId}/review-eligibility`.
- Produces: `POST /api/reviews` with `CreateReviewRequest(Guid OrderItemId, int Rating, string? Comment)`.
- Produces: `PUT /api/reviews/{reviewId}` with `UpdateReviewRequest(int Rating, string? Comment)`.

- [ ] **Step 1: Viết failing verified-purchase tests**

```csharp
[Theory]
[InlineData("Pending")]
[InlineData("Delivered")]
public async Task CreateReview_WhenOrderNotCompleted_ReturnsConflict(string status)
{
    var fixture = await SeedOrderItemAsync(status, ownedByCaller: true);
    using var client = await CreateCustomerClientAsync(fixture.UserId);

    var response = await client.PostAsJsonAsync("/api/reviews",
        new { orderItemId = fixture.OrderItemId, rating = 5, comment = "Tốt" });

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}

[Fact]
public async Task CreateReview_DerivesProductAndRejectsDuplicateOrderItem()
{
    var fixture = await SeedOrderItemAsync("Completed", ownedByCaller: true);
    using var client = await CreateCustomerClientAsync(fixture.UserId);

    var first = await client.PostAsJsonAsync("/api/reviews",
        new { orderItemId = fixture.OrderItemId, rating = 5, comment = "Tốt" });
    var second = await client.PostAsJsonAsync("/api/reviews",
        new { orderItemId = fixture.OrderItemId, rating = 4, comment = "Khá" });

    Assert.Equal(HttpStatusCode.Created, first.StatusCode);
    Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    Assert.Equal(fixture.ProductId, (await first.Content.ReadFromJsonAsync<ReviewDto>())!.ProductId);
}
```

Add explicit tests for 401, wrong owner 404, rating/comment 400, edit owner 200, edit non-owner 403, public pagination, aggregate average, and inactive/missing product 404.

- [ ] **Step 2: Chạy test xác nhận RED**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ReviewEndpointsTests"
```

- [ ] **Step 3: Implement contracts and grouped endpoints**

```csharp
public sealed record ReviewDto(
    Guid Id, Guid ProductId, string ReviewerName, int Rating,
    string? Comment, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record ProductReviewsDto(
    decimal AverageRating, int ReviewCount,
    IReadOnlyList<ReviewDto> Data, int Page, int PageSize, int TotalCount);

public sealed record ReviewEligibilityDto(
    bool CanReview, Guid? OrderItemId, Guid? ReviewId);
```

For create, join `OrderItems` to `Orders`, filter by request order item and current user, require `Completed`, then call `Review.Create` with the stored product ID. Check existing review before add and translate only the unique-order-item database violation to `409 REVIEW_ALREADY_EXISTS`.

For eligibility, order the caller's completed matching order items by
`Order.CreatedAtUtc` descending. Return the newest item without a review. If all
eligible items are reviewed, return the most recently updated owned `reviewId`
for edit with `canReview = false`.

- [ ] **Step 4: Chạy GREEN and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ReviewEndpointsTests"
git add backend/src/OnlineSupermarket.Api/Contracts/Review backend/src/OnlineSupermarket.Api/Endpoints/ReviewEndpoints.cs backend/src/OnlineSupermarket.Api/Program.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/ReviewEndpointsTests.cs
git commit -m "feat(reviews): expose verified review api"
```

---

### Task 4: Enrich order detail with exact review state

**Files:**
- Modify: `backend/src/OnlineSupermarket.Api/Contracts/Order/OrderContracts.cs`
- Modify: `backend/src/OnlineSupermarket.Api/Endpoints/OrderEndpoints.cs`
- Create: `backend/tests/OnlineSupermarket.Api.Tests/Endpoints/OrderEndpointsTests.cs`

**Interfaces:**
- Changes: `OrderItemDto` adds `OrderItemId`, `CanReview`, `ReviewId`.
- Consumes: `AppDbContext.Reviews` from Task 2.
- Changes: synchronous `MapToDetailDto` becomes `MapToDetailDtoAsync(Order, AppDbContext, CancellationToken)` and both customer/Admin detail handlers await it.

- [ ] **Step 1: Write failing DTO test**

```csharp
[Fact]
public async Task GetCompletedOrder_ReturnsReviewStatePerOrderItem()
{
    var fixture = await SeedCompletedOrderWithOneReviewedAndOneUnreviewedItemAsync();
    using var client = await CreateCustomerClientAsync(fixture.UserId);

    var order = await client.GetFromJsonAsync<OrderDetailDto>($"/api/orders/{fixture.OrderId}");

    Assert.False(order!.Items.Single(x => x.OrderItemId == fixture.ReviewedItemId).CanReview);
    Assert.NotNull(order.Items.Single(x => x.OrderItemId == fixture.ReviewedItemId).ReviewId);
    Assert.True(order.Items.Single(x => x.OrderItemId == fixture.UnreviewedItemId).CanReview);
}
```

- [ ] **Step 2: Replace per-item lookup with one batch query**

Load payment and review IDs for all order-item IDs before mapping. Do not issue a database query inside `Select`. Convert both detail handlers to await `MapToDetailDtoAsync`; this also removes the current synchronous payment query from the mapper.

```csharp
public sealed record OrderItemDto(
    Guid OrderItemId, Guid ProductId, string ProductName, string Sku,
    decimal UnitPrice, int Quantity, decimal LineTotal,
    bool CanReview, Guid? ReviewId);
```

`CanReview` is true only when order status is `Completed` and no review exists for that exact item.

- [ ] **Step 3: Run regression and commit**

```powershell
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~OrderEndpointsTests"
git add backend/src/OnlineSupermarket.Api/Contracts/Order/OrderContracts.cs backend/src/OnlineSupermarket.Api/Endpoints/OrderEndpoints.cs backend/tests/OnlineSupermarket.Api.Tests/Endpoints/OrderEndpointsTests.cs
git commit -m "feat(orders): expose item review eligibility"
```

---

### Task 5: Typed frontend review API

**Files:**
- Create: `frontend/src/api/reviewApi.ts`
- Create: `frontend/src/api/reviewApi.test.ts`
- Modify: `frontend/src/api/orderApi.ts`

**Interfaces:**
- Produces: `reviewApi.getProductReviews`, `getEligibility`, `createReview`, `updateReview`.
- Changes: TypeScript `OrderItemDto` mirrors backend review fields.

- [ ] **Step 1: Write failing request-shape tests**

```typescript
it('creates a review without client-supplied userId or productId', async () => {
  fetchMock.mockResolvedValueOnce(jsonResponse(reviewDto, 201))

  await reviewApi.createReview(token, {
    orderItemId: 'item-1', rating: 5, comment: 'Tốt',
  })

  expect(fetchMock).toHaveBeenCalledWith('/api/reviews', expect.objectContaining({
    method: 'POST',
    body: JSON.stringify({ orderItemId: 'item-1', rating: 5, comment: 'Tốt' }),
  }))
})
```

- [ ] **Step 2: Implement DTOs and methods**

```typescript
export type ReviewEligibilityDto = {
  canReview: boolean
  orderItemId: string | null
  reviewId: string | null
}

export const reviewApi = {
  getEligibility: (productId: string, token: string, signal?: AbortSignal) =>
    getJson<ReviewEligibilityDto>(`/products/${productId}/review-eligibility`, { token, signal }),
  createReview: (token: string, body: CreateReviewRequest) =>
    postJson<ReviewDto>('/reviews', body, { token }),
  updateReview: (reviewId: string, token: string, body: UpdateReviewRequest) =>
    putJson<ReviewDto>(`/reviews/${reviewId}`, body, { token }),
}
```

- [ ] **Step 3: Run tests and commit**

```powershell
npm --prefix frontend test -- --run src/api/reviewApi.test.ts src/api/orderApi.test.ts
git add frontend/src/api/reviewApi.ts frontend/src/api/reviewApi.test.ts frontend/src/api/orderApi.ts frontend/src/api/orderApi.test.ts
git commit -m "feat(frontend): add review api contracts"
```

---

### Task 6: Product review aggregate, list, and create/edit form

**Files:**
- Create: `frontend/src/features/reviews/ProductReviews.tsx`
- Create: `frontend/src/features/reviews/ReviewForm.tsx`
- Create: `frontend/src/features/reviews/ProductReviews.css`
- Create: `frontend/src/features/reviews/ProductReviews.test.tsx`
- Modify: `frontend/src/features/products/ProductDetailPage.tsx`
- Modify: `frontend/src/features/products/ProductDetail.test.tsx`

**Interfaces:**
- Consumes: `productId`, optional token, and Task 5 API.
- Produces: accessible region labelled `Đánh giá sản phẩm` and controlled form.

- [ ] **Step 1: Write failing UI state tests**

```typescript
it('shows create form only for an eligible completed purchase', async () => {
  reviewApi.getProductReviews.mockResolvedValue(reviewsResponse)
  reviewApi.getEligibility.mockResolvedValue({ canReview: true, orderItemId: 'oi-1', reviewId: null })

  render(<ProductReviews productId="p-1" />)

  expect(await screen.findByRole('heading', { name: 'Đánh giá sản phẩm' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Gửi đánh giá' })).toBeEnabled()
})

it('renders aggregate and retries a failed list request', async () => {
  reviewApi.getProductReviews
    .mockRejectedValueOnce(new Error('network'))
    .mockResolvedValueOnce(reviewsResponse)

  render(<ProductReviews productId="p-1" />)
  await user.click(await screen.findByRole('button', { name: 'Thử tải lại đánh giá' }))

  expect(await screen.findByText('4,5/5')).toBeInTheDocument()
})
```

Cover guest, ineligible, edit, validation, submit conflict, empty, loading, and pagination states.

- [ ] **Step 2: Implement components and mount below product content**

Use `<fieldset>` for 1–5 rating controls, a labelled textarea with `maxLength={2000}`, live remaining count, and `aria-live` success/error feedback. On successful create/update, reload aggregate/list and eligibility rather than manually guessing server state.

- [ ] **Step 3: Run component tests/build and commit**

```powershell
npm --prefix frontend test -- --run src/features/reviews/ProductReviews.test.tsx src/features/products/ProductDetail.test.tsx
npm --prefix frontend run build
git add frontend/src/features/reviews frontend/src/features/products/ProductDetailPage.tsx frontend/src/features/products/ProductDetail.test.tsx
git commit -m "feat(frontend): add product reviews experience"
```

---

### Task 7: Order-detail review actions

**Files:**
- Modify: `frontend/src/features/orders/OrderDetailPage.tsx`
- Modify: `frontend/src/features/orders/OrderDetailPage.test.tsx`
- Modify: `frontend/src/features/orders/OrderHistoryPage.css`

**Interfaces:**
- Consumes: `orderItemId`, `canReview`, `reviewId`, `productId` from order DTO.
- Produces: product link with review intent and reviewed/edit state.

- [ ] **Step 1: Write failing action tests**

```typescript
it('shows review action only for eligible items', async () => {
  orderApi.getOrder.mockResolvedValue(completedOrderWithEligibility)
  renderOrderDetail()

  expect(await screen.findByRole('link', { name: 'Viết đánh giá Sản phẩm A' }))
    .toHaveAttribute('href', '/product/p-1?reviewOrderItemId=oi-1#reviews')
  expect(screen.getByRole('link', { name: 'Sửa đánh giá Sản phẩm B' }))
    .toHaveAttribute('href', '/product/p-2?reviewId=r-2#reviews')
})
```

- [ ] **Step 2: Implement semantic item actions**

Extend `OrderItemRow` props to include review fields. Use normal links so deep links survive refresh. Product page may use the query only to focus the form; backend eligibility remains authoritative.

- [ ] **Step 3: Run review regression and commit**

```powershell
npm --prefix frontend test -- --run src/features/orders/OrderDetailPage.test.tsx src/features/products/ProductDetail.test.tsx src/features/reviews/ProductReviews.test.tsx
dotnet test backend/tests/OnlineSupermarket.Api.Tests/OnlineSupermarket.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ReviewEndpointsTests|FullyQualifiedName~OrderEndpointsTests"
git add frontend/src/features/orders/OrderDetailPage.tsx frontend/src/features/orders/OrderDetailPage.test.tsx frontend/src/features/orders/OrderHistoryPage.css
git commit -m "feat(frontend): link completed order items to reviews"
```
