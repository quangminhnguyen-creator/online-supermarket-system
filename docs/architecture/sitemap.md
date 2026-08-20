# Sitemap Hệ thống Siêu thị Điện tử Trực tuyến

Status: **OFFICIAL**
Ngày: 2026-08-19
Phạm vi: Guest, Customer, Admin; route đích; guard và trạng thái lỗi
Liên kết: Trace tới FR (Functional Requirement) và DFD Process

> **OFFICIAL:** Tài liệu này là canonical sitemap cho hệ thống. Tất cả routes mới phải được thêm vào đây và trace tới FR tương ứng.

## 1. Quy ước

- **Route**: Đường dẫn đích (không phải endpoint hiện tại).
- **Actor**: Guest, Customer, Admin.
- **Guard**: Điều kiện truy cập; từ chối nếu không đủ quyền.
- **States**: `empty` (không có dữ liệu), `loading`, `error`, `unauthorized`, `not-found`, `ready` (thành công).
- **FR-NNN**: Functional Requirement ID từ đặc tả.
- **DFD P.N**: Process ID từ DFD.

## 2. Cây Điều hướng Tổng thể

```
┌─ Landing / Home
│   │
│   ├─ About Us
│   ├─ Branch Info
│   └─ Contact
│
├─ GUEST PATH
│   │
│   ├─ /browse
│   │   ├─ /products (search/filter/pagination)
│   │   └─ /product/:id (detail)
│   │       └─ /compare (localStorage-based)
│   │
│   ├─ /auth
│   │   ├─ /login
│   │   ├─ /register
│   │   └─ /forgot-password
│   │
│   └─ [No checkout/review/admin]
│
├─ CUSTOMER PATH (Guest + additional)
│   │
│   ├─ /account
│   │   ├─ /profile (edit)
│   │   ├─ /addresses (CRUD)
│   │   └─ /settings
│   │
│   ├─ /shopping
│   │   ├─ /branch-selector
│   │   ├─ /cart
│   │   ├─ /checkout
│   │   │   ├─ Choose fulfilment (PICKUP/DELIVERY)
│   │   │   ├─ Choose payment (COD/VNPay/MoMo)
│   │   │   └─ Confirm Order
│   │   │
│   │   ├─ /payment/:id (display return URL result)
│   │   └─ /order-confirmation
│   │
│   ├─ /orders
│   │   ├─ /history (list)
│   │   ├─ /detail/:id (status, items, delivery snapshot)
│   │   └─ /review/:orderId/:itemId (create/edit)
│   │
│   └─ [No admin access]
│
└─ ADMIN PATH (Full access + reporting)
    │
    ├─ /admin/dashboard
    │   ├─ /sales-report
    │   ├─ /order-analytics
    │   ├─ /demand-forecast
    │   ├─ /stock-alerts
    │   └─ /ai-recommendations
    │
    ├─ /admin/catalog
    │   ├─ /categories (CRUD)
    │   ├─ /brands (CRUD)
    │   └─ /products (CRUD + bulk)
    │
    ├─ /admin/branch
    │   ├─ /branches (CRUD)
    │   ├─ /inventory (per branch)
    │   │   ├─ /branch/:id/stock
    │   │   └─ /branch/:id/transactions (history)
    │   └─ /pricing (manage selling_price per branch)
    │
    ├─ /admin/promotion
    │   ├─ /promotions (CRUD)
    │   └─ /coupon-codes (list/activate/deactivate)
    │
    ├─ /admin/orders
    │   ├─ /list (filter by status)
    │   ├─ /detail/:id (update status, history)
    │   └─ /fulfillment (PICKUP/DELIVERY management)
    │
    ├─ /admin/users
    │   ├─ /customers (list, lock/disable)
    │   └─ /customer/:id (detail, edit)
    │
    └─ /admin/settings
        ├─ /security (backup, audit log)
        └─ /integration (payment provider keys — env only, not UI)
```

## 3. Trang Chi tiết

### GUEST & CUSTOMER — Public Routes

