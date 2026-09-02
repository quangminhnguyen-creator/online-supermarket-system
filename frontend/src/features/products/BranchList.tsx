import type { BranchDto } from '../../api/branchApi'
import './BranchList.css'

interface Props {
  branches: BranchDto[]
  onSelect: (branchId: string) => void
}

export function BranchList({ branches, onSelect }: Props) {
  if (branches.length === 0) {
    return <p className="branch-list__empty">Đang cập nhật danh sách chi nhánh...</p>
  }

  return (
    <ul className="branch-tiles">
      {branches.map((branch) => (
        <li key={branch.id} className="branch-tile">
          <div className="branch-tile__head">
            <h3 className="branch-tile__name">{branch.name}</h3>
            {branch.isActive && <span className="branch-tile__badge">Đang hoạt động</span>}
          </div>
          <p className="branch-tile__addr">{branch.address}</p>
          {branch.phone && <p className="branch-tile__phone">☎ {branch.phone}</p>}
          <button
            type="button"
            className="branch-tile__cta"
            onClick={() => onSelect(branch.id)}
            aria-label={`Xem sản phẩm ${branch.name}`}
          >
            Xem sản phẩm
          </button>
        </li>
      ))}
    </ul>
  )
}
