import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type PropsWithChildren,
} from 'react'

export interface CompareProduct {
  id: string
  categoryId: string
  categoryName: string
  categorySlug: string
}

export interface CompareContextValue {
  compareProducts: CompareProduct[]
  isInCompare: (id: string) => boolean
  addToCompare: (product: CompareProduct) => boolean
  removeFromCompare: (id: string) => void
  clearCompare: () => void
  canAddMore: boolean
  hasProduct: boolean
  getDifferentCategoryWarning: (product: CompareProduct) => string | null
  openModal: () => void
  closeModal: () => void
  isModalOpen: boolean
}

const CompareContext = createContext<CompareContextValue | null>(null)

const MAX_COMPARE_PRODUCTS = 2

const UNCATEGORIZED_SLUG = 'uncategorized'

function getCompareBlockReason(
  existing: CompareProduct | undefined,
  candidate: CompareProduct,
): string | null {
  if (candidate.categorySlug === UNCATEGORIZED_SLUG ||
      existing?.categorySlug === UNCATEGORIZED_SLUG) {
    return 'Sản phẩm chưa được phân loại nên chưa thể so sánh.'
  }
  if (existing && existing.categoryId !== candidate.categoryId) {
    return 'Chỉ có thể so sánh các sản phẩm cùng loại.'
  }
  return null
}

export function CompareProvider({ children }: PropsWithChildren) {
  const [compareProducts, setCompareProducts] = useState<CompareProduct[]>([])
  const [isModalOpen, setIsModalOpen] = useState(false)

  // Listen for global open event
  useEffect(() => {
    const handleOpenModal = () => setIsModalOpen(true)
    window.addEventListener('open-compare-modal', handleOpenModal)
    return () => window.removeEventListener('open-compare-modal', handleOpenModal)
  }, [])

  const openModal = useCallback(() => setIsModalOpen(true), [])
  const closeModal = useCallback(() => setIsModalOpen(false), [])

  const isInCompare = useCallback(
    (id: string) => compareProducts.some((p) => p.id === id),
    [compareProducts]
  )

  const getDifferentCategoryWarning = useCallback(
    (product: CompareProduct): string | null =>
      getCompareBlockReason(compareProducts[0], product),
    [compareProducts]
  )

  const addToCompare = useCallback(
    (product: CompareProduct): boolean => {
      if (compareProducts.length >= MAX_COMPARE_PRODUCTS) return false

      // Check if already in compare
      if (compareProducts.some((p) => p.id === product.id)) return false

      if (getCompareBlockReason(compareProducts[0], product) !== null) return false

      setCompareProducts((prev) => [...prev, product])
      return true
    },
    [compareProducts]
  )

  const removeFromCompare = useCallback((id: string) => {
    setCompareProducts((prev) => prev.filter((p) => p.id !== id))
  }, [])

  const clearCompare = useCallback(() => {
    setCompareProducts([])
  }, [])

  const value: CompareContextValue = {
    compareProducts,
    isInCompare,
    addToCompare,
    removeFromCompare,
    clearCompare,
    canAddMore: compareProducts.length < MAX_COMPARE_PRODUCTS,
    hasProduct: compareProducts.length > 0,
    getDifferentCategoryWarning,
    openModal,
    closeModal,
    isModalOpen,
  }

  return (
    <CompareContext.Provider value={value}>{children}</CompareContext.Provider>
  )
}

export function useCompare(): CompareContextValue {
  const context = useContext(CompareContext)
  if (!context) {
    throw new Error('useCompare must be used within a CompareProvider')
  }
  return context
}
