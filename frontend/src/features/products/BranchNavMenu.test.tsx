import { render, screen, fireEvent } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { BranchNavMenu } from './BranchNavMenu'
import { branchApi, type BranchDto } from '../../api/branchApi'

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

const branches: BranchDto[] = [
  { id: 'b1', name: 'AptechMart Quận 1', address: '123 Nguyễn Huệ, Q1', phone: '028 3822 1234', latitude: null, longitude: null, isActive: true },
  { id: 'b2', name: 'AptechMart Quận 3', address: '456 CMT8, Q3', phone: null, latitude: null, longitude: null, isActive: true },
]

function renderMenu() {
  return render(
    <MemoryRouter>
      <BranchNavMenu />
    </MemoryRouter>,
  )
}

describe('BranchNavMenu', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    mockNavigate.mockReset()
  })

  it('renders a "Chi nhánh" label linking to the branches page', () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue([])
    renderMenu()
    expect(screen.getByRole('link', { name: 'Chi nhánh' })).toHaveAttribute('href', '/branches')
  })

  it('lists branches loaded from the API', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    renderMenu()
    expect(await screen.findByRole('menuitem', { name: /AptechMart Quận 1/ })).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: /AptechMart Quận 3/ })).toBeInTheDocument()
  })

  it('navigates to the branch-filtered products when an item is clicked', async () => {
    vi.spyOn(branchApi, 'getBranches').mockResolvedValue(branches)
    renderMenu()
    fireEvent.click(await screen.findByRole('menuitem', { name: /AptechMart Quận 1/ }))
    expect(mockNavigate).toHaveBeenCalledWith('/browse?branchId=b1')
  })
})
