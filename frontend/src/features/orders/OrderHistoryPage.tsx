import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { AuthModal } from '../auth/AuthModal'
import { useAuth } from '../auth/AuthContext'
import { orderApi, type PaginatedOrdersDto } from '../../api/orderApi'
import './OrderHistoryPage.css'

export function OrderHistoryPage() {
  const { isAuthenticated, isLoading: authLoading, accessToken } = useAuth()
  const [loginOpen, setLoginOpen] = useState(false)
  const [orders, setOrders] = useState<PaginatedOrdersDto | null>(null)

  useEffect(() => {
    if (!isAuthenticated || !accessToken) return
    const controller = new AbortController()
    orderApi.getOrders({ page: 1, pageSize: 10 }, accessToken, controller.signal)
      .then(setOrders)
    return () => controller.abort()
  }, [isAuthenticated, accessToken])

  if (authLoading) return <section className="orders-page" aria-busy="true"><h1>Đang tải đơn hàng</h1></section>
  if (!isAuthenticated) {
    return (
      <section className="orders-page orders-page--auth-required">
        <h1>Đăng nhập để xem đơn hàng</h1>
        <p>Vui lòng đăng nhập để xem lịch sử mua hàng của bạn.</p>
        <button type="button" onClick={() => setLoginOpen(true)}>Đăng nhập</button>
        <AuthModal isOpen={loginOpen} initialMode="login" onClose={() => setLoginOpen(false)} />
      </section>
    )
  }
  return (
    <section className="orders-page">
      <h1>Lịch sử đơn hàng</h1>
      {orders?.data.length === 0 && <p>Bạn chưa có đơn hàng nào.</p>}
      <Link to="/browse">Tiếp tục mua sắm</Link>
    </section>
  )
}
