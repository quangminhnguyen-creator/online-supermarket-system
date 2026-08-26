import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter, Routes, Route, useLocation } from 'react-router-dom'
import { catalogApi, type ProductDetailDto } from '../../api/catalogApi'
import { branchApi, type BranchDto } from '../../api/branchApi'
import { ApiError } from '../../api/httpClient'

// ProductDetailPage will be imported after we implement it
import { ProductDetailPage } from './ProductDetailPage'

const mockDetail: ProductDetailDto = {
  id: 'prod-1',
  name: 'iPhone 15 128GB',
  slug: 'iphone-15-128gb',
  sku: 'DT-APP-002',
  description: 'Màn hình Super Retina XDR.',
  basePrice: 22990000,
  unit: 'cái',
  imageUrl: null,
  categoryId: 'cat-1',
  categoryName: 'Điện thoại & Tablet',
  brandId: 'brand-1',
  brandName: 'Apple',
  branchInventory: null,
}

const branches: BranchDto[] = [
  { id: 'branch-1', name: 'AptechMart Quận 1', address: '123 Nguyễn Huệ', phone: null, latitude: null, longitude: null, isActive: true },
  { id: 'branch-2', name: 'AptechMart Quận 3', address: '456 Đường 3 Tháng 2', phone: null, latitude: null, longitude: null, isActive: true },
]

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="detail-location">{location.search}</div>
}

function renderDetail(entry = '/product/prod-1') {
  return render(
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        <Route path="/product/:id" element={<ProductDetailPage />} />
        <Route path="/browse" element={<div>Browse</div>} />
      </Routes>
      <LocationDisplay />
    </MemoryRouter>
  )
}

describe('ProductDetailPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('loads and renders base product detail without a branch', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue([])
    vi.spyOn(catalogApi, 'getProductById').mockResolvedValue(mockDetail)
    renderDetail()
    expect(screen.getByLabelText('Đang tải chi tiết sản phẩm')).toHaveAttribute('aria-busy', 'true')
    expect(await screen.findByRole('heading', { name: 'iPhone 15 128GB' })).toBeInTheDocument()
    expect(screen.getByText('Màn hình Super Retina XDR.')).toBeInTheDocument()
    expect(screen.getByText(/22\.990\.000/)).toBeInTheDocument()
    expect(screen.getByText('Chọn kho để xem giá và tồn kho')).toBeInTheDocument()
    expect(catalogApi.getProductById).toHaveBeenCalledWith('prod-1', undefined, expect.any(AbortSignal))
  })

  it('loads the branch from a deep link', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    vi.spyOn(catalogApi, 'getProductById').mockResolvedValue({
      ...mockDetail,
      branchInventory: { branchId: 'branch-1', sellingPrice: 21990000, availableQuantity: 7, onHand: 9 },
    })
    renderDetail('/product/prod-1?branchId=branch-1')
    expect(await screen.findByLabelText('Kho hàng')).toHaveValue('branch-1')
    expect(catalogApi.getProductById).toHaveBeenCalledWith('prod-1', 'branch-1', expect.any(AbortSignal))
  })

  it('updates branchId, preserves query keys, and refetches detail', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    const detailSpy = vi.spyOn(catalogApi, 'getProductById').mockResolvedValue(mockDetail)
    renderDetail('/product/prod-1?ref=browse')
    const select = await screen.findByLabelText('Kho hàng')
    const { fireEvent } = await import('@testing-library/react')
    fireEvent.change(select, { target: { value: 'branch-2' } })
    await waitFor(() => expect(detailSpy).toHaveBeenLastCalledWith(
      'prod-1', 'branch-2', expect.any(AbortSignal)
    ))
    expect(screen.getByTestId('detail-location')).toHaveTextContent('?ref=browse&branchId=branch-2')
  })

  it.each([
    [undefined, null, 'Chọn kho để xem giá và tồn kho', /22\.990\.000/],
    ['branch-1', { branchId: 'branch-1', sellingPrice: 21990000, availableQuantity: 7, onHand: 9 }, 'Còn 7 sản phẩm tại kho', /21\.990\.000/],
    ['branch-1', { branchId: 'branch-1', sellingPrice: 21990000, availableQuantity: 0, onHand: 2 }, 'Tạm hết hàng tại kho này', /21\.990\.000/],
    ['branch-1', null, 'Sản phẩm không có tại kho này', /22\.990\.000/],
  ])('renders price and inventory for branch %s', async (branchId, inventory, message, price) => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    vi.spyOn(catalogApi, 'getProductById').mockResolvedValue({ ...mockDetail, branchInventory: inventory })
    renderDetail(`/product/prod-1${branchId ? `?branchId=${branchId}` : ''}`)
    expect(await screen.findByText(message)).toBeInTheDocument()
    expect(screen.getByText(price)).toBeInTheDocument()
  })

  it('falls back when the product image cannot load', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue([])
    vi.spyOn(catalogApi, 'getProductById').mockResolvedValue({
      ...mockDetail,
      imageUrl: 'https://example.com/broken.jpg',
    })
    renderDetail()
    const { fireEvent } = await import('@testing-library/react')
    fireEvent.error(await screen.findByRole('img', { name: 'iPhone 15 128GB' }))
    expect(screen.getByText('Hình ảnh sản phẩm')).toBeInTheDocument()
  })

  it('renders a dedicated 404 state', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue([])
    vi.spyOn(catalogApi, 'getProductById').mockRejectedValue(new ApiError(404))
    renderDetail()
    expect(await screen.findByRole('heading', { name: 'Không tìm thấy sản phẩm' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Quay lại danh sách sản phẩm' }))
      .toHaveAttribute('href', '/browse')
  })

  it('retries a generic product error', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue([])
    const detailSpy = vi.spyOn(catalogApi, 'getProductById')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(mockDetail)
    renderDetail()
    const { fireEvent } = await import('@testing-library/react')
    fireEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByRole('heading', { name: 'iPhone 15 128GB' })).toBeInTheDocument()
    expect(detailSpy).toHaveBeenCalledTimes(2)
  })

  it('keeps product usable when branches fail and retries branches', async () => {
    const branchSpy = vi.spyOn(branchApi, 'getBranches')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(branches)
    vi.spyOn(catalogApi, 'getProductById').mockResolvedValue(mockDetail)
    renderDetail()
    expect(await screen.findByRole('heading', { name: 'iPhone 15 128GB' })).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Không thể tải danh sách kho')
    const { fireEvent } = await import('@testing-library/react')
    fireEvent.click(screen.getByRole('button', { name: 'Tải lại danh sách kho' }))
    expect(await screen.findByLabelText('Kho hàng')).toBeEnabled()
    expect(branchSpy).toHaveBeenCalledTimes(2)
  })

  it('exposes accessible detail semantics', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    vi.spyOn(catalogApi, 'getProductById').mockResolvedValue(mockDetail)
    renderDetail()
    expect(await screen.findByRole('navigation', { name: 'Breadcrumb' })).toBeInTheDocument()
    expect(screen.getByLabelText('Kho hàng')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'iPhone 15 128GB' })).toBeInTheDocument()
  })
})
