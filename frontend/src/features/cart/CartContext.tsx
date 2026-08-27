import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
  type PropsWithChildren,
} from 'react'
import { cartApi, type CartDto } from '../../api/cartApi'
import { ApiError } from '../../api/httpClient'
import { useAuth } from '../auth/AuthContext'

export type CartStatus = 'idle' | 'loading' | 'ready' | 'error'

export interface CartContextValue {
  status: CartStatus
  cart: CartDto | null
  errorMessage: string | null
  mutatingItemIds: ReadonlySet<string>
  isAddingItem: boolean
  isChangingBranch: boolean
  isClearing: boolean
  reloadCart: () => Promise<void>
  addItem: (productId: string, quantity: number) => Promise<CartDto>
  updateItemQuantity: (itemId: string, quantity: number) => Promise<CartDto>
  removeItem: (itemId: string) => Promise<CartDto>
  changeBranch: (branchId: string) => Promise<CartDto>
  clearCart: () => Promise<void>
}

const CartContext = createContext<CartContextValue | null>(null)

export function CartProvider({ children }: PropsWithChildren) {
  const { user, accessToken, isAuthenticated, isLoading } = useAuth()

  const [status, setStatus] = useState<CartStatus>('idle')
  const [cart, setCart] = useState<CartDto | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [mutatingItemIds, setMutatingItemIds] = useState<Set<string>>(() => new Set())
  const [isAddingItem, setIsAddingItem] = useState(false)
  const [isChangingBranch, setIsChangingBranch] = useState(false)
  const [isClearing, setIsClearing] = useState(false)

  const loadAbortRef = useRef<AbortController | null>(null)
  const loadVersionRef = useRef(0)

  const mutationQueueRef = useRef<Promise<void>>(Promise.resolve())
  const mutationControllersRef = useRef(new Set<AbortController>())
  const sessionVersionRef = useRef(0)

  const pendingItemMutationsRef = useRef<Map<string, number>>(new Map())
  const addingCountRef = useRef(0)
  const changingBranchCountRef = useRef(0)
  const clearingCountRef = useRef(0)

  const resetCartState = useCallback(() => {
    setStatus('idle')
    setCart(null)
    setErrorMessage(null)
  }, [])

  const startLoad = useCallback(async (token: string) => {
    const controller = new AbortController()
    const requestVersion = ++loadVersionRef.current
    loadAbortRef.current?.abort()
    loadAbortRef.current = controller
    setStatus('loading')
    setErrorMessage(null)
    try {
      const data = await cartApi.getCart(token, controller.signal)
      if (!controller.signal.aborted && requestVersion === loadVersionRef.current) {
        setCart(data)
        setStatus('ready')
      }
    } catch {
      if (!controller.signal.aborted && requestVersion === loadVersionRef.current) {
        setErrorMessage('Không thể tải giỏ hàng.')
        setStatus('error')
      }
    }
  }, [])

  const reloadCart = useCallback(async () => {
    if (accessToken) {
      await startLoad(accessToken)
    }
  }, [accessToken, startLoad])

  // Auth lifecycle effect for cart loading and reset
  useEffect(() => {
    if (isLoading) return
    if (!isAuthenticated || !user || !accessToken) {
      loadVersionRef.current += 1
      loadAbortRef.current?.abort()
      resetCartState()
      return
    }
    void startLoad(accessToken)
    return () => loadAbortRef.current?.abort()
  }, [accessToken, isAuthenticated, isLoading, resetCartState, startLoad, user?.id])

  // Session mutations abort effect
  useEffect(() => {
    const abortSessionMutations = () => {
      sessionVersionRef.current += 1
      mutationControllersRef.current.forEach((controller) => controller.abort())
      mutationControllersRef.current.clear()
      mutationQueueRef.current = Promise.resolve()
      pendingItemMutationsRef.current.clear()
      addingCountRef.current = 0
      changingBranchCountRef.current = 0
      clearingCountRef.current = 0
    }

    abortSessionMutations()
    setMutatingItemIds(new Set())
    setIsAddingItem(false)
    setIsChangingBranch(false)
    setIsClearing(false)

    return abortSessionMutations
  }, [accessToken, isAuthenticated, user?.id])

  const requireAccessToken = useCallback(() => {
    if (!accessToken) {
      throw new ApiError(401, { message: 'AUTH_REQUIRED' }, 'Authentication required')
    }
    return accessToken
  }, [accessToken])

  const enqueueMutation = useCallback(<T,>(operation: () => Promise<T>): Promise<T> => {
    const run = mutationQueueRef.current.then(operation, operation)
    mutationQueueRef.current = run.then(
      () => undefined,
      () => undefined
    )
    return run
  }, [])

  const runItemMutation = useCallback(
    async (
      itemId: string,
      request: (token: string, signal: AbortSignal) => Promise<CartDto>
    ): Promise<CartDto> => {
      const token = requireAccessToken()
      const controller = new AbortController()
      const requestVersion = sessionVersionRef.current
      mutationControllersRef.current.add(controller)

      const currentCount = (pendingItemMutationsRef.current.get(itemId) ?? 0) + 1
      pendingItemMutationsRef.current.set(itemId, currentCount)
      setMutatingItemIds(new Set(pendingItemMutationsRef.current.keys()))

      try {
        const next = await enqueueMutation(() => request(token, controller.signal))
        if (!controller.signal.aborted && requestVersion === sessionVersionRef.current) {
          setCart(next)
        }
        return next
      } finally {
        mutationControllersRef.current.delete(controller)
        const nextCount = (pendingItemMutationsRef.current.get(itemId) ?? 1) - 1
        if (nextCount <= 0) {
          pendingItemMutationsRef.current.delete(itemId)
        } else {
          pendingItemMutationsRef.current.set(itemId, nextCount)
        }
        setMutatingItemIds(new Set(pendingItemMutationsRef.current.keys()))
      }
    },
    [enqueueMutation, requireAccessToken]
  )

  const runGlobalMutation = useCallback(
    async (
      countRef: React.MutableRefObject<number>,
      setFlag: React.Dispatch<React.SetStateAction<boolean>>,
      request: (token: string, signal: AbortSignal) => Promise<CartDto>
    ): Promise<CartDto> => {
      const token = requireAccessToken()
      const controller = new AbortController()
      const requestVersion = sessionVersionRef.current
      mutationControllersRef.current.add(controller)

      countRef.current += 1
      setFlag(true)

      try {
        const next = await enqueueMutation(() => request(token, controller.signal))
        if (!controller.signal.aborted && requestVersion === sessionVersionRef.current) {
          setCart(next)
        }
        return next
      } finally {
        mutationControllersRef.current.delete(controller)
        countRef.current = Math.max(0, countRef.current - 1)
        setFlag(countRef.current > 0)
      }
    },
    [enqueueMutation, requireAccessToken]
  )

  const updateItemQuantity = useCallback(
    (itemId: string, quantity: number) =>
      runItemMutation(itemId, (token, signal) =>
        cartApi.updateItem(itemId, { quantity }, token, signal)
      ),
    [runItemMutation]
  )

  const removeItem = useCallback(
    (itemId: string) =>
      runItemMutation(itemId, (token, signal) =>
        cartApi.removeItem(itemId, token, signal)
      ),
    [runItemMutation]
  )

  const addItem = useCallback(
    (productId: string, quantity: number) =>
      runGlobalMutation(addingCountRef, setIsAddingItem, (token, signal) =>
        cartApi.addItem({ productId, quantity }, token, signal)
      ),
    [runGlobalMutation]
  )

  const changeBranch = useCallback(
    (branchId: string) =>
      runGlobalMutation(changingBranchCountRef, setIsChangingBranch, (token, signal) =>
        cartApi.changeBranch({ branchId }, token, signal)
      ),
    [runGlobalMutation]
  )

  const clearCart = useCallback(async () => {
    const current = cart
    if (!current) return
    await runGlobalMutation(clearingCountRef, setIsClearing, async (token, signal) => {
      await cartApi.clearCart(token, signal)
      return { ...current, items: [], totalItems: 0, subtotal: 0 }
    })
  }, [cart, runGlobalMutation])

  const value: CartContextValue = {
    status,
    cart,
    errorMessage,
    mutatingItemIds,
    isAddingItem,
    isChangingBranch,
    isClearing,
    reloadCart,
    addItem,
    updateItemQuantity,
    removeItem,
    changeBranch,
    clearCart,
  }

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}

export function useCart(): CartContextValue {
  const context = useContext(CartContext)
  if (!context) {
    throw new Error('useCart must be used within a CartProvider')
  }
  return context
}
