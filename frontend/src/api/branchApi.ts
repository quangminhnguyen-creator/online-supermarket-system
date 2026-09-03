import { getJson, type RequestOptions } from './httpClient'

export interface BranchDto {
  id: string
  name: string
  address: string
  phone: string | null
  latitude: number | null
  longitude: number | null
  isActive: boolean
}

export interface BranchProductInventoryDto {
  inventoryId: string
  productId: string
  productName: string
  sku: string
  sellingPrice: number
  quantityOnHand: number
  reservedQuantity: number
  availableQuantity: number
  reorderLevel: number
}

export interface BranchInventoryListDto {
  branchId: string
  products: BranchProductInventoryDto[]
}

export const branchApi = {
  async getBranches(options?: RequestOptions | AbortSignal): Promise<BranchDto[]> {
    return getJson<BranchDto[]>('/branches', options)
  },

  async getBranchById(id: string, options?: RequestOptions | AbortSignal): Promise<BranchDto> {
    return getJson<BranchDto>(`/branches/${id}`, options)
  },

  async getBranchInventory(
    id: string,
    options?: RequestOptions | AbortSignal
  ): Promise<BranchInventoryListDto> {
    return getJson<BranchInventoryListDto>(`/branches/${id}/inventory`, options)
  },
}
