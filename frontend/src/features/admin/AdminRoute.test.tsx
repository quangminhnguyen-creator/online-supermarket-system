import { render, screen } from '@testing-library/react'
import { expect, it, vi } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AdminRoute } from './AdminRoute'
import { useAuth } from '../auth/AuthContext'

vi.mock('../auth/AuthContext', () => ({
  useAuth: vi.fn(),
}))

const renderRoute = () => {
  render(
    <MemoryRouter initialEntries={['/admin/catalog/categories']}>
      <Routes>
        <Route path="/" element={<h1>Home Page</h1>} />
        <Route element={<AdminRoute />}>
          <Route path="/admin/catalog/categories" element={<h1>Admin Content</h1>} />
        </Route>
      </Routes>
    </MemoryRouter>
  )
}

it('shows loading when auth is loading', () => {
  vi.mocked(useAuth).mockReturnValue({ isLoading: true } as any)
  renderRoute()
  expect(screen.getByText(/đang tải/i)).toBeInTheDocument()
})

it('redirects to home if guest', () => {
  vi.mocked(useAuth).mockReturnValue({ isLoading: false, user: null } as any)
  renderRoute()
  expect(screen.getByRole('heading', { name: 'Home Page' })).toBeInTheDocument()
})

it('redirects to home if Customer', () => {
  vi.mocked(useAuth).mockReturnValue({ isLoading: false, user: { role: 'Customer' } } as any)
  renderRoute()
  expect(screen.getByRole('heading', { name: 'Home Page' })).toBeInTheDocument()
})

it('renders children if Admin', () => {
  vi.mocked(useAuth).mockReturnValue({ isLoading: false, user: { role: 'Admin' } } as any)
  renderRoute()
  expect(screen.getByRole('heading', { name: 'Admin Content' })).toBeInTheDocument()
})
