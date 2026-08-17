# Danh mục Yêu cầu Chức năng Canonical

Status: DRAFT

Ngày: 2026-08-16
Chu kỳ: Cycle 2
Phạm vi: 24 canonical Functional Requirements (FR-101..FR-115, FR-201..FR-209) và 5 Scope Drift items (SD-001..SD-005)

> **DRAFT Lifecycle Note:** Tài liệu này ở trạng thái `DRAFT` và là canonical Single Source of Truth cho tất cả FR và SD của dự án. Chỉ chuyển `OFFICIAL` sau khi Cycle 2 review chéo thành công và được phê duyệt chính thức. Mọi DFD, sitemap, và project-spec phải tham chiếu bảng này để tránh sai lệch đa nguồn.

## 1. Bảng Registry - Yêu cầu Chức năng (FR)

| ID | Actor | Description | Priority | Status | Acceptance criteria | API | Backlog | Owner | Reviewer |
|---|---|---|---|---|---|---|---|---|---|
| FR-101 | Guest, Customer | Duyệt sản phẩm, tìm kiếm, lọc theo danh mục/thương hiệu/khoảng giá | HIGH | DRAFT | Danh sách sản phẩm trả về theo điều kiện lọc; phân trang hoạt động | GET /api/products | PLANNED_CYCLE_4 | Dev | Rev |
| FR-102 | Guest, Customer | Chọn chi nhánh, xem giá và tồn kho theo chi nhánh | HIGH | DRAFT | Giá/tồn kho cập nhật khi đổi chi nhánh; khả dụng = on_hand - reserved | GET /api/branches | PLANNED_CYCLE_4 | Dev | Rev |
| FR-103 | Guest, Customer | Xem chi tiết sản phẩm, thuộc tính kỹ thuật, hình ảnh | MEDIUM | DRAFT | Trang detail hiển thị kỹ thuật, giá/tồn kho chi nhánh hiện tại | GET /api/products/:id | PLANNED_CYCLE_4 | Dev | Rev |
| FR-104 | Guest, Customer | So sánh tối đa 3–4 sản phẩm cùng danh mục | MEDIUM | DRAFT | Compare table lưu localStorage; reload giá/stock khi đổi branch | GET /api/products | PLANNED_CYCLE_4 | Dev | Rev |
| FR-105 | Customer | Quản lý hồ sơ (tên, email, điện thoại) | MEDIUM | DRAFT | Cập nhật profile; xác minh quyền ownership | PUT /api/account/profile | PLANNED_CYCLE_4 | Dev | Rev |
| FR-106 | Customer | Quản lý địa chỉ giao hàng (CRUD, đặt mặc định) | MEDIUM | DRAFT | Tối đa một địa chỉ mặc định/user; snapshot tại order time | POST /api/account/addresses | PLANNED_CYCLE_4 | Dev | Rev |
| FR-107 | Customer | Tạo/cập nhật giỏ hàng per branch | HIGH | DRAFT | Giỏ gắn (user, branch); thêm/bớt xác thực stock; đổi branch → reload | POST /api/cart | PLANNED_CYCLE_4 | Dev | Rev |
| FR-108 | Customer | Checkout: tính lại giá, promotion, phí; reserve stock transactional | HIGH | DRAFT | Backend tính toán; nếu thiếu → 409; nếu đủ → reserve + create order Pending | POST /api/checkout | PLANNED_CYCLE_4 | Dev | Rev |
| FR-109 | Customer | Chọn phương thức giao hàng (Pickup/Delivery) | HIGH | DRAFT | Pickup: chọn branch; Delivery: chọn address; snapshot lưu order | POST /api/checkout/fulfillment | PLANNED_CYCLE_4 | Dev | Rev |
| FR-110 | Customer | Chọn phương thức thanh toán (COD, VNPay, MoMo sandbox) | HIGH | DRAFT | COD: pending; VNPay/MoMo: return URL hiển thị; IPN hợp lệ cập nhật status | POST /api/checkout/payment | PLANNED_CYCLE_4 | Dev | Rev |
| FR-111 | Customer | Áp dụng mã khuyến mãi (coupon) | MEDIUM | DRAFT | Validate code; áp dụng discount; kiểm tra usage limit | POST /api/checkout/coupon | PLANNED_CYCLE_5 | Dev | Rev |
| FR-112 | Customer | Xem lịch sử đơn hàng, chi tiết, trạng thái | MEDIUM | DRAFT | Danh sách order; filter by status; xem snapshot person/address/items | GET /api/orders | PLANNED_CYCLE_4 | Dev | Rev |
| FR-113 | Customer | Đánh giá sản phẩm đã mua (1–5 sao + comment) | MEDIUM | DRAFT | Order Completed; per item tối đa 1 review; verified purchase enforce | POST /api/reviews | PLANNED_CYCLE_5 | Dev | Rev |
| FR-114 | Customer | Đăng ký tài khoản bằng email + password | HIGH | DRAFT | User tạo; mật khẩu hash; xác thực quyền | POST /api/auth/register | PLANNED_CYCLE_4 | Dev | Rev |
| FR-115 | Customer | Đăng nhập, refresh token, đăng xuất | HIGH | DRAFT | JWT access token + refresh token; token hết hạn → require login lại | POST /api/auth/login | PLANNED_CYCLE_4 | Dev | Rev |
| FR-201 | Admin | CRUD danh mục, thương hiệu | MEDIUM | DRAFT | Tạo, sửa, xóa; hỗ trợ cây cha-con; slug duy nhất | POST /api/admin/categories | PLANNED_CYCLE_4 | Dev | Rev |
| FR-202 | Admin | CRUD sản phẩm, upload hình | MEDIUM | DRAFT | Tạo, sửa, xóa (soft); SKU duy nhất; image_url lưu một ảnh chính | POST /api/admin/products | PLANNED_CYCLE_4 | Dev | Rev |
| FR-203 | Admin | Quản lý chi nhánh, tồn kho, giá per branch | HIGH | DRAFT | CRUD branch; set price + quantity per BranchInventory; reserve tracking | POST /api/admin/branches | PLANNED_CYCLE_4 | Dev | Rev |
| FR-204 | Admin | CRUD khuyến mãi (discount type, value, usage limit, time) | MEDIUM | DRAFT | Tạo, sửa, deactivate; code nullable (auto) hoặc giá trị (coupon) | POST /api/admin/promotions | PLANNED_CYCLE_5 | Dev | Rev |
| FR-205 | Admin | Quản lý đơn hàng, cập nhật trạng thái, ghi lịch sử | MEDIUM | DRAFT | Danh sách, filter; transition status; lịch sử ghi (from, to, note, timestamp) | GET /api/admin/orders | PLANNED_CYCLE_4 | Dev | Rev |
| FR-206 | Admin | Quản lý người dùng, lock/disable account | MEDIUM | DRAFT | Danh sách khách; action lock, disable; role enforcement | GET /api/admin/users | PLANNED_CYCLE_5 | Dev | Rev |
| FR-207 | Admin | Báo cáo doanh số (ngày, tuần, tháng; by category/brand) | LOW | DRAFT | Truy vấn Orders + OrderItems; subtotal, discount, fee, total tính chính xác | GET /api/admin/reports/sales | PLANNED_CYCLE_5 | Dev | Rev |
| FR-208 | Admin | Xem dự báo nhu cầu, cảnh báo tồn kho | LOW | DRAFT | Demand forecast (7–14 ngày); stock alert (predicted - on_hand > reorder_level) | GET /api/admin/forecast | PLANNED_CYCLE_5 | Dev | Rev |
| FR-209 | Customer, Admin | AI Recommendation: content-based fallback cho cold-start | LOW | DRAFT | ProductViewEvents logged; recommendation API trả danh sách (score, reason) | GET /api/recommendations | PLANNED_CYCLE_5 | Dev | Rev |

