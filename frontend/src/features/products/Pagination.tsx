import React from 'react'
import type { PaginationMeta } from '../../api/catalogApi'
import './Pagination.css'

export interface PaginationProps {
  meta: PaginationMeta
  onPageChange: (newPage: number) => void
  disabled?: boolean
}

export function Pagination({ meta, onPageChange, disabled = false }: PaginationProps) {
  const { page, pageSize, totalCount, totalPages } = meta

  if (totalPages <= 1 && totalCount <= pageSize) {
    if (totalCount === 0) return null
    return (
      <div className="pagination-wrapper pagination-wrapper--single">
        <span className="pagination-summary">
          Hiển thị <strong>{totalCount}</strong> sản phẩm
        </span>
      </div>
    )
  }

  const startItem = (page - 1) * pageSize + 1
  const endItem = Math.min(page * pageSize, totalCount)

  // Generate page numbers with ellipsis
  const getPageNumbers = (): (number | string)[] => {
    const pages: (number | string)[] = []
    const maxVisible = 5

    if (totalPages <= maxVisible + 2) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i)
      }
    } else {
      pages.push(1)

      let start = Math.max(2, page - 1)
      let end = Math.min(totalPages - 1, page + 1)

      if (page <= 3) {
        start = 2
        end = 4
      } else if (page >= totalPages - 2) {
        start = totalPages - 3
        end = totalPages - 1
      }

      if (start > 2) {
        pages.push('...')
      }

      for (let i = start; i <= end; i++) {
        pages.push(i)
      }

      if (end < totalPages - 1) {
        pages.push('...')
      }

      pages.push(totalPages)
    }

    return pages
  }

  const pageNumbers = getPageNumbers()

  return (
    <nav className="pagination-wrapper" aria-label="Điều hướng trang sản phẩm">
      <div className="pagination-summary">
        Hiển thị <strong>{startItem}</strong> - <strong>{endItem}</strong> trên tổng số{' '}
        <strong>{totalCount}</strong> sản phẩm
      </div>

      <div className="pagination-controls">
        <button
          type="button"
          className="pagination-nav-btn"
          onClick={() => onPageChange(page - 1)}
          disabled={disabled || page <= 1}
          aria-label="Trang trước"
        >
          ‹ Trước
        </button>

        <div className="pagination-pages">
          {pageNumbers.map((p, index) => {
            if (p === '...') {
              return (
                <span key={`ellipsis-${index}`} className="pagination-ellipsis">
                  …
                </span>
              )
            }

            const pageNum = Number(p)
            const isCurrent = pageNum === page

            return (
              <button
                key={pageNum}
                type="button"
                className={`pagination-page-btn ${isCurrent ? 'pagination-page-btn--active' : ''}`}
                onClick={() => onPageChange(pageNum)}
                disabled={disabled || isCurrent}
                aria-current={isCurrent ? 'page' : undefined}
                aria-label={`Trang ${pageNum}`}
              >
                {pageNum}
              </button>
            )
          })}
        </div>

        <button
          type="button"
          className="pagination-nav-btn"
          onClick={() => onPageChange(page + 1)}
          disabled={disabled || page >= totalPages}
          aria-label="Trang sau"
        >
          Sau ›
        </button>
      </div>
    </nav>
  )
}
