import React from 'react'
import { Outlet, NavLink } from 'react-router-dom'
import './AdminCatalog.css'

export function AdminLayout() {
  return (
    <div className="admin-layout">
      <nav className="admin-nav">
        <ul>
          <li><NavLink to="catalog/categories">Danh mục</NavLink></li>
          <li><NavLink to="catalog/brands">Thương hiệu</NavLink></li>
          <li><NavLink to="catalog/products">Sản phẩm</NavLink></li>
        </ul>
      </nav>
      <main className="admin-main">
        <Outlet />
      </main>
    </div>
  )
}
