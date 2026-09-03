import { getJson } from './httpClient'

export interface InventoryTransactionDto {
  id: string
  branchInventoryId: string
  transactionType: string
  quantityOnHandDelta: number
  reservedQuantityDelta: number
  quantityOnHandAfter: number
  reservedQuantityAfter: number
  referenceType: string
  referenceId: string | null
  actorUserId: string | null
  note: string | null
  createdAtUtc: string
}

export interface PaginatedInventoryTransactionsDto {
  data: InventoryTransactionDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const inventoryIntelligenceApi = {
  getTransactions: (
    inventoryId: string,
    params: { page: number; pageSize: number },
    token?: string,
    signal?: AbortSignal,
  ) =>
    getJson<PaginatedInventoryTransactionsDto>(
      `/admin/inventory/${encodeURIComponent(inventoryId)}/transactions` +
        `?page=${params.page}&pageSize=${params.pageSize}`,
      { token, signal },
    ),
}