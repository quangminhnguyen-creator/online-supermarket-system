# Đặc tả thiết kế hệ thống siêu thị trực tuyến

Ngày: 2026-08-05  
Thời gian thực hiện: 30 ngày  
Nhân sự: 3 thành viên

## 1. Mục tiêu

Xây dựng một hệ thống siêu thị trực tuyến có nhiều chi nhánh và tồn kho riêng. Khách hàng có thể tìm kiếm sản phẩm, mua hàng, chọn giao tận nơi hoặc nhận tại chi nhánh, thanh toán COD/VNPay sandbox/MoMo sandbox và đánh giá sản phẩm. Admin quản lý catalog, tồn kho, khuyến mãi, đơn hàng và báo cáo. Hai tính năng AI được triển khai cuối dự án: gợi ý sản phẩm cho khách và dự báo nhu cầu/cảnh báo nhập hàng cho admin.

Đây là đồ án có khả năng demo đầy đủ, không phải hệ thống production. Thiết kế vẫn giữ các nguyên tắc bảo mật căn bản và không lưu dữ liệu thẻ.

## 2. Phạm vi người dùng

### Guest

- Xem trang chủ, danh mục, thương hiệu và sản phẩm.
- Tìm kiếm, lọc, sắp xếp và phân trang.
- Chọn chi nhánh để xem giá và tồn kho tương ứng.
- So sánh một nhóm nhỏ sản phẩm.
- Không được đặt hàng hoặc đánh giá.

### Customer

- Đăng ký, đăng nhập, đăng xuất và đặt lại mật khẩu theo luồng demo.
- Quản lý hồ sơ và địa chỉ giao hàng.
- Thực hiện toàn bộ chức năng của Guest.
- Quản lý giỏ hàng theo chi nhánh.
- Áp dụng promotion/coupon hợp lệ.
- Chọn giao tận nơi hoặc nhận tại chi nhánh.
- Thanh toán COD, VNPay sandbox hoặc MoMo sandbox.
- Xem lịch sử và trạng thái đơn.
- Đánh giá sản phẩm thuộc đơn đã hoàn thành.
- Xem gợi ý sản phẩm.

### Admin

- Quản lý khách hàng và trạng thái tài khoản.
- Quản lý chi nhánh, danh mục, thương hiệu và sản phẩm.
- Quản lý giá và tồn kho theo chi nhánh.
- Quản lý promotion/coupon.
- Xử lý trạng thái đơn hàng.
- Xem báo cáo doanh số theo ngày, tuần, tháng, thương hiệu và danh mục.
- Xem dự báo nhu cầu và cảnh báo nhập hàng.

## 3. Công nghệ và kiến trúc

- Frontend: ReactJS, React Router, thư viện HTTP client và một UI library do nhóm thống nhất.
- Backend: ASP.NET Core Web API, Entity Framework Core.
- Database: MySQL.
- Authentication: ASP.NET Core Identity hoặc cơ chế hash mật khẩu chuẩn, JWT access token và refresh token.
- Payment: COD nội bộ; VNPay và MoMo sandbox.
- AI: recommendation dựa trên nội dung/hành vi và dự báo time-series đơn giản; ưu tiên triển khai trong .NET.
- Deployment demo: Docker Compose với frontend, backend và MySQL.

Kiến trúc backend là monolith phân lớp:

```text
Controllers → Application Services → Repositories/EF Core → MySQL
                         │
                  Payment/AI adapters
```

Các module nghiệp vụ gồm Identity, Catalog, Branch, Inventory, Cart, Promotion, Order, Payment, Fulfillment, Review, Reporting và AI.

## 4. Mô hình dữ liệu

### Identity và địa chỉ

- `Users`: UserId, Email, PasswordHash, FullName, Phone, Role, Status, CreatedAt.
- `RefreshTokens`: TokenId, UserId, TokenHash, ExpiresAt, RevokedAt.
- `Addresses`: AddressId, UserId, Province, District, Ward, Street, IsDefault.

### Catalog và chi nhánh

- `Branches`: BranchId, Name, Address, Phone, Latitude, Longitude, Status.
- `Categories`: CategoryId, Name, Slug, ParentCategoryId, Status.
- `Brands`: BrandId, Name, Slug, Status.
- `Products`: ProductId, CategoryId, BrandId, SKU, Name, Slug, Description, BasePrice, Unit, ImageUrl, Status.
- `BranchInventories`: BranchId, ProductId, SellingPrice, QuantityOnHand, ReservedQuantity, ReorderLevel, UpdatedAt.
- `InventoryTransactions`: TransactionId, BranchId, ProductId, Type, Quantity, ReferenceType, ReferenceId, CreatedAt.

### Cart, promotion và order

