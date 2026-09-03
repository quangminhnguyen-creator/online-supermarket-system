import { useEffect, useRef, useState } from 'react'
import { inventoryIntelligenceApi, type InventoryTransactionDto } from '../../api/inventoryIntelligenceApi'
import { useAuth } from '../auth/AuthContext'
import './AdminInventoryPage.css'

interface Props {
  inventoryId: string
  productName: string
  onClose: () => void
}

function isAbortError(error: unknown) {
  return error instanceof Error && error.name === 'AbortError'
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function formatDelta(value: number) {
  return value > 0 ? `+${value}` : String(value)
}

const typeLabels: Record<string, string> = {
  Reserve: 'Đặt giữ',
  Release: 'Nhả giữ',
  Sale: 'Bán',
  ManualAdjustment: 'Điều chỉnh',
}

export function AdminInventoryTransactions({ inventoryId, productName, onClose }: Props) {
  const { accessToken } = useAuth()
  const [transactions, setTransactions] = useState<InventoryTransactionDto[] | null>(null)
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [retryKey, setRetryKey] = useState(0)
  const dialogRef = useRef<HTMLDivElement>(null)
  const closeButtonRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!accessToken) return
    const controller = new AbortController()
    setLoadState('loading')
    inventoryIntelligenceApi
      .getTransactions(inventoryId, { page: 1, pageSize: 20 }, accessToken, controller.signal)
      .then((result) => {
        setTransactions(result.data)
        setLoadState('ready')
      })
      .catch((error) => {
        if (!isAbortError(error)) {
          setTransactions(null)
          setLoadState('error')
        }
      })
    return () => controller.abort()
  }, [inventoryId, accessToken, retryKey])

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKey)
    closeButtonRef.current?.focus()
    dialogRef.current?.focus()
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="admin-modal-overlay" onClick={onClose}>
      <div
        ref={dialogRef}
        className="admin-modal admin-history-modal"
        role="dialog"
        aria-modal="true"
        aria-label="Lịch sử giao dịch kho"
        tabIndex={-1}
        onClick={(event) => event.stopPropagation()}
      >
        <header className="admin-modal-header">
          <h2>Lịch sử giao dịch kho</h2>
          <button ref={closeButtonRef} type="button" className="admin-modal-close" onClick={onClose} aria-label="Đóng">
            ×
          </button>
        </header>
        <p className="admin-modal-sku">{productName}</p>

        {loadState === 'error' && (
          <section className="admin-alert" role="alert">
            <p>Không thể tải lịch sử giao dịch. Vui lòng thử lại.</p>
            <button type="button" onClick={() => setRetryKey((prev) => prev + 1)}>Thử lại</button>
          </section>
        )}

        {loadState === 'loading' && <p className="admin-loading" aria-busy="true">Đang tải lịch sử...</p>}

        {loadState === 'ready' && transactions && transactions.length === 0 && (
          <div className="admin-empty"><p>Chưa có giao dịch nào cho sản phẩm này.</p></div>
        )}

        {loadState === 'ready' && transactions && transactions.length > 0 && (
          <div className="admin-table-wrap">
            <table className="admin-table" aria-label="Lịch sử giao dịch kho">
              <thead>
                <tr>
                  <th scope="col">Thời điểm</th>
                  <th scope="col">Loại</th>
                  <th scope="col">Tồn thực (±)</th>
                  <th scope="col">Đang giữ (±)</th>
                  <th scope="col">Sau</th>
                  <th scope="col">Nguồn</th>
                  <th scope="col">Lý do / Người thực hiện</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map((tx) => (
                  <tr key={tx.id}>
                    <td>{formatDateTime(tx.createdAtUtc)}</td>
                    <td>{typeLabels[tx.transactionType] ?? tx.transactionType}</td>
                    <td>{formatDelta(tx.quantityOnHandDelta)}</td>
                    <td>{formatDelta(tx.reservedQuantityDelta)}</td>
                    <td>{tx.quantityOnHandAfter}</td>
                    <td>{tx.referenceType}</td>
                    <td>{tx.note ?? tx.actorUserId ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}