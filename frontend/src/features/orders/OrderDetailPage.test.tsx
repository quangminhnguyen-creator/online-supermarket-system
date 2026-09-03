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

// Track the current auth state so tests can toggle it
let currentAuth: { isAuthenticated: boolean; isLoading: boolean; accessToken: string | null } = { isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' }

vi.mock('../auth/AuthContext', () => ({
  AuthProvider: ({ children }: PropsWithChildren) => <>{children}</>,
  useAuth: () => currentAuth,
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
  items: [{ orderItemId: '00000000-0000-0000-0000-000000000302', productId: '00000000-0000-0000-0000-000000000301', productName: 'Điện thoại A', sku: 'DT-A', unitPrice: 50000, quantity: 2, lineTotal: 100000, canReview: false, reviewId: null }],
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
    currentAuth = { isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' }
  })
  afterEach(() => {
    localStorage.clear()
    mockGetOrderById.mockReset()
  })

  it('asks guests to login before viewing order detail', () => {
    // Toggle auth state to unauthenticated via the shared closure variable
    currentAuth = { isAuthenticated: false, isLoading: false, accessToken: null }
    mockGetOrderById.mockResolvedValue(orderDetail)
    renderDetail('/orders/history/00000000-0000-0000-0000-000000000001')
    // Component immediately shows auth required when not authenticated (no API call needed)
    expect(screen.getByRole('heading', { name: 'Đăng nhập để xem đơn hàng' })).toBeInTheDocument()
  })

  it('exposes accessible order detail sections', async () => {
    mockGetOrderById.mockResolvedValue(orderDetail)
    renderDetail('/orders/history/00000000-0000-0000-0000-000000000001')
    expect(await screen.findByRole('region', { name: 'Thông tin giao hàng' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Sản phẩm trong đơn' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Lịch sử trạng thái' })).toBeInTheDocument()
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
    expect(screen.getByRole('link', { name: 'Quay lại lịch sử đơn hàng' })).toBeInTheDocument()
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

  it('shows review action only for eligible items in completed orders', async () => {
    mockGetOrderById.mockResolvedValue({
      ...orderDetail,
      status: 'Completed',
      items: [
        {
          orderItemId: 'oi-1',
          productId: 'p-1',
          productName: 'Sản phẩm A',
          sku: 'SP-A',
          unitPrice: 50000,
          quantity: 1,
          lineTotal: 50000,
          canReview: true,
          reviewId: null,
        },
        {
          orderItemId: 'oi-2',
          productId: 'p-2',
          productName: 'Sản phẩm B',
          sku: 'SP-B',
          unitPrice: 65000,
          quantity: 1,
          lineTotal: 65000,
          canReview: false,
          reviewId: 'r-2',
        },
        {
          orderItemId: 'oi-3',
          productId: 'p-3',
          productName: 'Sản phẩm C',
          sku: 'SP-C',
          unitPrice: 20000,
          quantity: 1,
          lineTotal: 20000,
          canReview: false,
          reviewId: null,
        },
      ],
    })

    renderDetail()

    expect(await screen.findByRole('link', { name: 'Viết đánh giá Sản phẩm A' }))
      .toHaveAttribute('href', '/product/p-1?reviewOrderItemId=oi-1#reviews')
    expect(screen.getByRole('link', { name: 'Sửa đánh giá Sản phẩm B' }))
      .toHaveAttribute('href', '/product/p-2?reviewId=r-2#reviews')
    expect(screen.queryByRole('link', { name: /Sản phẩm C/i })).not.toBeInTheDocument()
  })
})
