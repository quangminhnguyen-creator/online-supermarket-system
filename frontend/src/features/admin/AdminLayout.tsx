import type { PropsWithChildren } from 'react'
import { NavLink } from 'react-router-dom'
import './AdminLayout.css'

interface NavItem {
  to: string
  label: string
  icon: string
  end?: boolean
  comingSoon?: boolean
}

const NAV_ITEMS: NavItem[] = [
  { to: '/admin/orders', label: 'Đơn hàng', icon: '🧾' },
  { to: '/admin/inventory', label: 'Kho & Giá', icon: '📦' },
  { to: '/admin/users', label: 'Người dùng', icon: '👥' },
]

export function AdminLayout({ children }: PropsWithChildren) {
  return (
    <div className="admin-shell">
      <aside className="admin-sidebar" aria-label="Điều hướng quản trị">
        <div className="admin-sidebar-brand">Bảng điều khiển</div>
        <nav className="admin-nav">
          {NAV_ITEMS.map((item) =>
            item.comingSoon ? (
              <span key={item.to} className="admin-nav-link admin-nav-link--disabled" aria-disabled="true">
                <span className="admin-nav-icon" aria-hidden="true">{item.icon}</span>
                <span>{item.label}</span>
                <span className="admin-nav-badge">Sắp có</span>
              </span>
            ) : (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) => `admin-nav-link ${isActive ? 'admin-nav-link--active' : ''}`}
              >
                <span className="admin-nav-icon" aria-hidden="true">{item.icon}</span>
                <span>{item.label}</span>
              </NavLink>
            ),
          )}
        </nav>
      </aside>
      <div className="admin-content">{children}</div>
    </div>
  )
}