#### `/` — Landing Page
- **Actor**: Guest, Customer
- **Guard**: None
- **Purpose**: Điểm vào chính; hiển thị tính năng, chi nhánh, link đến các phần.
- **States**: `ready` (tổng quát), `loading` (branch list), `error` (nếu API thất bại)
- **FR-Link**: FR-101 (Browse), FR-102 (View branch)
- **DFD-Link**: Context, P.1
- **Actions**: View About Us, Branch Info, Login, Register, Browse Products

#### `/browse` — Browse & Search
- **Actor**: Guest, Customer
- **Guard**: None
- **Purpose**: Tìm kiếm, lọc, phân trang sản phẩm; chọn chi nhánh.
- **States**:
  - `empty`: Không có sản phẩm (lọc quá cụ thể)
  - `loading`: Đang tải danh sách
  - `error`: API failed
  - `ready`: Danh sách + giá/tồn kho chi nhánh hiện tại
- **FR-Link**: FR-101 (Search/Filter), FR-103 (Branch-specific price/stock)
- **DFD-Link**: P.1.1 (Branch), P.1.2 (Search), P.1.3 (Price/Stock)
- **Actions**: Change branch, Filter (category/brand/price), Sort, Paginate

#### `/product/:id` — Product Detail
- **Actor**: Guest, Customer
- **Guard**: None
- **Purpose**: Xem chi tiết sản phẩm; giá, tồn kho chi nhánh; thêm vào giỏ hoặc so sánh.
- **States**: `loading`, `error`, `not-found`, `ready`
- **FR-Link**: FR-103 (Product detail), FR-104 (Technical attributes), FR-107 (Add to cart)
- **DFD-Link**: P.1.3 (Stock check)
- **Actions**: View specs, View images, Add to cart (Customer only), Add to compare (localStorage), Change branch

#### `/compare` — Product Comparison (Client-side)
- **Actor**: Guest, Customer
- **Guard**: None; localStorage managed client-side
- **Purpose**: So sánh 3–4 sản phẩm cùng danh mục (price, specs, stock).
- **States**: `empty` (chưa chọn), `ready` (compare table)
- **FR-Link**: FR-104 (Product comparison)
- **DFD-Link**: P.1.3 (Fetch current price/stock when branch changes)
- **Actions**: Remove item, Change branch (reload prices/stock), Clear compare

#### `/auth/login` — Login
- **Actor**: Guest → Customer (on success)
- **Guard**: Not authenticated
- **Purpose**: Đăng nhập bằng email + password.
- **States**: `ready`, `loading`, `error` (invalid credentials, rate limit)
- **FR-Link**: FR-115 (Authentication)
- **DFD-Link**: P.2.1
- **Actions**: Enter email/password, Submit, Forgot password link

#### `/auth/register` — Register
- **Actor**: Guest → Customer (on success)
- **Guard**: Not authenticated
- **Purpose**: Đăng ký tài khoản mới.
- **States**: `ready`, `loading`, `error` (email exists, validation failed, rate limit)
- **FR-Link**: FR-114 (Registration)
- **DFD-Link**: P.2.1
- **Actions**: Enter email, password, name, phone; Submit; Login link

#### `/auth/forgot-password` — Forgot Password (Out of scope for v1, placeholder)
- **Actor**: Guest
- **Guard**: Not authenticated
- **Purpose**: Placeholder; không gửi email thật trong v1.
- **States**: `ready`, `loading`, `error`
- **FR-Link**: Out of scope
- **DFD-Link**: None
- **Actions**: Enter email; Show message

---

### CUSTOMER — Account & Shopping Routes

#### `/account/profile` — Profile Management
- **Actor**: Customer
- **Guard**: Authenticated + role:Customer
- **Purpose**: Xem/sửa thông tin tài khoản (name, email, phone).
- **States**: `loading`, `error`, `ready`
- **FR-Link**: FR-105 (Profile management)
- **DFD-Link**: P.2.2
- **Actions**: Edit profile, Change password, Logout

#### `/account/addresses` — Address Management
- **Actor**: Customer
- **Guard**: Authenticated + role:Customer
- **Purpose**: Thêm/sửa/xóa/đặt mặc định địa chỉ giao hàng.
- **States**: `empty` (no addresses), `loading`, `error`, `ready` (address list)
- **FR-Link**: FR-106 (Address management)
- **DFD-Link**: P.2.3
- **Actions**: Add address, Edit, Delete, Set as default

