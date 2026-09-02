import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { BranchList } from './BranchList'
import type { BranchDto } from '../../api/branchApi'

const branches: BranchDto[] = [
  { id: 'b1', name: 'AptechMart Quận 1', address: '123 Nguyễn Huệ, Q1', phone: '028 3822 1234', latitude: null, longitude: null, isActive: true },
  { id: 'b2', name: 'AptechMart Quận 3', address: '456 CMT8, Q3', phone: null, latitude: null, longitude: null, isActive: true },
]

describe('BranchList', () => {
  it('renders branch cards with name, address and phone', () => {
    render(<BranchList branches={branches} onSelect={vi.fn()} />)
    expect(screen.getByRole('heading', { name: 'AptechMart Quận 1' })).toBeInTheDocument()
    expect(screen.getByText('123 Nguyễn Huệ, Q1')).toBeInTheDocument()
    expect(screen.getByText('☎ 028 3822 1234')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'AptechMart Quận 3' })).toBeInTheDocument()
  })

  it('calls onSelect with the branch id when the CTA is clicked', async () => {
    const onSelect = vi.fn()
    render(<BranchList branches={branches} onSelect={onSelect} />)
    await userEvent.click(screen.getByRole('button', { name: 'Xem sản phẩm AptechMart Quận 1' }))
    expect(onSelect).toHaveBeenCalledWith('b1')
  })

  it('shows a placeholder when there are no branches', () => {
    render(<BranchList branches={[]} onSelect={vi.fn()} />)
    expect(screen.getByText('Đang cập nhật danh sách chi nhánh...')).toBeInTheDocument()
  })
})
