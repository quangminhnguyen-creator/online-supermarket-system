import React from 'react'
import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { CheckoutSuccessPage } from './CheckoutSuccessPage'

describe('CheckoutSuccessPage', () => {
  it('renders checkout success with order id, payment status and next actions', () => {
    render(
      <MemoryRouter
        initialEntries={[
          '/shopping/checkout/success?orderId=order-123&paymentStatus=PendingCollection',
        ]}
      >
        <CheckoutSuccessPage />
      </MemoryRouter>
    )

    expect(
      screen.getByRole('heading', { name: 'Đặt hàng thành công' })
    ).toBeInTheDocument()
    expect(screen.getByText('order-123')).toBeInTheDocument()
    expect(screen.getByText('PendingCollection')).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Tiếp tục mua sắm' })
    ).toHaveAttribute('href', '/browse')
    expect(
      screen.getByRole('link', { name: 'Xem đơn hàng' })
    ).toHaveAttribute('href', '/orders/history')
  })

  it('links checkout success to order history', () => {
    render(
      <MemoryRouter initialEntries={['/shopping/checkout/success']}>
        <CheckoutSuccessPage />
      </MemoryRouter>
    )
    expect(screen.getByRole('link', { name: 'Xem đơn hàng' })).toHaveAttribute('href', '/orders/history')
  })
})
