import React, { useEffect, useRef } from 'react'

interface Props {
  isOpen: boolean
  title: string
  message: string
  confirmLabel: string
  isBusy?: boolean
  onCancel: () => void
  onConfirm: () => void
}

export function AdminConfirmDialog({
  isOpen,
  title,
  message,
  confirmLabel,
  isBusy,
  onCancel,
  onConfirm,
}: Props) {
  const dialogRef = useRef<HTMLDivElement>(null)
  const cancelBtnRef = useRef<HTMLButtonElement>(null)
  const previousFocusRef = useRef<HTMLElement | null>(null)

  useEffect(() => {
    if (isOpen) {
      previousFocusRef.current = document.activeElement as HTMLElement
      // Immediate and next-tick focus for test and browser compatibility
      cancelBtnRef.current?.focus()
      const timer = setTimeout(() => {
        cancelBtnRef.current?.focus()
      }, 0)
      return () => clearTimeout(timer)
    } else {
      previousFocusRef.current?.focus()
    }
  }, [isOpen])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!isOpen) return

      if (e.key === 'Escape' && !isBusy) {
        e.preventDefault()
        onCancel()
        return
      }

      if (e.key === 'Tab' && dialogRef.current) {
        const focusableElements = dialogRef.current.querySelectorAll<HTMLElement>(
          'button:not([disabled]), [tabindex]:not([tabindex="-1"])'
        )
        if (focusableElements.length === 0) return

        const firstElement = focusableElements[0]
        const lastElement = focusableElements[focusableElements.length - 1]

        if (e.shiftKey) {
          if (document.activeElement === firstElement) {
            e.preventDefault()
            lastElement.focus()
          }
        } else {
          if (document.activeElement === lastElement) {
            e.preventDefault()
            firstElement.focus()
          }
        }
      }
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, isBusy, onCancel])

  if (!isOpen) return null

  return (
    <div className="admin-dialog-overlay">
      <div
        ref={dialogRef}
        className="admin-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="dialog-title"
        aria-describedby="dialog-desc"
      >
        <h2 id="dialog-title">{title}</h2>
        <p id="dialog-desc">{message}</p>
        <div className="admin-dialog-actions">
          <button ref={cancelBtnRef} onClick={onCancel} disabled={isBusy}>
            Hủy
          </button>
          <button className="destructive" onClick={onConfirm} disabled={isBusy}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
