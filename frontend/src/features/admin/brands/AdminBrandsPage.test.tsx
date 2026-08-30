import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { expect, it, vi, beforeEach } from 'vitest'
import { AdminBrandsPage } from './AdminBrandsPage'
import { adminCatalogApi } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'
import { ApiError } from '../../../api/httpClient'

vi.mock('../../../api/adminCatalogApi')
vi.mock('../../auth/AuthContext')

beforeEach(() => {
  vi.mocked(useAuth).mockReturnValue({ accessToken: 'test-token' } as any)
})

it('loads and displays brands', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([
    { id: '1', name: 'Brand 1', slug: 'brand-1', isActive: true },
  ])

  render(<AdminBrandsPage />)
  
  expect(screen.getByText('Loading...')).toBeInTheDocument()
  
  await waitFor(() => {
    expect(screen.getByText(/Brand 1/)).toBeInTheDocument()
  })
})

it('creates a brand', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([])
  vi.mocked(adminCatalogApi.createBrand).mockResolvedValue({
    id: '3', name: 'New Brand', slug: 'new-brand', isActive: true
  })

  render(<AdminBrandsPage />)
  
  await waitFor(() => {
    expect(screen.queryByText('Loading...')).not.toBeInTheDocument()
  })
  
  fireEvent.change(screen.getByLabelText(/Tên/i), { target: { value: 'New Brand' } })
  fireEvent.change(screen.getByLabelText(/Slug/i), { target: { value: 'new-brand' } })
  fireEvent.click(screen.getByRole('button', { name: /Thêm/i }))
  
  await waitFor(() => {
    expect(adminCatalogApi.createBrand).toHaveBeenCalledWith(
      { name: 'New Brand', slug: 'new-brand' }
    )
  })
})

it('displays error on create failure', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([])
  vi.mocked(adminCatalogApi.createBrand).mockRejectedValue(new ApiError(409, { message: 'Slug exists' }))

  render(<AdminBrandsPage />)
  
  await waitFor(() => {
    expect(screen.queryByText('Loading...')).not.toBeInTheDocument()
  })
  
  fireEvent.change(screen.getByLabelText(/Tên/i), { target: { value: 'New Brand' } })
  fireEvent.change(screen.getByLabelText(/Slug/i), { target: { value: 'new-brand' } })
  fireEvent.click(screen.getByRole('button', { name: /Thêm/i }))
  
  await waitFor(() => {
    expect(screen.getByRole('alert')).toHaveTextContent('Slug exists')
  })
})