#### `/shopping/branch-selector` — Branch Selector
- **Actor**: Customer
- **Guard**: Authenticated + role:Customer
- **Purpose**: Chọn chi nhánh; ảnh hưởng tới giỏ hàng, giá, tồn kho.
- **States**: `loading`, `error`, `ready`
- **FR-Link**: FR-102 (Branch selection)
- **DFD-Link**: P.1.1, P.3.1
- **Actions**: View branches, Select (reload cart), View distance/hours

#### `/shopping/cart` — Shopping Cart
- **Actor**: Customer
- **Guard**: Authenticated + role:Customer
- **Purpose**: Xem giỏ hàng; thêm/bớt sản phẩm; cập nhật khi đổi chi nhánh.
- **States**: `empty` (no items), `loading`, `error`, `ready` (cart summary)
- **FR-Link**: FR-107 (Cart), FR-102 (Validate stock per branch)
- **DFD-Link**: P.3.2, P.3.3
- **Actions**:
  - Add quantity, Remove item, Clear cart
  - Change branch → Reload cart (validate availability)
  - Proceed to checkout

#### `/shopping/checkout` — Checkout (Multi-step)
- **Actor**: Customer
- **Guard**: Authenticated + role:Customer; Cart not empty
- **Purpose**: Finalize order; recalculate price/promotion/fee; reserve stock; choose fulfilment/payment.
- **States**: `loading`, `error` (409 insufficient stock, 400 validation), `ready` (checkout form)
- **FR-Link**: FR-108 (Checkout), FR-109 (Fulfilment), FR-110 (Payment), FR-111 (Promotions)
- **DFD-Link**: P.4 (all substeps), P.5 (payment method select)
- **Substeps**:
  1. Review cart summary (price recalculation)
  2. Enter/select delivery address or select pickup branch
  3. Choose fulfilment (PICKUP/DELIVERY)
  4. Apply coupon code (if any)
  5. Choose payment method (COD/VNPay/MoMo)
  6. Review order total
  7. Confirm → Create order + reserve stock (transactional)
- **Actions**: Back, Apply coupon, Change address, Select fulfilment, Select payment, Confirm

#### `/shopping/payment/:id` — Payment Result Display
- **Actor**: Customer
- **Guard**: Authenticated; Order ID belongs to user
- **Purpose**: Display return URL result dari VNPay/MoMo sandbox (không update trạng thái).
- **States**: `loading` (checking callback), `error` (payment failed), `ready` (success)
- **FR-Link**: FR-110 (Payment result)
- **DFD-Link**: P.5.3 (Return URL only; callback handled server-side)
- **Actions**: View result, Return to orders, Continue shopping
- **Note**: Actual status update happens via server-side IPN callback

#### `/shopping/order-confirmation` — Order Confirmation
- **Actor**: Customer
- **Guard**: Authenticated; Order just created
- **Purpose**: Xác nhận đơn hàng được tạo; hiển thị order ID, trạng thái.
- **States**: `ready`
- **FR-Link**: FR-108 (Order creation)
- **DFD-Link**: P.4.4
- **Actions**: View order details, Continue shopping, Go to orders

#### `/orders/history` — Order History
- **Actor**: Customer
- **Guard**: Authenticated + role:Customer
- **Purpose**: Danh sách đơn hàng của khách; filter theo trạng thái.
- **States**: `empty` (no orders), `loading`, `error`, `ready`
- **FR-Link**: FR-112 (Order history)
- **DFD-Link**: P.6.1
- **Actions**: Filter by status (Pending/Confirmed/Preparing/Ready/Completed), View detail, Create review

#### `/orders/detail/:id` — Order Detail
- **Actor**: Customer
- **Guard**: Authenticated; Order belongs to user
- **Purpose**: Xem chi tiết đơn hàng; trạng thái, items, snapshot người nhận/địa chỉ, thanh toán.
- **States**: `loading`, `error`, `not-found`, `ready`
- **FR-Link**: FR-112 (Order status), FR-109 (Fulfilment method display)
- **DFD-Link**: P.6.1
- **Actions**: View status history, View fulfilment snapshot, Create review (if Completed)

