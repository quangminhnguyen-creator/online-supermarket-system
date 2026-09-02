import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminBranchesPage } from './AdminBranchesPage'
import { adminApi } from '../../api/adminApi'
import { ApiError } from '../../api/httpClient'
import type { BranchDto } from '../../api/branchApi'

const mockAuth = { accessToken: 'jwt-token' as string | null }

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuth,
}))

const q1: BranchDto = {
  id: 'b1',
  name: 'AptechMart Quận 1',
  address: '123 Nguyễn Huệ, Q1',
  phone: '028 3822 1234',
  latitude: null,
  longitude: null,
  isActive: true,
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/admin/branches']}>
      <AdminBranchesPage />
    </MemoryRouter>,
  )
}

describe('AdminBranchesPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockAuth.accessToken = 'jwt-token'
  })

  it('renders branches from the admin API', async () => {
    vi.spyOn(adminApi, 'listBranches').mockResolvedValue([q1])
    renderPage()
    expect(await screen.findByRole('table', { name: 'Danh sách chi nhánh' })).toBeInTheDocument()
    expect(screen.getByText('AptechMart Quận 1')).toBeInTheDocument()
    expect(screen.getByText('123 Nguyễn Huệ, Q1')).toBeInTheDocument()
    expect(screen.getByText('Đang hoạt động')).toBeInTheDocument()
  })

  it('creates a new branch and reloads', async () => {
    const user = userEvent.setup()
    const list = vi.spyOn(adminApi, 'listBranches').mockResolvedValue([q1])
    const create = vi.spyOn(adminApi, 'createBranch').mockResolvedValue({ ...q1, id: 'b2', name: 'Thủ Đức' })
    renderPage()

    await user.click(await screen.findByRole('button', { name: '+ Tạo chi nhánh' }))
    const dialog = await screen.findByRole('dialog', { name: 'Tạo chi nhánh' })
    await user.type(within(dialog).getByLabelText('Tên chi nhánh'), 'AptechMart Thủ Đức')
    await user.type(within(dialog).getByLabelText('Địa chỉ'), '1 Võ Văn Ngân')
    await user.click(within(dialog).getByRole('button', { name: 'Tạo chi nhánh' }))

    expect(create).toHaveBeenCalledWith(
      { name: 'AptechMart Thủ Đức', address: '1 Võ Văn Ngân', phone: null },
      'jwt-token',
    )
    expect(list).toHaveBeenCalledTimes(2)
  })

  it('edits a branch and deactivates it', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'listBranches').mockResolvedValue([q1])
    const update = vi.spyOn(adminApi, 'updateBranch').mockResolvedValue({ ...q1, isActive: false })
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Sửa chi nhánh AptechMart Quận 1' }))
    const dialog = await screen.findByRole('dialog', { name: 'Sửa chi nhánh AptechMart Quận 1' })
    await user.click(within(dialog).getByLabelText(/Đang hoạt động/))
    await user.click(within(dialog).getByRole('button', { name: 'Lưu thay đổi' }))

    expect(update).toHaveBeenCalledWith(
      'b1',
      {
        name: 'AptechMart Quận 1',
        address: '123 Nguyễn Huệ, Q1',
        phone: '028 3822 1234',
        isActive: false,
      },
      'jwt-token',
    )
  })

  it('retries after a load failure', async () => {
    const spy = vi.spyOn(adminApi, 'listBranches')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce([q1])
    renderPage()
    await userEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByText('AptechMart Quận 1')).toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(2)
  })
})
