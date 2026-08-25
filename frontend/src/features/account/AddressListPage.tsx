import React, { useState, useEffect, useCallback } from 'react'
import { AccountLayout } from './AccountLayout'
import { AddressCard } from './AddressCard'
import { AddressModal } from './AddressModal'
import { useAuth } from '../auth/AuthContext'
import {
  getAddressesApi,
  createAddressApi,
  updateAddressApi,
  deleteAddressApi,
  setDefaultAddressApi,
  type AddressDto,
  type CreateAddressRequest,
  type UpdateAddressRequest,
} from '../../api/addressApi'

export function AddressListPage() {
  const { accessToken } = useAuth()

  const [addresses, setAddresses] = useState<AddressDto[]>([])
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  // Modal states
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingAddress, setEditingAddress] = useState<AddressDto | null>(null)

  // Delete confirmation modal state
  const [addressToDelete, setAddressToDelete] = useState<AddressDto | null>(null)

  const fetchAddresses = useCallback(async () => {
    if (!accessToken) return
    setLoading(true)
    setError(null)
    try {
      const data = await getAddressesApi(accessToken)
      setAddresses(data)
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách địa chỉ. Vui lòng thử lại.')
    } finally {
      setLoading(false)
    }
  }, [accessToken])

  useEffect(() => {
    fetchAddresses()
  }, [fetchAddresses])

  const handleOpenAddModal = () => {
    setEditingAddress(null)
    setIsModalOpen(true)
  }

  const handleOpenEditModal = (address: AddressDto) => {
    setEditingAddress(address)
    setIsModalOpen(true)
  }

  const handleSaveAddress = async (data: CreateAddressRequest | UpdateAddressRequest) => {
    if (!accessToken) return
    setSuccess(null)
    setError(null)

    if (editingAddress) {
      // Update
      await updateAddressApi(editingAddress.id, data as UpdateAddressRequest, accessToken)
      setSuccess('Cập nhật địa chỉ nhận hàng thành công!')
    } else {
      // Create
      await createAddressApi(data as CreateAddressRequest, accessToken)
      setSuccess('Thêm mới địa chỉ nhận hàng thành công!')
    }

    await fetchAddresses()
  }

  const handleSetDefault = async (address: AddressDto) => {
    if (!accessToken || address.isDefault) return
    setActionLoading(true)
    setSuccess(null)
    setError(null)
    try {
      await setDefaultAddressApi(address.id, accessToken)
      setSuccess(`Đã đặt "${address.recipientName} - ${address.street}" làm địa chỉ mặc định!`)
      await fetchAddresses()
    } catch (err: any) {
      setError(err.message || 'Không thể đặt làm địa chỉ mặc định.')
    } finally {
      setActionLoading(false)
    }
  }

  const handleConfirmDelete = async () => {
    if (!accessToken || !addressToDelete) return
    setActionLoading(true)
    setSuccess(null)
    setError(null)
    try {
      await deleteAddressApi(addressToDelete.id, accessToken)
      setSuccess(`Đã xóa địa chỉ của "${addressToDelete.recipientName}" thành công!`)
      setAddressToDelete(null)
      await fetchAddresses()
    } catch (err: any) {
      setError(err.message || 'Không thể xóa địa chỉ.')
    } finally {
      setActionLoading(false)
    }
  }

  return (
    <AccountLayout>
      <div className="address-management">
        <div className="address-management__header">
          <div>
            <h2 className="address-management__title">
              <span className="card-icon">📍</span> Sổ Địa Chỉ Nhận Hàng
            </h2>
            <p className="address-management__subtitle">
              Quản lý các địa chỉ nhận hàng của bạn để thanh toán thuận tiện hơn.
            </p>
          </div>
          <button
            type="button"
            className="btn-add-address"
            onClick={handleOpenAddModal}
            data-testid="btn-open-add-modal"
          >
            ➕ Thêm Địa Chỉ Mới
          </button>
        </div>

        {success && (
          <div className="alert alert--success" role="alert">
            <span>✓</span> {success}
          </div>
        )}

        {error && (
          <div className="alert alert--error" role="alert">
            <span>⚠</span> {error}
          </div>
        )}

        {loading ? (
          <div className="address-loading" data-testid="address-loading">
            <span className="spinner" />
            <p>Đang tải danh sách địa chỉ...</p>
          </div>
        ) : addresses.length === 0 ? (
          <div className="address-empty-card" data-testid="address-empty">
            <div className="address-empty-icon">📫</div>
            <h3>Chưa Có Địa Chỉ Giao Hàng Nào</h3>
            <p>Bạn chưa lưu địa chỉ giao hàng nào trong sổ địa chỉ.</p>
            <button
              type="button"
              className="btn-primary"
              onClick={handleOpenAddModal}
            >
              Thêm Địa Chỉ Đầu Tiên
            </button>
          </div>
        ) : (
          <div className="address-grid" data-testid="address-grid">
            {addresses.map((address) => (
              <AddressCard
                key={address.id}
                address={address}
                onEdit={handleOpenEditModal}
                onDelete={(addr) => setAddressToDelete(addr)}
                onSetDefault={handleSetDefault}
                isActionLoading={actionLoading}
              />
            ))}
          </div>
        )}

        {/* Modal Thêm/Sửa */}
        <AddressModal
          isOpen={isModalOpen}
          initialData={editingAddress}
          onClose={() => setIsModalOpen(false)}
          onSave={handleSaveAddress}
        />

        {/* Modal Xác nhận Xóa */}
        {addressToDelete && (
          <div className="auth-modal-overlay" role="dialog" aria-modal="true">
            <div className="auth-modal delete-confirm-modal">
              <div className="auth-modal__header">
                <h2>🗑️ Xác Nhận Xóa Địa Chỉ</h2>
                <button
                  type="button"
                  className="auth-modal__close"
                  onClick={() => setAddressToDelete(null)}
                >
                  &times;
                </button>
              </div>
              <div className="delete-confirm-body">
                <p>
                  Bạn có chắc chắn muốn xóa địa chỉ nhận hàng của{' '}
                  <strong>{addressToDelete.recipientName}</strong> (
                  {addressToDelete.street}, {addressToDelete.ward},{' '}
                  {addressToDelete.district}, {addressToDelete.city}) không?
                </p>
                <p className="delete-warning-note">Hành động này không thể hoàn tác.</p>
              </div>
              <div className="modal-actions">
                <button
                  type="button"
                  className="btn-cancel"
                  onClick={() => setAddressToDelete(null)}
                  disabled={actionLoading}
                >
                  Hủy Bỏ
                </button>
                <button
                  type="button"
                  className="btn-danger"
                  onClick={handleConfirmDelete}
                  disabled={actionLoading}
                  data-testid="btn-confirm-delete"
                >
                  {actionLoading ? 'Đang xóa...' : 'Xóa Địa Chỉ'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </AccountLayout>
  )
}
