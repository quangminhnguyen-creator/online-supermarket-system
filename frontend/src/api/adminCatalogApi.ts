export interface AdminCategoryDto {
  id: string
  name: string
  slug: string
  parentCategoryId: string | null
  isActive: boolean
}

export interface AdminBrandDto {
  id: string
  name: string
  slug: string
  isActive: boolean
}

export interface AdminProductDto {
  id: string
  categoryId: string
  categoryName: string
  brandId: string
  brandName: string
  sku: string
  name: string
  slug: string
  description: string | null
  basePrice: number
  unit: string
  imageUrl: string | null
  isActive: boolean
}

export interface UpsertCategoryRequest {
  name: string
  slug: string
  parentCategoryId: string | null
}

export interface UpsertBrandRequest {
  name: string
  slug: string
}

export interface UpsertProductRequest {
  categoryId: string
  brandId: string
  sku: string
  name: string
  slug: string
  description: string | null
  basePrice: number
  unit: string
  imageUrl: string | null
}

export interface UpdateCatalogStatusRequest {
  isActive: boolean
}

export interface PaginatedResponse<T> {
  items: T[]
  meta: {
    totalCount: number
    currentPage: number
    pageSize: number
    totalPages: number
  }
}

export interface AdminProductListParams {
  page?: number
  pageSize?: number
  search?: string
  categoryId?: string
  brandId?: string
  isActive?: boolean
}

import { getJson, postJson, putJson, patchJson } from './httpClient'

export const adminCatalogApi = {
  // Categories
  getCategories: () => getJson<AdminCategoryDto[]>('/admin/catalog/categories', { token: 'admin' }),
  createCategory: (req: UpsertCategoryRequest) => postJson<AdminCategoryDto>('/admin/catalog/categories', req, { token: 'admin' }),
  updateCategory: (id: string, req: UpsertCategoryRequest) => putJson<AdminCategoryDto>(`/admin/catalog/categories/${id}`, req, { token: 'admin' }),
  updateCategoryStatus: (id: string, req: UpdateCatalogStatusRequest) => patchJson<AdminCategoryDto>(`/admin/catalog/categories/${id}/status`, req, { token: 'admin' }),

  // Brands
  getBrands: () => getJson<AdminBrandDto[]>('/admin/catalog/brands', { token: 'admin' }),
  createBrand: (req: UpsertBrandRequest) => postJson<AdminBrandDto>('/admin/catalog/brands', req, { token: 'admin' }),
  updateBrand: (id: string, req: UpsertBrandRequest) => putJson<AdminBrandDto>(`/admin/catalog/brands/${id}`, req, { token: 'admin' }),
  updateBrandStatus: (id: string, req: UpdateCatalogStatusRequest) => patchJson<AdminBrandDto>(`/admin/catalog/brands/${id}/status`, req, { token: 'admin' }),

  // Products
  getProducts: (params?: AdminProductListParams) => {
    const searchParams = new URLSearchParams()
    if (params) {
      if (params.page) searchParams.append('page', params.page.toString())
      if (params.pageSize) searchParams.append('pageSize', params.pageSize.toString())
      if (params.search) searchParams.append('search', params.search)
      if (params.categoryId) searchParams.append('categoryId', params.categoryId)
      if (params.brandId) searchParams.append('brandId', params.brandId)
      if (params.isActive !== undefined) searchParams.append('isActive', params.isActive.toString())
    }
    const q = searchParams.toString()
    const path = q ? `/admin/catalog/products?${q}` : '/admin/catalog/products'
    return getJson<PaginatedResponse<AdminProductDto>>(path, { token: 'admin' })
  },
  createProduct: (req: UpsertProductRequest) => postJson<AdminProductDto>('/admin/catalog/products', req, { token: 'admin' }),
  updateProduct: (id: string, req: UpsertProductRequest) => putJson<AdminProductDto>(`/admin/catalog/products/${id}`, req, { token: 'admin' }),
  updateProductStatus: (id: string, req: UpdateCatalogStatusRequest) => patchJson<AdminProductDto>(`/admin/catalog/products/${id}/status`, req, { token: 'admin' }),
}
