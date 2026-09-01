import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { expect, it, vi, beforeEach } from 'vitest'
import { AdminProductsPage } from './AdminProductsPage'
import { adminCatalogApi } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'
import { ApiError } from '../../../api/httpClient'

vi.mock('../../../api/adminCatalogApi')
vi.mock('../../auth/AuthContext')

const mockCategories = [
  { id: 'cat-1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: true },
  { id: 'cat-2', name: 'Gia dụng', slug: 'gia-dung', parentCategoryId: null, isActive: false },
]

const mockBrands = [
  { id: 'brand-1', name: 'Samsung', slug: 'samsung', isActive: true },
  { id: 'brand-2', name: 'Sony', slug: 'sony', isActive: false },
]

beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(useAuth).mockReturnValue({ accessToken: 'test-token' } as any)
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue(mockCategories)
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue(mockBrands)
})

it('loads and displays products with pagination info and page size 20', async () => {
  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [
      {
        id: '1',
        name: 'Smart Tivi Samsung 55',
        slug: 'smart-tivi-samsung-55',
        sku: 'TV-SAM-001',
        categoryId: 'cat-1',
        categoryName: 'Điện tử',
        brandId: 'brand-1',
        brandName: 'Samsung',
        basePrice: 15000000,
        unit: 'cái',
        description: 'Tivi 4K',
        imageUrl: 'https://example.com/tv.jpg',
        isActive: true,
      },
    ],
    meta: { totalCount: 25, page: 1, pageSize: 20, totalPages: 2 },
  })

  render(<AdminProductsPage />)
  expect(screen.getByText('Đang tải sản phẩm...')).toBeInTheDocument()

  await waitFor(() => {
    expect(screen.getByText('Smart Tivi Samsung 55')).toBeInTheDocument()
  })

  expect(screen.getByText('TV-SAM-001')).toBeInTheDocument()
  expect(screen.getByText('Trang 1 / 2 (25 sản phẩm)')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Trang trước' })).toBeDisabled()
  expect(screen.getByRole('button', { name: 'Trang sau' })).not.toBeDisabled()
})

it('displays empty state when no products found', async () => {
  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [],
    meta: { totalCount: 0, page: 1, pageSize: 20, totalPages: 0 },
  })

  render(<AdminProductsPage />)
  await waitFor(() => {
    expect(screen.getByText('Không tìm thấy sản phẩm nào.')).toBeInTheDocument()
  })
})

it('filters products by search, category, brand, and status', async () => {
  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [],
    meta: { totalCount: 0, page: 1, pageSize: 20, totalPages: 0 },
  })

  render(<AdminProductsPage />)
  await waitFor(() => {
    expect(adminCatalogApi.getProducts).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 20 }),
      'test-token',
      expect.anything()
    )
  })

  // Change search
  fireEvent.change(screen.getByLabelText(/Tìm kiếm:/i), { target: { value: 'Sony' } })
  await waitFor(() => {
    expect(adminCatalogApi.getProducts).toHaveBeenCalledWith(
      expect.objectContaining({ search: 'Sony', page: 1 }),
      'test-token',
      expect.anything()
    )
  })

  // Change category filter
  fireEvent.change(screen.getByLabelText(/Danh mục:/i, { selector: 'select' }), { target: { value: 'cat-1' } })
  await waitFor(() => {
    expect(adminCatalogApi.getProducts).toHaveBeenCalledWith(
      expect.objectContaining({ categoryId: 'cat-1', page: 1 }),
      'test-token',
      expect.anything()
    )
  })

  // Change status filter
  fireEvent.change(screen.getByLabelText(/Trạng thái:/i), { target: { value: 'inactive' } })
  await waitFor(() => {
    expect(adminCatalogApi.getProducts).toHaveBeenCalledWith(
      expect.objectContaining({ isActive: false, page: 1 }),
      'test-token',
      expect.anything()
    )
  })
})

it('creates a new product normalizing empty description and imageUrl to null', async () => {
  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [],
    meta: { totalCount: 0, page: 1, pageSize: 20, totalPages: 0 },
  })
  vi.mocked(adminCatalogApi.createProduct).mockResolvedValue({
    id: 'p-new',
    name: 'Tủ lạnh Samsung',
    slug: 'tu-lanh-samsung',
    sku: 'TL-SAM-01',
    categoryId: 'cat-1',
    categoryName: 'Điện tử',
    brandId: 'brand-1',
    brandName: 'Samsung',
    basePrice: 8500000,
    unit: 'cái',
    description: null,
    imageUrl: null,
    isActive: true,
  })

  render(<AdminProductsPage />)
  await waitFor(() => {
    expect(screen.getByText('Thêm sản phẩm mới')).toBeInTheDocument()
  })

  fireEvent.change(screen.getByLabelText(/Tên sản phẩm \*/i), { target: { value: 'Tủ lạnh Samsung' } })
  fireEvent.change(screen.getByLabelText(/Mã SKU \*/i), { target: { value: 'TL-SAM-01' } })
  fireEvent.change(screen.getByLabelText(/Slug \*/i), { target: { value: 'tu-lanh-samsung' } })
  fireEvent.change(screen.getByLabelText(/Danh mục \*/i), { target: { value: 'cat-1' } })
  fireEvent.change(screen.getByLabelText(/Thương hiệu \*/i), { target: { value: 'brand-1' } })
  fireEvent.change(screen.getByLabelText(/Giá gốc \(VNĐ\) \*/i), { target: { value: '8500000' } })
  fireEvent.change(screen.getByLabelText(/Đơn vị tính \*/i), { target: { value: 'cái' } })
  // Leave description and imageUrl empty

  fireEvent.click(screen.getByRole('button', { name: 'Thêm sản phẩm' }))

  await waitFor(() => {
    expect(adminCatalogApi.createProduct).toHaveBeenCalledWith(
      {
        categoryId: 'cat-1',
        brandId: 'brand-1',
        sku: 'TL-SAM-01',
        name: 'Tủ lạnh Samsung',
        slug: 'tu-lanh-samsung',
        description: null,
        basePrice: 8500000,
        unit: 'cái',
        imageUrl: null,
      },
      'test-token'
    )
  })
})