- `Carts`: CartId, UserId, BranchId, UpdatedAt.
- `CartItems`: CartItemId, CartId, ProductId, Quantity.
- `Promotions`: PromotionId, Name, Type, Value, MinOrderAmount, StartAt, EndAt, Status.
- `PromotionProducts`: PromotionId, ProductId.
- `PromotionCategories`: PromotionId, CategoryId.
- `Coupons`: CouponId, Code, PromotionId, UsageLimit, UsedCount, Status.
- `Orders`: OrderId, UserId, BranchId, FulfillmentType, DeliveryAddressSnapshot, Subtotal, DiscountAmount, ShippingFee, TotalAmount, OrderStatus, PaymentStatus, CreatedAt.
- `OrderItems`: OrderItemId, OrderId, ProductId, ProductNameSnapshot, UnitPrice, Quantity, DiscountAmount, LineTotal.

### Payment và review

- `Payments`: PaymentId, OrderId, Method, Provider, ProviderTransactionId, RequestId, Amount, Status, PaidAt, CreatedAt.
- `PaymentCallbacks`: CallbackId, Provider, ExternalEventId, PayloadHash, ProcessedAt.
- `Reviews`: ReviewId, UserId, ProductId, OrderItemId, Rating, Comment, Status, CreatedAt.

### AI

- `ProductViewEvents`: EventId, UserId nullable, ProductId, BranchId, ViewedAt.
- `RecommendationResults`: UserId, ProductId, Score, Reason, GeneratedAt.
- `DemandForecasts`: BranchId, ProductId, ForecastDate, PredictedQuantity, GeneratedAt.
- `StockAlerts`: AlertId, BranchId, ProductId, AlertLevel, RecommendedQuantity, Status, CreatedAt.

Không có bảng hoặc cột lưu số thẻ, ngày hết hạn hay CVV. Thông tin nhạy cảm của cổng thanh toán nằm trong environment variables.

## 5. Luồng nghiệp vụ

### Catalog và chi nhánh

Khách chọn chi nhánh trước hoặc được gợi ý theo địa chỉ. Giá và tồn kho hiển thị lấy từ `BranchInventories`. Tìm kiếm hỗ trợ từ khóa, danh mục, thương hiệu, khoảng giá, sắp xếp và phân trang.

### Cart và checkout

Mỗi cart gắn với một chi nhánh. Khi đổi chi nhánh, backend kiểm tra lại giá và tồn kho; item không khả dụng được trả về để khách quyết định. Checkout luôn tính lại giá, khuyến mãi và tồn kho ở backend. Order được tạo ở `Pending` và số lượng được chuyển sang `ReservedQuantity` trong cùng transaction MySQL.

### Payment

- COD tạo đơn với `PaymentStatus = PendingCollection`.
- VNPay/MoMo sandbox tạo payment request và redirect khách sang trang sandbox.
- Return URL chỉ dùng hiển thị kết quả.
- IPN hợp lệ mới cập nhật trạng thái payment/order.
- Chữ ký, mã đơn, số tiền và trạng thái hiện tại phải được kiểm tra.
- `ExternalEventId` hoặc `RequestId` duy nhất ngăn callback lặp xử lý hai lần.
- Payment thất bại hoặc hết hạn giải phóng lượng hàng đã giữ.

### Order

Luồng hợp lệ:

```text
Pending → Confirmed → Preparing → ReadyForPickup/Shipping → Completed
Pending/Confirmed → Cancelled
```

Admin không được nhảy qua trạng thái không hợp lệ. Chỉ order `Completed` mới được review.

## 6. Tính năng AI

### Gợi ý sản phẩm

Giai đoạn đầu dùng content-based scoring:

- Cùng danh mục và thương hiệu với sản phẩm đã xem/mua.
- Khoảng giá gần với hành vi của khách.
- Thêm trọng số cho bán chạy và promotion.
- Loại sản phẩm ngừng bán hoặc hết hàng tại chi nhánh đã chọn.

Nếu khách chưa có lịch sử, hệ thống trả về sản phẩm bán chạy, cùng danh mục đang xem và sản phẩm khuyến mãi. AI lỗi không được làm lỗi trang; frontend dùng danh sách fallback.

### Dự báo nhu cầu và cảnh báo nhập hàng

- Seed 6–12 tháng order lịch sử giả lập theo sản phẩm/chi nhánh.
- Tổng hợp số lượng bán theo ngày.
- Dự báo 7–14 ngày bằng moving average hoặc ML.NET time-series nếu kịp tiến độ.
- Công thức cảnh báo: `PredictedDemand + SafetyStock > QuantityOnHand - ReservedQuantity`.
- Kết quả hiển thị mức `Đủ hàng`, `Sắp thiếu`, `Cần nhập` và số lượng đề xuất.

