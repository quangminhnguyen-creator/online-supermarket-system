import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminInventoryPage } from './AdminInventoryPage'
import { branchApi, type BranchProductInventoryDto } from '../../api/branchApi'
import { adminApi } from '../../api/adminApi'
import { inventoryIntelligenceApi } from '../../api/inventoryIntelligenceApi'
import { ApiError } from '../../api/httpClient'

const mockAuth = { accessToken: 'jwt-token' as string | null }

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuth,
}))

const branches = [
  { id: 'branch-1', name: 'AptechMart Quận 1', address: 'Q1', phone: null, latitude: null, longitude: null, isActive: true },
  { id: 'branch-2', name: 'AptechMart Quận 3', address: 'Q3', phone: null, latitude: null, longitude: null, isActive: true },
]

const inventoryBranch1: BranchProductInventoryDto[] = [
  { inventoryId: 'inv-1', productId: 'p1', productName: 'iPhone 15', sku: 'SKU-1', sellingPrice: 20000000, quantityOnHand: 30, reservedQuantity: 2, availableQuantity: 28, reorderLevel: 5 },
  { inventoryId: 'inv-2', productId: 'p2', productName: 'AirPods Pro 2', sku: 'SKU-2', sellingPrice: 5000000, quantityOnHand: 6, reservedQuantity: 4, availableQuantity: 2, reorderLevel: 5 },
]

function mockBranchApi(inventory = inventoryBranch1) {
  vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
  vi.spyOn(branchApi, 'getBranchInventory').mockResolvedValue({ branchId: 'branch-1', products: inventory })
}

function renderPage(url = '/admin/inventory') {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <AdminInventoryPage />
    </MemoryRouter>,
  )
}

describe('AdminInventoryPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockAuth.accessToken = 'jwt-token'
  })

  it('loads branches and shows the first branch inventory', async () => {
    mockBranchApi()
    renderPage()
    expect(await screen.findByRole('table', { name: 'Tồn kho chi nhánh' })).toBeInTheDocument()
    expect(screen.getByText('iPhone 15')).toBeInTheDocument()
    expect(screen.getByText('AirPods Pro 2')).toBeInTheDocument()
    // branch selector populated
    expect(screen.getByRole('option', { name: 'AptechMart Quận 1' })).toBeInTheDocument()
  })

  it('highlights rows at or below the reorder level', async () => {
    mockBranchApi()
    renderPage()
    // AirPods: available 2 <= reorder 5 -> low stock badge
    expect(await screen.findByText('Sắp hết')).toBeInTheDocument()
    expect(screen.getByText('2 sản phẩm')).toBeInTheDocument()
    expect(screen.getByText(/1 sản phẩm dưới định mức/)).toBeInTheDocument()
  })

  it('reloads inventory when the branch changes', async () => {
    const user = userEvent.setup()
    mockBranchApi()
    const getInventory = vi.mocked(branchApi.getBranchInventory)
    renderPage()
    await screen.findByRole('table', { name: 'Tồn kho chi nhánh' })
    await user.selectOptions(screen.getByLabelText('Chi nhánh'), 'branch-2')
    expect(getInventory).toHaveBeenLastCalledWith('branch-2', expect.objectContaining({ signal: expect.any(AbortSignal) }))
  })

  it('edits an item and reflects the saved values', async () => {
    const user = userEvent.setup()
    mockBranchApi()
    const adjust = vi.spyOn(adminApi, 'adjustInventory').mockResolvedValue({
      ...inventoryBranch1[0],
      sellingPrice: 18000000,
      quantityOnHand: 40,
      availableQuantity: 38,
    })
    renderPage()
    await user.click(await screen.findByRole('button', { name: 'Chỉnh sửa iPhone 15' }))
    const dialog = await screen.findByRole('dialog')
    const price = within(dialog).getByLabelText('Giá bán (₫)')
    await user.clear(price)
    await user.type(price, '18000000')
    const qty = within(dialog).getByLabelText('Tồn kho thực tế')
    await user.clear(qty)
    await user.type(qty, '40')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu thay đổi' }))

    expect(adjust).toHaveBeenCalledWith(
      'branch-1',
      { productId: 'p1', quantityOnHand: 40, sellingPrice: 18000000, reorderLevel: 5, reason: undefined },
      'jwt-token',
    )
    // Modal closes and the table shows the updated quantity
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(await screen.findByText('40')).toBeInTheDocument()
  })

  it('shows a validation error for a negative price without calling the API', async () => {
    const user = userEvent.setup()
    mockBranchApi()
    const adjust = vi.spyOn(adminApi, 'adjustInventory')
    renderPage()
    await user.click(await screen.findByRole('button', { name: 'Chỉnh sửa iPhone 15' }))
    const dialog = await screen.findByRole('dialog')
    const price = within(dialog).getByLabelText('Giá bán (₫)')
    await user.clear(price)
    await user.type(price, '-100')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu thay đổi' }))
    expect(within(dialog).getByRole('alert')).toHaveTextContent('Giá bán phải là số không âm.')
    expect(adjust).not.toHaveBeenCalled()
  })

  it('opens immutable transaction history for one inventory row', async () => {
    const user = userEvent.setup()
    mockBranchApi()
    vi.spyOn(inventoryIntelligenceApi, 'getTransactions')
      .mockResolvedValue({ data: [], totalCount: 0, page: 1, pageSize: 20 })

    renderPage()
    await user.click(await screen.findByRole('button', { name: 'Lịch sử kho iPhone 15' }))

    expect(await screen.findByRole('dialog', { name: 'Lịch sử giao dịch kho' })).toBeInTheDocument()
  })

  it('shows an error state and retries the inventory load', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    const getInventory = vi.spyOn(branchApi, 'getBranchInventory')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce({ branchId: 'branch-1', products: inventoryBranch1 })
    renderPage()
    await userEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByText('iPhone 15')).toBeInTheDocument()
    expect(getInventory).toHaveBeenCalledTimes(2)
  })
})
