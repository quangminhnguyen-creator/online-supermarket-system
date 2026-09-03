import { afterEach, expect, it, vi } from 'vitest'
import { recommendationApi } from './recommendationApi'

const SESSION_ID = '00000000-0000-4000-8000-000000000001'

function jsonResponse(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => vi.unstubAllGlobals())

it('recordView posts guest payload to the product view-events route', async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(null, 202))
  vi.stubGlobal('fetch', fetchMock)

  await recommendationApi.recordView('prod-1', { anonymousSessionId: SESSION_ID })

  expect(fetchMock).toHaveBeenCalledWith(
    '/api/products/prod-1/view-events',
    expect.objectContaining({
      method: 'POST',
      headers: expect.objectContaining({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({ anonymousSessionId: SESSION_ID }),
    }),
  )
})

it('recordView sends branch and bearer token for authenticated payload', async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(null, 202))
  vi.stubGlobal('fetch', fetchMock)

  await recommendationApi.recordView('prod-1', {
    anonymousSessionId: SESSION_ID,
    branchId: 'branch-1',
  }, 'jwt-token')

  const [, init] = fetchMock.mock.calls[0] as [string, RequestInit]
  expect(init.headers).toMatchObject({ Authorization: 'Bearer jwt-token' })
  expect(init.body).toBe(JSON.stringify({
    anonymousSessionId: SESSION_ID,
    branchId: 'branch-1',
  }))
})

it('mergeSession posts the anonymous id to the session merge route with token', async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ mergedCount: 2 }))
  vi.stubGlobal('fetch', fetchMock)

  const result = await recommendationApi.mergeSession(SESSION_ID, 'jwt-token')

  expect(result).toEqual({ mergedCount: 2 })
  expect(fetchMock).toHaveBeenCalledWith(
    '/api/recommendations/session/merge',
    expect.objectContaining({
      method: 'POST',
      headers: expect.objectContaining({ Authorization: 'Bearer jwt-token' }),
      body: JSON.stringify({ anonymousSessionId: SESSION_ID }),
    }),
  )
})