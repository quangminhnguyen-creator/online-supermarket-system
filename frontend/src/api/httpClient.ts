const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined

export const apiBaseUrl = (configuredBaseUrl ?? '/api').replace(/\/$/, '')

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly data?: any,
    message?: string
  ) {
    super(message ?? `API request failed with status ${status}`)
    this.name = 'ApiError'
  }
}

export type RequestOptions = {
  signal?: AbortSignal
  token?: string
}

export async function getJson<T>(
  path: string,
  options?: RequestOptions | AbortSignal
): Promise<T> {
  const signal = options instanceof AbortSignal ? options : options?.signal
  const token = !(options instanceof AbortSignal) ? options?.token : undefined

  const headers: Record<string, string> = {
    Accept: 'application/json',
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers,
    signal,
  })

  if (!response.ok) {
    let data: any
    try {
      data = await response.json()
    } catch {
      // ignore json parse error
    }
    throw new ApiError(response.status, data, data?.message)
  }

  return response.json() as Promise<T>
}

export async function postJson<T>(
  path: string,
  body: unknown,
  options?: RequestOptions | AbortSignal
): Promise<T> {
  const signal = options instanceof AbortSignal ? options : options?.signal
  const token = !(options instanceof AbortSignal) ? options?.token : undefined

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'POST',
    headers,
    body: JSON.stringify(body),
    signal,
  })

  if (!response.ok) {
    let data: any
    try {
      data = await response.json()
    } catch {
      // ignore
    }
    throw new ApiError(response.status, data, data?.message)
  }

  return response.json() as Promise<T>
}

export async function putJson<T>(
  path: string,
  body: unknown,
  options?: RequestOptions | AbortSignal
): Promise<T> {
  const signal = options instanceof AbortSignal ? options : options?.signal
  const token = !(options instanceof AbortSignal) ? options?.token : undefined

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'PUT',
    headers,
    body: JSON.stringify(body),
    signal,
  })

  if (!response.ok) {
    let data: any
    try {
      data = await response.json()
    } catch {
      // ignore
    }
    throw new ApiError(response.status, data, data?.message)
  }

  const text = await response.text()
  if (!text) {
    return {} as T
  }
  try {
    return JSON.parse(text) as T
  } catch {
    return {} as T
  }
}

export async function deleteJson<T>(
  path: string,
  options?: RequestOptions | AbortSignal
): Promise<T> {
  const signal = options instanceof AbortSignal ? options : options?.signal
  const token = !(options instanceof AbortSignal) ? options?.token : undefined

  const headers: Record<string, string> = {
    Accept: 'application/json',
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'DELETE',
    headers,
    signal,
  })

  if (!response.ok) {
    let data: any
    try {
      data = await response.json()
    } catch {
      // ignore
    }
    throw new ApiError(response.status, data, data?.message)
  }

  const text = await response.text()
  if (!text) {
    return {} as T
  }
  try {
    return JSON.parse(text) as T
  } catch {
    return {} as T
  }
}

