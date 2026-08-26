# Product Detail & Branch Switching Design

**Date:** 2026-08-26
**Status:** Approved for implementation planning
**Scope:** Frontend-only, using the existing backend contracts

## Context

The storefront already provides product browsing at `/`, `/browse`, and `/products`. Branch selection on the browse page is stored in the URL as `branchId`, but product cards do not currently navigate to a product detail route.

The backend and frontend API layer already support the required detail request:

- `GET /api/branches` returns active branches.
- `GET /api/products/{id}` returns base product information.
- `GET /api/products/{id}?branchId={guid}` additionally returns `branchInventory` when that product is stocked at the selected branch.
- `catalogApi.getProductById(id, branchId, options)` already wraps the product detail endpoint.

The implementation must use the live C# response contract and the existing TypeScript types: `ProductDetailDto.branchInventory` and `BranchInventoryDto.onHand`.

## Goals

- Add a public product detail page at `/product/:id`.
- Let users move from browse cards to detail pages using native links.
- Preserve the currently selected browse branch in the detail URL.
- Let users select or change a branch on the detail page.
- Refresh the branch-specific selling price and available quantity after a branch change.
- Provide accessible loading, not-found, error, no-branch, unavailable-at-branch, and ready states.
- Keep the page responsive and consistent with the existing storefront design.

## Non-goals

- No backend or database changes.
- No new product gallery or structured technical specification model.
- No cart, compare, review, promotion, or checkout integration.
- No global branch context and no `localStorage` persistence.
- No unrelated refactoring of the browse or application shell features.

## Chosen Architecture

The URL is the source of truth for branch selection:

```text
/product/{productId}
/product/{productId}?branchId={branchId}
```

This matches the existing `ProductBrowsePage` URL-state pattern and makes branch-specific pages refreshable, shareable, navigable with browser history, and easy to test. A global context would expand the scope and create premature coupling with cart and checkout behavior.

## Routing and Browse Navigation

`App.tsx` will register `ProductDetailPage` at `/product/:id`.

`ProductCard` will use React Router's `Link` rather than the current optional selection callback. Its props will include `branchId?: string` in addition to `branchName?: string`. The link destination will be:

- `/product/{id}` when no branch is selected.
- `/product/{id}?branchId={encodedBranchId}` when a branch is selected.

The card must not contain nested interactive controls. The current callback-only action button will become non-button link content within the single card link. This preserves middle-click, copy-link, focus, keyboard activation, and browser-native semantics.

`ProductGrid` will accept and forward `branchId`, and will remove the unused `onSelectProduct` callback. `ProductBrowsePage` will pass `currentFilters.branchId` and `activeBranch?.name` to the grid.

## Product Detail Page

`ProductDetailPage` owns route parsing, URL query state, asynchronous data state, and page rendering.

It reads:

- `id` from `useParams()`.
- `branchId` from `useSearchParams()`.

On initial render it starts independent requests for the branch list and product detail. The product request passes the URL branch ID to the existing `catalogApi.getProductById` call. Both request flows use `AbortController`; cleanup aborts superseded or unmounted requests.

The ready page contains:

- A breadcrumb back to the product browser.
- Product image with the same graceful placeholder behavior as product cards.
- Product name, brand, category, SKU, and unit.
- Description, or a neutral fallback when it is absent.
- A branch selector populated from `GET /api/branches`.
- A price and availability panel driven by the selected branch response.

Only fields in the current `ProductDetailDto` are rendered. SKU, unit, brand, and category form a compact product-information list; they are not presented as a fabricated technical-specification dataset.

## Branch Selection and Data Flow

The branch selector includes a first option named `Chọn kho` with an empty value.

When the selection changes:

1. Build a new `URLSearchParams` value while preserving unrelated query parameters.
2. Set or remove `branchId`.
3. Update the URL with `setSearchParams`.
4. Let the product-detail effect refetch using the new branch ID.

Changing branch does not call `/api/branches/{id}/inventory`; the product-detail endpoint is the authoritative source for the selected product's branch price and availability.

If a non-empty URL branch ID is not present in the active branch list, the page removes it from the URL with history replacement and returns to the no-branch state. This prevents an invalid or inactive branch from remaining selected.

## Price and Inventory Rules

The UI derives its display as follows:

| State | Price | Availability message |
|---|---|---|
| No branch selected | `basePrice` | `Chọn kho để xem giá và tồn kho` |
| Branch selected and `branchInventory.availableQuantity > 0` | `branchInventory.sellingPrice` | `Còn {availableQuantity} sản phẩm tại kho` |
| Branch selected and `branchInventory.availableQuantity === 0` | `branchInventory.sellingPrice` | `Tạm hết hàng tại kho này` |
| Branch selected and `branchInventory === null` | `basePrice` | `Sản phẩm không có tại kho này` |

