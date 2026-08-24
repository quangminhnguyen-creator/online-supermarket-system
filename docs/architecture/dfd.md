# DFD Hệ thống Siêu thị Điện tử Trực tuyến

Status: **OFFICIAL**  
Ngày cập nhật: 2026-08-23  
Phạm vi: 16 Bảng đã Migration, 7 Bảng mở rộng kế hoạch  
Cấu trúc: Context → Level 0 → Level 1 (chi tiết)

> **OFFICIAL:** Tài liệu này là canonical DFD cho hệ thống. Tất cả processes mới phải được thêm vào đây.

---

## 1. Quy ước ký hiệu

- **External Entity (E)**: Guest, Customer, Admin, Payment Provider (VNPay, MoMo, COD processor).
- **Process (P)**: Hoạt động xử lý dữ liệu; ID dạng `P.N` ở Level 0, `P.N.M` ở Level 1.
- **Data Store (D)**: Bảng hoặc nhóm bảng; ID dạng `D1`, `D2`, v.v.
- **Data Flow (→)**: Luồng dữ liệu giữa entity, process, store.
- **Cardinality**: `1:1`, `1:N` ghi chú trên flow nếu cần.

---

## 2. Context Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                   Siêu thị Điện tử Trực tuyến                  │
│                                                                  │
│                         [System]                                │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
       ↑                           ↑                       ↑
       │                           │                       │
    Guest          Customer     Admin          Payment Provider
    (Browse)     (Order, Pay)  (Manage)      (VNPay, MoMo, COD)
       │               │            │              │
       └───────────────┴────────────┴──────────────┘

Luồng:
- Guest → Xem sản phẩm, giá, tồn kho theo chi nhánh; không đặt hàng.
- Customer → Đặt hàng, thanh toán sandbox, quản lý địa chỉ/hồ sơ, xem lịch sử đơn.
- Admin → Quản lý người dùng, quản lý kho/giá chi nhánh, cập nhật trạng thái đơn hàng.
- Payment Provider → Nhận yêu cầu thanh toán, gửi callback/IPN xác nhận giao dịch.
```

---

## 3. Level 0 DFD

```
           Guest
             │
             ├─→ [P.1] Duyệt hàng & Tìm kiếm ──→ [D1] Catalog & [D3] Inventory
             │
           Customer
             │
             ├─→ [P.2] Quản lý tài khoản & Địa chỉ ──→ [D2] Identity & Addresses
             │
             ├─→ [P.3] Quản lý giỏ hàng ──→ [D3] Cart & Inventory
             │
             ├─→ [P.4] Checkout & Đặt hàng ──→ [D4] Orders & [D3] Inventory
             │
             ├─→ [P.5] Thanh toán & Callback ──→ [D6] Payments ──→ Payment Provider
             │                                              ↑
             │                                         Callback/IPN
             │
             └─→ [P.6] Đánh giá & Bình luận (Planned) ──→ [D7] Reviews

           Admin
             │
             ├─→ [P.7] Quản lý Catalog (Planned) ──→ [D1] Catalog
             │
             ├─→ [P.8] Quản lý Chi nhánh & Tồn kho ──→ [D3] Inventory & Branches
             │
             ├─→ [P.9] Quản lý Khuyến mãi (Planned) ──→ [D5] Promotions
             │
             ├─→ [P.10] Quản lý Đơn hàng ──→ [D4] Orders & Histories
             │
             ├─→ [P.11] Quản lý Người dùng ──→ [D2] Identity
             │
             └─→ [P.12] Báo cáo, Dự báo & Gợi ý (Planned) ──→ [D8] Analytics & AI

Data Store Groups:
  D1: CATEGORIES, BRANDS, PRODUCTS
  D2: USERS, REFRESH_TOKENS, PASSWORD_RESET_TOKENS, ADDRESSES
  D3: BRANCHES, BRANCH_INVENTORIES, CARTS, CART_ITEMS, INVENTORY_TRANSACTIONS
  D4: ORDERS, ORDER_ITEMS, ORDER_STATUS_HISTORIES
  D5: PROMOTIONS
  D6: PAYMENTS, PAYMENT_CALLBACKS
  D7: REVIEWS
  D8: PRODUCT_VIEW_EVENTS, RECOMMENDATION_RESULTS, DEMAND_FORECASTS, STOCK_ALERTS
