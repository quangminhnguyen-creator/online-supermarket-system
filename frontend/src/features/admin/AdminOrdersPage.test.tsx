import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminOrdersPage } from './AdminOrdersPage'
import { adminApi } from '../../api/adminApi'
import { ApiError } from '../../api/httpClient'

const mockAuth = { accessToken: 'jwt-token' as string | null }

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuth,
}))

const ordersResponse = {
  data: [
    {
      id: '00000000-0000-0000-0000-000000000001',
      createdAtUtc: '2026-08-28T01:00:00Z',
      totalAmount: 115000,
      status: 'Confirmed',
      fulfillmentType: 'Delivery',
      itemCount: 2,
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 10,
}

function renderPage(url = '/admin/orders') {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <AdminOrdersPage />
    </MemoryRouter>,
  )
}

describe('AdminOrdersPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockAuth.accessToken = 'jwt-token'
  })

  it('renders orders returned by the admin API', async () => {
    vi.spyOn(adminApi, 'listOrders').mockResolvedValue(ordersResponse)
    renderPage()
    expect(await screen.findByRole('heading', { level: 1, name: 'Quản lý đơn hàng' })).toBeInTheDocument()
    expect(await screen.findByRole('table', { name: 'Danh sách đơn hàng' })).toBeInTheDocument()
    expect(screen.getByText('115.000 ₫')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Confirmed')
    expect(screen.getByRole('link', { name: 'Xem chi tiết đơn 00000000-0000-0000-0000-000000000001' }))
      .toHaveAttribute('href', '/admin/orders/00000000-0000-0000-0000-000000000001')
  })

  it('shows an empty state when there are no orders', async () => {
    vi.spyOn(adminApi, 'listOrders').mockResolvedValue({ data: [], totalCount: 0, page: 1, pageSize: 10 })
    renderPage()
    expect(await screen.findByText('Không có đơn hàng nào khớp bộ lọc.')).toBeInTheDocument()
  })

  it('filters by status and resets to page 1', async () => {
    const user = userEvent.setup()
    const listOrders = vi.spyOn(adminApi, 'listOrders').mockResolvedValue({ ...ordersResponse, totalCount: 25 })
    renderPage('/admin/orders?page=3')
    await user.selectOptions(await screen.findByLabelText('Lọc trạng thái'), 'Confirmed')
    expect(listOrders).toHaveBeenLastCalledWith(
      { page: 1, pageSize: 10, status: 'Confirmed', userId: undefined },
      'jwt-token',
      expect.any(AbortSignal),
    )
    expect(await screen.findByText('Trang 1')).toBeInTheDocument()
  })

  it('moves to the next page through URL state', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'listOrders').mockResolvedValue({ ...ordersResponse, totalCount: 25 })
    renderPage()
    await user.click(await screen.findByRole('button', { name: 'Trang sau' }))
    expect(await screen.findByText('Trang 2')).toBeInTheDocument()
  })

  it('retries after a load failure', async () => {
    const spy = vi.spyOn(adminApi, 'listOrders')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce({ data: [], totalCount: 0, page: 1, pageSize: 10 })
    renderPage()
    await userEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByText('Không có đơn hàng nào khớp bộ lọc.')).toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(2)
  })
})
