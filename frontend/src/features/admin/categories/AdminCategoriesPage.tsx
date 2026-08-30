import React, { useEffect, useState } from 'react'
import { adminCatalogApi } from '../../../api/adminCatalogApi'
import { useAuth } from '../../auth/AuthContext'

interface Category {
  id: string
  name: string
  slug: string
  parentCategoryId: string | null
  isActive: boolean
}

export function AdminCategoriesPage() {
  const { accessToken } = useAuth()
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [parentCategoryId, setParentCategoryId] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    loadCategories()
  }, [accessToken])

  const loadCategories = async () => {
    if (!accessToken) return
    try {
      const data = await adminCatalogApi.getCategories()
      setCategories(data)
    } catch (err: any) {
      setError(err.message || 'Lỗi tải danh mục')
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
      const parentId = parentCategoryId.trim() === '' ? null : parentCategoryId
      await adminCatalogApi.createCategory({ name, slug, parentCategoryId: parentId })
      setName('')
      setSlug('')
      setParentCategoryId('')
      await loadCategories()
    } catch (err: any) {
      setError(err.data?.message || err.message || 'Lỗi thêm danh mục')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (loading) return <div>Loading...</div>

  return (
    <div className="admin-page">
      <h1>Admin Categories Page</h1>
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
        <label>
          Danh mục cha:
          <select value={parentCategoryId} onChange={e => setParentCategoryId(e.target.value)}>
            <option value="">Không có</option>
            {categories.map(c => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </label>
        <button type="submit" disabled={isSubmitting}>Thêm</button>
      </form>

      <ul className="admin-list">
        {categories.map(c => (
          <li key={c.id}>
            {c.name} ({c.slug}) - {c.isActive ? 'Active' : 'Inactive'}
          </li>
        ))}
      </ul>
    </div>
  )
}
