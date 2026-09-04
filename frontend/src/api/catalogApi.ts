import { httpClient } from './httpClient';

export interface ProductSummary {
  id: string;
  name: string;
  slug: string;
  sku: string;
  basePrice: number;
  sellingPrice: number;
  availableQuantity: number | null;
  unit: string;
  imageUrl: string;
  categoryId: string;
  categoryName: string;
  brandId: string;
  brandName: string;
}

export interface ProductDetail extends ProductSummary {
  description: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface Category {
  id: string;
  parentCategoryId: string | null;
  name: string;
  slug: string;
  isActive: boolean;
}

export interface Brand {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface ProductFilterParams {
  branchId?: string;
  categoryId?: string;
  brandId?: string;
  minPrice?: number;
  maxPrice?: number;
  search?: string;
  sortBy?: 'price_asc' | 'price_desc' | 'name_asc' | 'name_desc';
  page?: number;
  pageSize?: number;
}

export const catalogApi = {
  getProducts: async (params?: ProductFilterParams): Promise<PagedResult<ProductSummary>> => {
    const query = new URLSearchParams();
    if (params?.branchId) query.append('branchId', params.branchId);
    if (params?.categoryId) query.append('categoryId', params.categoryId);
    if (params?.brandId) query.append('brandId', params.brandId);
    if (params?.minPrice !== undefined) query.append('minPrice', params.minPrice.toString());
    if (params?.maxPrice !== undefined) query.append('maxPrice', params.maxPrice.toString());
    if (params?.search) query.append('search', params.search);
    if (params?.sortBy) query.append('sortBy', params.sortBy);
    if (params?.page) query.append('page', params.page.toString());
    if (params?.pageSize) query.append('pageSize', params.pageSize.toString());

    const url = `/api/catalog/products${query.toString() ? `?${query.toString()}` : ''}`;
    return httpClient.get<PagedResult<ProductSummary>>(url);
  },

  getProductBySlug: async (slug: string, branchId?: string): Promise<ProductDetail> => {
    const query = branchId ? `?branchId=${branchId}` : '';
    return httpClient.get<ProductDetail>(`/api/catalog/products/${slug}${query}`);
  },

  getCategories: async (): Promise<Category[]> => {
    return httpClient.get<Category[]>('/api/catalog/categories');
  },

  getBrands: async (): Promise<Brand[]> => {
    return httpClient.get<Brand[]>('/api/catalog/brands');
  }
};
