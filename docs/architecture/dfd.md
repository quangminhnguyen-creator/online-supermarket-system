# DFD Hệ thống Siêu thị Điện tử Trực tuyến

Status: DRAFT
Ngày: 2026-08-13
Phạm vi: 22 bảng, 4 thành viên, 30 ngày
Cấu trúc: Context → Level 0 → Level 1 (chi tiết)

> **DRAFT Lifecycle Note:** Tài liệu này ở trạng thái `DRAFT` và chưa phải canonical requirement source. Chỉ chuyển `OFFICIAL` sau khi canonical FR registry được tạo (`docs/requirements/functional-requirements.md`), remap đầy đủ DFD/sitemap, và review chéo thành công trong Cycle 2 trở lên.

## 1. Quy ước ký hiệu

- **External Entity (E)**: Guest, Customer, Admin, Payment Provider (VNPay, MoMo, COD processor).
- **Process (P)**: Hoạt động xử lý dữ liệu; ID dạng `P.N` ở Level 0, `P.N.M` ở Level 1.
- **Data Store (D)**: Bảng hoặc nhóm bảng; ID dạng `D1`, `D2`, v.v.
- **Data Flow (→)**: Luồng dữ liệu giữa entity, process, store.
- **Cardinality**: `1:1`, `1:N` ghi chú trên flow nếu cần; context và Level 0 đơn giản, Level 1 chi tiết.

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
- Guest → Xem sản phẩm, giá, tồn kho; không đặt hàng.
- Customer → Đặt hàng, thanh toán, xem lịch sử, đánh giá.
- Admin → Quản lý sản phẩm, tồn kho, khuyến mãi, đơn hàng, báo cáo.
- Payment Provider → Nhận yêu cầu thanh toán, trả callback/IPN.
```

## 3. Level 0 DFD

```
           Guest
             │
             ├─→ [P.1] Duyệt hàng & Tìm kiếm ──→ [D1] Catalog
             │
           Customer
             │
             ├─→ [P.2] Quản lý tài khoản ──→ [D2] Identity
             │
             ├─→ [P.3] Quản lý giỏ hàng ──→ [D3] Cart & Inventory
             │
             ├─→ [P.4] Checkout & Đặt hàng ──→ [D4] Orders
             │                              ↓
             │                      [D5] Promotions
             │
             ├─→ [P.5] Thanh toán ──→ [D6] Payments ──→ Payment Provider
             │                     ↑
             │              Callback/IPN
             │
             └─→ [P.6] Đánh giá & Bình luận ──→ [D7] Reviews

           Admin
             │
             ├─→ [P.7] Quản lý Catalog ──→ [D1] Catalog
             │
             ├─→ [P.8] Quản lý Chi nhánh & Tồn kho ──→ [D3] Inventory
             │
             ├─→ [P.9] Quản lý Khuyến mãi ──→ [D5] Promotions
             │
             ├─→ [P.10] Quản lý Đơn hàng ──→ [D4] Orders
             │
             ├─→ [P.11] Báo cáo & AI ──→ [D8] Analytics & AI
             │
             └─→ [P.12] Xem Gợi ý & Cảnh báo ──→ [D8] Analytics & AI

Data Store Groups:
  D1: CATEGORIES, BRANDS, PRODUCTS
  D2: USERS, ADDRESSES, REFRESH_TOKENS
  D3: BRANCHES, BRANCH_INVENTORIES, CARTS, CART_ITEMS, INVENTORY_TRANSACTIONS
  D4: ORDERS, ORDER_ITEMS, ORDER_STATUS_HISTORIES
  D5: PROMOTIONS
  D6: PAYMENTS, PAYMENT_CALLBACKS
  D7: REVIEWS
  D8: PRODUCT_VIEW_EVENTS, RECOMMENDATION_RESULTS, DEMAND_FORECASTS, STOCK_ALERTS
```

## 4. Level 1: Chi tiết từng Process

### P.1 — Duyệt hàng & Tìm kiếm (Guest/Customer)

```
      Guest/Customer
          │
          ├─→ [P.1.1] Chọn chi nhánh ──→ [D3] BRANCHES
          │
          ├─→ [P.1.2] Tìm kiếm/Lọc ──→ [D1] CATEGORIES, BRANDS, PRODUCTS
          │
          ├─→ [P.1.3] Xem giá & tồn kho ──→ [D3] BRANCH_INVENTORIES
          │
          └─→ [P.1.4] Ghi sự kiện xem ──→ [D8] PRODUCT_VIEW_EVENTS

