import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as authApi from '../../api/authApi'
import { AuthProvider } from './AuthContext'
import { LoginForm } from './LoginForm'
import { RegisterForm } from './RegisterForm'
import { UserMenu } from './UserMenu'

describe('Auth Feature', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  afterEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  describe('LoginForm', () => {
    it('shows validation error when submitted empty', async () => {
      render(
        <AuthProvider>
          <LoginForm onSwitchToRegister={() => {}} />
        </AuthProvider>
      )

      const submitButton = screen.getByRole('button', { name: 'Đăng nhập' })
      fireEvent.click(submitButton)

      expect(await screen.findByText('Vui lòng nhập đầy đủ email và mật khẩu.')).toBeInTheDocument()
    })

    it('submits credentials and calls login on valid input', async () => {
      const mockAuthResponse: authApi.AuthResponse = {
        accessToken: 'mock_jwt_token',
        refreshToken: 'mock_refresh_token',
        expiresInSeconds: 900,
        user: {
          id: 'user-1',
          email: 'test@example.com',
          fullName: 'Nguyen Van A',
          role: 'Customer',
        },
      }

      vi.spyOn(authApi, 'loginApi').mockResolvedValue(mockAuthResponse)
      const onSuccess = vi.fn()

      render(
        <AuthProvider>
          <LoginForm onSuccess={onSuccess} onSwitchToRegister={() => {}} />
        </AuthProvider>
      )

      fireEvent.change(screen.getByLabelText('Địa chỉ Email'), {
        target: { value: 'test@example.com' },
      })
      fireEvent.change(screen.getByLabelText('Mật khẩu'), {
        target: { value: 'Password@123' },
      })

      fireEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }))

      await waitFor(() => {
        expect(authApi.loginApi).toHaveBeenCalledWith({
          email: 'test@example.com',
          password: 'Password@123',
        })
        expect(onSuccess).toHaveBeenCalled()
      })
    })
  })

  describe('RegisterForm', () => {
    it('shows error when passwords do not match', async () => {
      render(
        <AuthProvider>
          <RegisterForm onSwitchToLogin={() => {}} />
        </AuthProvider>
      )

      fireEvent.change(screen.getByLabelText('Họ và tên *'), {
        target: { value: 'Nguyen Van B' },
      })
      fireEvent.change(screen.getByLabelText('Địa chỉ Email *'), {
        target: { value: 'test2@example.com' },
      })
      fireEvent.change(screen.getByLabelText('Mật khẩu *'), {
        target: { value: 'Password@123' },
      })
      fireEvent.change(screen.getByLabelText('Xác nhận mật khẩu *'), {
        target: { value: 'DifferentPassword' },
      })

      fireEvent.click(screen.getByRole('button', { name: 'Đăng ký' }))

      expect(await screen.findByText('Mật khẩu xác nhận không khớp.')).toBeInTheDocument()
    })

    it('submits registration on valid input', async () => {
      const mockRegisterResponse: authApi.RegisterResponse = {
        id: 'user-2',
        email: 'test2@example.com',
        fullName: 'Nguyen Van B',
        role: 'Customer',
      }
      const mockAuthResponse: authApi.AuthResponse = {
        accessToken: 'token',
        refreshToken: 'refresh',
        expiresInSeconds: 900,
        user: mockRegisterResponse,
      }

      vi.spyOn(authApi, 'registerApi').mockResolvedValue(mockRegisterResponse)
      vi.spyOn(authApi, 'loginApi').mockResolvedValue(mockAuthResponse)
      const onSuccess = vi.fn()

      render(
        <AuthProvider>
          <RegisterForm onSuccess={onSuccess} onSwitchToLogin={() => {}} />
        </AuthProvider>
      )

      fireEvent.change(screen.getByLabelText('Họ và tên *'), {
        target: { value: 'Nguyen Van B' },
      })
      fireEvent.change(screen.getByLabelText('Địa chỉ Email *'), {
        target: { value: 'test2@example.com' },
      })
      fireEvent.change(screen.getByLabelText('Mật khẩu *'), {
        target: { value: 'Password@123' },
      })
      fireEvent.change(screen.getByLabelText('Xác nhận mật khẩu *'), {
        target: { value: 'Password@123' },
      })

      fireEvent.click(screen.getByRole('button', { name: 'Đăng ký' }))

      await waitFor(() => {
        expect(authApi.registerApi).toHaveBeenCalled()
        expect(onSuccess).toHaveBeenCalled()
      })
    })
  })

  describe('UserMenu', () => {
    it('renders guest login/register buttons when not authenticated', async () => {
      render(
        <MemoryRouter>
          <AuthProvider>
            <UserMenu />
          </AuthProvider>
        </MemoryRouter>
      )

      expect(await screen.findByRole('button', { name: 'Đăng nhập' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Đăng ký' })).toBeInTheDocument()
    })

    it('renders user details and logout button when authenticated', async () => {
      localStorage.setItem('os_access_token', 'valid_token')
      vi.spyOn(authApi, 'getMeApi').mockResolvedValue({
        id: 'user-1',
        email: 'admin@example.com',
        fullName: 'Admin User',
        role: 'Admin',
      })

      render(
        <MemoryRouter>
          <AuthProvider>
            <UserMenu />
          </AuthProvider>
        </MemoryRouter>
      )

      expect(await screen.findByText('Admin User')).toBeInTheDocument()
      expect(screen.getByText('Quản trị viên')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Đăng xuất' })).toBeInTheDocument()
    })
  })
})