```

---

## 4. Level 1: Chi Tiết Từng Process

### P.1 — Duyệt hàng & Tìm kiếm (Guest/Customer)
```
      Guest/Customer
          │
          ├─→ [P.1.1] Chọn chi nhánh ──→ [D3] BRANCHES
          │
          ├─→ [P.1.2] Tìm kiếm / Lọc danh mục / Thương hiệu ──→ [D1] CATEGORIES, BRANDS, PRODUCTS
          │
          └─→ [P.1.3] Xem giá & tồn kho chi nhánh ──→ [D3] BRANCH_INVENTORIES

Output: Danh sách sản phẩm phân trang + giá & số lượng khả dụng tại chi nhánh đã chọn
```

### P.2 — Quản lý tài khoản & Địa chỉ (Customer/Admin)
```
      Customer / Admin
          │
          ├─→ [P.2.1] Đăng ký / Đăng nhập / Refresh Token ──→ [D2] USERS, REFRESH_TOKENS
          │
          ├─→ [P.2.2] Quên mật khẩu / Đặt lại mật khẩu ──→ [D2] PASSWORD_RESET_TOKENS
          │
          ├─→ [P.2.3] Cập nhật hồ sơ & Đổi mật khẩu ──→ [D2] USERS
          │
          ├─→ [P.2.4] CRUD Sổ địa chỉ & Đặt mặc định ──→ [D2] ADDRESSES
          │
          └─→ [P.2.5] (Admin) Khóa / Mở khóa tài khoản ──→ [D2] USERS

Output: JWT Access Token, User Profile, Danh sách địa chỉ, Trạng thái tài khoản
```

### P.3 — Quản lý giỏ hàng (Customer)
```
      Customer
          │
          ├─→ [P.3.1] Khởi tạo / Xem giỏ hàng theo chi nhánh ──→ [D3] CARTS, BRANCHES
          │
          ├─→ [P.3.2] Thêm sản phẩm vào giỏ (xác thực tồn kho) ──→ [D3] CART_ITEMS, BRANCH_INVENTORIES
          │
          ├─→ [P.3.3] Cập nhật số lượng / Xóa item khỏi giỏ ──→ [D3] CART_ITEMS
          │
          └─→ [P.3.4] Chuyển đổi chi nhánh mua sắm ──→ [D3] CARTS, BRANCH_INVENTORIES

Output: Giỏ hàng với danh sách item, đơn giá và tổng tiền cập nhật theo chi nhánh
```

### P.4 — Checkout & Đặt hàng (Customer)
```
      Customer
          │
          ├─→ [P.4.1] Tính toán tổng tiền & phí giao hàng (Pickup / Delivery)
          │
          ├─→ [P.4.2] Khóa giữ tồn kho giao dịch (Serializable Transaction) ──→ [D3] BRANCH_INVENTORIES (Reserve)
          │
          ├─→ [P.4.3] Lưu snapshot địa chỉ và sản phẩm ──→ [D4] ORDERS, ORDER_ITEMS
          │
          └─→ [P.4.4] Xóa sạch các mục trong giỏ hàng ──→ [D3] CART_ITEMS

Output: Đơn hàng mới (Status: Pending), Tồn kho được khóa giữ an toàn (Reserved Quantity)
```

### P.5 — Thanh toán & Callback (Customer ↔ Payment Provider)
```
      Customer                Payment Provider (VNPay / MoMo / COD)
          │                                  ↑
          ├─→ [P.5.1] Chọn phương thức thanh toán
          │
          ├─→ [P.5.2] Khởi tạo giao dịch (Pending) ──→ [D6] PAYMENTS
          │                                              ↓
          ├─→ [P.5.3] Điều hướng URL Sandbox / Trả kết quả hiển thị
          │                                              ↓
          └─→ [P.5.4] Tiếp nhận Webhook/IPN Callback ──→ [D6] PAYMENT_CALLBACKS
                                                         ↓
                                               Cập nhật [D6] PAYMENTS (Completed/Failed)
                                                         ↓
                                               Cập nhật [D4] ORDERS (Confirmed/Cancelled)
                                                         ↓
                                               (Nếu thất bại) Giải phóng Reserved Stock [D3]
```

### P.8 — Quản lý Chi nhánh & Tồn kho (Admin)
```
      Admin
          │
          ├─→ [P.8.1] Xem danh sách chi nhánh & kho ──→ [D3] BRANCHES, BRANCH_INVENTORIES
          │
          └─→ [P.8.2] Cập nhật giá bán, số lượng on-hand, định mức nhập ──→ [D3] BRANCH_INVENTORIES

