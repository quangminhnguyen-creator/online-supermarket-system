import React, { useEffect, useState } from 'react'
import { adminCatalogApi } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'

interface Brand {
  id: string
  name: string
  slug: string
  isActive: boolean
}

export function AdminBrandsPage() {
  const { accessToken } = useAuth()
  const [brands, setBrands] = useState<Brand[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    loadBrands()
  }, [accessToken])

  const loadBrands = async () => {
    if (!accessToken) return
    try {
      const data = await adminCatalogApi.getBrands()
      setBrands(data)
    } catch (err: any) {
      setError(err.message || 'Lỗi tải thương hiệu')
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
      await adminCatalogApi.createBrand({ name, slug })
      setName('')
      setSlug('')
      await loadBrands()
    } catch (err: any) {
      setError(err.data?.message || err.message || 'Lỗi thêm thương hiệu')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (loading) return <div>Loading...</div>

  return (
    <div className="admin-page">
      <h1>Admin Brands Page</h1>
      {error && <div role="alert" className="error">{error}</div>}
      
      <form onSubmit={handleCreate} className="admin-form">
        <label>
          Tên:
          <input value={name} onChange={e => setName(e.target.value)} required />
        </label>
        <label>
          Slug:
          <input value={slug} onChange={e => setSlug(e.target.value)} required />
        </label>
        <button type="submit" disabled={isSubmitting}>Thêm</button>
      </form>

      <ul className="admin-list">
        {brands.map(b => (
          <li key={b.id}>
            {b.name} ({b.slug}) - {b.isActive ? 'Active' : 'Inactive'}
          </li>
        ))}
      </ul>
    </div>
  )
}
