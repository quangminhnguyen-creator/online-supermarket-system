import { afterEach, expect, it, vi } from 'vitest'
import { inventoryIntelligenceApi, type PaginatedInventoryTransactionsDto } from './inventoryIntelligenceApi'

function jsonResponse(data: unknown) {
  return new Response(JSON.stringify(data), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => vi.unstubAllGlobals())

const token = 'jwt-token'
const response: PaginatedInventoryTransactionsDto = {
  data: [{
    id: '00000000-0000-0000-0000-000000000001',
    branchInventoryId: '00000000-0000-0000-0000-000000000101',
    transactionType: 'Sale',
    quantityOnHandDelta: -3,
    reservedQuantityDelta: -3,
    quantityOnHandAfter: 97,
    reservedQuantityAfter: 0,
    referenceType: 'Order',
    referenceId: '00000000-0000-0000-0000-000000000201',
    actorUserId: null,
    note: null,
    createdAtUtc: '2026-09-03T01:00:00Z',
  }],
  totalCount: 1,
  page: 1,
  pageSize: 20,
}

it('loads transactions with bearer token and page params', async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(response))
  vi.stubGlobal('fetch', fetchMock)

  await inventoryIntelligenceApi.getTransactions(
    '00000000-0000-0000-0000-000000000101',
    { page: 2, pageSize: 10 },
    token,
  )

  expect(fetchMock).toHaveBeenCalledWith(
    '/api/admin/inventory/00000000-0000-0000-0000-000000000101/transactions?page=2&pageSize=10',
    expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer jwt-token' }),
    }),
  )
})

it('encodes inventory id for the ledger route', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(response)))

  await inventoryIntelligenceApi.getTransactions('inv 1', { page: 1, pageSize: 20 }, token)

  expect(fetch).toHaveBeenCalledWith('/api/admin/inventory/inv%201/transactions?page=1&pageSize=20', expect.any(Object))
})