Output: Tồn kho và giá bán chi nhánh được cập nhật
```

### P.10 — Quản lý Đơn hàng (Customer / Admin)
```
      Customer / Admin
          │
          ├─→ [P.10.1] Xem danh sách & Chi tiết đơn hàng ──→ [D4] ORDERS, ORDER_ITEMS
          │
          ├─→ [P.10.2] (Admin) Cập nhật trạng thái đơn (Confirmed, Preparing, Shipping,...) ──→ [D4] ORDER_STATUS_HISTORIES
          │
          └─→ [P.10.3] Hủy đơn hàng (Tự động hoàn trả tồn kho Reserved/On-hand) ──→ [D3] BRANCH_INVENTORIES

Output: Chi tiết đơn hàng và lịch sử chuyển đổi trạng thái
```

---

## 5. Bảng Cân bằng Input/Output & Traceability FR/SD

| Process | Requirement Mapping | Input | Output | Data Stores |
|---|---|---|---|---|
| P.1 | FR-101, FR-102, FR-103, FR-104 | Guest/Customer, BranchId | Products, Categories, Brands, Stock | D1, D3 |
| P.2 | FR-105, FR-106, FR-114, FR-115, FR-206 | Customer, Admin | Tokens, Profiles, Addresses, User Status | D2 |
| P.3 | FR-107 | Customer, Cart Items, Branch | Cart Summary, Subtotal, Stock Check | D3 |
| P.4 | FR-108, FR-109 | Customer, Cart, Delivery Info | Order (Pending), Reserved Stock | D3, D4 |
| P.5 | FR-110 | Order, Payment Method, Callback | Payment Record, Order Confirmation | D6, D4, D3 |
| P.6 | FR-113 | Completed Order Item, Rating | Verified Review Record | D7 (Planned) |
| P.7 | FR-201, FR-202 | Admin Catalog Data | Categories, Brands, Products CRUD | D1 (Planned) |
| P.8 | FR-203 | Admin Inventory Adjustment | Adjusted Stock, Selling Price | D3 |
| P.9 | FR-111, FR-204 | Admin / Customer Coupon | Promotion Rules, Discount Application | D5 (Planned) |
| P.10 | FR-112, FR-205 | Customer / Admin | Order Details, Status Transitions | D4, D3 |
| P.11 | FR-207, FR-208 | System Orders Data | Sales Reports, Demand Forecasts | D8 (Planned) |
| P.12 | FR-209, SD-001 | Customer / Admin View Events | Recommendations, Stock Alerts | D8 (Planned) |

---

## 6. Implementation Status Phân Hệ

| Phân hệ / Process | Trạng thái | Entities Tham Gia |
|---|---|---|
| **P.1 Duyệt hàng & Tìm kiếm** | ✅ Đã hoàn thành | `Category`, `Brand`, `Product`, `Branch`, `BranchInventory` |
| **P.2 Quản lý tài khoản & Địa chỉ** | ✅ Đã hoàn thành | `User`, `RefreshToken`, `PasswordResetToken`, `Address` |
| **P.3 Quản lý giỏ hàng** | ✅ Đã hoàn thành | `Cart`, `CartItem`, `BranchInventory` |
| **P.4 Checkout & Đặt hàng** | ✅ Đã hoàn thành | `Order`, `OrderItem`, `BranchInventory` |
| **P.5 Thanh toán Sandbox & IPN** | ✅ Đã hoàn thành | `Payment`, `PaymentCallback`, `Order` |
| **P.6 Đánh giá sản phẩm** | ⏳ Kế hoạch mở rộng | `Review` |
| **P.7 Quản trị Catalog** | ⏳ Kế hoạch mở rộng | `Category`, `Brand`, `Product` |
| **P.8 Quản trị Chi nhánh & Tồn kho**| ✅ Đã hoàn thành | `Branch`, `BranchInventory` |
| **P.9 Quản trị Khuyến mãi** | ⏳ Kế hoạch mở rộng | `Promotion` |
| **P.10 Quản trị Đơn hàng** | ✅ Đã hoàn thành | `Order`, `OrderItem`, `OrderStatusHistory`, `BranchInventory` |
| **P.11 Quản trị Người dùng** | ✅ Đã hoàn thành | `User` |
| **P.12 Báo cáo, Dự báo & Gợi ý AI** | ⏳ Kế hoạch mở rộng | `ProductViewEvent`, `DemandForecast`, `StockAlert` |
