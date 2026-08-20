import type { PropsWithChildren } from 'react'
import { ApiStatus } from '../features/system/ApiStatus'
import { UserMenu } from '../features/auth/UserMenu'

export function AppShell({ children }: PropsWithChildren) {
  return (
    <div className="site-shell">
      <header className="site-header">
        <a className="brand" href="#top" aria-label="Online Supermarket — trang chủ">
          <span className="brand__mark">OS</span>
          <span>
            <strong>Online Supermarket</strong>
            <small>Tươi đúng chi nhánh</small>
          </span>
        </a>
        <nav className="site-nav" aria-label="Điều hướng chính">
          <a href="#catalog">Sản phẩm</a>
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
