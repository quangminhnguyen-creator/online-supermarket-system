import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { expect, it, vi, beforeEach } from 'vitest'
import { AdminProductsPage } from './AdminProductsPage'
import { adminCatalogApi } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'
import { ApiError } from '../../../api/httpClient'

vi.mock('../../../api/adminCatalogApi')
vi.mock('../../auth/AuthContext')

beforeEach(() => {
  vi.mocked(useAuth).mockReturnValue({ accessToken: 'test-token' } as any)
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([
    { id: 'cat-1', name: 'Cat 1', slug: 'cat-1', parentCategoryId: null, isActive: true },
  ])
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([
    { id: 'brand-1', name: 'Brand 1', slug: 'brand-1', isActive: true },
  ])
})

it('loads and displays products', async () => {
  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [
      { id: '1', name: 'Product 1', slug: 'p-1', sku: 'SKU1', categoryId: 'cat-1', categoryName: 'Cat 1', brandId: 'brand-1', brandName: 'Brand 1', basePrice: 100, unit: 'box', description: 'Desc', imageUrl: null, isActive: true },
    ],
    meta: { totalCount: 1, currentPage: 1, pageSize: 10, totalPages: 1 }
  })

  render(<AdminProductsPage />)
  
  expect(screen.getByText('Loading products...')).toBeInTheDocument()
  
  await waitFor(() => {
    expect(screen.getByText(/Product 1/)).toBeInTheDocument()
  })
})

it('creates a product', async () => {
  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [],
    meta: { totalCount: 0, currentPage: 1, pageSize: 10, totalPages: 0 }
  })
  vi.mocked(adminCatalogApi.createProduct).mockResolvedValue({
    id: '1', name: 'New Prod', slug: 'new-prod', sku: 'SKU2', categoryId: 'cat-1', categoryName: 'Cat 1', brandId: 'brand-1', brandName: 'Brand 1', basePrice: 200, unit: 'kg', description: '', imageUrl: null, isActive: true
  })

  render(<AdminProductsPage />)
  
  await waitFor(() => {
    expect(screen.queryByText('Loading products...')).not.toBeInTheDocument()
  })
  
  fireEvent.change(screen.getByLabelText(/Tên/i), { target: { value: 'New Prod' } })
  fireEvent.change(screen.getByLabelText(/Slug/i), { target: { value: 'new-prod' } })
  fireEvent.change(screen.getByLabelText(/SKU/i), { target: { value: 'SKU2' } })
  fireEvent.change(screen.getByLabelText(/Danh mục/i), { target: { value: 'cat-1' } })
  fireEvent.change(screen.getByLabelText(/Thương hiệu/i), { target: { value: 'brand-1' } })
  fireEvent.change(screen.getByLabelText(/Giá/i), { target: { value: '200' } })
  fireEvent.change(screen.getByLabelText(/Đơn vị/i), { target: { value: 'kg' } })
  
  fireEvent.click(screen.getByRole('button', { name: /Thêm/i }))
  
  await waitFor(() => {
    expect(adminCatalogApi.createProduct).toHaveBeenCalledWith(
      { name: 'New Prod', slug: 'new-prod', sku: 'SKU2', categoryId: 'cat-1', brandId: 'brand-1', basePrice: 200, unit: 'kg', description: '', imageUrl: null }
    )
  })
})
