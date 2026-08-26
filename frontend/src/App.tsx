import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AppShell } from './app/AppShell'
import { AuthProvider } from './features/auth/AuthContext'
import { ProductBrowsePage } from './features/products/ProductBrowsePage'
import { ProductDetailPage } from './features/products/ProductDetailPage'
import { ProfilePage } from './features/account/ProfilePage'
import { AddressListPage } from './features/account/AddressListPage'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
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
          </Routes>
        </AppShell>
      </AuthProvider>
    </BrowserRouter>
  )
}
