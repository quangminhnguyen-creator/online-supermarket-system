import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminPromotionsPage } from './AdminPromotionsPage'
import { adminApi, type PaginatedPromotionsDto, type PromotionDto } from '../../api/adminApi'
import { ApiError } from '../../api/httpClient'

const mockAuth = { accessToken: 'jwt-token' as string | null }

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuth,
}))

const welcome: PromotionDto = {
  id: 'p1',
  code: 'WELCOME10',
  discountType: 'Percentage',
  discountValue: 10,
  minOrderAmount: 0,
  usageLimit: null,
  usageCount: 3,
  isActive: true,
  createdAtUtc: '2026-08-01T00:00:00Z',
  updatedAtUtc: '2026-08-01T00:00:00Z',
}

function listResult(promotions: PromotionDto[]): PaginatedPromotionsDto {
  return { page: 1, pageSize: 20, totalCount: promotions.length, promotions }
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/admin/promotions']}>
      <AdminPromotionsPage />
    </MemoryRouter>,
  )
}

describe('AdminPromotionsPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockAuth.accessToken = 'jwt-token'
  })

  it('renders promotions from the admin API', async () => {
    vi.spyOn(adminApi, 'listPromotions').mockResolvedValue(listResult([welcome]))
    renderPage()
    expect(await screen.findByRole('table', { name: 'Danh sách khuyến mãi' })).toBeInTheDocument()
    expect(screen.getByText('WELCOME10')).toBeInTheDocument()
    expect(screen.getByText('10%')).toBeInTheDocument()
    expect(screen.getByText('3/∞')).toBeInTheDocument()
    expect(screen.getByText('Đang áp dụng')).toBeInTheDocument()
  })

  it('creates a new promotion and reloads the list', async () => {
    const user = userEvent.setup()
    const list = vi.spyOn(adminApi, 'listPromotions').mockResolvedValue(listResult([welcome]))
    const create = vi.spyOn(adminApi, 'createPromotion').mockResolvedValue({ ...welcome, id: 'p2', code: 'SUMMER' })
    renderPage()

    await user.click(await screen.findByRole('button', { name: '+ Tạo mã mới' }))
    const dialog = await screen.findByRole('dialog', { name: 'Tạo mã giảm giá' })
    await user.type(within(dialog).getByLabelText('Mã'), 'summer')
    await user.type(within(dialog).getByLabelText('Giá trị giảm'), '20')
    await user.click(within(dialog).getByRole('button', { name: 'Tạo mã' }))

    expect(create).toHaveBeenCalledWith(
      { code: 'summer', discountType: 'Percentage', discountValue: 20, minOrderAmount: 0, usageLimit: null },
      'jwt-token',
    )
    // list reloaded (initial + after save)
    expect(list).toHaveBeenCalledTimes(2)
  })

  it('surfaces a conflict when the code already exists', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'listPromotions').mockResolvedValue(listResult([welcome]))
    vi.spyOn(adminApi, 'createPromotion').mockRejectedValue(new ApiError(409, { message: 'exists' }))
    renderPage()

    await user.click(await screen.findByRole('button', { name: '+ Tạo mã mới' }))
    const dialog = await screen.findByRole('dialog', { name: 'Tạo mã giảm giá' })
    await user.type(within(dialog).getByLabelText('Mã'), 'WELCOME10')
    await user.type(within(dialog).getByLabelText('Giá trị giảm'), '5')
    await user.click(within(dialog).getByRole('button', { name: 'Tạo mã' }))

    expect(await within(dialog).findByText('Mã giảm giá này đã tồn tại.')).toBeInTheDocument()
  })

  it('edits an existing promotion', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'listPromotions').mockResolvedValue(listResult([welcome]))
    const update = vi.spyOn(adminApi, 'updatePromotion').mockResolvedValue({ ...welcome, discountValue: 15 })
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Sửa mã WELCOME10' }))
    const dialog = await screen.findByRole('dialog', { name: 'Sửa mã WELCOME10' })
    const valueInput = within(dialog).getByLabelText('Giá trị giảm')
    await user.clear(valueInput)
    await user.type(valueInput, '15')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu thay đổi' }))

    expect(update).toHaveBeenCalledWith(
      'p1',
      { discountValue: 15, minOrderAmount: 0, usageLimit: null, isActive: true },
      'jwt-token',
    )
  })

  it('retries after a load failure', async () => {
    const spy = vi.spyOn(adminApi, 'listPromotions')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(listResult([welcome]))
    renderPage()
    await userEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByText('WELCOME10')).toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(2)
  })
})