Không dùng LLM trả phí cho hai tính năng AI này.

## 7. Bảo mật

- Hash mật khẩu bằng ASP.NET Core Identity/PBKDF2 hoặc BCrypt.
- JWT access token ngắn hạn; refresh token lưu dạng hash và có thể thu hồi.
- Enforce role ở backend.
- Validate request, giới hạn upload và rate-limit endpoint đăng nhập/reset password.
- Không trả stack trace cho frontend.
- Kiểm tra chữ ký IPN VNPay/MoMo và chống xử lý lặp.
- Audit log các thay đổi quan trọng của Admin nếu còn thời gian; log thanh toán là bắt buộc.
- Secret lưu trong environment variables; repository chỉ có `.env.example`.

## 8. Kiểm thử

- Unit test: tính giá, promotion, trạng thái order, reserve/release inventory và cảnh báo nhập hàng.
- Integration test: API với MySQL test database.
- Payment test: chữ ký sai/đúng, callback lặp, sai amount, order không tồn tại.
- Authorization test: Customer không gọi được API Admin.
- Concurrency test: hai checkout tranh sản phẩm cuối cùng.
- AI test: cold-start fallback, loại sản phẩm hết hàng và kết quả forecast có dữ liệu.
- React test cho cart, checkout và màn hình Admin trọng yếu.
- Một E2E demo: duyệt hàng → cart → checkout → payment sandbox/COD → hoàn thành → review.

## 9. Phân công nhóm

### Thành viên A — Backend core

- Authentication, Catalog, Branch, Inventory.
- Cart, Promotion, Order và business rules.
- Database migrations và integration tests backend.

### Thành viên B — Frontend

- Storefront, tìm kiếm, sản phẩm, cart và checkout.
- Customer account, order history và review.
- Admin portal, dashboard/report và frontend tests.

### Thành viên C — Integration, payment, AI và QA

- VNPay/MoMo sandbox, COD và callback handling.
- Seed data, reporting queries.
- Recommendation, demand forecast và stock alert.
- Docker Compose, E2E, tài liệu và hỗ trợ QA.

Ba thành viên thống nhất API contract/OpenAPI từ đầu. Thành viên B dùng mock API trong khi A xây backend; C tích hợp payment sau khi Order API ổn định.

## 10. Kế hoạch 30 ngày cấp cao

- Ngày 1–3: khóa scope, wireframe, ERD, API contract, repository và Docker MySQL.
- Ngày 4–8: authentication, catalog, branch/inventory; storefront catalog.
- Ngày 9–14: cart, promotion, checkout, order; frontend tương ứng.
- Ngày 15–18: COD, VNPay/MoMo sandbox, order management.
- Ngày 19–21: review, reporting, admin dashboard và test nghiệp vụ chính.
- Ngày 22–24: tích hợp toàn hệ thống, sửa lỗi, hoàn thiện Docker và seed data.
- Ngày 25–27: recommendation, demand forecast và stock alert.
- Ngày 28–29: E2E, security checks, regression và chuẩn bị demo.
- Ngày 30: đóng băng code, tài liệu, slide và rehearsal.

Nếu tiến độ chậm, thứ tự cắt giảm là: compare nâng cao, forgot-password gửi email thật, audit log đầy đủ, ML.NET forecast (giữ moving average), sau đó mới giảm phạm vi AI UI. Không cắt checkout, tồn kho đa chi nhánh, payment sandbox hoặc phân quyền.

## 11. Tiêu chí hoàn thành

- Guest tìm kiếm/lọc/phân trang và xem giá/tồn kho theo chi nhánh.
- Customer đặt được COD, VNPay sandbox và MoMo sandbox.
- Hỗ trợ giao tận nơi và nhận tại chi nhánh.
- Không bán âm kho khi checkout đồng thời.
- Admin quản lý catalog, tồn kho, promotion, order và báo cáo.
- Customer chỉ review sản phẩm đã mua trong order hoàn thành.
- Recommendation có kết quả hợp lệ và cold-start fallback.
- Admin xem được forecast và stock alert từ dữ liệu seed.
- Các test nghiệp vụ/payment/authorization quan trọng chạy qua.
- Dự án chạy bằng Docker Compose và có README demo.

## 12. Ngoài phạm vi

- Thanh toán production và lưu thẻ.
- Microservices, Redis, message broker, Kubernetes và autoscaling.
- Theo dõi shipper thời gian thực hoặc tích hợp hãng vận chuyển thật.
- Hóa đơn điện tử, ERP, kế toán và quản lý nhà cung cấp đầy đủ.
- Ứng dụng mobile native.
- LLM chatbot hoặc dịch vụ AI trả phí.
