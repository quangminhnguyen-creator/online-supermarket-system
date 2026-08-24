import type { PropsWithChildren } from 'react'
import { Link } from 'react-router-dom'
import { ApiStatus } from '../features/system/ApiStatus'
import { UserMenu } from '../features/auth/UserMenu'

export function AppShell({ children }: PropsWithChildren) {
  return (
    <div className="site-shell">
      <header className="site-header">
        <Link className="brand" to="/" aria-label="Online Supermarket — trang chủ">
          <span className="brand__mark">OS</span>
          <span>
            <strong>Online Supermarket</strong>
            <small>Tươi đúng chi nhánh</small>
          </span>
        </Link>
        <nav className="site-nav" aria-label="Điều hướng chính">
          <Link to="/browse">Sản phẩm</Link>
          <a href="#branches">Chi nhánh</a>
          <a href="#roadmap">Lộ trình</a>
        </nav>
        <div className="header-actions">
          <UserMenu />
          <ApiStatus />
        </div>
      </header>
      <main id="top">{children}</main>
    </div>
  )
}
