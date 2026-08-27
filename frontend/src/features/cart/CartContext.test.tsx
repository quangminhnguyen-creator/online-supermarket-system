import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { useAuth } from '../auth/AuthContext'
import { cartApi, type CartDto, type CartItemDto } from '../../api/cartApi'
import { ApiError } from '../../api/httpClient'
import { CartProvider, useCart } from './CartContext'

vi.mock('../auth/AuthContext', () => ({ useAuth: vi.fn() }))
vi.mock('../../api/cartApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api/cartApi')>()
  return {
    ...actual,
    cartApi: {
      getCart: vi.fn(),
      addItem: vi.fn(),
      updateItem: vi.fn(),
      removeItem: vi.fn(),
      changeBranch: vi.fn(),
      clearCart: vi.fn(),
    },
  }
})

const useAuthMock = vi.mocked(useAuth)
const guestAuth: ReturnType<typeof useAuth> = {
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isLoading: false,
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  updateUser: vi.fn(),
  refreshUser: vi.fn(),
}
const authenticatedAuth: ReturnType<typeof useAuth> = {
  ...guestAuth,
  user: {
    id: 'user-1',
    email: 'user@example.com',
    fullName: 'Người mua',
    role: 'Customer',
  },
  accessToken: 'jwt-token',
  isAuthenticated: true,
}

const cartItem1: CartItemDto = {
  id: 'item-1',
  productId: 'prod-1',
  productName: 'Sản phẩm 1',
  sku: 'SKU-1',
  unitPrice: 100,
  quantity: 1,
  lineTotal: 100,
  availableQuantity: 10,
}
const cartItem2: CartItemDto = {
  id: 'item-2',
  productId: 'prod-2',
  productName: 'Sản phẩm 2',
  sku: 'SKU-2',
  unitPrice: 200,
  quantity: 1,
  lineTotal: 200,
  availableQuantity: 10,
}
const cartResponse: CartDto = {
  id: 'cart-1',
  userId: 'user-1',
  branchId: 'branch-1',
  items: [cartItem1, cartItem2],
  totalItems: 2,
  subtotal: 300,
}
const cartWithItem1Updated: CartDto = {
  ...cartResponse,
  items: [{ ...cartItem1, quantity: 2, lineTotal: 200 }, cartItem2],
  totalItems: 3,
  subtotal: 400,
}
const cartWithBothItemsUpdated: CartDto = {
  ...cartResponse,
  items: [
    { ...cartItem1, quantity: 2, lineTotal: 200 },
    { ...cartItem2, quantity: 3, lineTotal: 600 },
  ],
  totalItems: 5,
  subtotal: 800,
}
const updatedCart = cartWithItem1Updated

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

function CartProbe() {
  const { status, cart, errorMessage, reloadCart } = useCart()
  return (
    <>
      <output data-testid="status">{status}</output>
      <output data-testid="total">{cart?.totalItems ?? 'none'}</output>
      <output data-testid="error">{errorMessage ?? ''}</output>
      <button onClick={() => void reloadCart()}>reload</button>
    </>
  )
}

function MutationProbe() {
  const cartContext = useCart()
  return (
    <>
      <output data-testid="total">{cartContext.cart?.totalItems ?? 'none'}</output>
      <output data-testid="locked-item-1">
        {String(cartContext.mutatingItemIds.has('item-1'))}
      </output>
      <output data-testid="adding">{String(cartContext.isAddingItem)}</output>
      <button
        onClick={() =>
          void cartContext.updateItemQuantity('item-1', 2).catch(() => undefined)
        }
      >
        update-item-1
      </button>
      <button
        onClick={() =>
          void cartContext.updateItemQuantity('item-2', 3).catch(() => undefined)
        }
      >
        update-item-2
      </button>
      <button
        onClick={() =>
          void cartContext.removeItem('item-1').catch(() => undefined)
        }
      >
        remove-item-1
      </button>
      <button
        onClick={() =>
          void cartContext.addItem('prod-1', 1).catch(() => undefined)
        }
      >
        add-prod-1
      </button>
    </>
  )
}

async function renderAuthenticatedMutationProbe() {
  useAuthMock.mockReturnValue(authenticatedAuth)
  vi.mocked(cartApi.getCart).mockResolvedValue(cartResponse)
  const view = render(
    <CartProvider>
      <MutationProbe />
    </CartProvider>
  )
  await waitFor(() =>
    expect(screen.getByTestId('total')).not.toHaveTextContent('none')
  )
  return view
}

