import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { branchApi, type BranchDto } from '../../api/branchApi'
import { BranchList } from './BranchList'
import './BranchesPage.css'

function isAbortError(error: unknown) {
  return error instanceof Error && error.name === 'AbortError'
}

export function BranchesPage() {
  const navigate = useNavigate()
  const [branches, setBranches] = useState<BranchDto[] | null>(null)
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setLoadState('loading')
    branchApi
      .getBranches({ signal: controller.signal })
      .then((list) => {
        setBranches(list)
        setLoadState('ready')
      })
      .catch((error) => {
        if (!isAbortError(error)) {
          setBranches(null)
          setLoadState('error')
        }
      })
    return () => controller.abort()
  }, [retryKey])

  function handleSelect(branchId: string) {
    navigate(`/browse?branchId=${encodeURIComponent(branchId)}`)
  }

  return (
    <section className="branches-page" aria-label="Hệ thống chi nhánh">
      <header className="branches-page__header">
        <h1 className="branches-page__title">Hệ thống chi nhánh</h1>
        <p className="branches-page__lead">
          Chọn một chi nhánh để xem sản phẩm, giá bán và tồn kho tại nơi bạn muốn mua.
        </p>
      </header>

      {loadState === 'loading' && (
        <p className="branches-page__status" aria-busy="true">Đang tải chi nhánh...</p>
      )}

      {loadState === 'error' && (
        <div className="branches-page__status" role="alert">
          <p>Không thể tải danh sách chi nhánh. Vui lòng thử lại.</p>
          <button type="button" onClick={() => setRetryKey((k) => k + 1)}>Thử lại</button>
        </div>
      )}

      {loadState === 'ready' && branches && (
        <BranchList branches={branches} onSelect={handleSelect} />
      )}
    </section>
  )
}
