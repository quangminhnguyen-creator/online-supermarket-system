import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { PropsWithChildren } from 'react'
import { OrderHistoryPage } from './OrderHistoryPage'
import { orderApi } from '../../api/orderApi'

// Must be hoisted before vi.mock so the factory can reference it
const mockAuth = { isAuthenticated: false, isLoading: false, accessToken: null }

vi.mock('../auth/AuthContext', () => ({
  AuthProvider: ({ children }: PropsWithChildren) => <>{children}</>,
  useAuth: () => mockAuth,
}))

function renderHistory() {
  return render(
    <MemoryRouter>
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
    renderHistory()
    expect(screen.getByRole('heading', { name: 'Đăng nhập để xem đơn hàng' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Đăng nhập' })).toBeInTheDocument()
  })

  it('renders the history heading for authenticated users', async () => {
    mockAuth.isAuthenticated = true
    mockAuth.isLoading = false
    mockAuth.accessToken = 'jwt-token'
    vi.spyOn(orderApi, 'getOrders').mockResolvedValue({ data: [], totalCount: 0, page: 1, pageSize: 10 })
    renderHistory()
    expect(await screen.findByRole('heading', { level: 1, name: 'Lịch sử đơn hàng' })).toBeInTheDocument()
    expect(screen.getByText('Bạn chưa có đơn hàng nào.')).toBeInTheDocument()
  })
})