describe('CartContext', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('throws error when useCart is called outside CartProvider', () => {
    const originalConsoleError = console.error
    console.error = vi.fn()
    expect(() => render(<CartProbe />)).toThrow(
      'useCart must be used within a CartProvider'
    )
    console.error = originalConsoleError
  })

  it('does not load cart for a guest', () => {
    useAuthMock.mockReturnValue(guestAuth)
    render(
      <CartProvider>
        <CartProbe />
      </CartProvider>
    )
    expect(cartApi.getCart).not.toHaveBeenCalled()
    expect(screen.getByTestId('status')).toHaveTextContent('idle')
  })

  it('loads cart once after authentication', async () => {
    useAuthMock.mockReturnValue(authenticatedAuth)
    vi.mocked(cartApi.getCart).mockResolvedValue(cartResponse)
    render(
      <CartProvider>
        <CartProbe />
      </CartProvider>
    )
    expect(await screen.findByTestId('total')).toHaveTextContent('2')
    expect(cartApi.getCart).toHaveBeenCalledWith('jwt-token', expect.any(AbortSignal))
  })

  it('aborts an in-flight load on logout and ignores its result', async () => {
    const deferredCart = deferred<CartDto>()
    let capturedSignal: AbortSignal | undefined
    vi.mocked(cartApi.getCart).mockImplementation((_token, signal) => {
      capturedSignal = signal
      return deferredCart.promise
    })
    useAuthMock.mockReturnValue(authenticatedAuth)
    const view = render(
      <CartProvider>
        <CartProbe />
      </CartProvider>
    )
    await waitFor(() => expect(capturedSignal).toBeDefined())

    useAuthMock.mockReturnValue(guestAuth)
    view.rerender(
      <CartProvider>
        <CartProbe />
      </CartProvider>
    )

    expect(capturedSignal?.aborted).toBe(true)
    expect(screen.getByTestId('status')).toHaveTextContent('idle')
    deferredCart.resolve(cartResponse)
    await waitFor(() => expect(screen.getByTestId('status')).toHaveTextContent('idle'))
  })

  it('keeps an explicit error state and retries load', async () => {
    vi.mocked(cartApi.getCart)
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(cartResponse)
    useAuthMock.mockReturnValue(authenticatedAuth)
    render(
      <CartProvider>
        <CartProbe />
      </CartProvider>
    )
    expect(await screen.findByTestId('status')).toHaveTextContent('error')
    fireEvent.click(screen.getByRole('button', { name: 'reload' }))
    expect(await screen.findByTestId('status')).toHaveTextContent('ready')
  })

  it('serializes writes and applies responses in request order', async () => {
    const first = deferred<CartDto>()
    const second = deferred<CartDto>()
    vi.mocked(cartApi.updateItem)
      .mockReturnValueOnce(first.promise)
      .mockReturnValueOnce(second.promise)

    await renderAuthenticatedMutationProbe()
    fireEvent.click(screen.getByRole('button', { name: 'update-item-1' }))
    fireEvent.click(screen.getByRole('button', { name: 'update-item-2' }))
    await waitFor(() => expect(cartApi.updateItem).toHaveBeenCalledTimes(1))

    first.resolve(cartWithItem1Updated)
    await waitFor(() => expect(cartApi.updateItem).toHaveBeenCalledTimes(2))
    second.resolve(cartWithBothItemsUpdated)
    expect(await screen.findByTestId('total')).toHaveTextContent('5')
  })

  it('preserves the last cart when a mutation fails', async () => {
    vi.mocked(cartApi.removeItem).mockRejectedValue(new ApiError(500))
    await renderAuthenticatedMutationProbe()
    fireEvent.click(screen.getByRole('button', { name: 'remove-item-1' }))
    expect(await screen.findByTestId('total')).toHaveTextContent('2')
  })

  it('adds and removes an item id from mutatingItemIds on success', async () => {
    const deferredUpdate = deferred<CartDto>()
    vi.mocked(cartApi.updateItem).mockReturnValue(deferredUpdate.promise)
    await renderAuthenticatedMutationProbe()
    fireEvent.click(screen.getByRole('button', { name: 'update-item-1' }))
    expect(await screen.findByTestId('locked-item-1')).toHaveTextContent('true')
    deferredUpdate.resolve(updatedCart)
    await waitFor(() =>
      expect(screen.getByTestId('locked-item-1')).toHaveTextContent('false')
    )
  })

  it('removes an item id from mutatingItemIds on failure', async () => {
    vi.mocked(cartApi.updateItem).mockRejectedValue(
      new ApiError(409, { message: 'INSUFFICIENT_STOCK', availableQuantity: 1 })
    )
    await renderAuthenticatedMutationProbe()
    fireEvent.click(screen.getByRole('button', { name: 'update-item-1' }))
    expect(await screen.findByTestId('locked-item-1')).toHaveTextContent('false')
  })

  it('resets locks and mutation flags on logout', async () => {
    const pending = deferred<CartDto>()
    let mutationSignal: AbortSignal | undefined
    vi.mocked(cartApi.updateItem).mockImplementation(
      (_itemId, _data, _token, signal) => {
        mutationSignal = signal
        return pending.promise
      }
    )
    const view = await renderAuthenticatedMutationProbe()
    fireEvent.click(screen.getByRole('button', { name: 'update-item-1' }))
    expect(await screen.findByTestId('locked-item-1')).toHaveTextContent('true')
    await waitFor(() => expect(mutationSignal).toBeDefined())

    useAuthMock.mockReturnValue(guestAuth)
    view.rerender(
      <CartProvider>
        <MutationProbe />
      </CartProvider>
    )
    expect(screen.getByTestId('locked-item-1')).toHaveTextContent('false')
    expect(screen.getByTestId('total')).toHaveTextContent('none')
    expect(mutationSignal?.aborted).toBe(true)
    pending.resolve(updatedCart)
    await waitFor(() => expect(screen.getByTestId('total')).toHaveTextContent('none'))
  })

  it('resets a global mutation flag on logout', async () => {
    const pending = deferred<CartDto>()
    vi.mocked(cartApi.addItem).mockReturnValue(pending.promise)
    const view = await renderAuthenticatedMutationProbe()
    fireEvent.click(screen.getByRole('button', { name: 'add-prod-1' }))
    expect(await screen.findByTestId('adding')).toHaveTextContent('true')

    useAuthMock.mockReturnValue(guestAuth)
    view.rerender(
      <CartProvider>
        <MutationProbe />
      </CartProvider>
    )
    expect(screen.getByTestId('adding')).toHaveTextContent('false')
    pending.resolve(updatedCart)
  })

  it('keeps item lock when multiple mutations are queued on the same item until all finish', async () => {
    const first = deferred<CartDto>()
    const second = deferred<CartDto>()
    vi.mocked(cartApi.updateItem)
      .mockReturnValueOnce(first.promise)
      .mockReturnValueOnce(second.promise)

    await renderAuthenticatedMutationProbe()
    fireEvent.click(screen.getByRole('button', { name: 'update-item-1' }))
    fireEvent.click(screen.getByRole('button', { name: 'update-item-1' }))

    expect(await screen.findByTestId('locked-item-1')).toHaveTextContent('true')

    // First mutation completes, but second is still pending
    first.resolve(cartWithItem1Updated)
    await waitFor(() => expect(cartApi.updateItem).toHaveBeenCalledTimes(2))
    expect(screen.getByTestId('locked-item-1')).toHaveTextContent('true')

    // Second mutation completes, now lock is released
    second.resolve(cartWithItem1Updated)
    await waitFor(() =>
      expect(screen.getByTestId('locked-item-1')).toHaveTextContent('false')
    )
  })

  it('keeps global adding flag when multiple add mutations are queued until all finish', async () => {
    const first = deferred<CartDto>()
    const second = deferred<CartDto>()
    vi.mocked(cartApi.addItem)
      .mockReturnValueOnce(first.promise)
      .mockReturnValueOnce(second.promise)

    await renderAuthenticatedMutationProbe()
    fireEvent.click(screen.getByRole('button', { name: 'add-prod-1' }))
    fireEvent.click(screen.getByRole('button', { name: 'add-prod-1' }))

    expect(await screen.findByTestId('adding')).toHaveTextContent('true')

    first.resolve(cartResponse)
    await waitFor(() => expect(cartApi.addItem).toHaveBeenCalledTimes(2))
    expect(screen.getByTestId('adding')).toHaveTextContent('true')

    second.resolve(cartResponse)
    await waitFor(() => expect(screen.getByTestId('adding')).toHaveTextContent('false'))
  })
})
