import { BrowserRouter, Routes, Route, Outlet } from 'react-router-dom'
import { AppShell } from './app/AppShell'
import { AuthProvider } from './features/auth/AuthContext'
import { CartProvider } from './features/cart/CartContext'
import { ProductBrowsePage } from './features/products/ProductBrowsePage'
import { ProductDetailPage } from './features/products/ProductDetailPage'
import { BranchesPage } from './features/products/BranchesPage'
import { ProfilePage } from './features/account/ProfilePage'
import { AddressListPage } from './features/account/AddressListPage'
import { CartPage } from './features/cart/CartPage'
import { CheckoutPage } from './features/checkout/CheckoutPage'
import { CheckoutSuccessPage } from './features/checkout/CheckoutSuccessPage'
import { OrderHistoryPage } from './features/orders/OrderHistoryPage'
import { OrderDetailPage } from './features/orders/OrderDetailPage'
import { RequireAdmin } from './features/admin/RequireAdmin'
import { AdminLayout } from './features/admin/AdminLayout'
import { AdminOrdersPage } from './features/admin/AdminOrdersPage'
import { AdminOrderDetailPage } from './features/admin/AdminOrderDetailPage'
import { AdminInventoryPage } from './features/admin/AdminInventoryPage'
import { AdminUsersPage } from './features/admin/AdminUsersPage'
import { AdminPromotionsPage } from './features/admin/AdminPromotionsPage'
import { AdminBranchesPage } from './features/admin/AdminBranchesPage'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <CartProvider>
          <AppShell>
            <Routes>
              <Route path="/" element={<ProductBrowsePage />} />
              <Route path="/browse" element={<ProductBrowsePage />} />
              <Route path="/products" element={<ProductBrowsePage />} />
              <Route path="/product/:id" element={<ProductDetailPage />} />
              <Route path="/branches" element={<BranchesPage />} />
              <Route path="/account/profile" element={<ProfilePage />} />
              <Route path="/profile" element={<ProfilePage />} />
              <Route path="/account/addresses" element={<AddressListPage />} />
              <Route path="/addresses" element={<AddressListPage />} />
              <Route path="/shopping/cart" element={<CartPage />} />
              <Route path="/shopping/checkout" element={<CheckoutPage />} />
              <Route path="/shopping/checkout/success" element={<CheckoutSuccessPage />} />
              <Route path="/orders/history" element={<OrderHistoryPage />} />
              <Route path="/orders/history/:id" element={<OrderDetailPage />} />
              <Route
                path="/admin"
                element={
                  <RequireAdmin>
                    <AdminLayout>
                      <Outlet />
                    </AdminLayout>
                  </RequireAdmin>
                }
              >
                <Route index element={<AdminOrdersPage />} />
                <Route path="orders" element={<AdminOrdersPage />} />
                <Route path="orders/:id" element={<AdminOrderDetailPage />} />
                <Route path="branches" element={<AdminBranchesPage />} />
                <Route path="inventory" element={<AdminInventoryPage />} />
                <Route path="users" element={<AdminUsersPage />} />
                <Route path="promotions" element={<AdminPromotionsPage />} />
              </Route>
            </Routes>
          </AppShell>
        </CartProvider>
      </AuthProvider>
    </BrowserRouter>
  )
}
