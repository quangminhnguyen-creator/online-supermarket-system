import React, { useEffect, useState } from 'react'
import { adminCatalogApi, AdminProductDto, AdminCategoryDto, AdminBrandDto } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'

export function AdminProductsPage() {
  const { accessToken } = useAuth()
  const [products, setProducts] = useState<AdminProductDto[]>([])
  const [categories, setCategories] = useState<AdminCategoryDto[]>([])
  const [brands, setBrands] = useState<AdminBrandDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [sku, setSku] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [brandId, setBrandId] = useState('')
  const [basePrice, setBasePrice] = useState('')
  const [unit, setUnit] = useState('')
  const [description, setDescription] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    loadData()
  }, [accessToken])

  const loadData = async () => {
    if (!accessToken) return
    try {
      const [pData, cData, bData] = await Promise.all([
        adminCatalogApi.getProducts({ page: 1, pageSize: 100 }),
        adminCatalogApi.getCategories(),
        adminCatalogApi.getBrands()
      ])
      setProducts(pData.items)
      setCategories(cData)
      setBrands(bData)
      
      if (cData.length > 0) setCategoryId(cData[0].id)
      if (bData.length > 0) setBrandId(bData[0].id)
    } catch (err: any) {
      setError(err.message || 'Lỗi tải dữ liệu')
    } finally {
      setLoading(false)
    }
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!accessToken) return
    setIsSubmitting(true)
    setError(null)
    try {
      await adminCatalogApi.createProduct({
        name,
        slug,
        sku,
        categoryId,
        brandId,
        basePrice: parseFloat(basePrice) || 0,
        unit,
        description,
        imageUrl: null
      })
      setName('')
      setSlug('')
      setSku('')
      setBasePrice('')
      setUnit('')
      setDescription('')
      await loadData()
    } catch (err: any) {
      setError(err.data?.message || err.message || 'Lỗi thêm sản phẩm')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (loading) return <div>Loading products...</div>

  return (
    <div className="admin-page">
      <h1>Admin Products Page</h1>
      {error && <div role="alert" className="error">{error}</div>}
      
      <form onSubmit={handleCreate} className="admin-form admin-product-form">
        <label>Tên: <input value={name} onChange={e => setName(e.target.value)} required /></label>
        <label>Slug: <input value={slug} onChange={e => setSlug(e.target.value)} required /></label>
        <label>SKU: <input value={sku} onChange={e => setSku(e.target.value)} required /></label>
        
        <label>
          Danh mục:
          <select value={categoryId} onChange={e => setCategoryId(e.target.value)} required>
            <option value="">-- Chọn danh mục --</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </label>
        
        <label>
          Thương hiệu:
          <select value={brandId} onChange={e => setBrandId(e.target.value)} required>
            <option value="">-- Chọn thương hiệu --</option>
            {brands.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </label>
        
        <label>Giá: <input type="number" step="0.01" value={basePrice} onChange={e => setBasePrice(e.target.value)} required /></label>
        <label>Đơn vị: <input value={unit} onChange={e => setUnit(e.target.value)} required /></label>
        <label>Mô tả: <textarea value={description} onChange={e => setDescription(e.target.value)} /></label>
        
        <button type="submit" disabled={isSubmitting}>Thêm</button>
      </form>

      <ul className="admin-list">
        {products.map(p => (
          <li key={p.id}>
            <strong>{p.name}</strong> (SKU: {p.sku}) - {p.categoryName} / {p.brandName} - {p.basePrice}đ/{p.unit} - {p.isActive ? 'Active' : 'Inactive'}
          </li>
        ))}
      </ul>
    </div>
  )
}
