import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminOrderDetailPage } from './AdminOrderDetailPage'
import { adminApi } from '../../api/adminApi'
import { ApiError } from '../../api/httpClient'
import type { OrderDetailDto } from '../../api/orderApi'

const mockAuth = { accessToken: 'jwt-token' as string | null }

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuth,
}))

const baseOrder: OrderDetailDto = {
  id: 'ORDER-1',
  userId: 'USER-1',
  branchId: 'BRANCH-1',
  fulfillmentType: 'Delivery',
  recipientName: 'Nguyễn Văn A',
  recipientPhone: '0900000000',
  deliveryAddressSnapshot: '123 Đường Lê Lợi, Quận 1',
  subtotal: 100000,
  discountAmount: 0,
  shippingFee: 15000,
  totalAmount: 115000,
  promotionCodeSnapshot: null,
  status: 'Pending',
  createdAtUtc: '2026-08-28T01:00:00Z',
  updatedAtUtc: '2026-08-28T01:00:00Z',
  items: [
    { productId: 'P1', productName: 'iPhone 15', sku: 'SKU-1', unitPrice: 100000, quantity: 1, lineTotal: 100000 },
  ],
  statusHistory: [
    { fromStatus: 'Pending', toStatus: 'Pending', note: 'Order created', createdAtUtc: '2026-08-28T01:00:00Z' },
  ],
  payment: null,
}

function renderDetail() {
  return render(
    <MemoryRouter initialEntries={['/admin/orders/ORDER-1']}>
      <Routes>
        <Route path="/admin/orders/:id" element={<AdminOrderDetailPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('AdminOrderDetailPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockAuth.accessToken = 'jwt-token'
  })

  it('renders order detail from the admin API', async () => {
    vi.spyOn(adminApi, 'getOrderById').mockResolvedValue(baseOrder)
    renderDetail()
    expect(await screen.findByText('Nguyễn Văn A')).toBeInTheDocument()
    expect(screen.getByText('Tổng cộng: 115.000 ₫')).toBeInTheDocument()
    expect(screen.getByText('iPhone 15')).toBeInTheDocument()
    expect(adminApi.getOrderById).toHaveBeenCalledWith('ORDER-1', 'jwt-token', expect.any(AbortSignal))
  })

  it('only offers valid next statuses for the current state', async () => {
    vi.spyOn(adminApi, 'getOrderById').mockResolvedValue(baseOrder)
    renderDetail()
    await screen.findByLabelText('Trạng thái mới')
    // Pending -> { Confirmed, Cancelled }
    expect(screen.getByRole('option', { name: 'Đã xác nhận' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Đã hủy' })).toBeInTheDocument()
    // Not a valid transition from Pending
    expect(screen.queryByRole('option', { name: 'Đã giao hàng' })).not.toBeInTheDocument()
  })

  it('warns before cancelling an order', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'getOrderById').mockResolvedValue(baseOrder)
    renderDetail()
    await user.selectOptions(await screen.findByLabelText('Trạng thái mới'), 'Cancelled')
    expect(screen.getByText(/Hủy đơn sẽ tự động hoàn trả tồn kho/)).toBeInTheDocument()
  })

  it('submits a status update and reflects the new status', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'getOrderById').mockResolvedValue(baseOrder)
    const updateSpy = vi.spyOn(adminApi, 'updateOrderStatus').mockResolvedValue({ ...baseOrder, status: 'Confirmed' })
    renderDetail()
    await user.selectOptions(await screen.findByLabelText('Trạng thái mới'), 'Confirmed')
    await user.click(screen.getByRole('button', { name: 'Cập nhật trạng thái' }))
    expect(updateSpy).toHaveBeenCalledWith('ORDER-1', { status: 'Confirmed', note: undefined }, 'jwt-token')
    expect(await screen.findByRole('status')).toHaveTextContent('Confirmed')
  })

  it('shows the backend message when a transition is rejected', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'getOrderById').mockResolvedValue(baseOrder)
    vi.spyOn(adminApi, 'updateOrderStatus').mockRejectedValue(
      new ApiError(400, { message: 'Invalid transition from Pending to Delivered.' }, 'Invalid transition from Pending to Delivered.'),
    )
    renderDetail()
    await user.selectOptions(await screen.findByLabelText('Trạng thái mới'), 'Confirmed')
    await user.click(screen.getByRole('button', { name: 'Cập nhật trạng thái' }))
    expect(await screen.findByText('Invalid transition from Pending to Delivered.')).toBeInTheDocument()
  })

  it('shows a not-found state for a missing order', async () => {
    vi.spyOn(adminApi, 'getOrderById').mockRejectedValue(new ApiError(404, { message: 'Order not found.' }))
    renderDetail()
    expect(await screen.findByRole('heading', { name: 'Không tìm thấy đơn hàng' })).toBeInTheDocument()
  })
})
