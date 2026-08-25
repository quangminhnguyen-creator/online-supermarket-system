import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { ProfilePage } from './ProfilePage'
import { AddressListPage } from './AddressListPage'
import { AuthProvider } from '../auth/AuthContext'
import * as userApi from '../../api/userApi'
import * as addressApi from '../../api/addressApi'
import * as authApi from '../../api/authApi'

vi.mock('../../api/userApi')
vi.mock('../../api/addressApi')
vi.mock('../../api/authApi')

const mockUser: authApi.UserDto = {
  id: '78c7ed92-49be-45d9-a88b-31dc45a3e7c6',
  email: 'user1@test.com',
  fullName: 'Nguyen Van An',
  phone: '0912345678',
  role: 'Customer',
}

const mockAddresses: addressApi.AddressDto[] = [
  {
    id: 'addr-1',
    recipientName: 'Nguyen Van An',
    phone: '0912345678',
    street: '45 Lê Lai',
    ward: 'Bến Thành',
    district: 'Quận 1',
    city: 'TP.HCM',
    postalCode: '700000',
    isDefault: true,
    createdAtUtc: '2026-08-20T10:00:00Z',
    updatedAtUtc: '2026-08-20T10:00:00Z',
  },
  {
    id: 'addr-2',
    recipientName: 'Nguyen Van An (Công ty)',
    phone: '0912345678',
    street: '78 Nguyễn Trãi',
    ward: 'Phường 2',
    district: 'Quận 5',
    city: 'TP.HCM',
    postalCode: null,
    isDefault: false,
    createdAtUtc: '2026-08-21T10:00:00Z',
    updatedAtUtc: '2026-08-21T10:00:00Z',
  },
]

