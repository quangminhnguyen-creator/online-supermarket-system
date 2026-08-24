# Online Supermarket System (Hệ Thống Siêu Thị Điện Tử Đa Chi Nhánh)

Hệ thống bán hàng và siêu thị điện tử trực tuyến đa chi nhánh xây dựng trên nền tảng **.NET 10 (ASP.NET Core Minimal API)**, **Entity Framework Core 10**, **MySQL 8.4 LTS** và **React 19 (TypeScript + Vite 7)**.

Dự án hỗ trợ quản lý danh mục sản phẩm dùng chung toàn hệ thống, đồng thời duy trì giá bán và lượng tồn kho độc lập theo từng chi nhánh. Hệ thống cung cấp đầy đủ chu trình: xác thực bảo mật JWT, quản lý sổ địa chỉ, giỏ hàng theo chi nhánh, quy trình checkout giao dịch đảm bảo tính toàn vẹn tồn kho, thanh toán sandbox (COD, VNPay, MoMo) và quản lý đơn hàng.

---

## 🛠️ Công Nghệ Sử Dụng

- **Backend Framework**: .NET 10, ASP.NET Core Minimal APIs.
- **ORM & Data Access**: Entity Framework Core 10, MySQL Connector / Pomelo.
- **Database**: MySQL 8.4 LTS (hỗ trợ UUID v7/CHAR(36), chỉ mục duy nhất và quan hệ toàn vẹn).
- **Bảo mật & Xác thực**: JWT Bearer Access Token, Refresh Token xoay vòng (Rotation) chống lạm dụng, mã hóa mật khẩu bảo mật (PBKDF2/HMAC-SHA256).
- **Dịch vụ nền**: .NET BackgroundService tự động thu hồi và dọn dẹp refresh token quá hạn.
- **Frontend**: React 19, TypeScript 5.9, Vite 7, Vitest, Testing Library.
- **Môi trường & Container**: Docker Compose, Nginx Reverse Proxy.

---

## 📁 Cấu Trúc Dự Án

```text
online-supermarket-system/
├── backend/
│   ├── src/
│   │   ├── OnlineSupermarket.Api/            # HTTP Composition Root & Minimal API Endpoints
│   │   │   ├── Contracts/                    # Request/Response DTOs & Models
│   │   │   ├── Endpoints/                    # Endpoint Handlers (Auth, Catalog, Cart, Orders,...)
│   │   │   └── Program.cs                    # Pipeline & Dependency Injection setup
│   │   ├── OnlineSupermarket.Domain/         # Domain Layer (Entities, Enums, Business Invariants)
│   │   │   ├── Branches/                     # Chi nhánh
│   │   │   ├── Catalog/                      # Danh mục, Thương hiệu, Sản phẩm
│   │   │   ├── Identity/                     # User, RefreshToken, PasswordResetToken, Role, Status
│   │   │   ├── Inventory/                    # Tồn kho chi nhánh (BranchInventory)
│   │   │   ├── Orders/                       # Đơn hàng, OrderItem, Trạng thái đơn hàng
│   │   │   ├── Payments/                     # Thanh toán, Callback giao dịch
│   │   │   └── Shopping/                     # Giỏ hàng, CartItem, Địa chỉ giao hàng
│   │   └── OnlineSupermarket.Infrastructure/ # Infrastructure Layer (EF Core, MySQL, Services)
│   │       ├── BackgroundServices/           # RefreshTokenCleanupService
│   │       ├── Identity/                     # PasswordHasher, TokenService
│   │       ├── Persistence/                  # AppDbContext, Configurations, Migrations
│   │       └── Services/                     # PasswordResetService, EmailSender
│   └── tests/
│       ├── OnlineSupermarket.Api.Tests/            # Integration & Configuration Tests
│       ├── OnlineSupermarket.Domain.Tests/         # Domain Unit Tests & Invariants
│       └── OnlineSupermarket.Infrastructure.Tests/ # Data Access & Service Tests
├── frontend/                                 # React 19 Storefront & Client App
│   ├── src/
│   │   ├── api/                              # HTTP Client & API wrappers
│   │   ├── app/                              # App Shell & Navigation
│   │   ├── features/                         # Auth, Address, Profile, System Status
│   │   └── styles/                           # CSS Modules & Design System
├── docs/                                     # Toàn bộ tài liệu kỹ thuật & kiến trúc
│   ├── api/                                  # openapi.json (OpenAPI 3.1.1 Contract)
│   ├── architecture/                         # ERD, DFD, Sitemap chi tiết
│   ├── requirements/                         # functional-requirements.md (Canonical SSoT)
│   └── project-spec.html                     # Đặc tả dự án tổng thể
├── compose.yaml                              # Docker Compose orchestration
└── OnlineSupermarket.slnx                    # Solution file cho Visual Studio / Rider / dotnet CLI
```

