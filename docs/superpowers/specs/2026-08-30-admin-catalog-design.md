# Admin Catalog Design

## Mục tiêu

Triển khai trọn luồng quản trị catalog cho `FR-201` và `FR-202` trên kiến trúc Minimal API, EF Core và React hiện có. Admin có thể quản lý danh mục, thương hiệu và sản phẩm; khách hàng chỉ tiếp tục nhìn thấy dữ liệu đang hoạt động qua các API catalog công khai.

## Phạm vi

### Bao gồm

- Bảo vệ toàn bộ API và giao diện Admin Catalog bằng JWT và role `Admin`.
- Danh sách, tạo, cập nhật, vô hiệu hóa và khôi phục danh mục.
- Danh sách, tạo, cập nhật, vô hiệu hóa và khôi phục thương hiệu.
- Danh sách, tạo, cập nhật, vô hiệu hóa và khôi phục sản phẩm.
- Hỗ trợ cây danh mục một cấp hoặc nhiều cấp qua `ParentCategoryId` hiện có.
- Lưu ảnh sản phẩm dưới dạng `imageUrl` theo model hiện tại.
- Cập nhật OpenAPI và trạng thái yêu cầu chức năng sau khi kiểm thử đạt.

### Không bao gồm

- Upload và lưu file ảnh nhị phân; tính năng này cần một storage subsystem riêng.
- Quản lý tồn kho theo chi nhánh.
- Khuyến mãi, coupon, review, báo cáo và AI.
- Xóa vật lý dữ liệu catalog.

## Kiến trúc

### Backend

Tạo `AdminCatalogEndpoints` và đăng ký trong `Program.cs`. Route group `/api/admin/catalog` yêu cầu authentication và authorization policy/role `Admin`. Endpoint gọi trực tiếp `AppDbContext`, nhất quán với các endpoint hiện tại; không thêm application layer hoặc repository abstraction trong đợt này.

Các domain entity `Category`, `Brand` và `Product` nhận thêm các phương thức hành vi cần thiết để cập nhật dữ liệu và chuyển trạng thái active. Setter vẫn private; validation cốt lõi nằm trong domain, còn validation liên quan database nằm ở endpoint.

### Frontend

Tạo admin route guard dựa trên `AuthContext.user.role`, admin layout và ba trang:

- `/admin/catalog/categories`
- `/admin/catalog/brands`
- `/admin/catalog/products`

Mỗi trang dùng API client riêng, bảng danh sách và form tạo/sửa. Deactivate dùng confirmation dialog có `role="dialog"`, `aria-modal`, focus ban đầu vào nút hủy và nút xác nhận destructive; restore không dùng ngôn ngữ destructive. UI hiển thị loading, empty state, validation error, lỗi authorization và lỗi server bằng thông báo rõ ràng.

## API

### Categories

- `GET /api/admin/catalog/categories?includeInactive=true`
- `POST /api/admin/catalog/categories`
- `PUT /api/admin/catalog/categories/{id}`
- `PATCH /api/admin/catalog/categories/{id}/status`

Payload tạo/cập nhật gồm `name`, `slug`, `parentCategoryId`. Status payload gồm `isActive`.

### Brands

- `GET /api/admin/catalog/brands?includeInactive=true`
- `POST /api/admin/catalog/brands`
- `PUT /api/admin/catalog/brands/{id}`
- `PATCH /api/admin/catalog/brands/{id}/status`

Payload tạo/cập nhật gồm `name`, `slug`. Status payload gồm `isActive`.

### Products

- `GET /api/admin/catalog/products` với `search`, `categoryId`, `brandId`, `isActive`, `page`, `pageSize`
- `POST /api/admin/catalog/products`
- `PUT /api/admin/catalog/products/{id}`
- `PATCH /api/admin/catalog/products/{id}/status`

Payload tạo/cập nhật gồm `categoryId`, `brandId`, `sku`, `name`, `slug`, `description`, `basePrice`, `unit`, `imageUrl`. Status payload gồm `isActive`.

