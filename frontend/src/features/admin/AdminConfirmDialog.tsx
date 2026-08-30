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
  const cancelBtnRef = useRef<HTMLButtonElement>(null)
  const previousFocusRef = useRef<HTMLElement | null>(null)

  useEffect(() => {
    if (isOpen) {
      previousFocusRef.current = document.activeElement as HTMLElement
      cancelBtnRef.current?.focus()
    } else {
      previousFocusRef.current?.focus()
    }
  }, [isOpen])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen && !isBusy) {
        onCancel()
      }
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, isBusy, onCancel])

  if (!isOpen) return null

  return (
    <div className="admin-dialog-overlay">
      <div
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
