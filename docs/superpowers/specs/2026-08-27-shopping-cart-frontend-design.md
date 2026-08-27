# Shopping Cart Frontend Design

**Date:** 2026-08-27
**Status:** Approved for implementation planning
**Scope:** Frontend-only, using the existing authenticated cart contracts

## Context

The storefront already supports authentication, product browsing, and branch-aware product detail pages. The next dashboard item is a shopping-cart experience at `/shopping/cart`, backed by the existing authorized `/api/cart` endpoints.

The backend owns cart totals, branch assignment, stock validation, and all mutations. A cart belongs to one user and one branch. Changing its branch clears every cart item. The frontend must expose that behavior clearly and must not invent local cart totals or silently discard a cart.

The current cart response does not contain product image URLs. This design remains frontend-only, so the cart uses a neutral product placeholder and does not issue one product-detail request per item.

## Goals

- Add an authenticated cart experience at `/shopping/cart`.
- Add a cart link and server-backed total-quantity badge to the application header.
- Add quantity selection and `Thêm vào giỏ` behavior to `ProductDetailPage`.
- Support viewing, updating, removing, and clearing cart items.
- Support changing the cart branch with explicit confirmation before a non-empty cart is cleared.
- Keep every displayed cart, subtotal, line total, stock value, and item count authoritative to the latest accepted backend response.
- Prevent stale loads and racing mutations from overwriting newer cart state.
- Provide accessible loading, guest, error, empty, confirmation, mutation, and ready states.

## Non-goals

- No backend, database, or response-contract changes.
- No `imageUrl` addition to `CartDto` and no N+1 product-detail fetching.
- No anonymous or locally persisted cart.
- No automatic replay of a guest's add-to-cart action after login.
- No optimistic cart updates.
- No promotion, shipping, checkout, payment, inventory reservation, or order creation.
- No new state-management or UI dependency.
- No extraction of a shared stateful branch-selector component.

## Existing Backend Contract

All cart endpoints require a Bearer JWT. The frontend API base is already `/api`, so the client functions use paths beginning with `/cart`.

| Operation | HTTP request | Body | Success response |
|---|---|---|---|
| Load cart | `GET /api/cart` | None | `200 CartDto` |
| Add item | `POST /api/cart/items` | `{ productId, quantity }` | `200 CartDto` |
| Update quantity | `PUT /api/cart/items/{itemId}` | `{ quantity }` | `200 CartDto` |
| Remove item | `DELETE /api/cart/items/{itemId}` | None | `200 CartDto` |
| Change branch | `POST /api/cart/change-branch` | `{ branchId }` | `200 CartDto` |
| Clear cart | `DELETE /api/cart` | None | `204 No Content` |

The TypeScript types must mirror the C# JSON contract exactly:

```ts
export interface CartDto {
  id: string
  userId: string
  branchId: string
  items: CartItemDto[]
  totalItems: number
  subtotal: number
}

export interface CartItemDto {
  id: string
  productId: string
  productName: string
  sku: string
  unitPrice: number
  quantity: number
  lineTotal: number
  availableQuantity: number
}

export interface AddCartItemRequest {
  productId: string
  quantity: number
}

export interface UpdateCartItemRequest {
  quantity: number
}

export interface ChangeCartBranchRequest {
  branchId: string
}
```

Stock conflicts return HTTP `409` with `{ message: "INSUFFICIENT_STOCK", availableQuantity }`. Other existing failures include invalid quantity, unavailable products, missing cart items, and missing branches.

## Chosen Architecture

A `CartProvider` is nested inside `AuthProvider` and wraps `AppShell` plus all routes. It is the single frontend owner of server-backed cart state. `CartHeaderLink`, `ProductDetailPage`, and `CartPage` consume its public context instead of fetching or copying cart state independently.

The provider does not replace the backend as the source of truth. Every successful mutation replaces the current cart with the returned `CartDto`. Clearing the cart produces a local empty representation only after the server returns `204`, preserving the existing cart ID, user ID, and branch ID while setting `items`, `totalItems`, and `subtotal` to empty values.

The public context exposes:

