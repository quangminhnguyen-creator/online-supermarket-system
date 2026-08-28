import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { PropsWithChildren } from 'react'
import { OrderHistoryPage } from './OrderHistoryPage'
import { orderApi } from '../../api/orderApi'
import { ApiError } from '../../api/httpClient'

// Must be hoisted before vi.mock so the factory can reference it
const mockAuth: { isAuthenticated: boolean; isLoading: boolean; accessToken: string | null } = { isAuthenticated: false, isLoading: false, accessToken: null }

vi.mock('../auth/AuthContext', () => ({
  AuthProvider: ({ children }: PropsWithChildren) => <>{children}</>,
  useAuth: () => mockAuth,
}))

const orderListResponse = {
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

interface AuthParam {
  isAuthenticated?: boolean
  isLoading?: boolean
  accessToken?: string | null
  auth?: {
    isAuthenticated?: boolean
    isLoading?: boolean
    accessToken?: string | null
  }
}

function renderHistory(
  authParam: AuthParam = { isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' },
  url = '/orders/history'
) {
  const auth = authParam.auth ?? authParam
  mockAuth.isAuthenticated = auth.isAuthenticated ?? true
  mockAuth.isLoading = auth.isLoading ?? false
  mockAuth.accessToken = auth.accessToken !== undefined ? auth.accessToken : 'jwt-token'
  if (!vi.isMockFunction(orderApi.getOrders)) {
    vi.spyOn(orderApi, 'getOrders').mockResolvedValue({ data: [], totalCount: 0, page: 1, pageSize: 10 })
  }
  return render(
    <MemoryRouter initialEntries={[url]}>
      <OrderHistoryPage />
    </MemoryRouter>
  )
}

describe('OrderHistoryPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockAuth.isAuthenticated = false
    mockAuth.isLoading = false
    mockAuth.accessToken = null
  })

  it('asks guests to login before viewing order history', () => {
    renderHistory({ isAuthenticated: false, isLoading: false, accessToken: null }, '/orders/history')
    expect(screen.getByRole('heading', { name: 'Đăng nhập để xem đơn hàng' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Đăng nhập' })).toBeInTheDocument()
  })

  it('renders the history heading for authenticated users', async () => {
    mockAuth.isAuthenticated = true
    mockAuth.isLoading = false
    mockAuth.accessToken = 'jwt-token'
    vi.spyOn(orderApi, 'getOrders').mockResolvedValue({ data: [], totalCount: 0, page: 1, pageSize: 10 })
    renderHistory({ isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' })
    expect(await screen.findByRole('heading', { level: 1, name: 'Lịch sử đơn hàng' })).toBeInTheDocument()
    expect(await screen.findByText('Bạn chưa có đơn hàng nào.')).toBeInTheDocument()
  })

  it('renders order cards from backend data', async () => {
    vi.spyOn(orderApi, 'getOrders').mockResolvedValue(orderListResponse)
    renderHistory({ isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' })
    expect(await screen.findByText('Mã đơn: 00000000-0000-0000-0000-000000000001')).toBeInTheDocument()
    expect(screen.getByText('115.000 ₫')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Confirmed')
    expect(screen.getByRole('link', { name: 'Xem chi tiết 00000000-0000-0000-0000-000000000001' })).toHaveAttribute('href', '/orders/history/00000000-0000-0000-0000-000000000001')
  })

  it('loads filtered orders and resets to page 1 when status changes', async () => {
    const user = userEvent.setup()
    const getOrders = vi.spyOn(orderApi, 'getOrders').mockResolvedValue({ ...orderListResponse, totalCount: 25, page: 1 })
    renderHistory({ isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' }, '/orders/history?page=3')

    await user.selectOptions(await screen.findByLabelText('Lọc trạng thái'), 'Confirmed')

    expect(getOrders).toHaveBeenLastCalledWith({ page: 1, pageSize: 10, status: 'Confirmed' }, 'jwt-token', expect.any(AbortSignal))
    expect(await screen.findByRole('combobox', { name: 'Lọc trạng thái' })).toHaveValue('Confirmed')
    expect(await screen.findByText('Trang 1')).toBeInTheDocument()
  })

  it('moves to the next page through URL state', async () => {
    const user = userEvent.setup()
    vi.spyOn(orderApi, 'getOrders').mockResolvedValue({ ...orderListResponse, totalCount: 25, page: 1 })
    renderHistory({ isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' }, '/orders/history')
    await user.click(await screen.findByRole('button', { name: 'Trang sau' }))
    expect(await screen.findByText('Trang 2')).toBeInTheDocument()
  })

  it('maps backend statuses to Vietnamese labels without hiding raw status from tests', async () => {
    vi.spyOn(orderApi, 'getOrders').mockResolvedValue(orderListResponse)
    renderHistory({ isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' })
    expect(await screen.findByText('Đã xác nhận')).toBeInTheDocument()
    expect(screen.getByText('Confirmed', { selector: '.sr-only' })).toBeInTheDocument()
  })

  it('exposes accessible order history controls', async () => {
    vi.spyOn(orderApi, 'getOrders').mockResolvedValue(orderListResponse)
    renderHistory({ auth: { isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' } })
    expect(await screen.findByRole('main', { name: 'Lịch sử đơn hàng' })).toBeInTheDocument()
    expect(screen.getByLabelText('Lọc trạng thái')).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Phân trang đơn hàng' })).toBeInTheDocument()
  })

  it('retries list load failure', async () => {
    const spy = vi.spyOn(orderApi, 'getOrders')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce({ data: [], totalCount: 0, page: 1, pageSize: 10 })
    renderHistory({ isAuthenticated: true, isLoading: false, accessToken: 'jwt-token' })
    await userEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByText('Bạn chưa có đơn hàng nào.')).toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(2)
  })
})
