import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { expect, it, vi, beforeEach } from 'vitest'
import { AdminCategoriesPage } from './AdminCategoriesPage'
import { adminCatalogApi } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'
import { ApiError } from '../../../api/httpClient'

vi.mock('../../../api/adminCatalogApi')
vi.mock('../../auth/AuthContext')

beforeEach(() => {
  vi.mocked(useAuth).mockReturnValue({ accessToken: 'test-token' } as any)
})

it('loads and displays categories', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([
    { id: '1', name: 'Cat 1', slug: 'cat-1', parentCategoryId: null, isActive: true },
    { id: '2', name: 'Cat 2', slug: 'cat-2', parentCategoryId: '1', isActive: false },
  ])

  render(<AdminCategoriesPage />)
  
  expect(screen.getByText('Loading...')).toBeInTheDocument()
  
  await waitFor(() => {
    expect(screen.getByText('Cat 1')).toBeInTheDocument()
  })
  expect(screen.getByText('Cat 2')).toBeInTheDocument()
})

it('creates a category', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([])
  vi.mocked(adminCatalogApi.createCategory).mockResolvedValue({
    id: '3', name: 'New Cat', slug: 'new-cat', parentCategoryId: null, isActive: true
  })

  render(<AdminCategoriesPage />)
  
  await waitFor(() => {
    expect(screen.queryByText('Loading...')).not.toBeInTheDocument()
  })
  
  fireEvent.change(screen.getByLabelText(/Tên/i), { target: { value: 'New Cat' } })
  fireEvent.change(screen.getByLabelText(/Slug/i), { target: { value: 'new-cat' } })
  fireEvent.click(screen.getByRole('button', { name: /Thêm/i }))
  
  await waitFor(() => {
    expect(adminCatalogApi.createCategory).toHaveBeenCalledWith(
      { name: 'New Cat', slug: 'new-cat', parentCategoryId: null }
    )
  })
})

it('displays error on create failure', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([])
  vi.mocked(adminCatalogApi.createCategory).mockRejectedValue(new ApiError(409, { message: 'Slug exists' }))

  render(<AdminCategoriesPage />)
  
  await waitFor(() => {
    expect(screen.queryByText('Loading...')).not.toBeInTheDocument()
  })
  
  fireEvent.change(screen.getByLabelText(/Tên/i), { target: { value: 'New Cat' } })
  fireEvent.change(screen.getByLabelText(/Slug/i), { target: { value: 'new-cat' } })
  fireEvent.click(screen.getByRole('button', { name: /Thêm/i }))
  
  await waitFor(() => {
    expect(screen.getByRole('alert')).toHaveTextContent('Slug exists')
  })
})
