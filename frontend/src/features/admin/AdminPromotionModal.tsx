import { useState } from 'react'
import { adminApi, type PromotionDto } from '../../api/adminApi'
import { ApiError } from '../../api/httpClient'
import { useAuth } from '../auth/AuthContext'
import './AdminInventoryPage.css'

interface Props {
  mode: 'create' | 'edit'
  promotion?: PromotionDto
  onClose: () => void
  onSaved: (saved: PromotionDto, mode: 'create' | 'edit') => void
}

export function AdminPromotionModal({ mode, promotion, onClose, onSaved }: Props) {
  const { accessToken } = useAuth()
  const [code, setCode] = useState(promotion?.code ?? '')
  const [discountType, setDiscountType] = useState(promotion?.discountType ?? 'Percentage')
  const [discountValue, setDiscountValue] = useState(String(promotion?.discountValue ?? ''))
  const [minOrderAmount, setMinOrderAmount] = useState(String(promotion?.minOrderAmount ?? '0'))
  const [usageLimit, setUsageLimit] = useState(
    promotion?.usageLimit != null ? String(promotion.usageLimit) : '',
  )
  const [isActive, setIsActive] = useState(promotion?.isActive ?? true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function validate(): string | null {
    if (mode === 'create' && !code.trim()) return 'Vui lòng nhập mã.'
    const value = Number(discountValue)
    const min = Number(minOrderAmount)
    if (!Number.isFinite(value) || value <= 0) return 'Giá trị giảm phải lớn hơn 0.'
    if (discountType === 'Percentage' && value > 100) return 'Giảm theo phần trăm không vượt quá 100.'
    if (!Number.isFinite(min) || min < 0) return 'Đơn tối thiểu phải là số không âm.'
    if (usageLimit.trim() !== '') {
      const limit = Number(usageLimit)
      if (!Number.isInteger(limit) || limit < 1) return 'Giới hạn lượt dùng phải là số nguyên ≥ 1.'
    }
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

    const limit = usageLimit.trim() === '' ? null : Number(usageLimit)

    try {
      if (mode === 'create') {
        const created = await adminApi.createPromotion(
          {
            code: code.trim(),
            discountType,
            discountValue: Number(discountValue),
            minOrderAmount: Number(minOrderAmount),
            usageLimit: limit,
          },
          accessToken,
        )
        onSaved(created, 'create')
      } else {
        const updated = await adminApi.updatePromotion(
          promotion!.id,
          {
            discountValue: Number(discountValue),
            minOrderAmount: Number(minOrderAmount),
            usageLimit: limit,
            isActive,
          },
          accessToken,
        )
        onSaved(updated, 'edit')
      }
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError('Mã giảm giá này đã tồn tại.')
      } else if (err instanceof ApiError && err.data?.message) {
        setError(err.data.message)
      } else {
        setError('Không thể lưu. Vui lòng thử lại.')
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
        aria-label={mode === 'create' ? 'Tạo mã giảm giá' : `Sửa mã ${promotion?.code}`}
        onClick={(e) => e.stopPropagation()}
      >
        <header className="admin-modal-header">
          <h2>{mode === 'create' ? 'Tạo mã giảm giá' : `Sửa mã: ${promotion?.code}`}</h2>
          <button type="button" className="admin-modal-close" onClick={onClose} aria-label="Đóng">×</button>
        </header>
        <form onSubmit={submit}>
          {mode === 'create' && (
            <div className="admin-field">
              <label htmlFor="promo-code">Mã</label>
              <input
                id="promo-code"
                type="text"
                value={code}
                placeholder="VD: SUMMER20"
                onChange={(e) => setCode(e.target.value)}
              />
            </div>
          )}
          <div className="admin-field">
            <label htmlFor="promo-type">Loại giảm</label>
            <select
              id="promo-type"
              value={discountType}
              onChange={(e) => setDiscountType(e.target.value)}
              disabled={mode === 'edit'}
            >
              <option value="Percentage">Phần trăm (%)</option>
              <option value="FixedAmount">Số tiền cố định (₫)</option>
            </select>
          </div>
          <div className="admin-field">
            <label htmlFor="promo-value">Giá trị giảm</label>
            <input
              id="promo-value"
              type="text"
              inputMode="decimal"
              value={discountValue}
              onChange={(e) => setDiscountValue(e.target.value)}
            />
          </div>
          <div className="admin-field">
            <label htmlFor="promo-min">Đơn tối thiểu (₫)</label>
            <input
              id="promo-min"
              type="text"
              inputMode="numeric"
              value={minOrderAmount}
              onChange={(e) => setMinOrderAmount(e.target.value)}
            />
          </div>
          <div className="admin-field">
            <label htmlFor="promo-limit">Giới hạn lượt dùng (để trống = không giới hạn)</label>
            <input
              id="promo-limit"
              type="text"
              inputMode="numeric"
              value={usageLimit}
              placeholder="Không giới hạn"
              onChange={(e) => setUsageLimit(e.target.value)}
            />
          </div>
          {mode === 'edit' && (
            <div className="admin-field">
              <label htmlFor="promo-active">
                <input
                  id="promo-active"
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                />
                {' '}Đang áp dụng
              </label>
            </div>
          )}
          {error && <p className="admin-error" role="alert">{error}</p>}
          <div className="admin-modal-actions">
            <button type="button" className="admin-btn-secondary" onClick={onClose} disabled={submitting}>Hủy</button>
            <button type="submit" className="btn-primary" disabled={submitting}>
              {submitting ? 'Đang lưu...' : mode === 'create' ? 'Tạo mã' : 'Lưu thay đổi'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
