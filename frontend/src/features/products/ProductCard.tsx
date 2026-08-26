import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import type { ProductSummaryDto } from '../../api/catalogApi'
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

  const search = branchId ? `?branchId=${encodeURIComponent(branchId)}` : ''
  const href = `/product/${product.id}${search}`

  return (
    <Link
      className="product-card__link"
      to={href}
      aria-label={`Xem ${product.name}`}
    >
      <article
        className="product-card"
        data-testid={`product-card-${product.id}`}
      >
      <div className="product-card__image-container">
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
        <span className="product-card__category-badge">{product.categoryName}</span>
      </div>

      <div className="product-card__body">
        <div className="product-card__meta">
          <span className="product-card__brand">{product.brandName}</span>
          <span className="product-card__sku">SKU: {product.sku}</span>
        </div>

        <h3 className="product-card__name" title={product.name}>
          {product.name}
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
          <span className="product-card__action-btn" aria-hidden="true">Chi tiết</span>
        </div>
      </div>
    </article>
    </Link>
  )
}
