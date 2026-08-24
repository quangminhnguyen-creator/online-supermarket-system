import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AppShell } from './app/AppShell'
import { AuthProvider } from './features/auth/AuthContext'
import { ProductBrowsePage } from './features/products/ProductBrowsePage'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppShell>
          <Routes>
            <Route path="/" element={<ProductBrowsePage />} />
            <Route path="/browse" element={<ProductBrowsePage />} />
            <Route path="/products" element={<ProductBrowsePage />} />
          </Routes>
        </AppShell>
      </AuthProvider>
    </BrowserRouter>
  )
}
