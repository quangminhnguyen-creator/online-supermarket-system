import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import type { ProductSummaryDto } from '../../api/catalogApi'
import { useCompare } from '../compare/CompareContext'
import './ProductCard.css'

export interface ProductCardProps {
  product: ProductSummaryDto
  branchName?: string
  branchId?: string
}

export const formatPrice = (price: number): string => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
  }).format(price)
}

export function ProductCard({ product, branchName, branchId }: ProductCardProps) {
  const [imageError, setImageError] = useState(false)
  const { isInCompare, addToCompare, removeFromCompare, openModal, getDifferentCategoryWarning } = useCompare()

  const isInList = isInCompare(product.id)
  const search = branchId ? `?branchId=${encodeURIComponent(branchId)}` : ''
  const href = `/product/${product.id}${search}`

  const handleCompareClick = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()

    if (isInList) {
      removeFromCompare(product.id)
      return
    }

    const warning = getDifferentCategoryWarning({
      id: product.id,
      categoryId: product.categoryId ?? '',
      categoryName: product.categoryName,
      categorySlug: product.categorySlug ?? '',
    })

    if (warning) {
      alert(warning)
      return
    }

    const added = addToCompare({
      id: product.id,
      categoryId: product.categoryId ?? '',
      categoryName: product.categoryName,
      categorySlug: product.categorySlug ?? '',
    })

    if (added) {
      openModal()
    }
  }

  return (
    <article
      className="product-card"
      data-testid={`product-card-${product.id}`}
    >
      <div className="product-card__image-container">
        <Link
          className="product-card__link"
          to={href}
          aria-label={`Xem ${product.name}`}
        >
          {product.imageUrl && !imageError ? (
            <img
              src={product.imageUrl}
              alt={product.name}
              className="product-card__image"
              loading="lazy"
              onError={() => setImageError(true)}
            />
          ) : (
            <div className="product-card__placeholder">
              <svg
                className="product-card__placeholder-icon"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                aria-hidden="true"
              >
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
              </svg>
              <span>Hình ảnh sản phẩm</span>
            </div>
          )}
        </Link>
        <span className="product-card__category-badge">{product.categoryName}</span>
        <button
          type="button"
          className={`product-card__compare-btn${isInList ? ' product-card__compare-btn--active' : ''}`}
          onClick={handleCompareClick}
          aria-label={isInList ? 'Xóa khỏi so sánh' : 'Thêm vào so sánh'}
          title={isInList ? 'Xóa khỏi so sánh' : 'So sánh'}
        >
          {isInList ? (
            <svg viewBox="0 0 24 24" fill="currentColor">
              <path d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          ) : (
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path strokeLinecap="round" strokeLinejoin="round" d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
            </svg>
          )}
        </button>
      </div>

      <div className="product-card__body">
        <div className="product-card__meta">
          <span className="product-card__brand">{product.brandName}</span>
          <span className="product-card__sku">SKU: {product.sku}</span>
        </div>

        <h3 className="product-card__name" title={product.name}>
          <Link to={href} className="product-card__name-link">
            {product.name}
          </Link>
        </h3>

        <div className="product-card__footer">
          <div className="product-card__price-box">
            <span className="product-card__price">{formatPrice(product.basePrice)}</span>
            {branchName && (
              <span className="product-card__branch-hint">
                <span className="branch-dot" /> {branchName}
              </span>
            )}
          </div>
          <Link to={href} className="product-card__action-btn" aria-label={`Chi tiết ${product.name}`}>
            Chi tiết
          </Link>
        </div>
      </div>
    </article>
  )
}
