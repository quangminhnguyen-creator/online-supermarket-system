import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { AuthModal } from '../auth/AuthModal'
import { useAuth } from '../auth/AuthContext'
import { orderApi, type PaginatedOrdersDto } from '../../api/orderApi'
import './OrderHistoryPage.css'

const ORDER_STATUSES = ['', 'Pending', 'Confirmed', 'Preparing', 'Ready', 'Shipped', 'Delivered', 'Completed', 'Cancelled', 'Failed']
const PAGE_SIZE = 10

function isAbortError(error: unknown) {
  return error instanceof Error && error.name === 'AbortError'
}

function formatPrice(value: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Preparing: 'Đang chuẩn bị',
  Ready: 'Sẵn sàng nhận hàng',
  Shipped: 'Đang giao hàng',
  Delivered: 'Đã giao hàng',
  Completed: 'Hoàn tất',
  Cancelled: 'Đã hủy',
  Failed: 'Thất bại',
}

function formatStatus(status: string) {
  return STATUS_LABELS[status] ?? status
}

export function OrderHistoryPage() {
  const { isAuthenticated, isLoading: authLoading, accessToken } = useAuth()
  const [loginOpen, setLoginOpen] = useState(false)
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number(searchParams.get('page') ?? '1') || 1)
  const status = searchParams.get('status') || ''
  const [orders, setOrders] = useState<PaginatedOrdersDto | null>(null)
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    if (!isAuthenticated || !accessToken) return
    const controller = new AbortController()
    setLoadState('loading')
    orderApi.getOrders({ page, pageSize: PAGE_SIZE, status: status || undefined }, accessToken, controller.signal)
      .then((result) => {
        setOrders(result)
        setLoadState('ready')
      })
      .catch((error) => {
        if (!isAbortError(error)) setLoadState('error')
      })
    return () => controller.abort()
  }, [isAuthenticated, accessToken, page, status, retryKey])

  function setStatusFilter(nextStatus: string) {
    const next = new URLSearchParams(searchParams)
    next.set('page', '1')
    if (nextStatus) next.set('status', nextStatus)
    else next.delete('status')
    setSearchParams(next)
  }

  function setPage(nextPage: number) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(nextPage))
    setSearchParams(next)
  }

  if (!isAuthenticated) {
    return (
      <section className="orders-page orders-page--auth-required">
        <h1>Đăng nhập để xem đơn hàng</h1>
        <p>Vui lòng đăng nhập để xem lịch sử mua hàng của bạn.</p>
        <button type="button" onClick={() => setLoginOpen(true)}>Đăng nhập</button>
        <AuthModal isOpen={loginOpen} initialMode="login" onClose={() => setLoginOpen(false)} />
      </section>
    )
  }
  if (authLoading || loadState === 'loading') return <section className="orders-page" aria-busy="true"><p>Đang tải đơn hàng...</p></section>

  return (
    <main className="orders-page" role="main" aria-label="Lịch sử đơn hàng">
      <h1>Lịch sử đơn hàng</h1>
      {loadState === 'error' && (
        <section className="orders-empty-state" role="alert">
          <h2>Không thể tải đơn hàng</h2>
          <p>Vui lòng thử lại sau ít phút.</p>
          <button type="button" onClick={() => setRetryKey(prev => prev + 1)}>Thử lại</button>
        </section>
      )}
      <div className="orders-toolbar">
        <label htmlFor="order-status-filter">Lọc trạng thái</label>
        <select
          id="order-status-filter"
          value={status}
          onChange={(event) => setStatusFilter(event.target.value)}
        >
          <option value="">Tất cả trạng thái</option>
          {ORDER_STATUSES.filter(Boolean).map((value) => (
            <option key={value} value={value}>{value}</option>
          ))}
        </select>
      </div>
      {orders?.data.length === 0 && (
        <div className="orders-empty-state">
          <p>Bạn chưa có đơn hàng nào.</p>
          <Link to="/browse">Tiếp tục mua sắm</Link>
        </div>
      )}
      <div className="orders-list">
        {orders?.data.map((order) => (
          <article key={order.id} className="order-card">
            <h2>Mã đơn: {order.id}</h2>
            <p>{formatDate(order.createdAtUtc)}</p>
            <strong>{formatPrice(order.totalAmount)}</strong>
            <span className={`order-status order-status--${order.status.toLowerCase()}`} role="status">
              {formatStatus(order.status)}
              <span className="sr-only">{order.status}</span>
            </span>
            <p>{order.fulfillmentType} - {order.itemCount} sản phẩm</p>
            <Link to={`/orders/history/${encodeURIComponent(order.id)}`}>Xem chi tiết {order.id}</Link>
          </article>
        ))}
      </div>
      {orders && (
        <nav className="orders-pagination" aria-label="Phân trang đơn hàng">
          <button
            type="button"
            onClick={() => setPage(page - 1)}
            disabled={page <= 1}
          >
            Trang trước
          </button>
          <span>Trang {page}</span>
          <button
            type="button"
            onClick={() => setPage(page + 1)}
            disabled={page * PAGE_SIZE >= orders.totalCount}
          >
            Trang sau
          </button>
        </nav>
      )}
    </main>
  )
}
