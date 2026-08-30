import { afterEach, expect, it, vi } from 'vitest'
import { catalogApi } from './catalogApi'

const detailResponse = {
  id: 'prod-1',
  name: 'iPhone 15',
  slug: 'iphone-15',
  sku: 'DT-APP-002',
  description: 'Điện thoại Apple',
  basePrice: 22990000,
  unit: 'cái',
  imageUrl: null,
  categoryId: 'cat-1',
  categoryName: 'Điện thoại',
  categorySlug: 'dien-thoai',
  brandId: 'brand-1',
  brandName: 'Apple',
  branchInventory: null,
}

afterEach(() => vi.unstubAllGlobals())

it.each([
  [undefined, '/api/products/prod-1'],
  ['branch 1', '/api/products/prod-1?branchId=branch%201'],
])('builds the detail URL for branch %s', async (branchId, expectedUrl) => {
  const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(detailResponse), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  }))
  vi.stubGlobal('fetch', fetchMock)
  const detail = await catalogApi.getProductById('prod-1', branchId)
  expect(detail.categorySlug).toBe('dien-thoai')
  expect(fetchMock).toHaveBeenCalledWith(expectedUrl, expect.objectContaining({
    headers: { Accept: 'application/json' },
  }))
})
