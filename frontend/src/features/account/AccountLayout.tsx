import type { PropsWithChildren } from 'react'
import { NavLink, Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function AccountLayout({ children }: PropsWithChildren) {
  const { user, isAuthenticated } = useAuth()

  if (!isAuthenticated || !user) {
    return (
      <div className="account-container">
        <div className="account-unauth-card">
          <div className="account-unauth-icon">🔒</div>
          <h2>Yêu Cầu Đăng Nhập</h2>
          <p>Vui lòng đăng nhập để quản lý thông tin tài khoản và sổ địa chỉ giao hàng.</p>
          <Link to="/" className="btn-primary-link">
            Trở về trang chủ
          </Link>
        </div>
      </div>
    )
  }

  const initials = user.fullName
    .split(' ')
    .filter(Boolean)
    .map((p) => p[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()

  return (
    <div className="account-container">
      {/* Account Header Hero */}
      <div className="account-hero">
        <div className="account-hero-user">
          <div className="account-avatar">{initials || 'U'}</div>
          <div className="account-user-meta">
            <h1 className="account-user-name">{user.fullName}</h1>
            <div className="account-user-sub">
              <span className="account-user-email">{user.email}</span>
              <span className="account-role-pill">
                {user.role === 'Admin' ? 'Quản trị viên' : 'Khách hàng thân thiết'}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <nav className="account-nav-tabs" aria-label="Quản lý tài khoản">
        <NavLink
          to="/account/profile"
          className={({ isActive }) =>
            `account-tab-btn ${isActive ? 'account-tab-btn--active' : ''}`
          }
        >
          <span className="tab-icon">👤</span>
          <span>Thông Tin Hồ Sơ & Mật Khẩu</span>
        </NavLink>
        <NavLink
          to="/account/addresses"
          className={({ isActive }) =>
            `account-tab-btn ${isActive ? 'account-tab-btn--active' : ''}`
          }
        >
          <span className="tab-icon">📍</span>
          <span>Sổ Địa Chỉ Giao Hàng</span>
        </NavLink>
      </nav>

      {/* Content Area */}
      <div className="account-content">{children}</div>
    </div>
  )
}
