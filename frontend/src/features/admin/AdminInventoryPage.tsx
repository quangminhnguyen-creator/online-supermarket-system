import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { branchApi, type BranchDto, type BranchProductInventoryDto } from '../../api/branchApi'
import { AdminInventoryModal } from './AdminInventoryModal'
import './AdminOrdersPage.css'
import './AdminInventoryPage.css'

function isAbortError(error: unknown) {
  return error instanceof Error && error.name === 'AbortError'
}

function formatPrice(value: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value)
}

function isLowStock(item: BranchProductInventoryDto) {
  return item.availableQuantity <= item.reorderLevel
}

export function AdminInventoryPage() {
  const { accessToken } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const branchId = searchParams.get('branchId') || ''

  const [branches, setBranches] = useState<BranchDto[] | null>(null)
  const [inventory, setInventory] = useState<BranchProductInventoryDto[] | null>(null)
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [retryKey, setRetryKey] = useState(0)
  const [editing, setEditing] = useState<BranchProductInventoryDto | null>(null)

  // Load the branch list once.
  useEffect(() => {
    const controller = new AbortController()
    branchApi
      .getBranches({ signal: controller.signal })
      .then(setBranches)
      .catch((error) => {
        if (!isAbortError(error)) setBranches([])
      })
    return () => controller.abort()
  }, [])

  // Default to the first branch when none is selected in the URL.
  useEffect(() => {
    if (!branchId && branches && branches.length > 0) {
      const next = new URLSearchParams(searchParams)
      next.set('branchId', branches[0].id)
      setSearchParams(next, { replace: true })
    }
  }, [branchId, branches, searchParams, setSearchParams])

  // Load inventory for the selected branch.
  useEffect(() => {
    if (!branchId) return
    const controller = new AbortController()
    setLoadState('loading')
    branchApi
      .getBranchInventory(branchId, { signal: controller.signal })
      .then((result) => {
        setInventory(result.products)
        setLoadState('ready')
      })
      .catch((error) => {
        if (!isAbortError(error)) {
          setInventory(null)
          setLoadState('error')
        }
      })
    return () => controller.abort()
  }, [branchId, retryKey])

  function selectBranch(nextBranchId: string) {
    const next = new URLSearchParams(searchParams)
    next.set('branchId', nextBranchId)
    setSearchParams(next)
  }

  function onSaved(updated: BranchProductInventoryDto) {
    setInventory((prev) =>
      prev ? prev.map((row) => (row.productId === updated.productId ? updated : row)) : prev,
    )
    setEditing(null)
  }

  const lowStockCount = inventory?.filter(isLowStock).length ?? 0

  return (
    <main className="admin-page admin-inventory" role="main" aria-label="Quản lý kho và giá">
      <header className="admin-page-header">
        <h1>Kho & Giá theo chi nhánh</h1>
        <p className="admin-page-sub">Điều chỉnh giá bán, tồn kho và định mức nhập cho từng chi nhánh.</p>
      </header>

      <div className="admin-toolbar">
        <div className="admin-field">
          <label htmlFor="admin-inv-branch">Chi nhánh</label>
          <select
            id="admin-inv-branch"
            value={branchId}
            onChange={(event) => selectBranch(event.target.value)}
            disabled={!branches || branches.length === 0}
          >
            {!branches && <option value="">Đang tải chi nhánh...</option>}
            {branches && branches.length === 0 && <option value="">Không có chi nhánh</option>}
            {branches?.map((branch) => (
              <option key={branch.id} value={branch.id}>{branch.name}</option>
            ))}
          </select>
        </div>
        {loadState === 'ready' && inventory && (
          <p className="admin-inventory-summary">
            {inventory.length} sản phẩm
            {lowStockCount > 0 && (
              <span className="admin-lowstock-note"> · {lowStockCount} sản phẩm dưới định mức</span>
            )}
          </p>
        )}
      </div>

      {loadState === 'error' && (
        <section className="admin-alert" role="alert">
          <p>Không thể tải tồn kho chi nhánh. Vui lòng thử lại.</p>
          <button type="button" onClick={() => setRetryKey((prev) => prev + 1)}>Thử lại</button>
        </section>
      )}

      {loadState === 'loading' && <p className="admin-loading" aria-busy="true">Đang tải tồn kho...</p>}

      {loadState === 'ready' && inventory && inventory.length === 0 && (
        <div className="admin-empty"><p>Chi nhánh này chưa có sản phẩm nào trong kho.</p></div>
      )}

      {loadState === 'ready' && inventory && inventory.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table" aria-label="Tồn kho chi nhánh">
            <thead>
              <tr>
                <th scope="col">Sản phẩm</th>
                <th scope="col">SKU</th>
                <th scope="col">Giá bán</th>
                <th scope="col">Tồn thực</th>
                <th scope="col">Đang giữ</th>
                <th scope="col">Khả dụng</th>
                <th scope="col">Định mức</th>
                <th scope="col"><span className="sr-only">Hành động</span></th>
              </tr>
            </thead>
            <tbody>
              {inventory.map((item) => {
                const low = isLowStock(item)
                return (
                  <tr key={item.productId} className={low ? 'admin-inventory-row--low' : undefined}>
                    <td>{item.productName}</td>
                    <td><code>{item.sku}</code></td>
                    <td>{formatPrice(item.sellingPrice)}</td>
                    <td>{item.quantityOnHand}</td>
                    <td>{item.reservedQuantity}</td>
                    <td>
                      {item.availableQuantity}
                      {low && <span className="admin-lowstock-badge" role="status"> Sắp hết</span>}
                    </td>
                    <td>{item.reorderLevel}</td>
                    <td>
                      <button
                        type="button"
                        className="admin-link admin-link-btn"
                        onClick={() => setEditing(item)}
                        aria-label={`Chỉnh sửa ${item.productName}`}
                      >
                        Sửa
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {editing && (
        <AdminInventoryModal
          branchId={branchId}
          item={editing}
          onClose={() => setEditing(null)}
          onSaved={onSaved}
        />
      )}
    </main>
  )
}