Output: Danh sách sản phẩm + giá/tồn kho chi nhánh hiện tại
```

### P.2 — Quản lý tài khoản (Customer)

```
      Customer
          │
          ├─→ [P.2.1] Đăng ký/Đăng nhập ──→ [D2] USERS, REFRESH_TOKENS
          │
          ├─→ [P.2.2] Quản lý hồ sơ ──→ [D2] USERS
          │
          └─→ [P.2.3] Quản lý địa chỉ ──→ [D2] ADDRESSES

Output: Token, thông tin profile, danh sách địa chỉ
```

### P.3 — Quản lý giỏ hàng (Customer)

```
      Customer
          │
          ├─→ [P.3.1] Tạo/Chọn chi nhánh ──→ [D3] CARTS, BRANCHES
          │
          ├─→ [P.3.2] Thêm/Bớt sản phẩm ──→ [D3] CART_ITEMS, BRANCH_INVENTORIES
          │
          ├─→ [P.3.3] Cập nhật khi đổi chi nhánh ──→ [D3] BRANCH_INVENTORIES
          │
          └─→ Output: Giỏ hàng cân bằng (giá/tồn kho mới)
```

### P.4 — Checkout & Đặt hàng (Customer)

```
      Customer
          │
          ├─→ [P.4.1] Tính lại giá & promotion ──→ [D5] PROMOTIONS
          │                                   ↓
          │                            [D1] PRODUCTS
          │                                   ↓
          │                            [D3] BRANCH_INVENTORIES
          │
          ├─→ [P.4.2] Kiểm tra & reserve tồn kho (transactional) ──→ [D3] BRANCH_INVENTORIES
          │
          ├─→ [P.4.3] Chọn fulfilment (PICKUP/DELIVERY) ──→ snapshot địa chỉ
          │
          ├─→ [P.4.4] Tạo đơn hàng ──→ [D4] ORDERS, ORDER_ITEMS
          │
          └─→ Output: Order ID, trạng thái Pending, tồn kho reserved

Note: Nếu thiếu tồn kho → Trả 409, không tạo order/reservation
```

### P.5 — Thanh toán (Customer ↔ Payment Provider)

```
      Customer                Payment Provider
          │                        ↑
          ├─→ [P.5.1] Chọn phương thức (COD/VNPay/MoMo)
          │
          ├─→ [P.5.2] Tạo payment request ──→ [D6] PAYMENTS
          │
          ├─→ [P.5.3] Gửi tới payment provider ──→ VNPay/MoMo/COD
          │                                   ↓
          │                            Return URL (hiển thị)
          │
          └─→ [P.5.4] Xử lý callback IPN (hợp lệ) ──→ [D6] PAYMENT_CALLBACKS
                                                    ↓
                                            Cập nhật PAYMENTS.status
                                                    ↓
                                            Cập nhật ORDERS.payment_status
                                                    ↓
                                            Giải phóng/Xác nhận reservation

Note: Return URL không cập nhật trạng thái. Chỉ IPN hợp lệ (chữ ký, amount,
      external_event_id duy nhất) mới đổi trạng thái.
```

### P.6 — Đánh giá & Bình luận (Customer)

```
      Customer
          │
          ├─→ [P.6.1] Xem đơn hàng Completed ──→ [D4] ORDERS
          │
          ├─→ [P.6.2] Tạo review per OrderItem ──→ [D7] REVIEWS
          │                                   ↓
          │                            [D4] ORDER_ITEMS (verify purchase)
          │
          └─→ Output: Rating 1–5, comment; mỗi order item tối đa 1 review
```

### P.7 — Quản lý Catalog (Admin)

```
      Admin
          │
          ├─→ [P.7.1] CRUD danh mục, thương hiệu ──→ [D1] CATEGORIES, BRANDS
          │
          ├─→ [P.7.2] CRUD sản phẩm ──→ [D1] PRODUCTS
          │
          └─→ Output: Cập nhật catalog
```

### P.8 — Quản lý Chi nhánh & Tồn kho (Admin)

```
      Admin
          │
          ├─→ [P.8.1] Quản lý chi nhánh ──→ [D3] BRANCHES
          │
          ├─→ [P.8.2] Quản lý giá & tồn kho ──→ [D3] BRANCH_INVENTORIES
          │
          ├─→ [P.8.3] Ghi lịch sử giao dịch ──→ [D3] INVENTORY_TRANSACTIONS
          │
          └─→ Output: Cập nhật tồn kho, lịch sử
```

### P.9 — Quản lý Khuyến mãi (Admin)

```
      Admin
          │
          ├─→ [P.9.1] Tạo/Cập nhật promotion ──→ [D5] PROMOTIONS
          │
          └─→ Output: Promotion code, discount rules
