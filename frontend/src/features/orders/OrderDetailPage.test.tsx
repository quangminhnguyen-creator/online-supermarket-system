import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { PropsWithChildren } from 'react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { render, screen } from '@testing-library/react'
import { OrderDetailPage } from './OrderDetailPage'
import { ApiError } from '../../api/httpClient'

const mockGetOrderById = vi.hoisted(vi.fn)

vi.mock('../../api/orderApi', () => ({
  orderApi: {
    getOrderById: mockGetOrderById,
    getOrders: vi.fn(),
  },
  getOrderById: mockGetOrderById,
}))

vi.mock('../auth/AuthContext', () => ({
  AuthProvider: ({ children }: PropsWithChildren) => <>{children}</>,
  useAuth: () => ({ isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' }),
}))

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

function renderDetail(url = '/orders/history/00000000-0000-0000-0000-000000000001') {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <Routes>
        <Route path="/orders/history/:id" element={<OrderDetailPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('OrderDetailPage', () => {
  beforeEach(() => {
    localStorage.setItem('os_access_token', 'jwt-token')
  })
  afterEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it('renders order detail, items, totals, payment and timeline', async () => {
    mockGetOrderById.mockResolvedValue(orderDetail)
    renderDetail()
    expect(await screen.findByRole('heading', { name: 'Chi tiết đơn hàng 00000000-0000-0000-0000-000000000001' })).toBeInTheDocument()
    expect(screen.getByText('Điện thoại A')).toBeInTheDocument()
    expect(screen.getByText('Nguyen Van A')).toBeInTheDocument()
    expect(screen.getByText('PendingCollection')).toBeInTheDocument()
    expect(screen.getByText('Payment initiated: COD')).toBeInTheDocument()
    expect(screen.getByText('115.000 ₫')).toBeInTheDocument()
  })

  it('renders not found when backend returns 404', async () => {
    mockGetOrderById.mockRejectedValue(new ApiError(404, { message: 'Order not found.' }))
    renderDetail('/orders/history/00000000-0000-0000-0000-000000009999')
    expect(await screen.findByRole('heading', { name: 'Không tìm thấy đơn hàng' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Quay lại lịch sử đơn hàng' })).toHaveAttribute('href', '/orders/history')
  })

  it('retries a generic detail load failure', async () => {
    mockGetOrderById
      .mockRejectedValueOnce(new Error('Server error'))
      .mockResolvedValueOnce(orderDetail)
    renderDetail('/orders/history/00000000-0000-0000-0000-000000000001')
    expect(await screen.findByRole('button', { name: 'Thử lại' })).toBeInTheDocument()
    screen.getByRole('button', { name: 'Thử lại' }).click()
    expect(await screen.findByRole('heading', { name: 'Chi tiết đơn hàng 00000000-0000-0000-0000-000000000001' })).toBeInTheDocument()
    expect(mockGetOrderById.mock.calls.length).toBeGreaterThanOrEqual(2)
  })
})