#### `/orders/review/:orderId/:itemId` — Create/Edit Review
- **Actor**: Customer
- **Guard**: Authenticated; Order Completed; belongs to user; per OrderItem max 1 review
- **Purpose**: Đánh giá sản phẩm; rating 1–5 + comment.
- **States**: `loading`, `error`, `ready`, `unauthorized` (not eligible)
- **FR-Link**: FR-113 (Product review)
- **DFD-Link**: P.6.2
- **Actions**: Submit rating + comment, Cancel

---

### ADMIN — Dashboard & Management Routes

#### `/admin/dashboard` — Admin Dashboard
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Tổng quan doanh số, đơn hàng, dự báo, cảnh báo.
- **States**: `loading`, `error`, `ready`
- **FR-Link**: FR-208 (Stock alert), FR-209 (AI Recommendation)
- **DFD-Link**: P.11 (Forecast), P.12 (Alerts, Recommendations)
- **Actions**: View reports, View alerts, Drill down

#### `/admin/dashboard/sales-report` — Sales Report
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Báo cáo doanh số theo ngày/tuần/tháng, theo danh mục/thương hiệu.
- **States**: `loading`, `error`, `ready`
- **FR-Link**: FR-207 (Sales reporting)
- **DFD-Link**: P.11.1, P.11.2
- **Actions**: Filter by date range, category, brand; Export

#### `/admin/dashboard/demand-forecast` — Demand Forecast
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Xem dự báo nhu cầu 7–14 ngày cho từng sản phẩm/chi nhánh.
- **States**: `loading`, `error`, `ready`
- **FR-Link**: FR-208 (Demand forecast)
- **DFD-Link**: P.11.1
- **Actions**: View forecast, View confidence, Drill by product/branch

#### `/admin/dashboard/stock-alerts` — Stock Alerts
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Cảnh báo tồn kho dưới ngưỡng; cấp độ (Đủ/Sắp thiếu/Cần nhập).
- **States**: `loading`, `error`, `ready`, `empty` (no alerts)
- **FR-Link**: FR-208 (Stock alerts)
- **DFD-Link**: P.11.2
- **Actions**: View alerts, Acknowledge, View recommended quantity

#### `/admin/catalog/products` — Product Admin (CRUD)
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: CRUD sản phẩm; tên, SKU, danh mục, thương hiệu, mô tả kỹ thuật, hình.
- **States**: `empty`, `loading`, `error`, `ready`
- **FR-Link**: FR-202 (Product management)
- **DFD-Link**: P.7.2
- **Actions**: List, Create, Edit, Delete (soft), Bulk upload

#### `/admin/catalog/categories` — Category Admin (CRUD)
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: CRUD danh mục; hỗ trợ cây cha-con.
- **States**: `empty`, `loading`, `error`, `ready`
- **FR-Link**: FR-201 (Category management)
- **DFD-Link**: P.7.1
- **Actions**: List (tree), Create, Edit, Delete

#### `/admin/branch/inventory` — Branch Inventory Management
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Quản lý tồn kho theo chi nhánh; giá bán, số lượng, reorder level.
- **States**: `loading`, `error`, `ready`
- **FR-Link**: FR-203 (Inventory management)
- **DFD-Link**: P.8.2
- **Actions**: Select branch, View/Edit prices/quantities, View transaction history, Add stock

#### `/admin/branch/inventory/:branchId/transactions` — Inventory Transactions
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Lịch sử giao dịch tồn kho (StockIn, Reserve, Release, Sale, Adjustment).
- **States**: `loading`, `error`, `empty`, `ready`
- **FR-Link**: FR-203 (Inventory audit)
- **DFD-Link**: P.8.3
- **Actions**: Filter by type/date, View detail, Export

#### `/admin/promotion/promotions` — Promotion Admin (CRUD)
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: CRUD khuyến mãi; discount type, value, min order, usage limit, thời gian.
- **States**: `empty`, `loading`, `error`, `ready`
- **FR-Link**: FR-204 (Promotion management)
- **DFD-Link**: P.9.1
- **Actions**: List, Create, Edit, Deactivate, View usage