## 2. Bảng Scope Drift (SD)

| ID | Item | Status | Rule |
|---|---|---|---|
| SD-001 | Wishlist / Saved Items | DEFERRED | Khách có thể lưu sản phẩm để mua sau; chuyển Cycle 3 |
| SD-002 | Advanced Product Comparison (5+ items) | OUT_OF_SCOPE | Chỉ so sánh 3–4 sản phẩm trong MVP; expand sau |
| SD-003 | Real-time Shipper Tracking | OUT_OF_SCOPE | Theo dõi shipper thực nằm ngoài phạm vi; ngoài API mô phỏng |
| SD-004 | Warranty & Installation Service | OUT_OF_SCOPE | Quản lý bảo hành, cài đặt, IMEI cắt từ v1; xem Cycle 4+ |
| SD-005 | Guest Checkout (COD only) | DEFERRED | Guest có thể thanh toán COD mà không tạo account; Cycle 3 |

## 3. Hướng dẫn Sử dụng

### Truy vết Yêu cầu
- Mỗi FR/SD được gán ID ổn định để tham chiếu từ DFD (Process), Sitemap (Route), Project-spec, và code comment.
- Không được dùng ID cũ từ các bản nháp khác (như FR-301..FR-507 từ bản trước).
- Status mỗi dòng phải là `DRAFT`, `PROPOSED`, `APPROVED`, `IMPLEMENTED`, `VERIFIED`, `DEPRECATED`, `IN_PROGRESS`, hoặc `DONE`.
- Hiện tại (Cycle 2) tất cả FR ở `DRAFT`; không dùng `OFFICIAL` cho đến khi review chéo thành công.

