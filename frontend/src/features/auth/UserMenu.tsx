import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from './AuthContext'
import { AuthModal } from './AuthModal'

export function UserMenu() {
  const { user, isAuthenticated, isLoading, logout } = useAuth()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [modalMode, setModalMode] = useState<'login' | 'register'>('login')

  const openAuth = (mode: 'login' | 'register') => {
    setModalMode(mode)
    setIsModalOpen(true)
  }

  if (isLoading) {
    return <div className="user-menu user-menu--loading"><span className="spinner" /></div>
  }

  if (isAuthenticated && user) {
    const initials = user.fullName
      .split(' ')
      .filter(Boolean)
      .map((part) => part[0])
      .join('')
      .slice(0, 2)
      .toUpperCase()

    return (
      <div className="user-menu user-menu--authenticated">
        <Link to="/account/profile" className="user-profile-link" title="Quản lý hồ sơ cá nhân">
          <div className="user-profile">
            <div className="user-avatar" title={user.fullName}>
              {initials || 'U'}
            </div>
            <div className="user-info">
              <span className="user-name">{user.fullName}</span>
              <span className="user-role-badge">
                {user.role === 'Admin' ? 'Quản trị viên' : 'Khách hàng'}
              </span>
            </div>
          </div>
        </Link>
        <Link to="/account/addresses" className="btn-nav-address" title="Sổ địa chỉ giao hàng">
          📍 Địa chỉ
        </Link>
        <Link to="/orders/history" className="btn-nav-orders" title="Lịch sử đơn hàng">
          Đơn hàng
        </Link>
        {user.role === 'Admin' && (
          <Link to="/admin/orders" className="btn-nav-admin" title="Khu vực quản trị">
            ⚙️ Quản trị
          </Link>
        )}
        <button
          type="button"
          className="btn-logout"
          onClick={() => logout()}
          title="Đăng xuất khỏi hệ thống"
        >
          Đăng xuất
        </button>
      </div>
    )
  }

  return (
    <>
      <div className="user-menu user-menu--guest">
        <button
          type="button"
          className="btn-login"
          onClick={() => openAuth('login')}
        >
          Đăng nhập
        </button>
        <button
          type="button"
          className="btn-register"
          onClick={() => openAuth('register')}
        >
          Đăng ký
        </button>
      </div>

      <AuthModal
        isOpen={isModalOpen}
        initialMode={modalMode}
        onClose={() => setIsModalOpen(false)}
      />
    </>
  )
}
