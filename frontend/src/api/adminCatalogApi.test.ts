import { afterEach, expect, it, vi, describe } from 'vitest'
import { adminCatalogApi } from './adminCatalogApi'
import { ApiError } from './httpClient'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('adminCatalogApi', () => {
  const token = 'test-token-123'
  const controller = new AbortController()

  // ==========================================
  // CATEGORIES
  // ==========================================

  it('getCategories passes bearer token and signal', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('[]', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await adminCatalogApi.getCategories(token, controller.signal)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/categories',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: `Bearer ${token}`,
        }),
        signal: controller.signal,
      }),
    )
  })

  it('createCategory sends POST with payload and bearer token', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'c1', name: 'Cat' }), {
      status: 201,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    const req = { name: 'Cat', slug: 'cat', parentCategoryId: null }
    const result = await adminCatalogApi.createCategory(req, token, controller.signal)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/categories',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        }),
        body: JSON.stringify(req),
        signal: controller.signal,
      }),
    )
    expect(result.id).toBe('c1')
  })

  it('updateCategory sends PUT with URL and payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'c1', name: 'Cat Upd' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    const req = { name: 'Cat Upd', slug: 'cat-upd', parentCategoryId: 'parent-1' }
    await adminCatalogApi.updateCategory('c1', req, token, controller.signal)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/categories/c1',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(req),
        headers: expect.objectContaining({
          Authorization: `Bearer ${token}`,
        }),
        signal: controller.signal,
      }),
    )
  })

  it('updateCategoryStatus sends PATCH with status payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'c1', isActive: false }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await adminCatalogApi.updateCategoryStatus('c1', { isActive: false }, token, controller.signal)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/categories/c1/status',
      expect.objectContaining({
        method: 'PATCH',
        body: JSON.stringify({ isActive: false }),
        headers: expect.objectContaining({
          Authorization: `Bearer ${token}`,
        }),
      }),
    )
  })

  // ==========================================
  // BRANDS
  // ==========================================

  it('getBrands passes bearer token and signal', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('[]', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await adminCatalogApi.getBrands(token, controller.signal)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/brands',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: `Bearer ${token}`,
        }),
      }),
    )
  })

  it('createBrand sends POST with payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'b1', name: 'Brand' }), {
      status: 201,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    const req = { name: 'Brand', slug: 'brand' }
    await adminCatalogApi.createBrand(req, token)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/brands',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(req),
      }),
    )
  })

  it('updateBrand sends PUT with payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'b1', name: 'Brand Upd' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    const req = { name: 'Brand Upd', slug: 'brand-upd' }
    await adminCatalogApi.updateBrand('b1', req, token)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/brands/b1',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(req),
      }),
    )
  })

  it('updateBrandStatus sends PATCH with status payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'b1', isActive: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await adminCatalogApi.updateBrandStatus('b1', { isActive: true }, token)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/brands/b1/status',
      expect.objectContaining({
        method: 'PATCH',
        body: JSON.stringify({ isActive: true }),
      }),
    )
  })

  // ==========================================
  // PRODUCTS
  // ==========================================

  it('getProducts with all query params formats search string properly', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{"items":[],"meta":{}}', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await adminCatalogApi.getProducts({
      page: 2,
      pageSize: 20,
      search: 'test',
      categoryId: 'cat-123',
      brandId: 'brand-456',
      isActive: false,
    }, token, controller.signal)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/products?page=2&pageSize=20&search=test&categoryId=cat-123&brandId=brand-456&isActive=false',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: `Bearer ${token}`,
        }),
        signal: controller.signal,
      }),
    )
  })

  it('getProducts without params calls base products URL', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{"items":[],"meta":{}}', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await adminCatalogApi.getProducts(undefined, token)
    expect(fetchMock).toHaveBeenCalledWith('/api/admin/catalog/products', expect.anything())
  })

  it('createProduct sends POST with product payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'p1', sku: 'SKU1' }), {
      status: 201,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    const req = {
      categoryId: 'c1',
      brandId: 'b1',
      sku: 'SKU1',
      name: 'Product 1',
      slug: 'prod-1',
      description: 'Desc',
      basePrice: 1000,
      unit: 'cái',
      imageUrl: 'https://img.example/1.jpg',
    }
    await adminCatalogApi.createProduct(req, token)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/products',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(req),
      }),
    )
  })

  it('updateProduct sends PUT with product payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'p1', name: 'Product Upd' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    const req = {
      categoryId: 'c1',
      brandId: 'b1',
      sku: 'SKU1',
      name: 'Product Upd',
      slug: 'prod-upd',
      description: null,
      basePrice: 1500,
      unit: 'cái',
      imageUrl: null,
    }
    await adminCatalogApi.updateProduct('p1', req, token)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/products/p1',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(req),
      }),
    )
  })

  it('updateProductStatus sends PATCH with status payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 'p1', isActive: false }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await adminCatalogApi.updateProductStatus('p1', { isActive: false }, token)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/catalog/products/p1/status',
      expect.objectContaining({
        method: 'PATCH',
        body: JSON.stringify({ isActive: false }),
      }),
    )
  })

  it('throws ApiError with backend message when response fails', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ message: 'Category slug already exists.' }), {
      status: 409,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(adminCatalogApi.createCategory({ name: 'A', slug: 'a', parentCategoryId: null }, token))
      .rejects.toThrow(ApiError)
  })
})
