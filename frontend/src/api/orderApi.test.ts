import { afterEach, expect, it, vi } from 'vitest'
import { orderApi, type PaginatedOrdersDto } from './orderApi'

function jsonResponse(data: unknown) {
  return new Response(JSON.stringify(data), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => vi.unstubAllGlobals())

const token = 'jwt-token'
const listResponse: PaginatedOrdersDto = {
  data: [{
    id: '00000000-0000-0000-0000-000000000001',
    createdAtUtc: '2026-08-28T01:00:00Z',
    totalAmount: 115000,
    status: 'Confirmed',
    fulfillmentType: 'Delivery',
    itemCount: 2,
  }],
  totalCount: 1,
  page: 1,
  pageSize: 10,
}

const orderDetail = {
  id: '00000000-0000-0000-0000-000000000001',
  userId: '00000000-0000-0000-0000-000000000101',
  branchId: '00000000-0000-0000-0000-000000000201',
  fulfillmentType: 'Delivery',
  recipientName: 'Nguyen Van A',
  recipientPhone: '0900000000',
  deliveryAddressSnapshot: 'Nguyen Van A, 0900000000, 12 Nguyen Trai',
  subtotal: 100000,
  discountAmount: 0,
  shippingFee: 15000,
  totalAmount: 115000,
  promotionCodeSnapshot: null,
  status: 'Confirmed',
  createdAtUtc: '2026-08-28T01:00:00Z',
  updatedAtUtc: '2026-08-28T01:05:00Z',
  items: [{ productId: '00000000-0000-0000-0000-000000000301', productName: 'Điện thoại A', sku: 'DT-A', unitPrice: 50000, quantity: 2, lineTotal: 100000 }],
  statusHistory: [{ fromStatus: 'Pending', toStatus: 'Confirmed', note: 'Payment initiated: COD', createdAtUtc: '2026-08-28T01:05:00Z' }],
  payment: { id: '00000000-0000-0000-0000-000000000401', method: 'COD', status: 'PendingCollection', amount: 115000, providerTransactionId: null, createdAtUtc: '2026-08-28T01:05:00Z' },
}

it('loads paginated orders with bearer token and default params', async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(listResponse))
  vi.stubGlobal('fetch', fetchMock)

  await orderApi.getOrders({ page: 1, pageSize: 10 }, token)

  expect(fetchMock).toHaveBeenCalledWith('/api/orders?page=1&pageSize=10', expect.objectContaining({
    headers: expect.objectContaining({ Authorization: 'Bearer jwt-token' }),
  }))
})

it('loads an order detail by id', async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(orderDetail))
  vi.stubGlobal('fetch', fetchMock)

  await orderApi.getOrderById('00000000-0000-0000-0000-000000000001', token)

  expect(fetchMock).toHaveBeenCalledWith('/api/orders/00000000-0000-0000-0000-000000000001', expect.objectContaining({
    headers: expect.objectContaining({ Authorization: 'Bearer jwt-token' }),
  }))
})

it('adds status filter when provided', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(listResponse)))
  await orderApi.getOrders({ status: 'Confirmed', page: 2, pageSize: 5 }, token)
  expect(fetch).toHaveBeenCalledWith('/api/orders?page=2&pageSize=5&status=Confirmed', expect.any(Object))
})

it('encodes order id for detail route', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(orderDetail)))
  await orderApi.getOrderById('order 1', token)
  expect(fetch).toHaveBeenCalledWith('/api/orders/order%201', expect.any(Object))
})
