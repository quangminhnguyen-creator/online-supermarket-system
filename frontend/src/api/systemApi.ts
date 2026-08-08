import { getJson } from './httpClient'

export type HealthResponse = { status: string }

export function getHealth(signal?: AbortSignal): Promise<HealthResponse> {
  return getJson<HealthResponse>('/health', signal)
}