The frontend will not infer a low-stock threshold because the detail response does not include `reorderLevel`.

## Loading and Error Handling

- **Initial loading:** Show a detail-page skeleton with `aria-busy="true"`.
- **Branch change loading:** Reuse the loading state and disable branch interaction until the current detail request settles.
- **Product 404:** Show a dedicated `Không tìm thấy sản phẩm` state with a link back to `/browse`.
- **Other product errors:** Show an alert containing a safe Vietnamese message and a `Thử lại` action.
- **Branch-list error:** Keep product information usable, show an inline branch-selector error, and provide a branch-list retry action.
- **Missing image or image load failure:** Show the existing product image placeholder style.
- **Stale requests:** Abort old requests so a slower previous branch response cannot overwrite the current selection.

The page must not silently fall back to mock product data when the live API fails.

## Styling and Responsiveness

The page will use a dedicated `ProductDetailPage.css` file and the existing global color tokens. Desktop layout uses a two-column image/content composition. At mobile widths it becomes a single column, keeps the branch selector and price panel easy to reach, and avoids horizontal overflow at the repository's 320 px minimum viewport.

Focus states, labels, alert roles, image alternative text, and link semantics are required. Decorative icons must be hidden from assistive technology.

## Files

### Create

- `frontend/src/features/products/ProductDetailPage.tsx` — route page, fetch orchestration, detail states, branch selection, and rendering.
- `frontend/src/features/products/ProductDetailPage.css` — responsive detail-page styles.
- `frontend/src/features/products/ProductDetail.test.tsx` — route, detail, branch switching, inventory, loading, and error behavior.
- `frontend/src/api/catalogApi.test.ts` — verifies the existing detail client builds base and branch-specific request URLs.

### Modify

- `frontend/src/App.tsx` — register `/product/:id`.
- `frontend/src/features/products/ProductCard.tsx` — replace callback navigation with `Link` and add `branchId?: string`.
- `frontend/src/features/products/ProductCard.css` — preserve card presentation and visible focus behavior for the link.
- `frontend/src/features/products/ProductGrid.tsx` — forward `branchId` and remove the selection callback.
- `frontend/src/features/products/ProductBrowsePage.tsx` — pass the selected branch ID into the grid.
- `frontend/src/features/products/ProductBrowse.test.tsx` — update callback assertions to link-destination and branch-preservation assertions.

`frontend/src/api/catalogApi.ts` is consumed but does not require a new `getProductById` implementation because the method already exists.

## Testing Strategy

Tests use Vitest, Testing Library, and `MemoryRouter` following the existing product feature tests.

Required coverage:

1. `catalogApi.getProductById` requests `/products/{id}` without a branch and `/products/{id}?branchId=...` with a branch.
2. A product card renders an accessible link to the detail route.
3. The product-card link preserves the selected `branchId`.
4. `/product/:id` requests and renders base product details when no branch is selected.
5. A branch-specific deep link calls the detail API with that branch and shows selling price and available quantity.
6. Changing the branch updates the query string and refetches product detail.
7. Clearing the branch removes `branchId` and restores the base-price prompt.
8. `branchInventory === null` renders the unavailable-at-branch state.
9. Zero availability renders the out-of-stock state.
10. Product 404 renders the dedicated not-found state.
11. Other API failures render retry behavior.
12. Branch-list failure does not hide otherwise valid product details.
13. Image failure renders a placeholder.
14. Existing browse tests remain green after callback navigation is removed.

Verification commands:

```powershell
Set-Location frontend
npm.cmd test -- --run
npm.cmd run build
```

## Acceptance Criteria

- Visiting `/product/{validId}` renders current backend product data without authentication.
- Visiting `/product/{validId}?branchId={validBranchId}` renders that branch's selling price and availability.
- Changing the branch updates the URL and visible price/stock without a full-page navigation.
- Refresh, back, forward, copy-link, middle-click, and keyboard activation work through native URL and link behavior.
- Product cards opened from a filtered branch preserve the branch ID.
- No selected branch shows the base price and a clear selection prompt.
- Missing branch inventory is distinguished from zero available inventory.
- Invalid product IDs and API failures have explicit recovery paths.
- No backend contract, database schema, cart, compare, or global branch state is added.
- All frontend tests and the production build pass.
