import React from 'react'
import type { AddressDto } from '../../api/addressApi'

interface AddressCardProps {
  address: AddressDto
  onEdit: (address: AddressDto) => void
  onDelete: (address: AddressDto) => void
  onSetDefault: (address: AddressDto) => void
  isActionLoading?: boolean
}

export function AddressCard({
  address,
  onEdit,
  onDelete,
  onSetDefault,
  isActionLoading,
}: AddressCardProps) {
  const fullAddress = [
    address.street,
    address.ward,
    address.district,
    address.city,
    address.postalCode ? `(Mã Bưu Điện: ${address.postalCode})` : null,
  ]
    .filter(Boolean)
    .join(', ')

  return (
    <article
      className={`address-card ${address.isDefault ? 'address-card--default' : ''}`}
      data-testid={`address-card-${address.id}`}
    >
      <div className="address-card__header">
        <div className="address-card__meta">
          <div className="address-card__recipient">
            <span className="recipient-icon">👤</span>
            <strong className="recipient-name">{address.recipientName}</strong>
          </div>
          <span className="recipient-phone">
            <span className="phone-icon">📞</span> {address.phone}
          </span>
        </div>

        {address.isDefault && (
          <span className="address-badge-default" data-testid="default-badge">
            <span className="badge-check">✓</span> Mặc Định
          </span>
        )}
      </div>

      <div className="address-card__body">
        <p className="address-card__text">
          <span className="pin-icon">📍</span> {fullAddress}
        </p>
      </div>

      <div className="address-card__actions">
        {!address.isDefault && (
          <button
            type="button"
            className="btn-address-action btn-address-action--default"
            disabled={isActionLoading}
            onClick={() => onSetDefault(address)}
            data-testid={`btn-set-default-${address.id}`}
          >
            ⭐ Đặt làm mặc định
          </button>
        )}

        <button
          type="button"
          className="btn-address-action btn-address-action--edit"
          disabled={isActionLoading}
          onClick={() => onEdit(address)}
          data-testid={`btn-edit-${address.id}`}
        >
          ✏️ Sửa
        </button>

        <button
          type="button"
          className="btn-address-action btn-address-action--delete"
          disabled={isActionLoading}
          onClick={() => onDelete(address)}
          data-testid={`btn-delete-${address.id}`}
        >
          🗑️ Xóa
        </button>
      </div>
    </article>
  )
}
