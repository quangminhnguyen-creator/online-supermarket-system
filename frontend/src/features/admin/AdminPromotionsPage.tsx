import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { adminApi, type PaginatedPromotionsDto, type PromotionDto } from '../../api/adminApi'
import { formatPrice } from '../products/ProductCard'
import { AdminPromotionModal } from './AdminPromotionModal'
import './AdminOrdersPage.css'
import './AdminInventoryPage.css'
import './AdminPromotionsPage.css'

const PAGE_SIZE = 20

function isAbortError(error: unknown) {
  return error instanceof Error && error.name === 'AbortError'
}

function discountTypeLabel(type: string) {
  return type === 'Percentage' ? 'Phần trăm' : 'Số tiền cố định'
}

function discountValueLabel(promo: PromotionDto) {
  return promo.discountType === 'Percentage' ? `${promo.discountValue}%` : formatPrice(promo.discountValue)
}

function usageLabel(promo: PromotionDto) {
  return `${promo.usageCount}/${promo.usageLimit ?? '∞'}`
}

type ModalState = { mode: 'create' } | { mode: 'edit'; promotion: PromotionDto } | null

export function AdminPromotionsPage() {
  const { accessToken } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number(searchParams.get('page') ?? '1') || 1)

  const [data, setData] = useState<PaginatedPromotionsDto | null>(null)
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [retryKey, setRetryKey] = useState(0)
  const [modal, setModal] = useState<ModalState>(null)

  useEffect(() => {
    if (!accessToken) return
    const controller = new AbortController()
    setLoadState('loading')
    adminApi
      .listPromotions(page, PAGE_SIZE, accessToken, controller.signal)
      .then((result) => {
        setData(result)
        setLoadState('ready')
      })
      .catch((error) => {
        if (!isAbortError(error)) {
          setData(null)
          setLoadState('error')
        }
      })
    return () => controller.abort()
  }, [accessToken, page, retryKey])

  function setPage(nextPage: number) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(nextPage))
    setSearchParams(next)
  }

  function handleSaved() {
    setModal(null)
    setRetryKey((prev) => prev + 1)
  }

  return (
    <main className="admin-page admin-promotions" role="main" aria-label="Quản lý khuyến mãi">
      <header className="admin-page-header admin-promotions-header">
        <div>
          <h1>Quản lý khuyến mãi</h1>
          <p className="admin-page-sub">Tạo, chỉnh sửa và bật/tắt mã giảm giá.</p>
        </div>
        <button type="button" className="btn-primary admin-create-btn" onClick={() => setModal({ mode: 'create' })}>
          + Tạo mã mới
        </button>
      </header>

      {loadState === 'error' && (
        <section className="admin-alert" role="alert">
          <p>Không thể tải danh sách khuyến mãi. Vui lòng thử lại.</p>
          <button type="button" onClick={() => setRetryKey((prev) => prev + 1)}>Thử lại</button>
        </section>
      )}

      {loadState === 'loading' && <p className="admin-loading" aria-busy="true">Đang tải khuyến mãi...</p>}

      {loadState === 'ready' && data && data.promotions.length === 0 && (
        <div className="admin-empty"><p>Chưa có mã giảm giá nào. Bấm "Tạo mã mới" để thêm.</p></div>
      )}

      {loadState === 'ready' && data && data.promotions.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table" aria-label="Danh sách khuyến mãi">
            <thead>
              <tr>
                <th scope="col">Mã</th>
                <th scope="col">Loại</th>
                <th scope="col">Giá trị</th>
                <th scope="col">Đơn tối thiểu</th>
                <th scope="col">Đã dùng / Giới hạn</th>
                <th scope="col">Trạng thái</th>
                <th scope="col"><span className="sr-only">Hành động</span></th>
              </tr>
            </thead>
            <tbody>
              {data.promotions.map((promo) => (
                <tr key={promo.id}>
                  <td><code>{promo.code}</code></td>
                  <td>{discountTypeLabel(promo.discountType)}</td>
                  <td>{discountValueLabel(promo)}</td>
                  <td>{formatPrice(promo.minOrderAmount)}</td>
                  <td>{usageLabel(promo)}</td>
                  <td>
                    <span className={`admin-status admin-status--${promo.isActive ? 'active' : 'inactive'}`}>
                      {promo.isActive ? 'Đang áp dụng' : 'Đã tắt'}
                    </span>
                  </td>
                  <td>
                    <button
                      type="button"
                      className="admin-link admin-link-btn"
                      onClick={() => setModal({ mode: 'edit', promotion: promo })}
                      aria-label={`Sửa mã ${promo.code}`}
                    >
                      Sửa
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {data && (
        <nav className="admin-pagination" aria-label="Phân trang khuyến mãi">
          <button type="button" onClick={() => setPage(page - 1)} disabled={page <= 1}>Trang trước</button>
          <span>Trang {page}</span>
          <button
            type="button"
            onClick={() => setPage(page + 1)}
            disabled={page * PAGE_SIZE >= data.totalCount}
          >
            Trang sau
          </button>
        </nav>
      )}

      {modal?.mode === 'create' && (
        <AdminPromotionModal mode="create" onClose={() => setModal(null)} onSaved={handleSaved} />
      )}
      {modal?.mode === 'edit' && (
        <AdminPromotionModal
          mode="edit"
          promotion={modal.promotion}
          onClose={() => setModal(null)}
          onSaved={handleSaved}
        />
      )}
    </main>
  )
}
