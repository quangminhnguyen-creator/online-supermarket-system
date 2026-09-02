import type { PropsWithChildren } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import './AdminLayout.css'

/**
 * Client-side gate for the admin area. This is a UX convenience only —
 * the backend enforces authorization on every /api/admin/* endpoint via the
 * "AdminOnly" policy, so this component must never be treated as a security boundary.
 */
export function RequireAdmin({ children }: PropsWithChildren) {
  const { user, isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <section className="admin-guard" aria-busy="true">
        <p>Đang kiểm tra quyền truy cập...</p>
      </section>
    )
  }

  if (!isAuthenticated || !user) {
    return (
      <section className="admin-guard admin-guard--denied" role="alert">
        <div className="admin-guard-icon" aria-hidden="true">🔒</div>
        <h1>Yêu cầu đăng nhập</h1>
        <p>Vui lòng đăng nhập bằng tài khoản quản trị để truy cập khu vực này.</p>
        <Link to="/" className="btn-primary-link">Trở về trang chủ</Link>
      </section>
    )
  }

  if (user.role !== 'Admin') {
    return (
      <section className="admin-guard admin-guard--denied" role="alert">
        <div className="admin-guard-icon" aria-hidden="true">⛔</div>
        <h1>Không có quyền truy cập</h1>
        <p>Tài khoản của bạn không có quyền quản trị. Nếu cần hỗ trợ, vui lòng liên hệ quản trị viên hệ thống.</p>
        <Link to="/" className="btn-primary-link">Trở về trang chủ</Link>
      </section>
    )
  }

  return <>{children}</>
}
