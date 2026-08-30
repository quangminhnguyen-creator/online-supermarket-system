# Plan: Tính năng So sánh tối đa 2 sản phẩm cùng danh mục

## Context

**Vấn đề:** Người dùng muốn dễ dàng so sánh các sản phẩm cùng danh mục để đưa ra quyết định mua hàng.

**Giải pháp:** Thêm tính năng so sánh với:
- Modal/Popup hiển thị bảng so sánh
- Nút "So sánh" ở cả ProductCard và ProductDetailPage
- Hiển thị đầy đủ: Tên, Hình ảnh, Giá, Thương hiệu, Mô tả, Đơn vị, Tồn kho, SKU, Danh mục

---

## Các bước thực hiện

### 1. Tạo CompareContext (`frontend/src/features/compare/CompareContext.tsx`)

**Mục đích:** Quản lý state so sánh sản phẩm (thêm/xóa/reset)

```typescript
interface CompareProduct {
  id: string
  categoryId: string
  categoryName: string
}

interface CompareContextValue {
  compareProducts: CompareProduct[]
  isInCompare: (id: string) => boolean
  addToCompare: (product: CompareProduct) => void
  removeFromCompare: (id: string) => void
  clearCompare: () => void
  canAddMore: boolean
}
```

### 2. Tạo CompareModal (`frontend/src/features/compare/CompareModal.tsx`)

**Mục đích:** Modal popup hiển thị bảng so sánh

**Tính năng:**
- Header với tiêu đề "So sánh sản phẩm" và nút đóng
- Nút "Xóa tất cả" khi có sản phẩm
- Bảng so sánh với các cột: thuộc tính, Sản phẩm 1, Sản phẩm 2
- Loading state khi đang fetch chi tiết sản phẩm
- Nút "Xóa" cho từng sản phẩm
- Nút "Mua ngay" chuyển đến trang chi tiết sản phẩm

**Các thuộc tính so sánh:**
| Thuộc tính | Mô tả |
|------------|-------|
| Hình ảnh | Ảnh sản phẩm |
| Tên sản phẩm | Tên đầy đủ |
| SKU | Mã sản phẩm |
| Thương hiệu | Tên thương hiệu |
| Danh mục | Tên danh mục |
| Giá cơ bản | Giá gốc (format VND) |
| Giá bán | Giá tại kho (nếu chọn kho) |
| Tồn kho | Số lượng có sẵn |
| Đơn vị | Đơn vị tính |
| Mô tả | Mô tả sản phẩm |

### 3. Tạo CompareModal.css (`frontend/src/features/compare/CompareModal.css`)

**Styles:** Thiết kế modal với overlay, bảng so sánh responsive

### 4. Cập nhật App.tsx

**Thay đổi:**
- Import `CompareProvider` từ CompareContext
- Bọc `CompareProvider` quanh app

### 5. Thêm nút so sánh vào ProductCard

**File:** `frontend/src/features/products/ProductCard.tsx`

**Thay đổi:**
- Sử dụng `useCompare()` để lấy context
- Thêm icon/nút so sánh (checkmark khi đã chọn, icon khi chưa)
- Click vào nút không navigate, chỉ toggle compare state
- Validate: chỉ cho thêm nếu cùng danh mục với sản phẩm đã có

### 6. Thêm styles cho ProductCard

**File:** `frontend/src/features/products/ProductCard.css`

**Thêm:**
- Style cho nút so sánh (góc phải trên card)
- State "selected" khi đã thêm vào so sánh

### 7. Thêm nút so sánh vào ProductDetailPage

**File:** `frontend/src/features/products/ProductDetailPage.tsx`

**Thay đổi:**
- Import `useCompare()`
- Thêm nút "So sánh" trong purchase panel
- Nếu sản phẩm đã trong danh sách so sánh: hiển thị "Đã thêm so sánh" (disabled)
- Nếu chưa: hiển thị "So sánh với..."

### 8. Thêm styles cho ProductDetailPage

**File:** `frontend/src/features/products/ProductDetailPage.css`

**Thêm:** Style cho nút so sánh

---

## Files cần tạo mới

| File | Mô tả |
|------|-------|
| `frontend/src/features/compare/CompareContext.tsx` | Context quản lý state so sánh |
| `frontend/src/features/compare/CompareModal.tsx` | Component modal so sánh |
| `frontend/src/features/compare/CompareModal.css` | Styles cho modal so sánh |

## Files cần sửa

| File | Thay đổi |
|------|----------|
| `frontend/src/App.tsx` | Thêm CompareProvider |
| `frontend/src/features/products/ProductCard.tsx` | Thêm nút so sánh |
| `frontend/src/features/products/ProductCard.css` | Style nút so sánh |
| `frontend/src/features/products/ProductDetailPage.tsx` | Thêm nút so sánh |
| `frontend/src/features/products/ProductDetailPage.css` | Style nút so sánh |

---

## Validation Logic

**Thêm sản phẩm vào so sánh:**
1. Kiểm tra đã có 2 sản phẩm chưa → nếu đủ thì không cho thêm
2. Kiểm tra có sản phẩm nào đang so sánh không → nếu có, kiểm tra cùng categoryId
3. Nếu khác danh mục → hiển thị toast/warning "Chỉ có thể so sánh sản phẩm cùng danh mục"

**Xóa sản phẩm khỏi so sánh:**
- Cho phép xóa bất kỳ sản phẩm nào đang trong danh sách

---

## Verification

1. **Chạy ứng dụng:** `cd frontend && npm run dev`
2. **Test thêm sản phẩm vào so sánh:**
   - Mở ProductBrowsePage
   - Click nút so sánh trên 2 sản phẩm cùng danh mục
   - Verify modal hiển thị với 2 sản phẩm
3. **Test validation:**
   - Thử thêm sản phẩm khác danh mục → phải có warning
   - Thử thêm sản phẩm thứ 3 → không cho phép
4. **Test xóa:**
   - Xóa 1 sản phẩm khỏi modal → verify được xóa
   - Xóa tất cả → modal đóng
5. **Test ProductDetailPage:**
   - Mở trang chi tiết sản phẩm
   - Verify nút so sánh hiển thị đúng trạng thái
6. **Test responsive:** Kiểm tra bảng so sánh trên mobile
