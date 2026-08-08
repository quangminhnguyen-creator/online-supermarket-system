# Online Supermarket System

Đồ án siêu thị trực tuyến đa chi nhánh dùng React, ASP.NET Core và MySQL. Sprint 1 cung cấp solution backend, storefront shell, mô hình catalog/tồn kho, migration, Docker Compose, ERD và OpenAPI contract.

## Công nghệ

- Backend: .NET 10, ASP.NET Core Minimal API, EF Core 10 và MySQL Connector/NET.
- Frontend: React 19, TypeScript, Vite 7 và Vitest.
- Database: MySQL 8.4 LTS.
- Local deployment: Docker Compose.

## Cấu trúc

```text
backend/src/OnlineSupermarket.Api             HTTP composition root
backend/src/OnlineSupermarket.Domain          entity và business invariants
backend/src/OnlineSupermarket.Infrastructure  EF Core, MySQL và migrations
backend/tests                                 unit/integration tests
frontend                                     React storefront
docs/architecture                            ERD
docs/api                                     OpenAPI contract
```

## Chạy backend native

Yêu cầu .NET SDK 10.0.103.

```powershell
dotnet restore OnlineSupermarket.slnx
dotnet test OnlineSupermarket.slnx --no-restore
dotnet run --project backend/src/OnlineSupermarket.Api
```

Health endpoint: `http://localhost:5000/api/health` hoặc cổng hiển thị trong terminal.

Tạo/cập nhật migration bằng local tool đã khóa phiên bản:

```powershell
dotnet tool restore
dotnet dotnet-ef database update --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
```

## Chạy frontend native

Yêu cầu Node.js 24 LTS. Trên Windows PowerShell, dùng `npm.cmd` nếu Execution Policy chặn `npm.ps1`.

```powershell
Set-Location frontend
npm.cmd install
npm.cmd test -- --run
npm.cmd run dev
```

Vite chạy tại `http://localhost:5173` và proxy `/api` sang `http://localhost:8080`.

## Chạy full stack bằng Docker

Yêu cầu Docker Desktop có lệnh `docker compose`.

```powershell
Copy-Item .env.example .env
docker compose config --quiet
docker compose up --build -d
Invoke-RestMethod http://localhost:8080/api/health
Invoke-WebRequest http://localhost:5173
docker compose down
```

Không commit `.env`. Các giá trị trong `.env.example` chỉ dành cho local demo.

Compose hiện không tự động apply migration; smoke test trên chỉ xác nhận container và HTTP endpoint. Trước khi chạy demo có dữ liệu, cần apply migration có chủ đích trên MySQL 8.4 và xác minh năm bảng foundation cùng các unique index bắt buộc.

## Xuất OpenAPI

OpenAPI chỉ được bật trong môi trường Development:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/export-openapi.ps1
```

Contract được ghi vào `docs/api/openapi.json`.

## Tài liệu

- [Đặc tả hệ thống](docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md)
- [Kế hoạch Sprint 1](docs/superpowers/plans/2026-08-07-sprint-1-foundation.md)
- [ERD mục tiêu toàn hệ thống](docs/architecture/erd.md) — Sprint 1 mới hiện thực năm bảng foundation; các bảng còn lại là kế hoạch.

## Trạng thái Sprint 1

- Backend, frontend, test, migration năm bảng foundation, Docker config và OpenAPI: đã tạo.
- ERD mô tả mục tiêu toàn hệ thống; không đồng nghĩa toàn bộ 22 bảng đã được hiện thực trong Sprint 1.
- Docker smoke test: cần Docker Desktop; máy phát triển hiện tại chưa có `docker` trong PATH.
- Auth, catalog CRUD, cart, order, payment sandbox và AI: thuộc các sprint/kế hoạch tiếp theo.