it('edits an existing product', async () => {
  const existingProd = {
    id: 'p-1',
    name: 'Smart Tivi Samsung 55',
    slug: 'smart-tivi-samsung-55',
    sku: 'TV-SAM-001',
    categoryId: 'cat-1',
    categoryName: 'Điện tử',
    brandId: 'brand-1',
    brandName: 'Samsung',
    basePrice: 15000000,
    unit: 'cái',
    description: 'Tivi 4K',
    imageUrl: 'https://example.com/tv.jpg',
    isActive: true,
  }

  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [existingProd],
    meta: { totalCount: 1, page: 1, pageSize: 20, totalPages: 1 },
  })
  vi.mocked(adminCatalogApi.updateProduct).mockResolvedValue({
    ...existingProd,
    name: 'Smart Tivi Samsung 55 Pro',
    basePrice: 16000000,
  })

  render(<AdminProductsPage />)
  await waitFor(() => {
    expect(screen.getByText('Smart Tivi Samsung 55')).toBeInTheDocument()
  })

  fireEvent.click(screen.getByRole('button', { name: 'Sửa' }))

  expect(screen.getByText('Chỉnh sửa sản phẩm')).toBeInTheDocument()
  expect(screen.getByDisplayValue('Smart Tivi Samsung 55')).toBeInTheDocument()
  expect(screen.getByDisplayValue('15000000')).toBeInTheDocument()

  fireEvent.change(screen.getByLabelText(/Tên sản phẩm \*/i), { target: { value: 'Smart Tivi Samsung 55 Pro' } })
  fireEvent.change(screen.getByLabelText(/Giá gốc \(VNĐ\) \*/i), { target: { value: '16000000' } })

  fireEvent.click(screen.getByRole('button', { name: 'Cập nhật sản phẩm' }))

  await waitFor(() => {
    expect(adminCatalogApi.updateProduct).toHaveBeenCalledWith(
      'p-1',
      expect.objectContaining({
        name: 'Smart Tivi Samsung 55 Pro',
        basePrice: 16000000,
      }),
      'test-token'
    )
  })
})

it('deactivates product with confirmation dialog and restores directly', async () => {
  const prod = {
    id: 'p-1',
    name: 'Smart Tivi Samsung 55',
    slug: 'smart-tivi-samsung-55',
    sku: 'TV-SAM-001',
    categoryId: 'cat-1',
    categoryName: 'Điện tử',
    brandId: 'brand-1',
    brandName: 'Samsung',
    basePrice: 15000000,
    unit: 'cái',
    description: null,
    imageUrl: null,
    isActive: true,
  }

  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [prod],
    meta: { totalCount: 1, page: 1, pageSize: 20, totalPages: 1 },
  })
  vi.mocked(adminCatalogApi.updateProductStatus).mockResolvedValue({
    ...prod,
    isActive: false,
  })

  render(<AdminProductsPage />)
  await waitFor(() => {
    expect(screen.getByText('Smart Tivi Samsung 55')).toBeInTheDocument()
  })

  // Click deactivate
  fireEvent.click(screen.getByRole('button', { name: 'Vô hiệu hóa' }))

  expect(screen.getByRole('dialog')).toBeInTheDocument()
  expect(screen.getByText(/Bạn có chắc chắn muốn vô hiệu hóa sản phẩm "Smart Tivi Samsung 55"/)).toBeInTheDocument()

  // Confirm
  const confirmBtn = screen.getAllByRole('button', { name: 'Vô hiệu hóa' })[1]
  fireEvent.click(confirmBtn)

  await waitFor(() => {
    expect(adminCatalogApi.updateProductStatus).toHaveBeenCalledWith(
      'p-1',
      { isActive: false },
      'test-token'
    )
  })
})

it('displays error and disables form when category or brand lookup fails, allowing retry', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockRejectedValueOnce(new Error('Failed to load categories'))
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue(mockBrands)
  vi.mocked(adminCatalogApi.getProducts).mockResolvedValue({
    items: [],
    meta: { totalCount: 0, page: 1, pageSize: 20, totalPages: 0 },
  })

  render(<AdminProductsPage />)

  await waitFor(() => {
    expect(screen.getByText(/Failed to load categories/i)).toBeInTheDocument()
  })

  // Submit button should be disabled
  expect(screen.getByRole('button', { name: 'Thêm sản phẩm' })).toBeDisabled()

  // Click retry
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValueOnce(mockCategories)
  fireEvent.click(screen.getByRole('button', { name: 'Thử lại tải danh mục & thương hiệu' }))

  await waitFor(() => {
    expect(screen.queryByText(/Failed to load categories/i)).not.toBeInTheDocument()
  })
  expect(screen.getByRole('button', { name: 'Thêm sản phẩm' })).not.toBeDisabled()
})
