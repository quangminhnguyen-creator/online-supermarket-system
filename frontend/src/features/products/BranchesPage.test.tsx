import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { BranchesPage } from './BranchesPage'
import { branchApi, type BranchDto } from '../../api/branchApi'
import { ApiError } from '../../api/httpClient'

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

const branches: BranchDto[] = [
  { id: 'b1', name: 'AptechMart Quận 1', address: '123 Nguyễn Huệ, Q1', phone: '028 3822 1234', latitude: null, longitude: null, isActive: true },
  { id: 'b2', name: 'AptechMart Quận 3', address: '456 CMT8, Q3', phone: null, latitude: null, longitude: null, isActive: true },
]

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/branches']}>
      <BranchesPage />
    </MemoryRouter>,
  )
}

describe('BranchesPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockNavigate.mockReset()
  })

  it('loads and lists branches', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    renderPage()
    expect(await screen.findByRole('heading', { level: 1, name: 'Hệ thống chi nhánh' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'AptechMart Quận 1' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'AptechMart Quận 3' })).toBeInTheDocument()
  })

  it('navigates to the branch-filtered browse page when a branch is chosen', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    renderPage()
    await userEvent.click(await screen.findByRole('button', { name: 'Xem sản phẩm AptechMart Quận 1' }))
    expect(mockNavigate).toHaveBeenCalledWith('/browse?branchId=b1')
  })

  it('shows an error state and retries', async () => {
    const spy = vi.spyOn(branchApi, 'getBranches')
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(branches)
    renderPage()
    await userEvent.click(await screen.findByRole('button', { name: 'Thử lại' }))
    expect(await screen.findByRole('heading', { name: 'AptechMart Quận 1' })).toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(2)
  })
})
