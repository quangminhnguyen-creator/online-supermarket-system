import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { catalogApi, type ProductDetailDto } from '../../api/catalogApi'
import { branchApi, type BranchDto } from '../../api/branchApi'
import { formatPrice } from '../products/ProductCard'
import { useCompare, type CompareProduct } from './CompareContext'
import './CompareModal.css'

type LoadingState = 'idle' | 'loading' | 'error'

interface ProductDetailState {
  product: ProductDetailDto | null
  state: LoadingState
  error: string | null
  fetchedBranchId: string | null
}

export function CompareModal() {
  const { compareProducts, removeFromCompare, clearCompare, isModalOpen, closeModal } = useCompare()
  const [productStates, setProductStates] = useState<Map<string, ProductDetailState>>(
    new Map()
  )
  const [selectedBranchId, setSelectedBranchId] = useState<string | null>(null)
  const [branches, setBranches] = useState<BranchDto[]>([])
  const [branchesError, setBranchesError] = useState(false)
  const [branchesRetryKey, setBranchesRetryKey] = useState(0)
  const modalRef = useRef<HTMLDivElement>(null)
  const closeButtonRef = useRef<HTMLButtonElement>(null)

// Keep product state in sync with the compare list, preserving loaded cache
  useEffect(() => {
    setProductStates((prev) => {
      const next = new Map(prev)
      compareProducts.forEach((product) => {
        if (!next.has(product.id)) {
          next.set(product.id, {
            product: null,
            state: 'idle',
            error: null,
            fetchedBranchId: null,
          })
        }
      })
      for (const id of next.keys()) {
        if (!compareProducts.some((p) => p.id === id)) {
          next.delete(id)
        }
      }
      return next
    })
  }, [compareProducts])

  // Focus close button when modal opens
  useEffect(() => {
    if (isModalOpen) {
      closeButtonRef.current?.focus()
    }
  }, [isModalOpen])

  // Fetch active branches for the selector
  useEffect(() => {
    const controller = new AbortController()
    setBranchesError(false)
    branchApi
      .getBranches(controller.signal)
      .then((data) => setBranches(data))
      .catch(() => {
        if (controller.signal.aborted) return
        setBranchesError(true)
      })
    return () => controller.abort()
  }, [branchesRetryKey])

  // Fetch product details
  useEffect(() => {
    const controllers: AbortController[] = []

    const fetchProductDetails = async (product: CompareProduct) => {
      const controller = new AbortController()
      controllers.push(controller)
      setProductStates((prev) => {
        const next = new Map(prev)
        next.set(product.id, {
          ...next.get(product.id)!,
          state: 'loading',
          fetchedBranchId: selectedBranchId,
        })
        return next
      })

      try {
        const detail = await catalogApi.getProductById(
          product.id,
          selectedBranchId ?? undefined,
          controller.signal
        )
        if (controller.signal.aborted) return
        setProductStates((prev) => {
          const next = new Map(prev)
          next.set(product.id, {
            product: detail,
            state: 'idle',
            error: null,
            fetchedBranchId: selectedBranchId,
          })
          return next
        })
      } catch {
        if (!controller.signal.aborted) {
          setProductStates((prev) => {
            const next = new Map(prev)
            next.set(product.id, {
              product: null,
              state: 'error',
              error: 'Không thể tải chi tiết sản phẩm.',
              fetchedBranchId: selectedBranchId,
            })
            return next
          })
          return
        }
        // Aborted: drop stale data so a later open refetches cleanly
        setProductStates((prev) => {
          const next = new Map(prev)
          next.set(product.id, {
            product: null,
            state: 'idle',
            error: null,
            fetchedBranchId: null,
          })
          return next
        })
      }
    }

    if (isModalOpen && compareProducts.length > 0) {
      compareProducts.forEach((product) => {
        const current = productStates.get(product.id)
        const fetchingCurrentBranch =
          current?.state === 'loading' && current?.fetchedBranchId === selectedBranchId
        const cachedForCurrentBranch =
          current?.state === 'idle' &&
          current?.product != null &&
          current?.fetchedBranchId === selectedBranchId
        if (!fetchingCurrentBranch && !cachedForCurrentBranch) {
          void fetchProductDetails(product)
        }
      })
    }

    return () => {
      controllers.forEach((controller) => controller.abort())
    }
  }, [isModalOpen, compareProducts, selectedBranchId])

  // Handle escape key
  useEffect(() => {
    if (!isModalOpen) return

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        closeModal()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isModalOpen, closeModal])

  // Handle click outside
  const handleOverlayClick = (event: React.MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      closeModal()
    }
  }

  if (!isModalOpen) return null

  const renderStockStatus = (product: ProductDetailDto) => {
    if (!selectedBranchId) {
      return <span className="compare-stock neutral">Chọn kho để xem tồn kho</span>
    }
    if (!product.branchInventory) {
      return <span className="compare-stock unavailable">Không có tại kho này</span>
    }
    if (product.branchInventory.availableQuantity === 0) {
      return <span className="compare-stock unavailable">Hết hàng</span>
    }
    return (
      <span className="compare-stock available">
        Còn {product.branchInventory.availableQuantity} sản phẩm
      </span>
    )
  }

  const isAnyLoading = Array.from(productStates.values()).some((s) => s.state === 'loading')
  const hasProducts = compareProducts.length > 0

  return (
    <div
      className="compare-modal-overlay"
      onClick={handleOverlayClick}
      role="dialog"
      aria-modal="true"
      aria-labelledby="compare-modal-title"
    >
      <div className="compare-modal" ref={modalRef}>
        <header className="compare-modal__header">
          <h2 id="compare-modal-title">So sánh sản phẩm</h2>
          <div className="compare-modal__actions">
            {hasProducts && (
              <button
                type="button"
                className="compare-modal__clear-btn"
                onClick={() => {
                  clearCompare()
                  closeModal()
                }}
              >
                Xóa tất cả
              </button>
            )}
            <button
              ref={closeButtonRef}
              type="button"
              className="compare-modal__close-btn"
              onClick={closeModal}
              aria-label="Đóng"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </header>

        {!hasProducts ? (
          <div className="compare-modal__empty">
            <p>Chưa có sản phẩm nào để so sánh.</p>
            <p className="compare-modal__hint">
              Nhấn nút so sánh trên thẻ sản phẩm để thêm vào danh sách so sánh.
            </p>
          </div>
        ) : (
          <>
            {/* Branch selector */}
            <div className="compare-modal__branch-selector">
              <label htmlFor="compare-branch-select">Kho hàng:</label>
              <select
                id="compare-branch-select"
                value={selectedBranchId ?? ''}
                onChange={(e) => setSelectedBranchId(e.target.value || null)}
                disabled={branchesError}
              >
                <option value="">Tất cả chi nhánh</option>
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>
                    {branch.name}
                  </option>
                ))}
              </select>
              {branchesError && (
                <button
                  type="button"
                  className="compare-modal__branch-retry"
                  onClick={() => setBranchesRetryKey((k) => k + 1)}
                >
                  Tải lại danh sách kho
                </button>
              )}
              {isAnyLoading && <span className="compare-loading-indicator">Đang tải...</span>}
            </div>

            {/* Compare table */}
            <div className="compare-table-wrapper">
              <table className="compare-table">
                <thead>
                  <tr>
                    <th className="compare-table__attribute-col">Thuộc tính</th>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      return (
                        <th key={product.id} className="compare-table__product-col">
                          <div className="compare-table__product-header">
                            <button
                              type="button"
                              className="compare-table__remove-btn"
                              onClick={() => removeFromCompare(product.id)}
                              aria-label={`Xóa ${product.categoryName} khỏi danh sách so sánh`}
                            >
                              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                              </svg>
                            </button>
                            {state?.state === 'loading' ? (
                              <div className="compare-skeleton" />
                            ) : state?.state === 'error' ? (
                              <span className="compare-error">{state.error}</span>
                            ) : (
                              <span className="compare-table__product-name">
                                {product.categoryName}
                              </span>
                            )}
                          </div>
                        </th>
                      )
                    })}
                    {/* Empty column for second product slot if only one product */}
                    {compareProducts.length === 1 && (
                      <th className="compare-table__product-col compare-table__empty-slot">
                        <span className="compare-table__empty-text">Chọn sản phẩm thứ 2</span>
                      </th>
                    )}
                  </tr>
                </thead>
                <tbody>
                  {/* Product Image */}
                  <tr className="compare-table__image-row">
                    <td className="compare-table__attribute">Hình ảnh</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      const detail = state?.product
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-image-skeleton shimmer" />
                          ) : detail?.imageUrl ? (
                            <img
                              src={detail.imageUrl}
                              alt={detail.name}
                              className="compare-image"
                            />
                          ) : (
                            <div className="compare-image-placeholder">No Image</div>
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Product Name */}
                  <tr>
                    <td className="compare-table__attribute">Tên sản phẩm</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      const detail = state?.product
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : detail?.name ? (
                            <Link
                              to={`/product/${detail.id}${selectedBranchId ? `?branchId=${selectedBranchId}` : ''}`}
                              className="compare-product-link"
                              onClick={closeModal}
                            >
                              {detail.name}
                            </Link>
                          ) : (
                            <span className="compare-error">{state?.error ?? '-'}</span>
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Buy Now */}
                  <tr>
                    <td className="compare-table__attribute">Mua ngay</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      const detail = state?.product
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : detail ? (
                            <Link
                              to={`/product/${detail.id}${selectedBranchId ? `?branchId=${selectedBranchId}` : ''}`}
                              className="compare-buy-now"
                              onClick={closeModal}
                            >
                              Mua ngay
                            </Link>
                          ) : (
                            <span className="compare-error">{state?.error ?? '-'}</span>
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* SKU */}
                  <tr>
                    <td className="compare-table__attribute">SKU</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : (
                            state?.product?.sku ?? '-'
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Brand */}
                  <tr>
                    <td className="compare-table__attribute">Thương hiệu</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : (
                            state?.product?.brandName ?? '-'
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Category */}
                  <tr>
                    <td className="compare-table__attribute">Danh mục</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : (
                            state?.product?.categoryName ?? '-'
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Base Price */}
                  <tr>
                    <td className="compare-table__attribute">Giá gốc</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : state?.product?.basePrice !== undefined ? (
                            <span className="compare-price-base">
                              {formatPrice(state.product.basePrice)}
                            </span>
                          ) : (
                            '-'
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Selling Price */}
                  <tr>
                    <td className="compare-table__attribute">Giá bán</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      const inventory = state?.product?.branchInventory
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : !selectedBranchId ? (
                            <span className="compare-neutral">-</span>
                          ) : inventory ? (
                            <span className="compare-price-selling">
                              {formatPrice(inventory.sellingPrice)}
                            </span>
                          ) : (
                            <span className="compare-neutral">-</span>
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Stock */}
                  <tr>
                    <td className="compare-table__attribute">Tồn kho</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : state?.product ? (
                            renderStockStatus(state.product)
                          ) : (
                            '-'
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Unit */}
                  <tr>
                    <td className="compare-table__attribute">Đơn vị</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : (
                            state?.product?.unit ?? '-'
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>

                  {/* Description */}
                  <tr className="compare-table__description-row">
                    <td className="compare-table__attribute">Mô tả</td>
                    {compareProducts.map((product) => {
                      const state = productStates.get(product.id)
                      return (
                        <td key={product.id} className="compare-table__cell">
                          {state?.state === 'loading' ? (
                            <div className="compare-skeleton" />
                          ) : (
                            <p className="compare-description">
                              {state?.product?.description || 'Không có mô tả.'}
                            </p>
                          )}
                        </td>
                      )
                    })}
                    {compareProducts.length === 1 && (
                      <td className="compare-table__cell compare-table__empty-slot" />
                    )}
                  </tr>
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