```ts
export type CartStatus = 'idle' | 'loading' | 'ready' | 'error'

export interface CartContextValue {
  status: CartStatus
  cart: CartDto | null
  errorMessage: string | null
  mutatingItemIds: ReadonlySet<string>
  isAddingItem: boolean
  isChangingBranch: boolean
  isClearing: boolean
  reloadCart: () => Promise<void>
  addItem: (productId: string, quantity: number) => Promise<CartDto>
  updateItemQuantity: (itemId: string, quantity: number) => Promise<CartDto>
  removeItem: (itemId: string) => Promise<CartDto>
  changeBranch: (branchId: string) => Promise<CartDto>
  clearCart: () => Promise<void>
}
```

`mutatingItemIds` is exposed as read-only and updated through new `Set` instances. Row controls use `mutatingItemIds.has(item.id)` to disable themselves and show a row-level progress indicator. The provider also serializes all cart writes through one mutation queue. Per-item locks prevent duplicate actions on one row; serialization prevents full-cart responses from different item mutations arriving out of order and replacing newer state with an older snapshot. Locks and global mutation flags are released in `finally` on both success and failure.

## Authentication and Cart Lifecycle

- While authentication is initializing, the cart remains `idle` and does not request data.
- After a user is authenticated and an access token exists, the provider loads `GET /api/cart`.
- Guests never request protected cart endpoints.
- If the token or user changes, or the provider unmounts, the current load controller and every in-flight mutation controller are aborted.
- On logout, all request controllers are aborted; the detached write queue is reset for the next session; and all cart state, locks, flags, and errors are reset.
- A monotonically increasing auth-session version guards state writes so a mock or transport that resolves after abort cannot restore a previous user's cart.
- A failed load renders an explicit retry state; it does not fabricate an empty cart.
- Mutation failures retain the last successfully displayed cart.

`AbortController` follows the stale-request pattern already used by the product detail implementation. It is a pattern to reuse, not a new shared request abstraction.

When a guest selects `Thêm vào giỏ`, the product page opens `AuthModal`. When a guest visits `/shopping/cart`, the page stays at that URL and shows an authentication-required panel whose action opens the same modal. Successful login closes the modal and causes `CartProvider` to load the cart in place. The attempted action is not stored or replayed; the user must select `Thêm vào giỏ` again.

## Product Detail Add-to-Cart Flow

`ProductDetailPage` keeps its existing branch selector and branch-specific product request. The task adds a positive-integer quantity control and a `Thêm vào giỏ` button.

The button is enabled only when:

- Product detail has loaded.
- A valid branch is selected.
- `branchInventory` exists.
- `branchInventory.availableQuantity > 0`.
- No add or branch-change operation is already running.

The quantity defaults to `1` and is constrained using the currently displayed `branchInventory.availableQuantity`. This is an early usability check only. The server remains authoritative because stock may change after the product response was loaded. A `409 INSUFFICIENT_STOCK` response displays `Chỉ còn {availableQuantity} sản phẩm` and leaves the user's selected product and quantity available for correction or retry.

For an authenticated user, add-to-cart proceeds as follows:

1. Ensure the cart load has completed.
2. If the selected product branch equals `cart.branchId`, call `addItem` directly.
3. If the branch differs and the cart is empty, call `changeBranch`, wait for success, then call `addItem`.
4. If the branch differs and the cart contains items, open an accessible confirmation dialog explaining that changing the branch clears the cart.
5. On cancel, make no request and preserve the current cart.
6. On confirmation, call `changeBranch`, wait for its response to update context, and then call `addItem` as a separate request.

If `changeBranch` succeeds and `addItem` fails, the provider keeps the new-branch cart returned by the first request. The product page displays the add failure and a `Thử lại` action. Retry calls only `addItem`; it does not revert the branch and does not call `changeBranch` again.

## Cart Page

`CartPage` is registered at `/shopping/cart` and consumes `CartProvider`.

The page supports:

- An authentication-required state for guests.
- A loading skeleton for the initial authenticated load.
- A load error with `Thử lại`.
- An empty-cart state with a native link to `/browse`.
- A ready state containing the active branch, item list, server subtotal, and server total quantity.

Each `CartItemRow` displays a neutral product placeholder, product name, SKU, unit price, quantity controls, available quantity, line total, and a remove action. It does not fetch a product detail record for an image. Quantity controls provide decrement, editable positive-integer input, increment, and explicit removal. Invalid local values are not submitted. Increment is disabled at the displayed stock limit, but `409` handling remains required for stale stock.