```

### P.10 — Quản lý Đơn hàng (Admin)

```
      Admin
          │
          ├─→ [P.10.1] Xem danh sách đơn ──→ [D4] ORDERS
          │
          ├─→ [P.10.2] Cập nhật trạng thái ──→ [D4] ORDER_STATUS_HISTORIES
          │
          └─→ Output: Cập nhật order status
```

### P.11 — Báo cáo & AI (Admin)

```
      Admin
          │
          ├─→ [P.11.1] Tính demand forecast ──→ [D8] DEMAND_FORECASTS
          │                              ↓
          │                       Seed data từ ORDERS
          │
          ├─→ [P.11.2] Tạo stock alert ──→ [D8] STOCK_ALERTS
          │                            ↓
          │                     Dựa trên DEMAND_FORECASTS
          │                            ↓
          │                     + BRANCH_INVENTORIES
          │
          └─→ Output: Cảnh báo nhập hàng, dự báo
```

### P.12 — Xem Gợi ý & Cảnh báo (Admin)

```
      Admin
          │
          ├─→ [P.12.1] Xem recommendation ──→ [D8] RECOMMENDATION_RESULTS
          │
          ├─→ [P.12.2] Xem stock alert ──→ [D8] STOCK_ALERTS
          │
          └─→ Output: Dashboard AI, cảnh báo
```

## 5. Bảng Cân bằng Input/Output & Traceability FR/SD

| Process | Requirement mapping | Input | Output | Kiểm tra |
|---|---|---|---|---|
| P.1 | FR-101, FR-102, FR-103, FR-104 | Guest/Customer, Branch | Products+Price+Stock | ✓ Tồn kho từ D3 |
| P.2 | FR-105, FR-106, FR-114, FR-115 | Customer | Token, Profile | ✓ Auth từ D2 |
| P.3 | FR-107 | Customer, Branch | Cart Items | ✓ Cart gắn user+branch |
| P.4 | FR-108, FR-109, FR-110, FR-111 | Customer, Cart | Order + Reserved | ✓ Atomic reserve |
| P.5 | FR-110 | Order + Payment | Payment Status | ✓ IPN validates |
| P.6 | FR-113 | Completed Order | Review | ✓ Verified purchase |
| P.7 | FR-201, FR-202 | Admin | Catalog | ✓ CRUD (D1) |
| P.8 | FR-203 | Admin | Inventory | ✓ CRUD (D3) |
| P.9 | FR-204 | Admin | Promotion | ✓ CRUD (D5) |
| P.10 | FR-112, FR-205 | Admin | Order Status | ✓ History (D4) |
| P.11 | FR-207, FR-208 | Seed Data | Forecast + Alert | ✓ From D8 |
| P.12 | FR-208, FR-209, SD-001 | Admin | AI Dashboard | ✓ Display (D8) |

## 6. Ranh giới Bảo mật & Tính nhất quán

### Checkout Transactional (P.4)

Tất cả thao tác phải trong **một MySQL transaction**:

1. Lock `BRANCH_INVENTORIES` (SELECT...FOR UPDATE) theo thứ tự tăng (branch_id, product_id).
2. Kiểm tra `available_quantity = on_hand - reserved` ≥ required.
3. Nếu thiếu → Rollback, trả HTTP 409, không tạo order/reservation dở dang.
4. Nếu đủ → Tăng `reserved_quantity`, insert `Orders` + `OrderItems`, commit.

### Payment Idempotency & Security (P.5)

- Return URL chỉ **hiển thị** kết quả; không cập nhật trạng thái.
- IPN/Callback phải:
  - Xác minh **chữ ký** (VNPay/MoMo).
  - Kiểm tra **số tiền** khớp order.
  - Kiểm tra **external_event_id** duy nhất (chống lặp).
  - Chỉ khi hợp lệ mới cập nhật `PAYMENTS`, `ORDERS`, giải phóng/xác nhận reservation.

### Authorization (P.1–P.12)

- Guest: Chỉ P.1 (duyệt).
- Customer: P.1–P.6.
- Admin: P.7–P.12 + báo cáo.
- Backend enforcement bắt buộc trên mỗi endpoint.

## 7. Ràng buộc & Giới hạn

- Mỗi Customer có **tối đa một cart** per branch; đổi branch → tải lại cart.
- Mỗi order item được review **tối đa một lần**.
- Promotion sử dụng **tối đa một** per order.
- Payment callback **không lặp** (unique provider + external_event_id).
- Không lưu card number, CVV, expiry.