describe('Hồ Sơ & Sổ Địa Chỉ (Profile & Address Management)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.setItem('os_access_token', 'valid-test-token')
    localStorage.setItem('os_refresh_token', 'valid-refresh-token')
    vi.mocked(authApi.getMeApi).mockResolvedValue(mockUser)
  })

  describe('ProfilePage', () => {
    it('renders user profile details and form fields', async () => {
      render(
        <MemoryRouter>
          <AuthProvider>
            <ProfilePage />
          </AuthProvider>
        </MemoryRouter>
      )

      await waitFor(() => {
        expect(screen.getByTestId('input-fullname')).toHaveValue('Nguyen Van An')
        expect(screen.getByTestId('input-phone')).toHaveValue('0912345678')
        expect(screen.getByLabelText(/Địa chỉ Email/i)).toHaveValue('user1@test.com')
      })
    })

    it('submits updated profile and displays success message', async () => {
      vi.mocked(userApi.updateProfileApi).mockResolvedValue({
        message: 'Cập nhật thông tin hồ sơ thành công!',
      })

      render(
        <MemoryRouter>
          <AuthProvider>
            <ProfilePage />
          </AuthProvider>
        </MemoryRouter>
      )

      await waitFor(() => {
        expect(screen.getByTestId('input-fullname')).toHaveValue('Nguyen Van An')
      })

      fireEvent.change(screen.getByTestId('input-fullname'), {
        target: { value: 'Nguyen Van An Updated' },
      })
      fireEvent.change(screen.getByTestId('input-phone'), {
        target: { value: '0988776655' },
      })

      fireEvent.click(screen.getByTestId('btn-save-profile'))

      await waitFor(() => {
        expect(userApi.updateProfileApi).toHaveBeenCalledWith(
          { fullName: 'Nguyen Van An Updated', phone: '0988776655' },
          'valid-test-token'
        )
        expect(screen.getByText(/Cập nhật thông tin hồ sơ thành công!/i)).toBeInTheDocument()
      })
    })

    it('validates and submits password change', async () => {
      vi.mocked(userApi.changePasswordApi).mockResolvedValue({
        message: 'Đổi mật khẩu thành công!',
      })

      render(
        <MemoryRouter>
          <AuthProvider>
            <ProfilePage />
          </AuthProvider>
        </MemoryRouter>
      )

      await waitFor(() => {
        expect(screen.getByTestId('input-current-password')).toBeInTheDocument()
      })

      fireEvent.change(screen.getByTestId('input-current-password'), {
        target: { value: 'Test@123' },
      })
      fireEvent.change(screen.getByTestId('input-new-password'), {
        target: { value: 'NewPass@456' },
      })
      fireEvent.change(screen.getByTestId('input-confirm-password'), {
        target: { value: 'NewPass@456' },
      })

      fireEvent.click(screen.getByTestId('btn-change-password'))

      await waitFor(() => {
        expect(userApi.changePasswordApi).toHaveBeenCalledWith(
          { currentPassword: 'Test@123', newPassword: 'NewPass@456' },
          'valid-test-token'
        )
        expect(screen.getByText(/Đổi mật khẩu thành công!/i)).toBeInTheDocument()
      })
    })
  })

  describe('AddressListPage', () => {
    it('loads and renders address list with default badge', async () => {
      vi.mocked(addressApi.getAddressesApi).mockResolvedValue(mockAddresses)

      render(
        <MemoryRouter>
          <AuthProvider>
            <AddressListPage />
          </AuthProvider>
        </MemoryRouter>
      )

      await waitFor(() => {
        expect(screen.getByText('45 Lê Lai, Bến Thành, Quận 1, TP.HCM, (Mã Bưu Điện: 700000)')).toBeInTheDocument()
        expect(screen.getByTestId('default-badge')).toBeInTheDocument()
        expect(screen.getByText('78 Nguyễn Trãi, Phường 2, Quận 5, TP.HCM')).toBeInTheDocument()
      })
    })

    it('creates new address through AddressModal', async () => {
      vi.mocked(addressApi.getAddressesApi).mockResolvedValue(mockAddresses)
      vi.mocked(addressApi.createAddressApi).mockResolvedValue({
        id: 'addr-3',
        recipientName: 'Le Thi C',
        phone: '0909090909',
        street: '100 Hai Ba Trung',
        ward: 'Phường 6',
        district: 'Quận 3',
        city: 'TP.HCM',
        postalCode: null,
        isDefault: false,
        createdAtUtc: '2026-08-22T10:00:00Z',
        updatedAtUtc: '2026-08-22T10:00:00Z',
      })

      render(
        <MemoryRouter>
          <AuthProvider>
            <AddressListPage />
          </AuthProvider>
        </MemoryRouter>
      )

      await waitFor(() => {
        expect(screen.getByTestId('btn-open-add-modal')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByTestId('btn-open-add-modal'))

      // Fill in modal
      fireEvent.change(screen.getByTestId('input-recipient'), { target: { value: 'Le Thi C' } })
      fireEvent.change(screen.getByTestId('input-addr-phone'), { target: { value: '0909090909' } })
      fireEvent.change(screen.getByTestId('input-street'), { target: { value: '100 Hai Ba Trung' } })
      fireEvent.change(screen.getByTestId('input-ward'), { target: { value: 'Phường 6' } })
      fireEvent.change(screen.getByTestId('input-district'), { target: { value: 'Quận 3' } })
      fireEvent.change(screen.getByTestId('input-city'), { target: { value: 'TP.HCM' } })

      fireEvent.click(screen.getByTestId('btn-save-address'))

      await waitFor(() => {
        expect(addressApi.createAddressApi).toHaveBeenCalledWith(
          {
            recipientName: 'Le Thi C',
            phone: '0909090909',
            street: '100 Hai Ba Trung',
            ward: 'Phường 6',
            district: 'Quận 3',
            city: 'TP.HCM',
            postalCode: null,
          },
          'valid-test-token'
        )
      })
    })

    it('sets address as default', async () => {
      vi.mocked(addressApi.getAddressesApi).mockResolvedValue(mockAddresses)
      vi.mocked(addressApi.setDefaultAddressApi).mockResolvedValue({
        message: 'Address set as default.',
      })

      render(
        <MemoryRouter>
          <AuthProvider>
            <AddressListPage />
          </AuthProvider>
        </MemoryRouter>
      )

      await waitFor(() => {
        expect(screen.getByTestId('btn-set-default-addr-2')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByTestId('btn-set-default-addr-2'))

      await waitFor(() => {
        expect(addressApi.setDefaultAddressApi).toHaveBeenCalledWith('addr-2', 'valid-test-token')
      })
    })

    it('deletes address after confirmation', async () => {
      vi.mocked(addressApi.getAddressesApi).mockResolvedValue(mockAddresses)
      vi.mocked(addressApi.deleteAddressApi).mockResolvedValue({
        message: 'Address deleted.',
      })

      render(
        <MemoryRouter>
          <AuthProvider>
            <AddressListPage />
          </AuthProvider>
        </MemoryRouter>
      )

      await waitFor(() => {
        expect(screen.getByTestId('btn-delete-addr-2')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByTestId('btn-delete-addr-2'))

      // Confirm modal pops up
      expect(screen.getByText(/Xác Nhận Xóa Địa Chỉ/i)).toBeInTheDocument()
      fireEvent.click(screen.getByTestId('btn-confirm-delete'))

      await waitFor(() => {
        expect(addressApi.deleteAddressApi).toHaveBeenCalledWith('addr-2', 'valid-test-token')
      })
    })
  })
})
