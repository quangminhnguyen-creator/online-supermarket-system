import { describe, it, expect, vi, afterEach } from 'vitest'
import { cartApi, type CartDto } from './cartApi'

function jsonResponse(data: unknown) {
  return new Response(JSON.stringify(data), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  })
}

const token = 'jwt-token'
const cartResponse: CartDto = {
  id: 'cart-1',
  userId: 'user-1',
  branchId: 'branch-1',
  items: [],
  totalItems: 0,
  subtotal: 0,
}

describe('cartApi', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it('adds an item with bearer token and exact body', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(cartResponse))
    vi.stubGlobal('fetch', fetchMock)

    const result = await cartApi.addItem({ productId: 'prod-1', quantity: 2 }, token)

    expect(fetchMock).toHaveBeenCalledWith('/api/cart/items', expect.objectContaining({
      method: 'POST',
      headers: expect.objectContaining({
        Authorization: 'Bearer jwt-token',
        'Content-Type': 'application/json',
        Accept: 'application/json',
      }),
      body: JSON.stringify({ productId: 'prod-1', quantity: 2 }),
    }))
    expect(result).toEqual(cartResponse)
  })

  it('loads the current cart with GET', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(cartResponse))
    vi.stubGlobal('fetch', fetchMock)

    const result = await cartApi.getCart(token)

    expect(fetchMock).toHaveBeenCalledWith('/api/cart', expect.objectContaining({
      headers: expect.objectContaining({
        Authorization: 'Bearer jwt-token',
        Accept: 'application/json',
      }),
    }))
    expect(result).toEqual(cartResponse)
  })

  it('removes an item with DELETE', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(cartResponse))
    vi.stubGlobal('fetch', fetchMock)

    const result = await cartApi.removeItem('item-1', token)

    expect(fetchMock).toHaveBeenCalledWith('/api/cart/items/item-1', expect.objectContaining({
      method: 'DELETE',
      headers: expect.objectContaining({
        Authorization: 'Bearer jwt-token',
        Accept: 'application/json',
      }),
    }))
    expect(result).toEqual(cartResponse)
  })

  it('updates quantity with PUT', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(cartResponse))
    vi.stubGlobal('fetch', fetchMock)

    const result = await cartApi.updateItem('item-1', { quantity: 3 }, token)

    expect(fetchMock).toHaveBeenCalledWith('/api/cart/items/item-1', expect.objectContaining({
      method: 'PUT',
      headers: expect.objectContaining({
        Authorization: 'Bearer jwt-token',
        'Content-Type': 'application/json',
        Accept: 'application/json',
      }),
      body: JSON.stringify({ quantity: 3 }),
    }))
    expect(result).toEqual(cartResponse)
  })

  it('changes branch with POST', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(cartResponse))
    vi.stubGlobal('fetch', fetchMock)

    const result = await cartApi.changeBranch({ branchId: 'branch-2' }, token)

    expect(fetchMock).toHaveBeenCalledWith('/api/cart/change-branch', expect.objectContaining({
      method: 'POST',
      headers: expect.objectContaining({
        Authorization: 'Bearer jwt-token',
        'Content-Type': 'application/json',
        Accept: 'application/json',
      }),
      body: JSON.stringify({ branchId: 'branch-2' }),
    }))
    expect(result).toEqual(cartResponse)
  })

  it('accepts empty 204 when clearing cart', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(cartApi.clearCart(token)).resolves.toBeUndefined()
    expect(fetchMock).toHaveBeenCalledWith('/api/cart', expect.objectContaining({
      method: 'DELETE',
      headers: expect.objectContaining({
        Authorization: 'Bearer jwt-token',
        Accept: 'application/json',
      }),
    }))
  })
})
