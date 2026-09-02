import type { PropsWithChildren } from 'react'
import { Link } from 'react-router-dom'
import { ApiStatus } from '../features/system/ApiStatus'
import { UserMenu } from '../features/auth/UserMenu'
import { CartHeaderLink } from '../features/cart/CartHeaderLink'
import { CompareHeaderLink } from '../features/compare/CompareHeaderLink'
import { BranchNavMenu } from '../features/products/BranchNavMenu'

export function AppShell({ children }: PropsWithChildren) {
  return (
    <div className="site-shell">
      <header className="site-header">
        <Link className="brand" to="/" aria-label="AptechMart — trang chủ">
          <span className="brand__mark">AM</span>
          <span>
            <strong>AptechMart</strong>
            <small>Siêu thị điện tử</small>
          </span>
        </Link>
        <nav className="site-nav" aria-label="Điều hướng chính">
          <Link to="/browse">Sản phẩm</Link>
          <BranchNavMenu />
          <a href="#roadmap">Lộ trình</a>
        </nav>
        <div className="header-actions">
          <CompareHeaderLink />
          <CartHeaderLink />
          <UserMenu />
          <ApiStatus />
        </div>
      </header>
      <main id="top">{children}</main>
    </div>
  )
}