API danh sách sản phẩm trả dữ liệu phân trang theo envelope catalog công khai hiện có. Các mutation thành công trả representation mới nhất của resource.

## Quy tắc nghiệp vụ và validation

- `name`, `slug`, `sku` và `unit` được trim; trường bắt buộc không được rỗng.
- Slug danh mục và thương hiệu phải duy nhất không phân biệt hoa thường trong từng bảng.
- Slug và SKU sản phẩm phải duy nhất không phân biệt hoa thường.
- `basePrice` không âm.
- Khi tạo Product hoặc đổi `categoryId`/`brandId`, association đích phải tồn tại và đang active. Khi chỉ sửa trường khác, Admin được giữ association cũ đã inactive để có thể chỉnh dữ liệu; Product đó vẫn bị ẩn khỏi catalog công khai.
- Chỉ restore Product khi Category và Brand hiện tại đều active.
- Parent category phải tồn tại, đang active, không được trỏ vào chính nó hoặc tạo chu trình.
- Không cho deactivate category/brand khi còn product active trực tiếp tham chiếu; API trả `409 Conflict` cùng thông báo hành động khắc phục.
- Không cho deactivate category cha khi còn category con active.
- Xóa được biểu diễn bằng `IsActive = false`; restore dùng cùng status endpoint.
- Catalog API công khai tiếp tục chỉ trả resource active.

## Phân quyền và lỗi

- Chưa đăng nhập: `401 Unauthorized`.
- Đã đăng nhập nhưng không phải Admin: `403 Forbidden`.
- Payload không hợp lệ hoặc quan hệ không hợp lệ: `400 Bad Request` theo Problem Details.
- Không tìm thấy resource: `404 Not Found`.
- Trùng slug/SKU hoặc resource đang được sử dụng: `409 Conflict`.
- Frontend route guard điều hướng người chưa đăng nhập tới luồng đăng nhập và người không có quyền về trang chủ; backend vẫn là ranh giới bảo mật bắt buộc.

## Luồng dữ liệu

1. Admin đăng nhập và nhận JWT chứa role.
2. Route guard xác nhận trạng thái UI rồi tải dữ liệu qua admin API client.
3. Backend xác thực JWT/role, validate payload và trạng thái database.
4. Domain entity áp dụng thay đổi; EF Core lưu transaction của request.
5. API trả DTO mới nhất; frontend cập nhật danh sách và thông báo kết quả.
6. Storefront tự động phản ánh thay đổi active qua catalog API công khai.

## Kiểm thử

### Backend

- Domain tests cho update, activate/deactivate và validation giá/ID.
- Endpoint tests cho `401`, `403` và Admin success path.
- CRUD tests cho cả ba resource.
- Case-insensitive uniqueness tests cho slug/SKU.
- Category cycle và invalid relationship tests.
- Conflict tests khi deactivate resource đang được sử dụng.
- Kiểm tra catalog công khai không lộ resource inactive.
- OpenAPI contract tests cho endpoint mới.

### Frontend

- Route guard cho guest, customer và admin.
- API client truyền bearer token, query và payload đúng.
- Tải danh sách, loading, empty state và server error.
- Tạo/cập nhật thành công và hiển thị validation error.
- Xác nhận deactivate/restore và refresh state sau mutation.
- Build TypeScript và toàn bộ test suite hiện có phải pass.

## Tiêu chí hoàn thành

- Admin thực hiện được toàn bộ luồng quản lý category, brand và product từ giao diện.
- API từ chối guest/customer và không xóa vật lý dữ liệu.
- Quy tắc uniqueness, quan hệ cây và tham chiếu active được bảo vệ bằng test.
- Storefront chỉ hiển thị catalog active sau mọi mutation.
- OpenAPI mô tả endpoint và schema mới.
- `FR-201` và `FR-202` chỉ chuyển sang `IMPLEMENTED/DONE` sau khi backend tests, frontend tests và frontend build đều thành công.
