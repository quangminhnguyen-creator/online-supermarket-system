import { useState, useEffect } from 'react'
import type { CartDto, CartItemDto } from '../../api/cartApi'
import { formatPrice } from '../products/ProductCard'

export interface CartItemRowProps {
  item: CartItemDto
  isMutating: boolean
  errorMessage?: string
  onUpdate: (itemId: string, quantity: number) => Promise<CartDto>
  onRemove: (itemId: string) => Promise<CartDto>
}

export function CartItemRow({
  item,
  isMutating,
  errorMessage,
  onUpdate,
  onRemove,
}: CartItemRowProps) {
  const [draftQuantity, setDraftQuantity] = useState(String(item.quantity))

  useEffect(() => {
    setDraftQuantity(String(item.quantity))
  }, [item.quantity])

  const submitQuantity = async (nextQuantity: number) => {
    if (!Number.isInteger(nextQuantity) || nextQuantity < 1) {
      setDraftQuantity(String(item.quantity))
      return
    }
    if (nextQuantity === item.quantity) {
      setDraftQuantity(String(item.quantity))
      return
    }
    try {
      await onUpdate(item.id, nextQuantity)
    } catch {
      setDraftQuantity(String(item.quantity))
    }
  }

  const atStockLimit = item.availableQuantity <= item.quantity

  return (
    <article className="cart-item">
      <div
        className="cart-item__placeholder"
        role="img"
        aria-label={'Ảnh thay thế cho ' + item.productName}
      >
        SP
      </div>
      <div className="cart-item__details">
        <h2 className="cart-item__title">{item.productName}</h2>
        <p className="cart-item__sku">{item.sku}</p>
        <p className="cart-item__unit-price">Đơn giá: {formatPrice(item.unitPrice)}</p>
        <p className="cart-item__stock">Còn {item.availableQuantity} sản phẩm tại kho</p>
        <strong className="cart-item__line-total">
          Thành tiền: {formatPrice(item.lineTotal)}
        </strong>
      </div>
      <div className="cart-item__actions">
        <div className="cart-item__quantity-controls">
          <button
            type="button"
            className="cart-item__qty-btn"
            aria-label={'Giảm số lượng ' + item.productName}
            disabled={isMutating || item.quantity <= 1}
            onClick={() => void submitQuantity(item.quantity - 1)}
          >
            −
          </button>
          <input
            className="cart-item__qty-input"
            aria-label={'Số lượng ' + item.productName}
            inputMode="numeric"
            value={draftQuantity}
            disabled={isMutating}
            onChange={(event) => setDraftQuantity(event.target.value)}
            onBlur={() => void submitQuantity(Number(draftQuantity))}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.currentTarget.blur()
              }
            }}
          />
          <button
            type="button"
            className="cart-item__qty-btn"
            aria-label={'Tăng số lượng ' + item.productName}
            disabled={isMutating || atStockLimit}
            onClick={() => void submitQuantity(item.quantity + 1)}
          >
            +
          </button>
        </div>
        <button
          type="button"
          className="cart-item__remove-btn"
          aria-label={'Xóa ' + item.productName}
          disabled={isMutating}
          onClick={() => void onRemove(item.id).catch(() => undefined)}
        >
          Xóa
        </button>
      </div>
      {isMutating && (
        <span className="cart-item__status" role="status">
          Đang cập nhật {item.productName}
        </span>
      )}
      {errorMessage && (
        <p className="cart-item__error" role="alert">
          {errorMessage}
        </p>
      )}
    </article>
  )
}
