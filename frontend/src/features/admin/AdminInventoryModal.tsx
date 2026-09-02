import { useEffect, useState } from 'react'
import { adminApi } from '../../api/adminApi'
import { ApiError } from '../../api/httpClient'
import type { BranchProductInventoryDto } from '../../api/branchApi'
import { useAuth } from '../auth/AuthContext'
import './AdminInventoryPage.css'

interface Props {
  branchId: string
  item: BranchProductInventoryDto
  onClose: () => void
  onSaved: (updated: BranchProductInventoryDto) => void
}

export function AdminInventoryModal({ branchId, item, onClose, onSaved }: Props) {
  const { accessToken } = useAuth()
  const [sellingPrice, setSellingPrice] = useState(String(item.sellingPrice))
  const [quantityOnHand, setQuantityOnHand] = useState(String(item.quantityOnHand))
  const [reorderLevel, setReorderLevel] = useState(String(item.reorderLevel))
  const [reason, setReason] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose])

  function validate(): string | null {
    const price = Number(sellingPrice)
    const qty = Number(quantityOnHand)
    const reorder = Number(reorderLevel)
    if (!Number.isFinite(price) || price < 0) return 'Giá bán phải là số không âm.'
    if (!Number.isInteger(qty) || qty < 0) return 'Tồn kho phải là số nguyên không âm.'
    if (!Number.isInteger(reorder) || reorder < 0) return 'Định mức nhập phải là số nguyên không âm.'
    return null
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    const validationError = validate()
    if (validationError) {
      setError(validationError)
      return
    }
    if (!accessToken) return
    setSubmitting(true)
    setError(null)
    try {
      const updated = await adminApi.adjustInventory(
        branchId,
        {
          productId: item.productId,
          quantityOnHand: Number(quantityOnHand),
          sellingPrice: Number(sellingPrice),
          reorderLevel: Number(reorderLevel),
          reason: reason.trim() || undefined,
        },
        accessToken,
      )
      onSaved(updated)
    } catch (err) {
      if (err instanceof ApiError && err.data?.message) {
        setError(err.data.message)
      } else {
        setError('Không thể cập nhật. Vui lòng thử lại.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="admin-modal-overlay" onClick={onClose}>
      <div
        className="admin-modal"
        role="dialog"
        aria-modal="true"
        aria-label={`Chỉnh sửa tồn kho: ${item.productName}`}
        onClick={(event) => event.stopPropagation()}
      >
        <header className="admin-modal-header">
          <h2>Chỉnh sửa: {item.productName}</h2>
          <button type="button" className="admin-modal-close" onClick={onClose} aria-label="Đóng">×</button>
        </header>
        <p className="admin-modal-sku"><code>{item.sku}</code></p>
        <form onSubmit={submit}>
          <div className="admin-field">
            <label htmlFor="inv-price">Giá bán (₫)</label>
            <input
              id="inv-price"
              type="text"
              inputMode="decimal"
              value={sellingPrice}
              onChange={(event) => setSellingPrice(event.target.value)}
            />
          </div>
          <div className="admin-field">
            <label htmlFor="inv-qty">Tồn kho thực tế</label>
            <input
              id="inv-qty"
              type="text"
              inputMode="numeric"
              value={quantityOnHand}
              onChange={(event) => setQuantityOnHand(event.target.value)}
            />
            <span className="admin-field-hint">Đang giữ (reserved): {item.reservedQuantity} — không chỉnh trực tiếp.</span>
          </div>
          <div className="admin-field">
            <label htmlFor="inv-reorder">Định mức nhập (reorder level)</label>
            <input
              id="inv-reorder"
              type="text"
              inputMode="numeric"
              value={reorderLevel}
              onChange={(event) => setReorderLevel(event.target.value)}
            />
          </div>
          <div className="admin-field">
            <label htmlFor="inv-reason">Lý do (tùy chọn)</label>
            <input
              id="inv-reason"
              type="text"
              value={reason}
              placeholder="Nhập hàng / kiểm kê / điều chỉnh..."
              onChange={(event) => setReason(event.target.value)}
            />
          </div>
          {error && <p className="admin-error" role="alert">{error}</p>}
          <div className="admin-modal-actions">
            <button type="button" className="admin-btn-secondary" onClick={onClose} disabled={submitting}>Hủy</button>
            <button type="submit" className="btn-primary" disabled={submitting}>
              {submitting ? 'Đang lưu...' : 'Lưu thay đổi'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
