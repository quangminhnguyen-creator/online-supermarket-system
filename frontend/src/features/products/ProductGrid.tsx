import React from 'react'
import type { ProductSummaryDto } from '../../api/catalogApi'
import { ProductCard } from './ProductCard'
import './ProductGrid.css'

export interface ProductGridProps {
  products: ProductSummaryDto[]
  loading: boolean
  error?: string | null
  branchName?: string
  onRetry?: () => void
  onResetFilters?: () => void
  onSelectProduct?: (product: ProductSummaryDto) => void
}

export function ProductGrid({
  products,
  loading,
  error,
  branchName,
  onRetry,
  onResetFilters,
  onSelectProduct,
}: ProductGridProps) {
  if (error) {
    return (
      <div className="product-grid__error" role="alert">
        <div className="product-grid__error-icon">⚠️</div>
        <h3>Không thể tải danh sách sản phẩm</h3>
        <p>{error}</p>
        {onRetry && (
          <button type="button" className="product-grid__retry-btn" onClick={onRetry}>
            Thử lại
          </button>
        )}
      </div>
    )
  }

  if (loading) {
    return (
      <div className="product-grid" aria-busy="true" aria-label="Đang tải sản phẩm...">
        {Array.from({ length: 8 }).map((_, index) => (
          <div key={index} className="product-card-skeleton" data-testid="product-skeleton">
            <div className="skeleton-image shimmer" />
            <div className="skeleton-body">
              <div className="skeleton-meta shimmer" />
              <div className="skeleton-title shimmer" />
              <div className="skeleton-title-short shimmer" />
              <div className="skeleton-footer">
                <div className="skeleton-price shimmer" />
                <div className="skeleton-btn shimmer" />
              </div>
            </div>
          </div>
        ))}
      </div>
    )
  }

  if (products.length === 0) {
    return (
      <div className="product-grid__empty" data-testid="products-empty-state">
        <div className="product-grid__empty-icon" aria-hidden="true">
          🛒
        </div>
        <h3>Không tìm thấy sản phẩm phù hợp</h3>
        <p>Thử điều chỉnh hoặc xóa các tiêu chí bộ lọc để tìm kiếm thêm sản phẩm.</p>
        {onResetFilters && (
          <button
            type="button"
            className="product-grid__reset-filters-btn"
            onClick={onResetFilters}
          >
            Xóa bộ lọc tìm kiếm
          </button>
        )}
      </div>
    )
  }

  return (
    <div className="product-grid" data-testid="product-grid">
      {products.map((product) => (
        <ProductCard
          key={product.id}
          product={product}
          branchName={branchName}
          onSelect={onSelectProduct}
        />
      ))}
    </div>
  )
}
