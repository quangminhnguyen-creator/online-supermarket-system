import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { expect, it, vi, beforeEach } from 'vitest'
import { AdminCategoriesPage } from './AdminCategoriesPage'
import { adminCatalogApi } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'
import { ApiError } from '../../../api/httpClient'

vi.mock('../../../api/adminCatalogApi')
vi.mock('../../auth/AuthContext')

beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(useAuth).mockReturnValue({ accessToken: 'test-token' } as any)
})

it('loads and displays categories and parent names', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([
    { id: '1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: true },
    { id: '2', name: 'Tivi', slug: 'tivi', parentCategoryId: '1', isActive: false },
  ])

  render(<AdminCategoriesPage />)
  expect(screen.getByText('Đang tải danh mục...')).toBeInTheDocument()

  await waitFor(() => {
    expect(screen.getByRole('cell', { name: 'dien-tu' })).toBeInTheDocument()
  })
  expect(screen.getByRole('cell', { name: 'tivi' })).toBeInTheDocument()
  expect(screen.getByText('Hoạt động')).toBeInTheDocument()
  expect(screen.getAllByText('Vô hiệu hóa').length).toBeGreaterThan(0)
})

it('displays empty state when no categories exist', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([])

  render(<AdminCategoriesPage />)
  await waitFor(() => {
    expect(screen.getByText('Không có danh mục nào.')).toBeInTheDocument()
  })
})

it('creates a new category', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([
    { id: '1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: true },
  ])
  vi.mocked(adminCatalogApi.createCategory).mockResolvedValue({
    id: '3', name: 'Gia dụng', slug: 'gia-dung', parentCategoryId: '1', isActive: true,
  })

  render(<AdminCategoriesPage />)
  await waitFor(() => {
    expect(screen.getByRole('cell', { name: 'dien-tu' })).toBeInTheDocument()
  })

  fireEvent.change(screen.getByLabelText(/Tên danh mục/i), { target: { value: 'Gia dụng' } })
  fireEvent.change(screen.getByLabelText(/^Slug:/i), { target: { value: 'gia-dung' } })
  fireEvent.change(screen.getByLabelText(/Danh mục cha/i), { target: { value: '1' } })

  fireEvent.click(screen.getByRole('button', { name: 'Thêm danh mục' }))

  await waitFor(() => {
    expect(adminCatalogApi.createCategory).toHaveBeenCalledWith(
      { name: 'Gia dụng', slug: 'gia-dung', parentCategoryId: '1' },
      'test-token'
    )
  })
})

it('edits an existing category and excludes self and descendants from parent options', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([
    { id: '1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: true },
    { id: '2', name: 'Tivi', slug: 'tivi', parentCategoryId: '1', isActive: true },
    { id: '3', name: 'Smart Tivi', slug: 'smart-tivi', parentCategoryId: '2', isActive: true },
    { id: '4', name: 'Gia dụng', slug: 'gia-dung', parentCategoryId: null, isActive: true },
  ])
  vi.mocked(adminCatalogApi.updateCategory).mockResolvedValue({
    id: '2', name: 'Tivi Màn hình', slug: 'tivi-man-hinh', parentCategoryId: '4', isActive: true,
  })

  render(<AdminCategoriesPage />)
  await waitFor(() => {
    expect(screen.getByRole('cell', { name: 'tivi' })).toBeInTheDocument()
  })

  // Click edit on Tivi (id: 2)
  const editButtons = screen.getAllByRole('button', { name: 'Sửa' })
  fireEvent.click(editButtons[1]) // Second row is Tivi

  expect(screen.getByText('Chỉnh sửa danh mục')).toBeInTheDocument()
  expect(screen.getByDisplayValue('Tivi')).toBeInTheDocument()

  // In parent dropdown, 'Tivi' (self, id: 2) and 'Smart Tivi' (child, id: 3) must NOT be present as valid options
  const parentSelect = screen.getByLabelText(/Danh mục cha/i) as HTMLSelectElement
  const optionValues = Array.from(parentSelect.options).map(o => o.value)
  expect(optionValues).not.toContain('2')
  expect(optionValues).not.toContain('3')
  expect(optionValues).toContain('1')
  expect(optionValues).toContain('4')

  fireEvent.change(screen.getByLabelText(/Tên danh mục/i), { target: { value: 'Tivi Màn hình' } })
  fireEvent.change(screen.getByLabelText(/^Slug:/i), { target: { value: 'tivi-man-hinh' } })
  fireEvent.change(parentSelect, { target: { value: '4' } })

  fireEvent.click(screen.getByRole('button', { name: 'Cập nhật' }))

  await waitFor(() => {
    expect(adminCatalogApi.updateCategory).toHaveBeenCalledWith(
      '2',
      { name: 'Tivi Màn hình', slug: 'tivi-man-hinh', parentCategoryId: '4' },
      'test-token'
    )
  })
})

it('deactivates category via confirmation dialog', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([
    { id: '1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: true },
  ])
  vi.mocked(adminCatalogApi.updateCategoryStatus).mockResolvedValue({
    id: '1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: false,
  })

  render(<AdminCategoriesPage />)
  await waitFor(() => {
    expect(screen.getByRole('cell', { name: 'dien-tu' })).toBeInTheDocument()
  })

  fireEvent.click(screen.getByRole('button', { name: 'Vô hiệu hóa' }))

  // Dialog opened
  expect(screen.getByRole('dialog')).toBeInTheDocument()
  expect(screen.getByText(/Bạn có chắc chắn muốn vô hiệu hóa danh mục "Điện tử"?/)).toBeInTheDocument()

  // Confirm in dialog
  const confirmBtn = screen.getAllByRole('button', { name: 'Vô hiệu hóa' })[1]
  fireEvent.click(confirmBtn)

  await waitFor(() => {
    expect(adminCatalogApi.updateCategoryStatus).toHaveBeenCalledWith(
      '1',
      { isActive: false },
      'test-token'
    )
  })
})

it('restores inactive category directly without destructive dialog', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockResolvedValue([
    { id: '1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: false },
  ])
  vi.mocked(adminCatalogApi.updateCategoryStatus).mockResolvedValue({
    id: '1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: true,
  })

  render(<AdminCategoriesPage />)
  await waitFor(() => {
    expect(screen.getByRole('cell', { name: 'dien-tu' })).toBeInTheDocument()
  })

  fireEvent.click(screen.getByRole('button', { name: 'Kích hoạt lại' }))

  expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

  await waitFor(() => {
    expect(adminCatalogApi.updateCategoryStatus).toHaveBeenCalledWith(
      '1',
      { isActive: true },
      'test-token'
    )
  })
})

it('displays retry button on load failure', async () => {
  vi.mocked(adminCatalogApi.getCategories).mockRejectedValueOnce(new Error('Network error'))

  render(<AdminCategoriesPage />)
  await waitFor(() => {
    expect(screen.getByText('Network error')).toBeInTheDocument()
  })

  vi.mocked(adminCatalogApi.getCategories).mockResolvedValueOnce([
    { id: '1', name: 'Điện tử', slug: 'dien-tu', parentCategoryId: null, isActive: true },
  ])

  fireEvent.click(screen.getByRole('button', { name: 'Thử lại' }))

  await waitFor(() => {
    expect(screen.getByRole('cell', { name: 'dien-tu' })).toBeInTheDocument()
  })
})
