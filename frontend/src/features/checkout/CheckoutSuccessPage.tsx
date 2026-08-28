import { Link, useSearchParams } from 'react-router-dom'
import './CheckoutPage.css'

export function CheckoutSuccessPage() {
  const [searchParams] = useSearchParams()
  const orderId = searchParams.get('orderId') || ''
  const paymentStatus = searchParams.get('paymentStatus') || ''

  return (
    <section className="checkout-page checkout-success-page">
      <div className="checkout-success-card">
        <div className="checkout-success-icon" aria-hidden="true">✓</div>
        <h1>Đặt hàng thành công</h1>
        <p className="checkout-success-message">
          Cảm ơn bạn đã mua sắm tại AptechMart. Đơn hàng của bạn đã được ghi nhận.
        </p>

        <div className="checkout-success-details">
          <div className="checkout-success-row">
            <span>Mã đơn hàng:</span>
            <strong>{orderId || 'N/A'}</strong>
          </div>
          <div className="checkout-success-row">
            <span>Trạng thái thanh toán:</span>
            <strong>{paymentStatus || 'Pending'}</strong>
          </div>
        </div>

        <div className="checkout-success-actions">
          <Link to="/browse" className="checkout-btn checkout-btn--primary">
            Tiếp tục mua sắm
          </Link>
          <Link to="/orders/history" className="checkout-btn checkout-btn--secondary">
            Xem đơn hàng
          </Link>
        </div>
      </div>
    </section>
  )
}
