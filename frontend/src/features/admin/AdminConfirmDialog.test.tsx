import React, { useState } from 'react'
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

it('handles Escape key to cancel when not busy', () => {
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
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(onCancel).toHaveBeenCalledTimes(1)
})

it('does not trigger onCancel on Escape when isBusy is true', () => {
  const onCancel = vi.fn()
  render(
    <AdminConfirmDialog
      isOpen={true}
      title="Confirm"
      message="Are you sure?"
      confirmLabel="Yes"
      isBusy={true}
      onCancel={onCancel}
      onConfirm={() => {}}
    />
  )
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(onCancel).not.toHaveBeenCalled()
})

it('sets initial focus to cancel button when opened and returns focus when closed', () => {
  function TestWrapper() {
    const [open, setOpen] = useState(false)
    return (
      <div>
        <button data-testid="trigger-btn" onClick={() => setOpen(true)}>
          Open Dialog
        </button>
        <AdminConfirmDialog
          isOpen={open}
          title="Confirm"
          message="Are you sure?"
          confirmLabel="Yes"
          onCancel={() => setOpen(false)}
          onConfirm={() => setOpen(false)}
        />
      </div>
    )
  }

  render(<TestWrapper />)
  const triggerBtn = screen.getByTestId('trigger-btn')
  triggerBtn.focus()
  expect(document.activeElement).toBe(triggerBtn)

  fireEvent.click(triggerBtn)
  const cancelBtn = screen.getByRole('button', { name: 'Hủy' })
  expect(document.activeElement).toBe(cancelBtn)

  fireEvent.click(cancelBtn)
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  expect(document.activeElement).toBe(triggerBtn)
})

it('traps focus inside dialog on Tab and Shift+Tab', () => {
  render(
    <AdminConfirmDialog
      isOpen={true}
      title="Confirm"
      message="Are you sure?"
      confirmLabel="Confirm Action"
      onCancel={() => {}}
      onConfirm={() => {}}
    />
  )

  const cancelBtn = screen.getByRole('button', { name: 'Hủy' })
  const confirmBtn = screen.getByRole('button', { name: 'Confirm Action' })

  // Focus starts on cancelBtn
  cancelBtn.focus()
  expect(document.activeElement).toBe(cancelBtn)

  // Shift+Tab on first element wraps to last element
  fireEvent.keyDown(document, { key: 'Tab', shiftKey: true })
  expect(document.activeElement).toBe(confirmBtn)

  // Tab on last element wraps to first element
  fireEvent.keyDown(document, { key: 'Tab', shiftKey: false })
  expect(document.activeElement).toBe(cancelBtn)
})
