import React, { useState, useEffect } from 'react'
import { AccountLayout } from './AccountLayout'
import { useAuth } from '../auth/AuthContext'
import { updateProfileApi, changePasswordApi } from '../../api/userApi'

export function ProfilePage() {
  const { user, accessToken, updateUser } = useAuth()

  // Profile Form state
  const [fullName, setFullName] = useState(user?.fullName || '')
  const [phone, setPhone] = useState(user?.phone || '')
  const [profileLoading, setProfileLoading] = useState(false)
  const [profileSuccess, setProfileSuccess] = useState<string | null>(null)
  const [profileError, setProfileError] = useState<string | null>(null)

  // Password Form state
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordLoading, setPasswordLoading] = useState(false)
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)

  useEffect(() => {
    if (user) {
      setFullName(user.fullName)
      setPhone(user.phone || '')
    }
  }, [user])

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!accessToken) return

    setProfileLoading(true)
    setProfileSuccess(null)
    setProfileError(null)

    try {
      const res = await updateProfileApi(
        {
          fullName: fullName.trim(),
          phone: phone.trim() || null,
        },
        accessToken
      )

      updateUser({
        fullName: fullName.trim(),
        phone: phone.trim() || null,
      })

      setProfileSuccess(res.message || 'Cập nhật thông tin hồ sơ thành công!')
    } catch (err: any) {
      setProfileError(err.message || 'Không thể cập nhật hồ sơ. Vui lòng thử lại.')
    } finally {
      setProfileLoading(false)
    }
  }

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!accessToken) return

    setPasswordSuccess(null)
    setPasswordError(null)

    if (newPassword.length < 6) {
      setPasswordError('Mật khẩu mới phải có ít nhất 6 ký tự.')
      return
    }

    if (newPassword !== confirmPassword) {
      setPasswordError('Mật khẩu xác nhận không khớp.')
      return
    }

    setPasswordLoading(true)

    try {
      const res = await changePasswordApi(
        {
          currentPassword,
          newPassword,
        },
        accessToken
      )

      setPasswordSuccess(res.message || 'Đổi mật khẩu thành công!')
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (err: any) {
      setPasswordError(err.message || 'Đổi mật khẩu thất bại. Vui lòng kiểm tra lại mật khẩu hiện tại.')
    } finally {
      setPasswordLoading(false)
    }
  }

  return (
    <AccountLayout>
      <div className="profile-grid">
        {/* Profile Card */}
        <section className="account-card" aria-labelledby="profile-heading">
          <div className="account-card-header">
            <h2 id="profile-heading" className="account-card-title">
              <span className="card-icon">📝</span> Thông Tin Cá Nhân
            </h2>
            <p className="account-card-desc">Cập nhật họ và tên và số điện thoại liên hệ của bạn.</p>
          </div>

          {profileSuccess && (
            <div className="alert alert--success" role="alert">
              <span>✓</span> {profileSuccess}
            </div>
          )}

          {profileError && (
            <div className="alert alert--error" role="alert">
              <span>⚠</span> {profileError}
            </div>
          )}

          <form onSubmit={handleUpdateProfile} className="account-form" data-testid="profile-form">
            <div className="form-group">
              <label htmlFor="profile-email">Địa chỉ Email (Bất biến)</label>
              <input
                id="profile-email"
                type="email"
                value={user?.email || ''}
                disabled
                className="input-field input-field--disabled"
                title="Email đăng nhập không thể thay đổi"
              />
              <small className="form-help">Email dùng để đăng nhập và nhận thông báo đơn hàng.</small>
            </div>

            <div className="form-group">
              <label htmlFor="profile-fullname">Họ và tên *</label>
              <input
                id="profile-fullname"
                type="text"
                required
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="VD: Nguyễn Văn An"
                className="input-field"
                data-testid="input-fullname"
              />
            </div>

            <div className="form-group">
              <label htmlFor="profile-phone">Số điện thoại liên hệ</label>
              <input
                id="profile-phone"
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="VD: 0912345678"
                className="input-field"
                data-testid="input-phone"
              />
            </div>

            <div className="form-actions">
              <button
                type="submit"
                disabled={profileLoading}
                className="btn-primary"
                data-testid="btn-save-profile"
              >
                {profileLoading ? 'Đang lưu...' : 'Lưu Thay Đổi'}
              </button>
            </div>
          </form>
        </section>

        {/* Change Password Card */}
        <section className="account-card" aria-labelledby="password-heading">
          <div className="account-card-header">
            <h2 id="password-heading" className="account-card-title">
              <span className="card-icon">🔑</span> Đổi Mật Khẩu
            </h2>
            <p className="account-card-desc">Bảo vệ tài khoản bằng cách sử dụng mật khẩu mạnh.</p>
          </div>

          {passwordSuccess && (
            <div className="alert alert--success" role="alert">
              <span>✓</span> {passwordSuccess}
            </div>
          )}

          {passwordError && (
            <div className="alert alert--error" role="alert">
              <span>⚠</span> {passwordError}
            </div>
          )}

          <form onSubmit={handleChangePassword} className="account-form" data-testid="password-form">
            <div className="form-group">
              <label htmlFor="current-password">Mật khẩu hiện tại *</label>
              <input
                id="current-password"
                type="password"
                required
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                placeholder="Nhập mật khẩu hiện tại"
                className="input-field"
                data-testid="input-current-password"
              />
            </div>

            <div className="form-group">
              <label htmlFor="new-password">Mật khẩu mới *</label>
              <input
                id="new-password"
                type="password"
                required
                minLength={6}
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="Tối thiểu 6 ký tự"
                className="input-field"
                data-testid="input-new-password"
              />
            </div>

            <div className="form-group">
              <label htmlFor="confirm-password">Xác nhận mật khẩu mới *</label>
              <input
                id="confirm-password"
                type="password"
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Nhập lại mật khẩu mới"
                className="input-field"
                data-testid="input-confirm-password"
              />
            </div>

            <div className="form-actions">
              <button
                type="submit"
                disabled={passwordLoading}
                className="btn-secondary-action"
                data-testid="btn-change-password"
              >
                {passwordLoading ? 'Đang cập nhật...' : 'Cập Nhật Mật Khẩu'}
              </button>
            </div>
          </form>
        </section>
      </div>
    </AccountLayout>
  )
}
