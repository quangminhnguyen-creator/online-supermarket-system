import { getJson, putJson } from './httpClient'
import type { OrderDetailDto, PaginatedOrdersDto } from './orderApi'
import type { BranchProductInventoryDto } from './branchApi'

export interface InventoryAdjustmentBody {
  productId: string
  quantityOnHand: number
  sellingPrice?: number
  reorderLevel?: number
  reason?: string
}

export interface UserSummaryDto {
  id: string
  email: string
  fullName: string
  phone: string | null
  role: string
  status: string
  createdAtUtc: string
  updatedAtUtc: string
}

// Note: the admin users endpoint returns { page, pageSize, totalCount, users },
// which is a different shape from the orders endpoint ({ data, ... }).
export interface PaginatedUsersDto {
  page: number
  pageSize: number
  totalCount: number
  users: UserSummaryDto[]
}

export interface GetAllOrdersParams {
  status?: string
  userId?: string
  page?: number
  pageSize?: number
}

function buildAdminOrdersQuery(params: GetAllOrdersParams) {
  const search = new URLSearchParams()
  search.set('page', String(params.page ?? 1))
  search.set('pageSize', String(params.pageSize ?? 10))
  if (params.status) search.set('status', params.status)
  if (params.userId) search.set('userId', params.userId)
  return search.toString()
}

export const adminApi = {
  // Users
  listUsers: (page: number, pageSize: number, token: string, signal?: AbortSignal) =>
    getJson<PaginatedUsersDto>(`/admin/users?page=${page}&pageSize=${pageSize}`, { token, signal }),
  updateUserStatus: (id: string, status: string, token: string, signal?: AbortSignal) =>
    putJson<{ message: string }>(`/admin/users/${encodeURIComponent(id)}/status`, { status }, { token, signal }),

  // Orders
  listOrders: (params: GetAllOrdersParams, token: string, signal?: AbortSignal) =>
    getJson<PaginatedOrdersDto>('/admin/orders?' + buildAdminOrdersQuery(params), { token, signal }),
  getOrderById: (id: string, token: string, signal?: AbortSignal) =>
    getJson<OrderDetailDto>('/admin/orders/' + encodeURIComponent(id), { token, signal }),
  updateOrderStatus: (
    id: string,
    body: { status: string; note?: string },
    token: string,
    signal?: AbortSignal,
  ) =>
    putJson<OrderDetailDto>('/admin/orders/' + encodeURIComponent(id) + '/status', body, { token, signal }),

  // Inventory & price (branch-scoped). Note: this only UPDATES an existing
  // (branch, product) inventory row; the backend returns 404 if none exists.
  adjustInventory: (
    branchId: string,
    body: InventoryAdjustmentBody,
    token: string,
    signal?: AbortSignal,
  ) =>
    putJson<BranchProductInventoryDto>(
      `/admin/branches/${encodeURIComponent(branchId)}/inventory`,
      body,
      { token, signal },
    ),
}
