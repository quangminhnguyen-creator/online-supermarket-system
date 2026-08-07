# Sprint 1 ERD

Sprint 1 tạo năm bảng nền tảng cho catalog và tồn kho đa chi nhánh. Giá bán và số lượng không nằm trực tiếp trên `products`; chúng thuộc quan hệ `branch_inventories` để mỗi chi nhánh có dữ liệu riêng.

```mermaid
erDiagram
    BRANCHES ||--o{ BRANCH_INVENTORIES : stocks
    PRODUCTS ||--o{ BRANCH_INVENTORIES : stocked_at
    CATEGORIES ||--o{ PRODUCTS : classifies
    CATEGORIES o|--o{ CATEGORIES : parent_of
    BRANDS ||--o{ PRODUCTS : brands

    BRANCHES {
        char36 id PK
        varchar150 name
        varchar300 address
        varchar20 phone
        decimal latitude
        decimal longitude
        boolean is_active
    }

    CATEGORIES {
        char36 id PK
        varchar120 name
        varchar140 slug UK
        char36 parent_category_id FK
        boolean is_active
    }

    BRANDS {
        char36 id PK
        varchar120 name
        varchar140 slug UK
        boolean is_active
    }

    PRODUCTS {
        char36 id PK
        char36 category_id FK
        char36 brand_id FK
        varchar64 sku UK
        varchar200 name
        varchar220 slug UK
        text description
        decimal base_price
        varchar30 unit
        varchar500 image_url
        boolean is_active
    }

    BRANCH_INVENTORIES {
        char36 id PK
        char36 branch_id FK
        char36 product_id FK
        decimal selling_price
        int quantity_on_hand
        int reserved_quantity
        int reorder_level
        datetime updated_at_utc
    }
```

## Ràng buộc chính

- `(branch_id, product_id)` là unique key trên `branch_inventories`.
- `sku`, product `slug`, category `slug` và brand `slug` là duy nhất.
- `available_quantity = quantity_on_hand - reserved_quantity` được tính trong domain, không lưu thành cột.
- Xóa category/brand/product/branch đang được tham chiếu bị hạn chế; category cha bị xóa sẽ đặt `parent_category_id` thành `NULL`.
- Tiền dùng `decimal(18,2)`; identifier dùng UUID lưu dưới dạng `char(36)`.
