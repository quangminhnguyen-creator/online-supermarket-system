import { deleteJson, getJson, postJson, putJson } from './httpClient'

export interface CartItemDto {
  id: string
  productId: string
  productName: string
  sku: string
  unitPrice: number
  quantity: number
  lineTotal: number
  availableQuantity: number
}

export interface CartDto {
  id: string
  userId: string
  branchId: string
  items: CartItemDto[]
  totalItems: number
  subtotal: number
}

export interface AddCartItemRequest {
  productId: string
  quantity: number
}

export interface UpdateCartItemRequest {
  quantity: number
}

export interface ChangeCartBranchRequest {
  branchId: string
}

export const cartApi = {
  getCart: (token: string, signal?: AbortSignal) =>
    getJson<CartDto>('/cart', { token, signal }),
  addItem: (data: AddCartItemRequest, token: string, signal?: AbortSignal) =>
    postJson<CartDto>('/cart/items', data, { token, signal }),
  updateItem: (itemId: string, data: UpdateCartItemRequest, token: string, signal?: AbortSignal) =>
    putJson<CartDto>('/cart/items/' + encodeURIComponent(itemId), data, { token, signal }),
  removeItem: (itemId: string, token: string, signal?: AbortSignal) =>
    deleteJson<CartDto>('/cart/items/' + encodeURIComponent(itemId), { token, signal }),
  changeBranch: (data: ChangeCartBranchRequest, token: string, signal?: AbortSignal) =>
    postJson<CartDto>('/cart/change-branch', data, { token, signal }),
  clearCart: async (token: string, signal?: AbortSignal): Promise<void> => {
    await deleteJson<Record<string, never>>('/cart', { token, signal })
  },
}
