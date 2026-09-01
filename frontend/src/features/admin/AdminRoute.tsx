import React from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function AdminRoute() {
  const { user, isLoading } = useAuth()

  if (isLoading) {
    return <div>Đang tải...</div>
  }

  if (!user || user.role !== 'Admin') {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
