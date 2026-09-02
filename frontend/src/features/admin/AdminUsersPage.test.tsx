import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminUsersPage } from './AdminUsersPage'
import { adminApi, type PaginatedUsersDto } from '../../api/adminApi'
import { ApiError } from '../../api/httpClient'

const mockAuth = { accessToken: 'jwt-token' as string | null }

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => mockAuth,
}))

const usersResponse: PaginatedUsersDto = {
  page: 1,
  pageSize: 20,
  totalCount: 1,
  users: [
    {
      id: 'u1',
      email: 'khach@test.com',
      fullName: 'Nguyễn Văn A',
      phone: '0900000000',
      role: 'Customer',
      status: 'Active',
      createdAtUtc: '2026-08-01T00:00:00Z',
      updatedAtUtc: '2026-08-01T00:00:00Z',
    },
  ],
}

function renderPage(url = '/admin/users') {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <AdminUsersPage />
    </MemoryRouter>,
  )
}

describe('AdminUsersPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockAuth.accessToken = 'jwt-token'
  })

  it('renders users from the admin API', async () => {
    vi.spyOn(adminApi, 'listUsers').mockResolvedValue(usersResponse)
    renderPage()
    expect(await screen.findByRole('table', { name: 'Danh sách người dùng' })).toBeInTheDocument()
    expect(screen.getByText('khach@test.com')).toBeInTheDocument()
    expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument()
    expect(screen.getByText('Khách hàng')).toBeInTheDocument()
    expect(adminApi.listUsers).toHaveBeenCalledWith(1, 20, 'jwt-token', expect.any(AbortSignal))
  })

  it('confirms and applies a status change', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'listUsers').mockResolvedValue(usersResponse)
    const update = vi.spyOn(adminApi, 'updateUserStatus').mockResolvedValue({ message: 'ok' })
    renderPage()

    await user.selectOptions(await screen.findByLabelText('Đổi trạng thái khach@test.com'), 'Locked')
    const dialog = await screen.findByRole('dialog', { name: 'Xác nhận đổi trạng thái' })
    expect(within(dialog).getByText(/Đã khóa/)).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Xác nhận' }))
    expect(update).toHaveBeenCalledWith('u1', 'Locked', 'jwt-token')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    // Row select now reflects the new status
    expect(screen.getByLabelText('Đổi trạng thái khach@test.com')).toHaveValue('Locked')
  })

  it('cancels a status change without calling the API', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'listUsers').mockResolvedValue(usersResponse)
    const update = vi.spyOn(adminApi, 'updateUserStatus')
    renderPage()

    await user.selectOptions(await screen.findByLabelText('Đổi trạng thái khach@test.com'), 'Disabled')
    const dialog = await screen.findByRole('dialog', { name: 'Xác nhận đổi trạng thái' })
    await user.click(within(dialog).getByRole('button', { name: 'Hủy' }))

    expect(update).not.toHaveBeenCalled()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Đổi trạng thái khach@test.com')).toHaveValue('Active')
  })

  it('shows the backend message when a status update fails', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminApi, 'listUsers').mockResolvedValue(usersResponse)
    vi.spyOn(adminApi, 'updateUserStatus').mockRejectedValue(
      new ApiError(400, { message: 'Invalid status.' }, 'Invalid status.'),
    )
    renderPage()

    await user.selectOptions(await screen.findByLabelText('Đổi trạng thái khach@test.com'), 'Locked')
    const dialog = await screen.findByRole('dialog', { name: 'Xác nhận đổi trạng thái' })
    await user.click(within(dialog).getByRole('button', { name: 'Xác nhận' }))
    expect(await within(dialog).findByText('Invalid status.')).toBeInTheDocument()
  })

  it('retries after a load failure', async () => {
    const spy = vi.spyOn(adminApi, 'listUsers')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(usersResponse)
    renderPage()
    await userEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByText('khach@test.com')).toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(2)
  })
})
