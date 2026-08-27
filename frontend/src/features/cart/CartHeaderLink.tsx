import { Link } from 'react-router-dom'
import { useCart } from './CartContext'
import './CartHeaderLink.css'

export function CartHeaderLink() {
  const { status, cart } = useCart()
  const count = status === 'ready' ? cart?.totalItems ?? 0 : 0

  return (
    <Link
      to="/shopping/cart"
      className="cart-header-link"
      aria-label={`Giỏ hàng${count > 0 ? ` (${count} sản phẩm)` : ''}`}
    >
      <span aria-hidden="true" className="cart-header-link__icon">
        🛒
      </span>
      <span>Giỏ hàng</span>
      {count > 0 && (
        <span
          className="cart-header-link__badge"
          aria-label={count + ' sản phẩm trong giỏ'}
        >
          {count}
        </span>
      )}
    </Link>
  )
}