---

## 🚀 Hướng Dẫn Chạy Dự Án

### 1. Chạy Backend Native (.NET 10)

**Yêu cầu**: .NET SDK 10.0+ và MySQL 8.4 đang chạy trên máy host.

1. Thiết lập chuỗi kết nối MySQL trên PowerShell:
   ```powershell
   $env:ConnectionStrings__DefaultConnection = "Server=localhost;Port=3306;Database=online_supermarket;User=supermarket_app;Password=change_me"
   ```
2. Khôi phục packages và chạy tests:
   ```powershell
   dotnet restore OnlineSupermarket.slnx
   dotnet test OnlineSupermarket.slnx
   ```
3. Cập nhật Database Migration:
   ```powershell
   dotnet tool restore
   dotnet dotnet-ef database update --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
   ```
4. Khởi chạy Backend API:
   ```powershell
   dotnet run --project backend/src/OnlineSupermarket.Api
   ```
   - API mặc định lắng nghe tại: `http://localhost:5072` (hoặc cấu hình qua `launchSettings.json`).
   - Kiểm tra Health Endpoint: `http://localhost:5072/api/health`
   - OpenAPI Documentation (môi trường Dev): `http://localhost:5072/openapi/v1.json`

---

### 2. Chạy Frontend Native (React 19 + Vite)

**Yêu cầu**: Node.js 20+ LTS (khuyên dùng Node.js 22 hoặc 24).

```powershell
Set-Location frontend
npm.cmd install
npm.cmd test -- --run
npm.cmd run dev
```

- Giao diện người dùng sẽ chạy tại: `http://localhost:5173`.
- Vite đã cấu hình proxy `/api` tự động chuyển tiếp request về backend (`http://localhost:5072` hoặc cổng API được chỉ định).

---

### 3. Khởi Chạy Toàn Bộ Bằng Docker Compose

**Yêu cầu**: Docker Desktop đã cài đặt và đang chạy.

```powershell
# 1. Tạo file cấu hình môi trường từ mẫu
Copy-Item .env.example .env

# 2. Khởi tạo và khởi chạy các containers (MySQL 8.4, Backend API, Frontend Nginx)
docker compose up --build -d

# 3. Kiểm tra trạng thái hệ thống
Invoke-RestMethod http://localhost:8080/api/health
Invoke-WebRequest http://localhost:5173

# 4. Tắt các containers khi không dùng
docker compose down
```

---

## 📡 Danh Mục API Endpoints Chính

| Phân hệ | Endpoint | Phương thức | Mô tả |
|---|---|---|---|
| **System** | `/api/health` | `GET` | Kiểm tra trạng thái hoạt động của hệ thống |
| **Auth** | `/api/auth/register` | `POST` | Đăng ký tài khoản khách hàng mới |
| | `/api/auth/login` | `POST` | Đăng nhập nhận Access Token và Refresh Token |
| | `/api/auth/refresh` | `POST` | Làm mới Access Token bằng Refresh Token |
| | `/api/auth/logout` | `POST` | Đăng xuất và thu hồi Refresh Token |
| | `/api/auth/me` | `GET` | Lấy thông tin user hiện tại từ Token |
| | `/api/auth/password-reset` | `POST` | Yêu cầu gửi email đặt lại mật khẩu |
| | `/api/auth/password-reset/confirm` | `POST` | Xác nhận mật khẩu mới bằng token |
| **Users** | `/api/users/me` | `PUT` | Cập nhật họ tên, số điện thoại |
| | `/api/users/me/password` | `PUT` | Đổi mật khẩu tài khoản |
| | `/api/admin/users` | `GET` | *(Admin)* Danh sách người dùng hệ thống |
| | `/api/admin/users/{id}/status` | `PUT` | *(Admin)* Khóa / Mở khóa tài khoản |
| **Addresses**| `/api/users/me/addresses` | `GET`, `POST` | Danh sách & Thêm mới địa chỉ nhận hàng |
| | `/api/users/me/addresses/{id}` | `PUT`, `DELETE`| Cập nhật / Xóa địa chỉ nhận hàng |
| | `/api/users/me/addresses/{id}/default`| `PUT` | Đặt làm địa chỉ giao hàng mặc định |
| **Catalog** | `/api/categories` | `GET` | Danh mục sản phẩm dạng phân cấp |
| | `/api/brands` | `GET` | Danh sách thương hiệu |
| | `/api/products` | `GET` | Tìm kiếm, lọc sản phẩm (giá, danh mục, từ khóa, chi nhánh) |
| | `/api/products/{id}` | `GET` | Chi tiết sản phẩm kèm giá & tồn kho chi nhánh |
| **Branches**| `/api/branches` | `GET` | Danh sách chi nhánh siêu thị |
| | `/api/branches/{id}` | `GET` | Chi tiết chi nhánh |
| | `/api/branches/{id}/inventory` | `GET` | Tồn kho & giá bán sản phẩm tại chi nhánh |
| | `/api/admin/branches/{branchId}/inventory` | `PUT` | *(Admin)* Điều chỉnh giá bán, tồn kho, định mức nhập |
| **Cart** | `/api/cart` | `GET`, `DELETE` | Xem giỏ hàng / Xóa toàn bộ giỏ hàng |
| | `/api/cart/items` | `POST` | Thêm sản phẩm vào giỏ hàng (kiểm tra tồn kho chi nhánh) |
| | `/api/cart/items/{itemId}` | `PUT`, `DELETE` | Cập nhật số lượng / Xóa sản phẩm khỏi giỏ |
| | `/api/cart/change-branch` | `POST` | Đổi chi nhánh mua hàng |
| **Checkout**| `/api/checkout` | `POST` | Đặt hàng, khóa giữ tồn kho (reserve), tính phí giao hàng |
| | `/api/checkout/payment` | `POST` | Khởi tạo giao dịch thanh toán (COD, VNPay, MoMo) |
| | `/api/checkout/payment/callback` | `POST` | Webhook / IPN tiếp nhận kết quả thanh toán từ cổng |
| **Orders** | `/api/orders` | `GET` | Lịch sử đơn hàng của khách hàng |
| | `/api/orders/{id}` | `GET` | Chi tiết đơn hàng và lịch sử chuyển trạng thái |
| | `/api/admin/orders` | `GET` | *(Admin)* Xem toàn bộ đơn hàng hệ thống |
| | `/api/admin/orders/{id}/status`| `PUT` | *(Admin)* Cập nhật trạng thái đơn hàng |

