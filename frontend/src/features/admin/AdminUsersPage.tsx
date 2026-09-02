import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { adminApi, type PaginatedUsersDto, type UserSummaryDto } from '../../api/adminApi'
import { ApiError } from '../../api/httpClient'
import './AdminOrdersPage.css'
import './AdminInventoryPage.css'
import './AdminUsersPage.css'

const PAGE_SIZE = 20

const USER_STATUSES = ['Active', 'Locked', 'Disabled'] as const

const USER_STATUS_LABELS: Record<string, string> = {
  Active: 'Đang hoạt động',
  Locked: 'Đã khóa',
  Disabled: 'Vô hiệu hóa',
}

const ROLE_LABELS: Record<string, string> = {
  Admin: 'Quản trị viên',
  Customer: 'Khách hàng',
}

function isAbortError(error: unknown) {
  return error instanceof Error && error.name === 'AbortError'
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium' }).format(new Date(value))
}

function userStatusLabel(status: string) {
  return USER_STATUS_LABELS[status] ?? status
}

export function AdminUsersPage() {
  const { accessToken } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number(searchParams.get('page') ?? '1') || 1)

  const [data, setData] = useState<PaginatedUsersDto | null>(null)
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [retryKey, setRetryKey] = useState(0)
  const [pending, setPending] = useState<{ user: UserSummaryDto; newStatus: string } | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  useEffect(() => {
    if (!accessToken) return
    const controller = new AbortController()
    setLoadState('loading')
    adminApi
      .listUsers(page, PAGE_SIZE, accessToken, controller.signal)
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

  function requestChange(user: UserSummaryDto, newStatus: string) {
    if (newStatus === user.status) return
    setActionError(null)
    setPending({ user, newStatus })
  }

  function cancelChange() {
    setPending(null)
    setActionError(null)
  }

  async function confirmChange() {
    if (!pending || !accessToken) return
    setSubmitting(true)
    setActionError(null)
    try {
      await adminApi.updateUserStatus(pending.user.id, pending.newStatus, accessToken)
      setData((prev) =>
        prev
          ? {
              ...prev,
              users: prev.users.map((u) =>
                u.id === pending.user.id ? { ...u, status: pending.newStatus } : u,
              ),
            }
          : prev,
      )
      setPending(null)
    } catch (err) {
      if (err instanceof ApiError && err.data?.message) {
        setActionError(err.data.message)
      } else {
        setActionError('Không thể cập nhật trạng thái. Vui lòng thử lại.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="admin-page admin-users" role="main" aria-label="Quản lý người dùng">
      <header className="admin-page-header">
        <h1>Quản lý người dùng</h1>
        <p className="admin-page-sub">Xem danh sách tài khoản và khóa / mở khóa / vô hiệu hóa.</p>
      </header>

      {loadState === 'error' && (
        <section className="admin-alert" role="alert">
          <p>Không thể tải danh sách người dùng. Vui lòng thử lại.</p>
          <button type="button" onClick={() => setRetryKey((prev) => prev + 1)}>Thử lại</button>
        </section>
      )}

      {loadState === 'loading' && <p className="admin-loading" aria-busy="true">Đang tải người dùng...</p>}

      {loadState === 'ready' && data && data.users.length === 0 && (
        <div className="admin-empty"><p>Không có người dùng nào.</p></div>
      )}

      {loadState === 'ready' && data && data.users.length > 0 && (
        <div className="admin-table-wrap">
          <table className="admin-table" aria-label="Danh sách người dùng">
            <thead>
              <tr>
                <th scope="col">Email</th>
                <th scope="col">Họ tên</th>
                <th scope="col">SĐT</th>
                <th scope="col">Vai trò</th>
                <th scope="col">Trạng thái</th>
                <th scope="col">Ngày tạo</th>
                <th scope="col">Đổi trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {data.users.map((user) => (
                <tr key={user.id}>
                  <td>{user.email}</td>
                  <td>{user.fullName}</td>
                  <td>{user.phone || '—'}</td>
                  <td>
                    <span className={`admin-role admin-role--${user.role.toLowerCase()}`}>
                      {ROLE_LABELS[user.role] ?? user.role}
                    </span>
                  </td>
                  <td>
                    <span className={`admin-status admin-status--${user.status.toLowerCase()}`}>
                      {userStatusLabel(user.status)}
                      <span className="sr-only">{user.status}</span>
                    </span>
                  </td>
                  <td>{formatDate(user.createdAtUtc)}</td>
                  <td>
                    <select
                      value={user.status}
                      aria-label={`Đổi trạng thái ${user.email}`}
                      onChange={(event) => requestChange(user, event.target.value)}
                    >
                      {USER_STATUSES.map((status) => (
                        <option key={status} value={status}>{USER_STATUS_LABELS[status]}</option>
                      ))}
                    </select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {data && (
        <nav className="admin-pagination" aria-label="Phân trang người dùng">
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

      {pending && (
        <div className="admin-modal-overlay" onClick={cancelChange}>
          <div
            className="admin-modal"
            role="dialog"
            aria-modal="true"
            aria-label="Xác nhận đổi trạng thái"
            onClick={(event) => event.stopPropagation()}
          >
            <header className="admin-modal-header">
              <h2>Xác nhận</h2>
              <button type="button" className="admin-modal-close" onClick={cancelChange} aria-label="Đóng">×</button>
            </header>
            <p>
              Đổi trạng thái tài khoản <strong>{pending.user.email}</strong> từ
              {' '}"{userStatusLabel(pending.user.status)}" sang "{userStatusLabel(pending.newStatus)}"?
            </p>
            {pending.newStatus !== 'Active' && (
              <p className="admin-warning" role="alert">
                Người dùng sẽ không thể đăng nhập khi tài khoản bị khóa hoặc vô hiệu hóa.
              </p>
            )}
            {actionError && <p className="admin-error" role="alert">{actionError}</p>}
            <div className="admin-modal-actions">
              <button type="button" className="admin-btn-secondary" onClick={cancelChange} disabled={submitting}>Hủy</button>
              <button type="button" className="btn-primary" onClick={confirmChange} disabled={submitting}>
                {submitting ? 'Đang lưu...' : 'Xác nhận'}
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  )
}
