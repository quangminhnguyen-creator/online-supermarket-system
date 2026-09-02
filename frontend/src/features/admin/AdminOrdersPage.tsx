import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { adminApi } from '../../api/adminApi'
import type { PaginatedOrdersDto } from '../../api/orderApi'
import { ORDER_STATUSES, formatStatus } from './orderStatus'
import './AdminOrdersPage.css'

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

function shortId(id: string) {
  return id.slice(0, 8)
}

export function AdminOrdersPage() {
  const { accessToken } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number(searchParams.get('page') ?? '1') || 1)
  const status = searchParams.get('status') || ''
  const userId = searchParams.get('userId') || ''
  const [userIdInput, setUserIdInput] = useState(userId)
  const [orders, setOrders] = useState<PaginatedOrdersDto | null>(null)
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    setUserIdInput(userId)
  }, [userId])

  useEffect(() => {
    if (!accessToken) return
    const controller = new AbortController()
    setLoadState('loading')
    adminApi
      .listOrders(
        { page, pageSize: PAGE_SIZE, status: status || undefined, userId: userId || undefined },
        accessToken,
        controller.signal,
      )
      .then((result) => {
        setOrders(result)
        setLoadState('ready')
      })
      .catch((error) => {
        if (!isAbortError(error)) {
          setOrders(null)
          setLoadState('error')
        }
      })
    return () => controller.abort()
  }, [accessToken, page, status, userId, retryKey])

  function setStatusFilter(nextStatus: string) {
    const next = new URLSearchParams(searchParams)
    next.set('page', '1')
    if (nextStatus) next.set('status', nextStatus)
    else next.delete('status')
    setSearchParams(next)
  }

  function applyUserIdFilter(event: React.FormEvent) {
    event.preventDefault()
    const next = new URLSearchParams(searchParams)
    next.set('page', '1')
    const trimmed = userIdInput.trim()
    if (trimmed) next.set('userId', trimmed)
    else next.delete('userId')
    setSearchParams(next)
  }

  function setPage(nextPage: number) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(nextPage))
    setSearchParams(next)
  }

  return (
    <main className="admin-page admin-orders" role="main" aria-label="Quản lý đơn hàng">
      <header className="admin-page-header">
        <h1>Quản lý đơn hàng</h1>
        <p className="admin-page-sub">Xem toàn bộ đơn hàng của hệ thống và cập nhật trạng thái.</p>
      </header>

      <div className="admin-toolbar">
        <div className="admin-field">
          <label htmlFor="admin-order-status">Lọc trạng thái</label>
          <select
            id="admin-order-status"
            value={status}
            onChange={(event) => setStatusFilter(event.target.value)}
          >
            <option value="">Tất cả trạng thái</option>
            {ORDER_STATUSES.map((value) => (
              <option key={value} value={value}>{formatStatus(value)}</option>
            ))}
          </select>
        </div>
        <form className="admin-field" onSubmit={applyUserIdFilter}>
          <label htmlFor="admin-order-user">Lọc theo mã khách (userId)</label>
          <div className="admin-field-inline">
            <input
              id="admin-order-user"
              type="text"
              value={userIdInput}
              placeholder="Dán userId..."
              onChange={(event) => setUserIdInput(event.target.value)}
            />
            <button type="submit">Lọc</button>
          </div>
        </form>
      </div>

      {loadState === 'error' && (
        <section className="admin-alert" role="alert">
          <p>Không thể tải danh sách đơn hàng. Vui lòng thử lại.</p>
          <button type="button" onClick={() => setRetryKey((prev) => prev + 1)}>Thử lại</button>
        </section>
      )}

      {loadState === 'loading' && (
        <p className="admin-loading" aria-busy="true">Đang tải đơn hàng...</p>
      )}

      {loadState === 'ready' && orders && orders.data.length === 0 && (
        <div className="admin-empty">
          <p>Không có đơn hàng nào khớp bộ lọc.</p>
        </div>
      )}

      {loadState === 'ready' && orders && orders.data.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table" aria-label="Danh sách đơn hàng">
            <thead>
              <tr>
                <th scope="col">Mã đơn</th>
                <th scope="col">Ngày tạo</th>
                <th scope="col">Hình thức</th>
                <th scope="col">Số món</th>
                <th scope="col">Tổng tiền</th>
                <th scope="col">Trạng thái</th>
                <th scope="col"><span className="sr-only">Hành động</span></th>
              </tr>
            </thead>
            <tbody>
              {orders.data.map((order) => (
                <tr key={order.id}>
                  <td><code title={order.id}>{shortId(order.id)}</code></td>
                  <td>{formatDate(order.createdAtUtc)}</td>
                  <td>{order.fulfillmentType}</td>
                  <td>{order.itemCount}</td>
                  <td>{formatPrice(order.totalAmount)}</td>
                  <td>
                    <span className={`admin-status admin-status--${order.status.toLowerCase()}`} role="status">
                      {formatStatus(order.status)}
                      <span className="sr-only">{order.status}</span>
                    </span>
                  </td>
                  <td>
                    <Link
                      to={`/admin/orders/${encodeURIComponent(order.id)}`}
                      className="admin-link"
                      aria-label={`Xem chi tiết đơn ${order.id}`}
                    >
                      Chi tiết
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {orders && (
        <nav className="admin-pagination" aria-label="Phân trang đơn hàng">
          <button type="button" onClick={() => setPage(page - 1)} disabled={page <= 1}>Trang trước</button>
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