During an item mutation, that row's decrement, input, increment, and remove controls are disabled and replaced or accompanied by a progress indicator. They are re-enabled after either success or failure. A failed mutation keeps the last successful row values and renders a row-associated message.

The cart page loads active branches through the existing `branchApi`. Its branch dropdown is intentionally page-specific:

- The product-detail selector updates URL state and refetches one product.
- The cart selector performs an authenticated mutation that may clear all items.

Extracting one stateful selector would couple unrelated behavior. The pages share `BranchDto`, `branchApi`, copy conventions, and visual tokens instead.

Selecting a different branch in an empty cart changes it immediately. Selecting one in a non-empty cart opens `BranchChangeConfirmDialog`; confirmation calls `changeBranch` and cancel restores the current selection without a request.

The order summary uses `cart.subtotal` directly. `Tiến hành thanh toán` is a disabled button with a `Sắp có` label because `/shopping/checkout` belongs to the next dashboard item. Clearing the whole cart requires a separate explicit confirmation and only updates the UI after the `204` response.

## Header Integration

`CartHeaderLink` is a native React Router link to `/shopping/cart` and is visible to both guests and authenticated users. When authenticated cart data is ready and `totalItems > 0`, it displays that server total as an accessible badge. The badge counts total quantity, not the number of item rows. It is hidden while unauthenticated, idle, loading, or empty, and is cleared immediately on logout.

## Loading, Error, and Concurrency Rules

- Initial load errors replace only the load view and provide retry.
- Mutation errors preserve the last successfully accepted cart.
- `400` invalid quantity uses a safe Vietnamese validation message.
- `404` product, item, cart, or branch failures use an action-specific Vietnamese message and offer cart reload when the local snapshot may be stale.
- `409 INSUFFICIENT_STOCK` includes the response's `availableQuantity`.
- Generic network or server errors use a safe Vietnamese message and an explicit retry where the failed action is repeatable.
- Aborted load and mutation requests never produce visible errors or stale state writes.
- Duplicate actions on a mutating row are blocked.
- All write calls are serialized, and a failure does not prevent later queued calls from running.
- The UI never rolls a confirmed branch change back after a later add failure.

## Accessibility, Styling, and Responsiveness

- Cart links use native link semantics.
- Quantity controls have product-specific accessible names.
- Loading regions use `aria-busy`; error messages use appropriate alert semantics.
- Confirmation overlays use `role="dialog"`, `aria-modal="true"`, labelled title and description, Escape handling, initial focus, and focus return.
- Disabled controls expose their disabled state and do not rely on color alone.
- Row progress text is available to assistive technology.
- Desktop uses a cart-list and order-summary composition; mobile becomes a single column with no horizontal overflow at 320 px.
- Styling reuses existing global color tokens and price formatting. No new icon, modal, or state library is introduced.

## Files

### Create

- `frontend/src/api/cartApi.ts` — exact cart DTO/request types and six authenticated API functions.
- `frontend/src/api/cartApi.test.ts` — request contract coverage, including the empty `204` clear response.
- `frontend/src/features/cart/CartContext.tsx` — authenticated lifecycle, authoritative cart state, serialized actions, abort handling, and mutation locks.
- `frontend/src/features/cart/CartContext.test.tsx` — lifecycle, stale-request, mutation sequencing, lock, and failure coverage.
- `frontend/src/features/cart/CartHeaderLink.tsx` — header link and total-quantity badge.
- `frontend/src/features/cart/CartHeaderLink.css` — responsive link, badge, focus, and loading styles.
- `frontend/src/features/cart/CartPage.tsx` — route-level guest, loading, error, empty, and ready states.
- `frontend/src/features/cart/CartPage.css` — responsive cart, rows, summary, and feedback styles.
- `frontend/src/features/cart/CartItemRow.tsx` — item presentation, quantity editing, row lock, and remove action.
- `frontend/src/features/cart/BranchChangeConfirmDialog.tsx` — accessible destructive branch-change confirmation.
- `frontend/src/features/cart/CartPage.test.tsx` — page, item, branch, confirmation, error, and accessibility behavior.

### Modify

- `frontend/src/App.tsx` — nest `CartProvider` under `AuthProvider` and register `/shopping/cart`.
- `frontend/src/app/AppShell.tsx` — render `CartHeaderLink` in the header actions.
- `frontend/src/features/products/ProductDetailPage.tsx` — quantity, authentication prompt, add flow, branch-change confirmation, failure, and retry behavior.
- `frontend/src/features/products/ProductDetailPage.css` — quantity, add feedback, progress, and confirmation presentation.
- `frontend/src/features/products/ProductDetail.test.tsx` — add-to-cart, authentication, branch sequencing, stock conflict, and retry regression tests.

