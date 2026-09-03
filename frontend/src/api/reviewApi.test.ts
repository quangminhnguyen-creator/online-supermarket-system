import { describe, it, expect, vi, beforeEach } from 'vitest'
import { reviewApi, type ReviewDto } from './reviewApi'

describe('reviewApi', () => {
  const token = 'jwt-token'
  const sampleReview: ReviewDto = {
    id: 'rev-1',
    productId: 'prod-1',
    reviewerName: 'Nguyen Van A',
    rating: 5,
    comment: 'Tuyệt vời',
    createdAtUtc: '2026-09-01T00:00:00Z',
    updatedAtUtc: '2026-09-01T00:00:00Z',
  }

  beforeEach(() => {
    vi.restoreAllMocks()
  })

  function jsonResponse(body: unknown, status = 200): Response {
    return {
      ok: status >= 200 && status < 300,
      status,
      json: async () => body,
      text: async () => JSON.stringify(body),
      headers: new Headers({ 'content-type': 'application/json' }),
    } as unknown as Response
  }

  it('fetches product reviews with pagination parameters', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({
      averageRating: 4.5,
      reviewCount: 1,
      data: [sampleReview],
      page: 1,
      pageSize: 10,
      totalCount: 1,
    }))
    vi.stubGlobal('fetch', fetchMock)

    const result = await reviewApi.getProductReviews('prod-1', 1, 10)

    expect(fetchMock).toHaveBeenCalledWith('/api/products/prod-1/reviews?page=1&pageSize=10', expect.any(Object))
    expect(result.averageRating).toBe(4.5)
    expect(result.data).toHaveLength(1)
  })

  it('fetches review eligibility with auth token', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({
      canReview: true,
      orderItemId: 'item-1',
      reviewId: null,
    }))
    vi.stubGlobal('fetch', fetchMock)

    const result = await reviewApi.getEligibility('prod-1', token)

    expect(fetchMock).toHaveBeenCalledWith('/api/products/prod-1/review-eligibility', expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer jwt-token' }),
    }))
    expect(result.canReview).toBe(true)
    expect(result.orderItemId).toBe('item-1')
  })

  it('creates a review without client-supplied userId or productId', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(sampleReview, 201))
    vi.stubGlobal('fetch', fetchMock)

    const result = await reviewApi.createReview(token, {
      orderItemId: 'item-1',
      rating: 5,
      comment: 'Tốt',
    })

    expect(fetchMock).toHaveBeenCalledWith('/api/reviews', expect.objectContaining({
      method: 'POST',
      headers: expect.objectContaining({
        Authorization: 'Bearer jwt-token',
        'Content-Type': 'application/json',
      }),
      body: JSON.stringify({ orderItemId: 'item-1', rating: 5, comment: 'Tốt' }),
    }))
    expect(result.id).toBe('rev-1')
  })

  it('updates an existing review by id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ ...sampleReview, rating: 4, comment: 'Đã sửa' }))
    vi.stubGlobal('fetch', fetchMock)

    const result = await reviewApi.updateReview('rev-1', token, {
      rating: 4,
      comment: 'Đã sửa',
    })

    expect(fetchMock).toHaveBeenCalledWith('/api/reviews/rev-1', expect.objectContaining({
      method: 'PUT',
      headers: expect.objectContaining({
        Authorization: 'Bearer jwt-token',
        'Content-Type': 'application/json',
      }),
      body: JSON.stringify({ rating: 4, comment: 'Đã sửa' }),
    }))
    expect(result.rating).toBe(4)
    expect(result.comment).toBe('Đã sửa')
  })

  it('fetches a single review by id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(sampleReview))
    vi.stubGlobal('fetch', fetchMock)

    const result = await reviewApi.getReviewById('rev-1')

    expect(fetchMock).toHaveBeenCalledWith('/api/reviews/rev-1', expect.any(Object))
    expect(result.id).toBe('rev-1')
    expect(result.rating).toBe(5)
  })
})
