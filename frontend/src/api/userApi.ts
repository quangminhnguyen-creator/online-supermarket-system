import { putJson } from './httpClient'

export interface UpdateProfileRequest {
  fullName: string
  phone?: string | null
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface ApiResponse {
  message: string
}

export function updateProfileApi(data: UpdateProfileRequest, token: string, signal?: AbortSignal): Promise<ApiResponse> {
  return putJson<ApiResponse>('/users/me', data, { token, signal })
}

export function changePasswordApi(data: ChangePasswordRequest, token: string, signal?: AbortSignal): Promise<ApiResponse> {
  return putJson<ApiResponse>('/users/me/password', data, { token, signal })
}