### Priority
- Hỗ trợ: `MUST`, `SHOULD`, `COULD`, `WONT`, `HIGH`, `MEDIUM`, `LOW`, `P0`, `P1`, `P2`, `P3`, `CRITICAL`.
- HIGH/MUST: Checkout, thanh toán, tồn kho, auth, xác thực.
- MEDIUM: Profile, giỏ, đánh giá, quản lý sản phẩm.
- LOW: Báo cáo nâng cao, AI, ngoài MVP.

### API & Backlog Tokens
- `API`: Endpoint GET/POST/PUT/DELETE hoặc token hợp lệ (`PLANNED_CYCLE_4`, `PLANNED_CYCLE_5`, TASK-NNN, #123, URL).
- `Backlog`: Điểm xếp hạng công việc hoặc token kế hoạch; cùng quy tắc token hợp lệ.
- `PLANNED_CYCLE_4`, `PLANNED_CYCLE_5`: Không có implementation trong Cycle 2; để sau.

### Ownership & Review
- Mỗi FR được gán một Owner (dev) và Reviewer (QA/reviewer).
- Không ảnh hưởng tới truy vết kỹ thuật; chỉ quản lý trách nhiệm.

### Scope Drift (SD)
- Các yêu cầu nằm ngoài MVP hoặc bị hoãn lại.
- Status: `DEFERRED` (sẽ triển khai sau) hoặc `OUT_OF_SCOPE` (không triển khai, ra sáng kiến sau).
- Mỗi SD phải được tham chiếu tại least một route (sitemap) hoặc process (DFD) nếu liên quan.

## 4. Tóm tắt Traceability

Dự án Siêu thị Điện tử chứa:
- **24 Functional Requirements** (FR-101 đến FR-209): Xác định chức năng chính cho Guest, Customer, Admin.
- **5 Scope Drift items** (SD-001 đến SD-005): Nhu cầu hoãn lại hoặc ngoài phạm vi.

Tất cả phải được ánh xạ trong:
- **DFD** (docs/architecture/dfd.md): Mỗi Process P.1–P.12 gắn ít nhất một FR hoặc SD.
- **Sitemap** (docs/architecture/sitemap.md): Mỗi Route MVP gắn ít nhất một FR; DEFERRED/OUT_OF_SCOPE gắn SD; INFORMATIONAL gắn NON_FR_INFORMATIONAL.
- **Project-spec** (docs/project-spec.html): Link tới canonical registry này thay vì bảng tĩnh.

Validator `node .opencode/tools/requirements-policy.mjs validate` kiểm tra toàn bộ traceability.
