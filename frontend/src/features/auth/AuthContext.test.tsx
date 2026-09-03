import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as authApi from '../../api/authApi'
import type { AuthResponse, UserDto } from '../../api/authApi'
import { recommendationApi } from '../../api/recommendationApi'
import { rotateAnonymousSessionId } from '../recommendations/recommendationSession'
import { AuthProvider, useAuth } from './AuthContext'

vi.mock('../recommendations/recommendationSession', () => ({
  getOrCreateAnonymousSessionId: vi.fn(() => '00000000-0000-4000-8000-000000000001'),
  rotateAnonymousSessionId: vi.fn(() => '00000000-0000-4000-8000-000000000002'),
}))

vi.mock('../../api/recommendationApi', () => ({
  recommendationApi: { mergeSession: vi.fn() },
}))

const user: UserDto = {
  id: 'user-1',
  email: 'test@example.com',
  fullName: 'Nguyen Van A',
  role: 'Customer',
}

const authResponse: AuthResponse = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token',
  expiresInSeconds: 900,
  user,
}

const mergeSessionMock = vi.mocked(recommendationApi.mergeSession)
const rotateMock = vi.mocked(rotateAnonymousSessionId)

function LoginProbe() {
  const { login, user: currentUser } = useAuth()
  return (
    <div>
      <button onClick={() => void login({ email: 'test@example.com', password: 'Password@123' })}>
        Đăng nhập
      </button>
      {currentUser && <span>Tài khoản: {currentUser.fullName}</span>}
    </div>
  )
}

describe('AuthContext session merge', () => {
  beforeEach(() => {
    localStorage.clear()
    mergeSessionMock.mockReset()
    mergeSessionMock.mockResolvedValue({ mergedCount: 2 })
    rotateMock.mockClear()
  })

  afterEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it('merges the anonymous session once after login and rotates on success', async () => {
    vi.spyOn(authApi, 'loginApi').mockResolvedValue(authResponse)

    render(
      <AuthProvider>
        <LoginProbe />
      </AuthProvider>,
    )

    screen.getByRole('button', { name: 'Đăng nhập' }).click()

    await screen.findByText('Tài khoản: Nguyen Van A')

    await waitFor(() => {
      expect(mergeSessionMock).toHaveBeenCalledTimes(1)
      expect(mergeSessionMock).toHaveBeenCalledWith(
        '00000000-0000-4000-8000-000000000001',
        'access-token',
      )
      expect(rotateMock).toHaveBeenCalledTimes(1)
    })
  })

  it('keeps login successful when the session merge fails', async () => {
    vi.spyOn(authApi, 'loginApi').mockResolvedValue(authResponse)
    mergeSessionMock.mockRejectedValue(new Error('offline'))

    render(
      <AuthProvider>
        <LoginProbe />
      </AuthProvider>,
    )

    screen.getByRole('button', { name: 'Đăng nhập' }).click()

    await screen.findByText('Tài khoản: Nguyen Van A')

    await waitFor(() => expect(mergeSessionMock).toHaveBeenCalledTimes(1))
    expect(rotateMock).not.toHaveBeenCalled()
  })

  it('merges once after session restore with a stored access token', async () => {
    localStorage.setItem('os_access_token', 'stored-token')
    localStorage.setItem('os_refresh_token', 'stored-refresh')
    vi.spyOn(authApi, 'getMeApi').mockResolvedValue(user)

    render(
      <AuthProvider>
        <LoginProbe />
      </AuthProvider>,
    )

    await screen.findByText('Tài khoản: Nguyen Van A')

    await waitFor(() => {
      expect(mergeSessionMock).toHaveBeenCalledTimes(1)
      expect(mergeSessionMock).toHaveBeenCalledWith(
        '00000000-0000-4000-8000-000000000001',
        'stored-token',
      )
    })
  })
})