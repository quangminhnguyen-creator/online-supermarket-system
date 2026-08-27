import { useEffect, useRef } from 'react'

export interface BranchChangeConfirmDialogProps {
  isOpen: boolean
  isBusy?: boolean
  returnFocusRef?: React.RefObject<HTMLElement | null>
  onCancel: () => void
  onConfirm: () => void | Promise<void>
}

export function BranchChangeConfirmDialog(props: BranchChangeConfirmDialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!props.isOpen) return
    cancelRef.current?.focus()
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !props.isBusy) props.onCancel()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => {
      window.removeEventListener('keydown', onKeyDown)
      props.returnFocusRef?.current?.focus()
    }
  }, [props.isBusy, props.isOpen, props.onCancel, props.returnFocusRef])

  if (!props.isOpen) return null

  return (
    <div className="cart-dialog-overlay">
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="branch-dialog-title"
        aria-describedby="branch-dialog-description"
        className="cart-dialog"
      >
        <h2 id="branch-dialog-title">Đổi kho và xóa giỏ hiện tại?</h2>
        <p id="branch-dialog-description">
          Đổi kho sẽ xóa toàn bộ sản phẩm đang có trong giỏ.
        </p>
        <div className="cart-dialog__actions">
          <button
            ref={cancelRef}
            type="button"
            className="cart-dialog__cancel-btn"
            disabled={props.isBusy}
            onClick={props.onCancel}
          >
            Giữ giỏ hiện tại
          </button>
          <button
            type="button"
            className="cart-dialog__confirm-btn"
            disabled={props.isBusy}
            onClick={() => void props.onConfirm()}
          >
            Đổi kho và xóa giỏ
          </button>
        </div>
      </section>
    </div>
  )
}