#### `/admin/orders/list` — Order Management List
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Danh sách tất cả đơn hàng; filter theo trạng thái, ngày, khách.
- **States**: `empty`, `loading`, `error`, `ready`
- **FR-Link**: FR-205 (Order management)
- **DFD-Link**: P.10.1
- **Actions**: Filter, Sort, Search, Select order for detail/update

#### `/admin/orders/detail/:id` — Order Detail & Status Update
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Xem chi tiết đơn hàng; cập nhật trạng thái, ghi chú.
- **States**: `loading`, `error`, `not-found`, `ready`
- **FR-Link**: FR-205 (Order status update)
- **DFD-Link**: P.10.2
- **Actions**: Transition status (Pending→Confirmed→Preparing→Ready/Shipping→Completed), Add note, View history

#### `/admin/orders/fulfillment` — Fulfillment Management
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Quản lý giao hàng; tách PICKUP vs DELIVERY; xem snapshot địa chỉ.
- **States**: `loading`, `error`, `empty`, `ready`
- **FR-Link**: FR-205 (Fulfillment tracking)
- **DFD-Link**: P.10 (snapshot from order creation)
- **Actions**: Filter by method, View address snapshot, Update delivery status, Generate picking slip (PICKUP)

#### `/admin/users/customers` — Customer Management
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Danh sách khách hàng; lock/disable tài khoản.
- **States**: `loading`, `error`, `empty`, `ready`
- **FR-Link**: FR-206 (User management)
- **DFD-Link**: P.2
- **Actions**: Search, View detail, Lock, Disable, Reset password

#### `/admin/settings/security` — Security & Backup
- **Actor**: Admin
- **Guard**: Authenticated + role:Admin
- **Purpose**: Audit log, backup, security settings.
- **States**: `loading`, `error`, `ready`
- **FR-Link**: NFR-501 (Audit), NFR-502 (Backup)
- **DFD-Link**: None (off-path)
- **Actions**: View audit log, Trigger backup, Configure retention

---

## 4. Bảng Traceability

| Route | Scope | Actor | Requirement | DFD | Fulfilment | Guard |
|---|---|---|---|---|---|---|
| `/` | MVP | Guest, Customer | FR-101, FR-102 | Context, P.1 | N/A | None |
| `/browse` | MVP | Guest, Customer | FR-101, FR-102, FR-103, FR-104 | P.1.1–P.1.3 | Display | None |
| `/product/:id` | MVP | Guest, Customer | FR-103, FR-104 | P.1.3 | Display | None |
| `/compare` | MVP | Guest, Customer | FR-104 | P.1.3 | Display | None |
| `/auth/login` | MVP | Guest | FR-114, FR-115 | P.2.1 | Display | NotAuth |
| `/auth/register` | MVP | Guest | FR-114, FR-115 | P.2.1 | Display | NotAuth |
| `/auth/forgot-password` | INFORMATIONAL | Guest | NON_FR_INFORMATIONAL | None | Display | NotAuth |
| `/account/profile` | MVP | Customer | FR-105 | P.2.2 | Form | Auth+Customer |
| `/account/addresses` | MVP | Customer | FR-106 | P.2.3 | Form | Auth+Customer |
| `/shopping/branch-selector` | MVP | Customer | FR-102 | P.1.1, P.3.1 | Form | Auth+Customer |
| `/shopping/cart` | MVP | Customer | FR-107, FR-102 | P.3.2–P.3.3 | Form | Auth+Customer |
| `/shopping/checkout` | MVP | Customer | FR-108, FR-109, FR-110, FR-111 | P.4, P.5 | Form | Auth+Customer, Cart not empty |
| `/shopping/payment/:id` | MVP | Customer | FR-110 | P.5.3 | Display | Auth+Customer |
| `/shopping/order-confirmation` | MVP | Customer | FR-108 | P.4.4 | Display | Auth+Customer |
| `/orders/history` | MVP | Customer | FR-112 | P.6.1 | Display | Auth+Customer |
| `/orders/detail/:id` | MVP | Customer | FR-112, FR-109 | P.6.1 | Display | Auth+Customer, Ownership check |
| `/orders/review/:orderId/:itemId` | MVP | Customer | FR-113 | P.6.2 | Form | Auth+Customer, Order Completed, No prior review |
| `/admin/dashboard` | MVP | Admin | FR-208, FR-209 | P.11–P.12 | Display | Auth+Admin |
| `/admin/dashboard/sales-report` | DEFERRED | Admin | SD-001 | P.11.1, P.11.2 | Display | Auth+Admin |
| `/admin/dashboard/demand-forecast` | MVP | Admin | FR-208 | P.11.1 | Display | Auth+Admin |
| `/admin/dashboard/stock-alerts` | MVP | Admin | FR-208 | P.11.2 | Display | Auth+Admin |
| `/admin/catalog/products` | MVP | Admin | FR-202 | P.7.2 | CRUD | Auth+Admin |
| `/admin/catalog/categories` | MVP | Admin | FR-201 | P.7.1 | CRUD | Auth+Admin |
| `/admin/branch/inventory` | MVP | Admin | FR-203 | P.8.2 | CRUD | Auth+Admin |
| `/admin/branch/inventory/:branchId/transactions` | MVP | Admin | FR-203 | P.8.3 | Display | Auth+Admin |
| `/admin/promotion/promotions` | MVP | Admin | FR-204 | P.9.1 | CRUD | Auth+Admin |
| `/admin/orders/list` | MVP | Admin | FR-205 | P.10.1–P.10.2 | Display | Auth+Admin |
| `/admin/orders/detail/:id` | MVP | Admin | FR-205 | P.10.2 | Form | Auth+Admin |
| `/admin/orders/fulfillment` | MVP | Admin | FR-205 | P.10 | Display | Auth+Admin |
| `/admin/users/customers` | MVP | Admin | FR-206 | P.2 | CRUD | Auth+Admin |
| `/admin/settings/security` | INFORMATIONAL | Admin | NON_FR_INFORMATIONAL | None | Display | Auth+Admin |

