import { afterEach, expect, it, vi, describe } from 'vitest'
import { adminCatalogApi } from './adminCatalogApi'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('adminCatalogApi', () => {
  it('calls correct URL for getCategories', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('[]', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)
    
    await adminCatalogApi.getCategories()
    expect(fetchMock).toHaveBeenCalledWith('/api/admin/catalog/categories', expect.anything())
  })

  it('calls correct URL for getBrands', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('[]', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)
    
    await adminCatalogApi.getBrands()
    expect(fetchMock).toHaveBeenCalledWith('/api/admin/catalog/brands', expect.anything())
  })

  it('calls correct URL for getProducts with params', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{"items":[],"meta":{}}', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)
    
    await adminCatalogApi.getProducts({ page: 2, search: 'test', isActive: false })
    expect(fetchMock).toHaveBeenCalledWith('/api/admin/catalog/products?page=2&search=test&isActive=false', expect.anything())
  })
})