No backend file is modified.

## Testing Strategy

Tests use Vitest, Testing Library, and `MemoryRouter`, following the current frontend conventions.

### API client

1. Every function uses the exact method and `/cart` path.
2. Every protected request sends the provided Bearer token.
3. Add, update, and branch-change requests send the exact body shape.
4. Clear-cart accepts a successful response with no JSON body.
5. API errors preserve status and parsed response data for UI-specific handling.

### Cart provider

1. Guests do not load a cart.
2. Authentication triggers one cart load.
3. Logout, token change, and unmount abort the current load and every in-flight mutation, reset the detached queue, and prevent stale state writes.
4. Successful responses replace the current cart.
5. Load and mutation failures do not fabricate or wipe a previously displayed cart.
6. Write requests are serialized and full-cart responses cannot overwrite state out of order.
7. `mutatingItemIds` disables the affected row's decrement, increment, input, and remove controls while the item is mutating.
8. A row's controls are re-enabled after mutation success.
9. A row's controls are re-enabled after mutation failure.
10. All flags and locks reset on logout, even when an aborted transport resolves later.

### Product detail integration

1. No branch, unavailable inventory, and zero stock disable adding.
2. Quantity defaults to one and respects the displayed availability.
3. A guest add attempt opens login without calling a cart endpoint.
4. Login leaves the user on the current product URL and does not replay the add.
5. A same-branch add calls only `addItem`.
6. An empty-cart branch mismatch calls `changeBranch` and then `addItem`.
7. A non-empty branch mismatch requires confirmation.
8. Cancelling branch change sends no mutation.
9. `changeBranch` success followed by `addItem` failure keeps the new-branch cart and shows retry.
10. Retry after `changeBranch → addItem` failure calls only `addItem`, not `changeBranch` again.
11. `409 INSUFFICIENT_STOCK` displays the server availability.

### Cart page and header

1. Guest, loading, error, empty, and ready states render correctly.
2. Header badge uses `totalItems`, not `items.length`, and resets on logout.
3. Increment, decrement, direct quantity submit, remove, and clear invoke the correct actions.
4. The affected row is disabled and shows progress during mutation.
5. Mutation errors preserve the displayed item and expose recovery feedback.
6. Empty-cart branch change is immediate.
7. Non-empty branch change requires confirmation and cancel sends no request.
8. Product placeholders render without product-detail requests.
9. The checkout control is disabled and labelled `Sắp có`.
10. Dialogs and interactive controls expose the required accessible semantics.

Verification commands:

```powershell
Set-Location frontend
npm.cmd test -- --run src/api/cartApi.test.ts
npm.cmd test -- --run src/features/cart/CartContext.test.tsx
npm.cmd test -- --run src/features/products/ProductDetail.test.tsx
npm.cmd test -- --run src/features/cart/CartPage.test.tsx
npm.cmd test -- --run
npm.cmd run build
```

## Acceptance Criteria

- `/shopping/cart` is registered and remains at the same URL while asking a guest to authenticate.
- Authenticated users see the cart returned by `GET /api/cart` and can retry load failures.
- The header provides a native cart link and an accurate server-backed total-quantity badge.
- Product detail allows adding a valid in-stock quantity for the selected branch.
- Guest add attempts open login and are not replayed automatically.
- Same-branch adds use one add request.
- Branch-mismatch adds preserve the exact `changeBranch` then `addItem` order.
- A non-empty cart cannot be cleared by branch change without explicit confirmation.
- If add fails after a confirmed branch change, the new branch remains selected and retry only repeats add.
- Item update, removal, branch change, and clear operations display only backend-confirmed cart data.
- Per-item controls lock during mutation and unlock after both success and failure.
- Concurrent writes cannot apply full-cart responses out of order.
- Stock conflicts show the backend's current available quantity.
- Cart rows use a neutral placeholder and do not create N+1 product requests.
- Checkout remains visibly unavailable until the separate checkout task.
- No backend contract, database schema, anonymous-cart storage, or unrelated frontend subsystem is added.
- All focused tests, the full frontend suite, and the production build pass.
