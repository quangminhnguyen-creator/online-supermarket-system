import { render, screen, fireEvent } from '@testing-library/react'
import { expect, it, vi } from 'vitest'
import { AdminConfirmDialog } from './AdminConfirmDialog'

it('renders nothing when not open', () => {
  render(
    <AdminConfirmDialog
      isOpen={false}
      title="Confirm"
      message="Are you sure?"
      confirmLabel="Yes"
      onCancel={() => {}}
      onConfirm={() => {}}
    />
  )
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
})

it('renders dialog when open', () => {
  render(
    <AdminConfirmDialog
      isOpen={true}
      title="Confirm Action"
      message="Are you very sure?"
      confirmLabel="Do it"
      onCancel={() => {}}
      onConfirm={() => {}}
    />
  )
  expect(screen.getByRole('dialog')).toBeInTheDocument()
  expect(screen.getByText('Confirm Action')).toBeInTheDocument()
  expect(screen.getByText('Are you very sure?')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Hủy' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Do it' })).toBeInTheDocument()
})

it('calls onCancel when Cancel is clicked', () => {
  const onCancel = vi.fn()
  render(
    <AdminConfirmDialog
      isOpen={true}
      title="Confirm"
      message="Are you sure?"
      confirmLabel="Yes"
      onCancel={onCancel}
      onConfirm={() => {}}
    />
  )
  fireEvent.click(screen.getByRole('button', { name: 'Hủy' }))
  expect(onCancel).toHaveBeenCalled()
})

it('calls onConfirm when Confirm is clicked', () => {
  const onConfirm = vi.fn()
  render(
    <AdminConfirmDialog
      isOpen={true}
      title="Confirm"
      message="Are you sure?"
      confirmLabel="Yes"
      onCancel={() => {}}
      onConfirm={onConfirm}
    />
  )
  fireEvent.click(screen.getByRole('button', { name: 'Yes' }))
  expect(onConfirm).toHaveBeenCalled()
})

it('disables buttons when isBusy is true', () => {
  render(
    <AdminConfirmDialog
      isOpen={true}
      title="Confirm"
      message="Are you sure?"
      confirmLabel="Yes"
      isBusy={true}
      onCancel={() => {}}
      onConfirm={() => {}}
    />
  )
  expect(screen.getByRole('button', { name: 'Hủy' })).toBeDisabled()
  expect(screen.getByRole('button', { name: 'Yes' })).toBeDisabled()
})
