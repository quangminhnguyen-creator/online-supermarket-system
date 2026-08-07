import type { PropsWithChildren } from 'react'
import { ApiStatus } from '../features/system/ApiStatus'

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
        <ApiStatus />
      </header>
      <main id="top">{children}</main>
    </div>
  )
}
