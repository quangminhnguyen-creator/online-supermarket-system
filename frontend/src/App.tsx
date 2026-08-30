import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AppShell } from './app/AppShell'
import { AuthProvider } from './features/auth/AuthContext'
import { CartProvider } from './features/cart/CartContext'
import { CompareProvider } from './features/compare/CompareContext'
import { CompareModal } from './features/compare/CompareModal'
import { ProductBrowsePage } from './features/products/ProductBrowsePage'
import { ProductDetailPage } from './features/products/ProductDetailPage'
import { ProfilePage } from './features/account/ProfilePage'
import { AddressListPage } from './features/account/AddressListPage'
import { CartPage } from './features/cart/CartPage'
import { CheckoutPage } from './features/checkout/CheckoutPage'
import { CheckoutSuccessPage } from './features/checkout/CheckoutSuccessPage'
import { OrderHistoryPage } from './features/orders/OrderHistoryPage'
import { OrderDetailPage } from './features/orders/OrderDetailPage'

function CompareAppShell() {
  return (
    <>
      <AppShell>
        <Routes>
          <Route path="/" element={<ProductBrowsePage />} />
          <Route path="/browse" element={<ProductBrowsePage />} />
          <Route path="/products" element={<ProductBrowsePage />} />
          <Route path="/product/:id" element={<ProductDetailPage />} />
          <Route path="/account/profile" element={<ProfilePage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/account/addresses" element={<AddressListPage />} />
          <Route path="/addresses" element={<AddressListPage />} />
          <Route path="/shopping/cart" element={<CartPage />} />
          <Route path="/shopping/checkout" element={<CheckoutPage />} />
          <Route path="/shopping/checkout/success" element={<CheckoutSuccessPage />} />
          <Route path="/orders/history" element={<OrderHistoryPage />} />
          <Route path="/orders/history/:id" element={<OrderDetailPage />} />
        </Routes>
      </AppShell>
      <CompareModal />
    </>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <CartProvider>
          <CompareProvider>
            <CompareAppShell />
          </CompareProvider>
        </CartProvider>
      </AuthProvider>
    </BrowserRouter>
  )
}
