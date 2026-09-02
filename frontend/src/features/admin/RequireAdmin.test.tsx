import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { RequireAdmin } from './RequireAdmin'

const mockAuth: { user: { role: string } | null; isAuthenticated: boolean; isLoading: boolean } = {
  user: null,
  isAuthenticated: false,
  isLoading: false,
}

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuth,
}))

function renderGuard() {
  return render(
    <MemoryRouter>
      <RequireAdmin>
        <div>NỘI DUNG QUẢN TRỊ</div>
      </RequireAdmin>
    </MemoryRouter>,
  )
}

describe('RequireAdmin', () => {
  afterEach(() => {
    mockAuth.user = null
    mockAuth.isAuthenticated = false
    mockAuth.isLoading = false
  })

  it('shows a loading state while auth is resolving', () => {
    mockAuth.isLoading = true
    renderGuard()
    expect(screen.getByText('Đang kiểm tra quyền truy cập...')).toBeInTheDocument()
    expect(screen.queryByText('NỘI DUNG QUẢN TRỊ')).not.toBeInTheDocument()
  })

  it('blocks guests with a login prompt', () => {
    mockAuth.isAuthenticated = false
    mockAuth.user = null
    renderGuard()
    expect(screen.getByRole('heading', { name: 'Yêu cầu đăng nhập' })).toBeInTheDocument()
    expect(screen.queryByText('NỘI DUNG QUẢN TRỊ')).not.toBeInTheDocument()
  })

  it('blocks authenticated non-admin users', () => {
    mockAuth.isAuthenticated = true
    mockAuth.user = { role: 'Customer' }
    renderGuard()
    expect(screen.getByRole('heading', { name: 'Không có quyền truy cập' })).toBeInTheDocument()
    expect(screen.queryByText('NỘI DUNG QUẢN TRỊ')).not.toBeInTheDocument()
  })

  it('renders children for admins', () => {
    mockAuth.isAuthenticated = true
    mockAuth.user = { role: 'Admin' }
    renderGuard()
    expect(screen.getByText('NỘI DUNG QUẢN TRỊ')).toBeInTheDocument()
  })
})
