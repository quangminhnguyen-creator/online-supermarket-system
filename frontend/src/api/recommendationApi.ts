import { postJson } from './httpClient'

export interface RecordProductViewRequest {
  anonymousSessionId: string
  branchId?: string
}

export interface MergeSessionResponse {
  mergedCount: number
}

export const recommendationApi = {
  recordView: (
    productId: string,
    request: RecordProductViewRequest,
    token?: string,
    signal?: AbortSignal,
  ) =>
    postJson<unknown>(
      `/products/${encodeURIComponent(productId)}/view-events`,
      request,
      { token, signal },
    ),

  mergeSession: (anonymousSessionId: string, token: string, signal?: AbortSignal) =>
    postJson<MergeSessionResponse>(
      '/recommendations/session/merge',
      { anonymousSessionId },
      { token, signal },
    ),
}