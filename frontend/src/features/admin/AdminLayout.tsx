import React from 'react'
import { Outlet, NavLink } from 'react-router-dom'
import './AdminCatalog.css'

export function AdminLayout() {
  return (
    <div className="admin-layout">
      <nav className="admin-nav">
        <ul>
          <li><NavLink to="catalog/categories">Categories</NavLink></li>
          <li><NavLink to="catalog/brands">Brands</NavLink></li>
          <li><NavLink to="catalog/products">Products</NavLink></li>
        </ul>
      </nav>
      <main className="admin-main">
        <Outlet />
      </main>
    </div>
  )
}
