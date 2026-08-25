import { getJson, postJson, putJson, deleteJson } from './httpClient'

export interface AddressDto {
  id: string
  recipientName: string
  phone: string
  street: string
  ward: string
  district: string
  city: string
  postalCode?: string | null
  isDefault: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateAddressRequest {
  recipientName: string
  phone: string
  street: string
  ward: string
  district: string
  city: string
  postalCode?: string | null
}

export interface UpdateAddressRequest {
  recipientName: string
  phone: string
  street: string
  ward: string
  district: string
  city: string
  postalCode?: string | null
}

export interface ApiResponse {
  message: string
}

export function getAddressesApi(token: string, signal?: AbortSignal): Promise<AddressDto[]> {
  return getJson<AddressDto[]>('/users/me/addresses', { token, signal })
}

export function createAddressApi(data: CreateAddressRequest, token: string, signal?: AbortSignal): Promise<AddressDto> {
  return postJson<AddressDto>('/users/me/addresses', data, { token, signal })
}

export function updateAddressApi(id: string, data: UpdateAddressRequest, token: string, signal?: AbortSignal): Promise<ApiResponse> {
  return putJson<ApiResponse>(`/users/me/addresses/${id}`, data, { token, signal })
}

export function deleteAddressApi(id: string, token: string, signal?: AbortSignal): Promise<ApiResponse> {
  return deleteJson<ApiResponse>(`/users/me/addresses/${id}`, { token, signal })
}

export function setDefaultAddressApi(id: string, token: string, signal?: AbortSignal): Promise<ApiResponse> {
  return putJson<ApiResponse>(`/users/me/addresses/${id}/default`, {}, { token, signal })
}
