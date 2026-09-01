import React, { useEffect, useRef, useState } from 'react'
import {
  adminCatalogApi,
  AdminProductDto,
  AdminCategoryDto,
  AdminBrandDto,
  UpsertProductRequest,
} from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'
import { AdminConfirmDialog } from '../AdminConfirmDialog'

const PAGE_SIZE = 20

export function AdminProductsPage() {
  const { accessToken } = useAuth()

  // Data states
  const [products, setProducts] = useState<AdminProductDto[]>([])
  const [categories, setCategories] = useState<AdminCategoryDto[]>([])
  const [brands, setBrands] = useState<AdminBrandDto[]>([])

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [lookupLoading, setLookupLoading] = useState(true)
  const [lookupError, setLookupError] = useState<string | null>(null)

  // Pagination states
  const [currentPage, setCurrentPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)

  // Filter states
  const [search, setSearch] = useState('')
  const [filterCategoryId, setFilterCategoryId] = useState('')
  const [filterBrandId, setFilterBrandId] = useState('')
  const [filterStatus, setFilterStatus] = useState<'all' | 'active' | 'inactive'>('all')

  // Form states
  const [editingId, setEditingId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [sku, setSku] = useState('')
  const [slug, setSlug] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [brandId, setBrandId] = useState('')
  const [basePrice, setBasePrice] = useState('')
  const [unit, setUnit] = useState('')
  const [description, setDescription] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Deactivate dialog state
  const [deactivateDialog, setDeactivateDialog] = useState<{
    isOpen: boolean
    product: AdminProductDto | null
    isBusy: boolean
  }>({
    isOpen: false,
    product: null,
    isBusy: false,
  })

  // Action busy state for individual items
  const [actionBusyId, setActionBusyId] = useState<string | null>(null)

  const abortControllerRef = useRef<AbortController | null>(null)
  const lookupAbortRef = useRef<AbortController | null>(null)

  // Load categories and brands on mount
  const loadLookups = async () => {
    if (!accessToken) return
    lookupAbortRef.current?.abort()
    const controller = new AbortController()
    lookupAbortRef.current = controller

    setLookupLoading(true)
    setLookupError(null)
    try {
      const [cats, brs] = await Promise.all([
        adminCatalogApi.getCategories(accessToken, controller.signal),
        adminCatalogApi.getBrands(accessToken, controller.signal),
      ])
      if (lookupAbortRef.current === controller && !controller.signal.aborted) {
        setCategories(cats)
        setBrands(brs)
        setLookupLoading(false)
      }
    } catch (err: any) {
      if (lookupAbortRef.current === controller && !controller.signal.aborted && err.name !== 'AbortError') {
        setLookupError(err.data?.message || err.message || 'Lỗi tải danh mục và thương hiệu')
        setLookupLoading(false)
      }
    }
  }

  useEffect(() => {
    loadLookups()
    return () => {
      lookupAbortRef.current?.abort()
    }
  }, [accessToken])

  // Load products when filters or page change
  const loadProducts = async () => {
    if (!accessToken) return
    abortControllerRef.current?.abort()
    const controller = new AbortController()
    abortControllerRef.current = controller

    setLoading(true)
    setError(null)

    let isActiveParam: boolean | undefined = undefined
    if (filterStatus === 'active') isActiveParam = true
    if (filterStatus === 'inactive') isActiveParam = false

    try {
      const response = await adminCatalogApi.getProducts(
        {
          page: currentPage,
          pageSize: PAGE_SIZE,
          search: search.trim() || undefined,
          categoryId: filterCategoryId || undefined,
          brandId: filterBrandId || undefined,
          isActive: isActiveParam,
        },
        accessToken,
        controller.signal
      )

      if (abortControllerRef.current === controller && !controller.signal.aborted) {
        setProducts(response.items)
        setTotalCount(response.meta.totalCount)
        setTotalPages(response.meta.totalPages)
        setLoading(false)
      }
    } catch (err: any) {
      if (abortControllerRef.current === controller && !controller.signal.aborted && err.name !== 'AbortError') {
        setError(err.data?.message || err.message || 'Lỗi tải danh sách sản phẩm')
        setProducts([])
        setLoading(false)
      }
    }
  }

  useEffect(() => {
    loadProducts()
    return () => {
      abortControllerRef.current?.abort()
    }
  }, [accessToken, currentPage, search, filterCategoryId, filterBrandId, filterStatus])

  // Reset page when filter changes
  const handleSearchChange = (val: string) => {
    setSearch(val)
    setCurrentPage(1)
  }

  const handleFilterCategoryChange = (val: string) => {
    setFilterCategoryId(val)
    setCurrentPage(1)
  }

  const handleFilterBrandChange = (val: string) => {
    setFilterBrandId(val)
    setCurrentPage(1)
  }

  const handleFilterStatusChange = (val: 'all' | 'active' | 'inactive') => {
    setFilterStatus(val)
    setCurrentPage(1)
  }

  const resetForm = () => {
    setEditingId(null)
    setName('')
    setSku('')
    setSlug('')
    setCategoryId('')
    setBrandId('')
    setBasePrice('')
    setUnit('')
    setDescription('')
    setImageUrl('')
    setFormError(null)
  }

  const handleStartEdit = (prod: AdminProductDto) => {
    setEditingId(prod.id)
    setName(prod.name)
    setSku(prod.sku)
    setSlug(prod.slug)
    setCategoryId(prod.categoryId)
    setBrandId(prod.brandId)
    setBasePrice(prod.basePrice.toString())
    setUnit(prod.unit)
    setDescription(prod.description || '')
    setImageUrl(prod.imageUrl || '')
    setFormError(null)
    if (typeof window.scrollTo === 'function') {
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  }

  const handleCancelEdit = () => {
    resetForm()
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!accessToken) return

    setFormError(null)

    const trimmedName = name.trim()
    const trimmedSku = sku.trim()
    const trimmedSlug = slug.trim()
    const trimmedUnit = unit.trim()
    const parsedPrice = parseFloat(basePrice)

    if (!trimmedName || !trimmedSku || !trimmedSlug || !categoryId || !brandId || !trimmedUnit) {
      setFormError('Vui lòng điền đầy đủ các trường bắt buộc.')
      return
    }

    if (isNaN(parsedPrice) || parsedPrice < 0) {
      setFormError('Giá sản phẩm phải là số không âm.')
      return
    }

    const payload: UpsertProductRequest = {
      categoryId,
      brandId,
      sku: trimmedSku,
      name: trimmedName,
      slug: trimmedSlug,
      description: description.trim() || null,
      basePrice: parsedPrice,
      unit: trimmedUnit,
      imageUrl: imageUrl.trim() || null,
    }

    setIsSubmitting(true)
    try {
      if (editingId) {
        await adminCatalogApi.updateProduct(editingId, payload, accessToken)
      } else {
        await adminCatalogApi.createProduct(payload, accessToken)
      }
      resetForm()
      await loadProducts()
    } catch (err: any) {
      setFormError(err.data?.message || err.message || 'Lỗi khi lưu sản phẩm')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleOpenDeactivateDialog = (prod: AdminProductDto) => {
    setDeactivateDialog({
      isOpen: true,
      product: prod,
      isBusy: false,
    })
  }

  const handleCloseDeactivateDialog = () => {
    if (deactivateDialog.isBusy) return
    setDeactivateDialog({ isOpen: false, product: null, isBusy: false })
  }

  const handleConfirmDeactivate = async () => {
    if (!accessToken || !deactivateDialog.product) return

    setDeactivateDialog(prev => ({ ...prev, isBusy: true }))
    try {
      await adminCatalogApi.updateProductStatus(
        deactivateDialog.product.id,
        { isActive: false },
        accessToken
      )
      setDeactivateDialog({ isOpen: false, product: null, isBusy: false })
      await loadProducts()
    } catch (err: any) {
      setError(err.data?.message || err.message || 'Lỗi vô hiệu hóa sản phẩm')
      setDeactivateDialog(prev => ({ ...prev, isBusy: false, isOpen: false }))
    }
  }

  const handleRestore = async (prod: AdminProductDto) => {
    if (!accessToken || actionBusyId) return

    setActionBusyId(prod.id)
    setError(null)
    try {
      await adminCatalogApi.updateProductStatus(prod.id, { isActive: true }, accessToken)
      await loadProducts()
    } catch (err: any) {
      setError(err.data?.message || err.message || 'Lỗi kích hoạt lại sản phẩm')
    } finally {
      setActionBusyId(null)
    }
  }

  const isFormDisabled = isSubmitting || lookupLoading || lookupError !== null

  return (
    <div className="admin-page">
      <header className="admin-header">
        <h1>Quản lý sản phẩm</h1>
      </header>

      {lookupError && (
        <div role="alert" className="admin-error-alert">
          <span>{lookupError}</span>
          <button type="button" onClick={loadLookups}>Thử lại tải danh mục & thương hiệu</button>
        </div>
      )}

      {error && (
        <div role="alert" className="admin-error-alert">
          <span>{error}</span>
          <button type="button" onClick={loadProducts}>Thử lại</button>
        </div>
      )}

      {/* Form Add / Edit */}
      <div className="admin-card">
        <h2>{editingId ? 'Chỉnh sửa sản phẩm' : 'Thêm sản phẩm mới'}</h2>
        {formError && <div role="alert" className="admin-error-alert">{formError}</div>}

        <form onSubmit={handleSubmit} className="admin-form admin-product-form">
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="prod-name">Tên sản phẩm *:</label>
              <input
                id="prod-name"
                type="text"
                value={name}
                onChange={e => setName(e.target.value)}
                disabled={isFormDisabled}
                placeholder="VD: Smart Tivi Samsung 55 inch..."
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="prod-sku">Mã SKU *:</label>
              <input
                id="prod-sku"
                type="text"
                value={sku}
                onChange={e => setSku(e.target.value)}
                disabled={isFormDisabled}
                placeholder="VD: TV-SAM-55-01"
                required
              />
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="prod-slug">Slug *:</label>
              <input
                id="prod-slug"
                type="text"
                value={slug}
                onChange={e => setSlug(e.target.value)}
                disabled={isFormDisabled}
                placeholder="VD: smart-tivi-samsung-55-inch"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="prod-category">Danh mục *:</label>
              <select
                id="prod-category"
                value={categoryId}
                onChange={e => setCategoryId(e.target.value)}
                disabled={isFormDisabled}
                required
              >
                <option value="">
                  {lookupLoading ? '-- Đang tải danh mục... --' : lookupError ? '-- Lỗi tải danh mục --' : '-- Chọn danh mục --'}
                </option>
                {categories.map(c => (
                  <option key={c.id} value={c.id}>
                    {c.name} {!c.isActive ? '[Vô hiệu hóa]' : ''}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="prod-brand">Thương hiệu *:</label>
              <select
                id="prod-brand"
                value={brandId}
                onChange={e => setBrandId(e.target.value)}
                disabled={isFormDisabled}
                required
              >
                <option value="">
                  {lookupLoading ? '-- Đang tải thương hiệu... --' : lookupError ? '-- Lỗi tải thương hiệu --' : '-- Chọn thương hiệu --'}
                </option>
                {brands.map(b => (
                  <option key={b.id} value={b.id}>
                    {b.name} {!b.isActive ? '[Vô hiệu hóa]' : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="prod-price">Giá gốc (VNĐ) *:</label>
              <input
                id="prod-price"
                type="number"
                min="0"
                step="any"
                value={basePrice}
                onChange={e => setBasePrice(e.target.value)}
                disabled={isFormDisabled}
                placeholder="VD: 12500000"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="prod-unit">Đơn vị tính *:</label>
              <input
                id="prod-unit"
                type="text"
                value={unit}
                onChange={e => setUnit(e.target.value)}
                disabled={isFormDisabled}
                placeholder="VD: cái, hộp, kg..."
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="prod-image">URL hình ảnh:</label>
              <input
                id="prod-image"
                type="text"
                value={imageUrl}
                onChange={e => setImageUrl(e.target.value)}
                disabled={isFormDisabled}
                placeholder="VD: https://example.com/images/product.jpg"
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="prod-desc">Mô tả sản phẩm:</label>
            <textarea
              id="prod-desc"
              rows={3}
              value={description}
              onChange={e => setDescription(e.target.value)}
              disabled={isFormDisabled}
              placeholder="Mô tả thông số hoặc đặc điểm nổi bật..."
            />
          </div>

          <div className="form-actions">
            <button type="submit" disabled={isFormDisabled} className="btn btn-primary">
              {isSubmitting ? 'Đang lưu...' : editingId ? 'Cập nhật sản phẩm' : 'Thêm sản phẩm'}
            </button>
            {editingId && (
              <button
                type="button"
                onClick={handleCancelEdit}
                disabled={isSubmitting}
                className="btn btn-secondary"
              >
                Hủy chỉnh sửa
              </button>
            )}
          </div>
        </form>
      </div>

      {/* Filter and Products List */}
      <div className="admin-card">
        <h2>Danh sách sản phẩm ({totalCount})</h2>

        {/* Filters */}
        <div className="admin-filters">
          <div className="filter-group">
            <label htmlFor="filter-search">Tìm kiếm:</label>
            <input
              id="filter-search"
              type="text"
              value={search}
              onChange={e => handleSearchChange(e.target.value)}
              placeholder="Tên sản phẩm hoặc SKU..."
            />
          </div>

          <div className="filter-group">
            <label htmlFor="filter-cat">Danh mục:</label>
            <select
              id="filter-cat"
              value={filterCategoryId}
              onChange={e => handleFilterCategoryChange(e.target.value)}
            >
              <option value="">Tất cả danh mục</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div className="filter-group">
            <label htmlFor="filter-brand">Thương hiệu:</label>
            <select
              id="filter-brand"
              value={filterBrandId}
              onChange={e => handleFilterBrandChange(e.target.value)}
            >
              <option value="">Tất cả thương hiệu</option>
              {brands.map(b => (
                <option key={b.id} value={b.id}>{b.name}</option>
              ))}
            </select>
          </div>

          <div className="filter-group">
            <label htmlFor="filter-status">Trạng thái:</label>
            <select
              id="filter-status"
              value={filterStatus}
              onChange={e => handleFilterStatusChange(e.target.value as any)}
            >
              <option value="all">Tất cả</option>
              <option value="active">Hoạt động</option>
              <option value="inactive">Vô hiệu hóa</option>
            </select>
          </div>
        </div>

        {/* Product Table */}
        {loading ? (
          <p className="admin-loading">Đang tải sản phẩm...</p>
        ) : products.length === 0 ? (
          <p className="admin-empty">Không tìm thấy sản phẩm nào.</p>
        ) : (
          <>
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Ảnh</th>
                  <th>SKU</th>
                  <th>Tên sản phẩm</th>
                  <th>Danh mục</th>
                  <th>Thương hiệu</th>
                  <th>Giá gốc</th>
                  <th>Đơn vị</th>
                  <th>Trạng thái</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {products.map(p => (
                  <tr key={p.id} className={!p.isActive ? 'row-inactive' : ''}>
                    <td>
                      {p.imageUrl ? (
                        <img
                          src={p.imageUrl}
                          alt={p.name}
                          className="admin-product-thumb"
                          onError={e => {
                            ;(e.target as HTMLElement).style.display = 'none'
                          }}
                        />
                      ) : (
                        <span className="admin-product-thumb-placeholder">—</span>
                      )}
                    </td>
                    <td><code>{p.sku}</code></td>
                    <td><strong>{p.name}</strong></td>
                    <td>{p.categoryName}</td>
                    <td>{p.brandName}</td>
                    <td>{p.basePrice.toLocaleString('vi-VN')} đ</td>
                    <td>{p.unit}</td>
                    <td>
                      <span className={`badge ${p.isActive ? 'badge-active' : 'badge-inactive'}`}>
                        {p.isActive ? 'Hoạt động' : 'Vô hiệu hóa'}
                      </span>
                    </td>
                    <td className="table-actions">
                      <button
                        type="button"
                        onClick={() => handleStartEdit(p)}
                        disabled={actionBusyId === p.id}
                        className="btn btn-sm btn-secondary"
                      >
                        Sửa
                      </button>
                      {p.isActive ? (
                        <button
                          type="button"
                          onClick={() => handleOpenDeactivateDialog(p)}
                          disabled={actionBusyId === p.id}
                          className="btn btn-sm btn-danger"
                        >
                          Vô hiệu hóa
                        </button>
                      ) : (
                        <button
                          type="button"
                          onClick={() => handleRestore(p)}
                          disabled={actionBusyId === p.id}
                          className="btn btn-sm btn-success"
                        >
                          {actionBusyId === p.id ? 'Đang xử lý...' : 'Kích hoạt lại'}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Pagination Controls */}
            {totalPages > 1 && (
              <div className="admin-pagination">
                <button
                  type="button"
                  disabled={currentPage <= 1}
                  onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                  className="btn btn-sm btn-secondary"
                >
                  Trang trước
                </button>
                <span className="pagination-info">
                  Trang {currentPage} / {totalPages} ({totalCount} sản phẩm)
                </span>
                <button
                  type="button"
                  disabled={currentPage >= totalPages}
                  onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                  className="btn btn-sm btn-secondary"
                >
                  Trang sau
                </button>
              </div>
            )}
          </>
        )}
      </div>

      {/* Confirmation Dialog for Deactivation */}
      <AdminConfirmDialog
        isOpen={deactivateDialog.isOpen}
        title="Vô hiệu hóa sản phẩm"
        message={`Bạn có chắc chắn muốn vô hiệu hóa sản phẩm "${deactivateDialog.product?.name}" (SKU: ${deactivateDialog.product?.sku})?`}
        confirmLabel="Vô hiệu hóa"
        onConfirm={handleConfirmDeactivate}
        onCancel={handleCloseDeactivateDialog}
        isBusy={deactivateDialog.isBusy}
      />
    </div>
  )
}
