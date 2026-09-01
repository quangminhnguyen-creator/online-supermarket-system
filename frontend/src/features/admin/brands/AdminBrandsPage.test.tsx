import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { expect, it, vi, beforeEach } from 'vitest'
import { AdminBrandsPage } from './AdminBrandsPage'
import { adminCatalogApi } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'
import { ApiError } from '../../../api/httpClient'

vi.mock('../../../api/adminCatalogApi')
vi.mock('../../auth/AuthContext')

beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(useAuth).mockReturnValue({ accessToken: 'test-token' } as any)
})

it('loads and displays brands', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([
    { id: '1', name: 'Samsung', slug: 'samsung', isActive: true },
    { id: '2', name: 'Sony', slug: 'sony', isActive: false },
  ])

  render(<AdminBrandsPage />)
  expect(screen.getByText('Đang tải thương hiệu...')).toBeInTheDocument()

  await waitFor(() => {
    expect(screen.getByText('Samsung')).toBeInTheDocument()
  })
  expect(screen.getByText('Sony')).toBeInTheDocument()
  expect(screen.getByText('Hoạt động')).toBeInTheDocument()
  expect(screen.getAllByText('Vô hiệu hóa').length).toBeGreaterThan(0)
})

it('displays empty state when no brands exist', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([])

  render(<AdminBrandsPage />)
  await waitFor(() => {
    expect(screen.getByText('Không có thương hiệu nào.')).toBeInTheDocument()
  })
})

it('creates a new brand', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([])
  vi.mocked(adminCatalogApi.createBrand).mockResolvedValue({
    id: '3', name: 'LG', slug: 'lg', isActive: true,
  })

  render(<AdminBrandsPage />)
  await waitFor(() => {
    expect(screen.getByText('Không có thương hiệu nào.')).toBeInTheDocument()
  })

  fireEvent.change(screen.getByLabelText(/Tên thương hiệu/i), { target: { value: 'LG' } })
  fireEvent.change(screen.getByLabelText(/^Slug:/i), { target: { value: 'lg' } })

  fireEvent.click(screen.getByRole('button', { name: 'Thêm thương hiệu' }))

  await waitFor(() => {
    expect(adminCatalogApi.createBrand).toHaveBeenCalledWith(
      { name: 'LG', slug: 'lg' },
      'test-token'
    )
  })
})

it('edits an existing brand', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([
    { id: '1', name: 'Samsung Old', slug: 'samsung-old', isActive: true },
  ])
  vi.mocked(adminCatalogApi.updateBrand).mockResolvedValue({
    id: '1', name: 'Samsung New', slug: 'samsung-new', isActive: true,
  })

  render(<AdminBrandsPage />)
  await waitFor(() => {
    expect(screen.getByText('Samsung Old')).toBeInTheDocument()
  })

  fireEvent.click(screen.getByRole('button', { name: 'Sửa' }))

  expect(screen.getByText('Chỉnh sửa thương hiệu')).toBeInTheDocument()
  expect(screen.getByDisplayValue('Samsung Old')).toBeInTheDocument()

  fireEvent.change(screen.getByLabelText(/Tên thương hiệu/i), { target: { value: 'Samsung New' } })
  fireEvent.change(screen.getByLabelText(/^Slug:/i), { target: { value: 'samsung-new' } })

  fireEvent.click(screen.getByRole('button', { name: 'Cập nhật' }))

  await waitFor(() => {
    expect(adminCatalogApi.updateBrand).toHaveBeenCalledWith(
      '1',
      { name: 'Samsung New', slug: 'samsung-new' },
      'test-token'
    )
  })
})

it('deactivates brand via confirmation dialog', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([
    { id: '1', name: 'Samsung', slug: 'samsung', isActive: true },
  ])
  vi.mocked(adminCatalogApi.updateBrandStatus).mockResolvedValue({
    id: '1', name: 'Samsung', slug: 'samsung', isActive: false,
  })

  render(<AdminBrandsPage />)
  await waitFor(() => {
    expect(screen.getByText('Samsung')).toBeInTheDocument()
  })

  fireEvent.click(screen.getByRole('button', { name: 'Vô hiệu hóa' }))

  expect(screen.getByRole('dialog')).toBeInTheDocument()
  expect(screen.getByText(/Bạn có chắc chắn muốn vô hiệu hóa thương hiệu "Samsung"?/)).toBeInTheDocument()

  const confirmBtn = screen.getAllByRole('button', { name: 'Vô hiệu hóa' })[1]
  fireEvent.click(confirmBtn)

  await waitFor(() => {
    expect(adminCatalogApi.updateBrandStatus).toHaveBeenCalledWith(
      '1',
      { isActive: false },
      'test-token'
    )
  })
})

it('restores inactive brand directly', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockResolvedValue([
    { id: '1', name: 'Samsung', slug: 'samsung', isActive: false },
  ])
  vi.mocked(adminCatalogApi.updateBrandStatus).mockResolvedValue({
    id: '1', name: 'Samsung', slug: 'samsung', isActive: true,
  })

  render(<AdminBrandsPage />)
  await waitFor(() => {
    expect(screen.getByText('Samsung')).toBeInTheDocument()
  })

  fireEvent.click(screen.getByRole('button', { name: 'Kích hoạt lại' }))

  await waitFor(() => {
    expect(adminCatalogApi.updateBrandStatus).toHaveBeenCalledWith(
      '1',
      { isActive: true },
      'test-token'
    )
  })
})

it('displays retry on load failure', async () => {
  vi.mocked(adminCatalogApi.getBrands).mockRejectedValueOnce(new Error('Fetch failed'))

  render(<AdminBrandsPage />)
  await waitFor(() => {
    expect(screen.getByText('Fetch failed')).toBeInTheDocument()
  })

  vi.mocked(adminCatalogApi.getBrands).mockResolvedValueOnce([
    { id: '1', name: 'Samsung', slug: 'samsung', isActive: true },
  ])

  fireEvent.click(screen.getByRole('button', { name: 'Thử lại' }))

  await waitFor(() => {
    expect(screen.getByText('Samsung')).toBeInTheDocument()
  })
})