---

## 5. Implementation Status

| Route | Status | Notes |
|---|---|---|
| `/` | ✅ Implemented | Landing page |
| `/api/health` | ✅ Implemented | Health check endpoint |
| `/api/auth/register` | ✅ Implemented | FR-114 |
| `/api/auth/login` | ✅ Implemented | FR-115 |
| `/api/auth/refresh` | ✅ Implemented | Token refresh |
| `/api/auth/logout` | ✅ Implemented | Logout + revoke token |
| `/api/auth/me` | ✅ Implemented | Get current user |
| `/browse` | 🔄 Planned | FR-101 |
| `/product/:id` | 🔄 Planned | FR-103 |
| `/shopping/cart` | 🔄 Planned | FR-107 |
| `/shopping/checkout` | 🔄 Planned | FR-108, FR-109, FR-110, FR-111 |
| `/orders/history` | 🔄 Planned | FR-112 |
| `/admin/catalog/*` | 🔄 Planned | FR-201, FR-202 |
| `/admin/branch/*` | 🔄 Planned | FR-203 |
| `/admin/orders/*` | 🔄 Planned | FR-205 |

---

## 6. Error Handling & States per Route

### Universal Error States

- **401 Unauthorized**: User not authenticated or token expired; redirect to login.
- **403 Forbidden**: User lacks permission (role mismatch, ownership check failed).
- **404 Not Found**: Resource does not exist.
- **409 Conflict**: Insufficient stock during checkout.
- **422 Unprocessable Entity**: Validation failed (email format, price range, etc.).
- **500 Internal Server Error**: Server error; show user-friendly message + error ID for support.
- **Rate Limit (429)**: Too many requests; backoff and retry.

### Per-Route Handling

- `/shopping/checkout`: On 409 → Show "Insufficient stock" message; suggest reducing quantity or changing branch.
- `/shopping/payment/:id`: Polling callback status; timeout after 5 min → show "Payment pending" with check-order-status link.
- `/admin/orders/fulfillment`: If order deleted → Show "Order no longer exists".

---

## 7. Responsive & Accessibility

- All routes responsive on mobile/tablet/desktop.
- Print-friendly for invoices, picking slips (PICKUP fulfillment).
- Color contrast ≥ 4.5:1 for text; ≥ 3:1 for UI components.
- Keyboard navigation: Tab, Enter, Escape.
- ARIA labels for form inputs, buttons, alerts.
- Form validation feedback in real-time + on submit.
