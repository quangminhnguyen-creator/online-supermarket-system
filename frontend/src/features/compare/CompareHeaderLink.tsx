import { useCompare } from './CompareContext'
import './CompareHeaderLink.css'

export function CompareHeaderLink() {
  const { compareProducts, openModal } = useCompare()
  const count = compareProducts.length

  return (
    <button
      type="button"
      className="compare-header-link"
      onClick={openModal}
      aria-label={`So sánh sản phẩm${count > 0 ? ` (${count} sản phẩm)` : ''}`}
    >
      <span aria-hidden="true" className="compare-header-link__icon">
        ⇄
      </span>
      <span>So sánh</span>
      {count > 0 && (
        <span
          className="compare-header-link__badge"
          aria-label={count + ' sản phẩm đang so sánh'}
        >
          {count}
        </span>
      )}
    </button>
  )
}