---

## 📊 Trạng Thái Triển Khai Hiện Tại

| Nhóm chức năng | Hiện trạng | Ghi chú |
|---|---|---|
| **Identity & Authentication** | ✅ Hoàn thành | Đăng ký, Đăng nhập JWT, Refresh Token, Thu hồi token, Đặt lại mật khẩu qua email, Background dọn dẹp |
| **User Profile & Address Book** | ✅ Hoàn thành | Quản lý thông tin cá nhân, CRUD địa chỉ giao hàng, đặt địa chỉ mặc định transactional |
| **Multi-Branch Catalog & Stock** | ✅ Hoàn thành | Danh mục, Thương hiệu, Sản phẩm, Tồn kho độc lập theo từng chi nhánh, Quản lý giá theo chi nhánh |
| **Cart Management** | ✅ Hoàn thành | Giỏ hàng gắn với User & Chi nhánh, kiểm tra tồn kho tức thì, đổi chi nhánh cập nhật giá |
| **Transactional Checkout** | ✅ Hoàn thành | Đặt hàng giao dịch (Serializable / Retry), khóa giữ tồn kho (Reserved Stock), snapshot thông tin giao hàng |
| **Payment Gateway Sandbox** | ✅ Hoàn thành | COD, mô phỏng VNPay & MoMo sandbox URL, xử lý callback IPN và tự động hoàn trả tồn kho nếu thất bại |
| **Order Management** | ✅ Hoàn thành | Lịch sử đơn hàng, chi tiết đơn hàng, lịch sử thay đổi trạng thái, Admin cập nhật trạng thái đơn |
| **Promotions & Coupons** | ⏳ Kế hoạch mở rộng | Mô hình bảng đã thiết kế trong ERD, dự kiến triển khai engine coupon ở sprint tiếp theo |
| **Reviews & Ratings** | ⏳ Kế hoạch mở rộng | Đánh giá sản phẩm sau khi đơn hàng hoàn thành (Verified Purchase) |
| **Demand Forecast & AI** | ⏳ Kế hoạch mở rộng | Dự báo nhu cầu 7-14 ngày, cảnh báo tồn kho, gợi ý sản phẩm cold-start |

---

## 📚 Tài Liệu Kỹ Thuật

- [Danh mục Yêu cầu Chức năng Canonical (SSoT)](docs/requirements/functional-requirements.md)
- [Thiết kế Cơ sở Dữ liệu & ERD Toàn Hệ Thống](docs/architecture/erd.md)
- [Sơ đồ Luồng Dữ liệu (DFD Context, Level 0 & Level 1)](docs/architecture/dfd.md)
- [Sơ đồ Cây Điều hướng & Sitemap Chi Tiết](docs/architecture/sitemap.md)
- [Hợp đồng Giao tiếp API (OpenAPI 3.1.1 Contract)](docs/api/openapi.json)
- [Đặc tả Dự án Tổng Thể (Project Spec HTML)](docs/project-spec.html)
- [Bảng Theo Dõi Tiến Độ Thành Viên 1 & 3 (Backend & Frontend Dashboard)](docs/progress-member-1-3.html)
