import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminInventoryTransactions } from './AdminInventoryTransactions'
import { inventoryIntelligenceApi, type InventoryTransactionDto } from '../../api/inventoryIntelligenceApi'
import { ApiError } from '../../api/httpClient'

const mockAuth = { accessToken: 'jwt-token' as string | null }

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuth,
}))

const transactions: InventoryTransactionDto[] = [
  {
    id: 't1',
    branchInventoryId: 'inv-1',
    transactionType: 'Reserve',
    quantityOnHandDelta: 0,
    reservedQuantityDelta: 2,
    quantityOnHandAfter: 30,
    reservedQuantityAfter: 2,
    referenceType: 'Order',
    referenceId: 'o1',
    actorUserId: null,
    note: null,
    createdAtUtc: '2026-09-01T02:00:00Z',
  },
  {
    id: 't2',
    branchInventoryId: 'inv-1',
    transactionType: 'ManualAdjustment',
    quantityOnHandDelta: 5,
    reservedQuantityDelta: 0,
    quantityOnHandAfter: 35,
    reservedQuantityAfter: 2,
    referenceType: 'AdminAdjustment',
    referenceId: null,
    actorUserId: 'u1',
    note: 'kiểm kê',
    createdAtUtc: '2026-09-02T02:00:00Z',
  },
]

describe('AdminInventoryTransactions', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockAuth.accessToken = 'jwt-token'
  })

  it('renders the ledger table for an inventory row', async () => {
    vi.spyOn(inventoryIntelligenceApi, 'getTransactions')
      .mockResolvedValue({ data: transactions, totalCount: 2, page: 1, pageSize: 20 })

    render(<AdminInventoryTransactions inventoryId="inv-1" productName="iPhone 15" onClose={() => {}} />)

    expect(await screen.findByRole('table', { name: 'Lịch sử giao dịch kho' })).toBeInTheDocument()
    expect(screen.getByText('iPhone 15')).toBeInTheDocument()
    expect(screen.getByText('Đặt giữ')).toBeInTheDocument()
    expect(screen.getByText('kiểm kê')).toBeInTheDocument()
    expect(inventoryIntelligenceApi.getTransactions).toHaveBeenCalledWith(
      'inv-1',
      expect.objectContaining({ page: 1, pageSize: 20 }),
      'jwt-token',
      expect.any(AbortSignal),
    )
  })

  it('shows an empty state when there are no transactions', async () => {
    vi.spyOn(inventoryIntelligenceApi, 'getTransactions')
      .mockResolvedValue({ data: [], totalCount: 0, page: 1, pageSize: 20 })

    render(<AdminInventoryTransactions inventoryId="inv-1" productName="iPhone 15" onClose={() => {}} />)

    expect(await screen.findByText('Chưa có giao dịch nào cho sản phẩm này.')).toBeInTheDocument()
  })

  it('shows an error state and retries', async () => {
    const getTransactions = vi.spyOn(inventoryIntelligenceApi, 'getTransactions')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce({ data: transactions, totalCount: 2, page: 1, pageSize: 20 })

    render(<AdminInventoryTransactions inventoryId="inv-1" productName="iPhone 15" onClose={() => {}} />)

    await userEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByRole('table', { name: 'Lịch sử giao dịch kho' })).toBeInTheDocument()
    expect(getTransactions).toHaveBeenCalledTimes(2)
  })

  it('closes via the close button', async () => {
    const onClose = vi.fn()
    vi.spyOn(inventoryIntelligenceApi, 'getTransactions')
      .mockResolvedValue({ data: [], totalCount: 0, page: 1, pageSize: 20 })

    render(<AdminInventoryTransactions inventoryId="inv-1" productName="iPhone 15" onClose={onClose} />)

    const dialog = await screen.findByRole('dialog', { name: 'Lịch sử giao dịch kho' })
    await userEvent.click(within(dialog).getByRole('button', { name: 'Đóng' }))
    expect(onClose).toHaveBeenCalled()
  })
})