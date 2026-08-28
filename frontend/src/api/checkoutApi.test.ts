import { afterEach, expect, it, vi } from 'vitest'
import { checkoutApi, type CheckoutResponse } from './checkoutApi'

function jsonResponse(data: unknown) {
  return new Response(JSON.stringify(data), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => vi.unstubAllGlobals())

const token = 'jwt-token'
const checkoutResponse: CheckoutResponse = {
  orderId: 'order-1',
  subtotal: 100000,
  discountAmount: 0,
  shippingFee: 0,
  totalAmount: 100000,
  status: 'Pending',
  payment: {
    paymentId: '00000000-0000-0000-0000-000000000000',
    method: '',
    status: 'Pending',
    checkoutUrl: null,
  },
}

it('creates a pickup order with bearer token', async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(checkoutResponse))
  vi.stubGlobal('fetch', fetchMock)

  await checkoutApi.checkout({ fulfillmentType: 'Pickup' }, token)

  expect(fetchMock).toHaveBeenCalledWith(
    '/api/checkout',
    expect.objectContaining({
      method: 'POST',
      headers: expect.objectContaining({ Authorization: 'Bearer jwt-token' }),
      body: JSON.stringify({ fulfillmentType: 'Pickup' }),
    })
  )
})

it('initiates payment with exact body', async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    jsonResponse({
      paymentId: 'payment-1',
      method: 'COD',
      status: 'PendingCollection',
      checkoutUrl: null,
    })
  )
  vi.stubGlobal('fetch', fetchMock)

  await checkoutApi.initiatePayment({ orderId: 'order-1', method: 'COD' }, token)

  expect(fetchMock).toHaveBeenCalledWith(
    '/api/checkout/payment',
    expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ orderId: 'order-1', method: 'COD' }),
    })
  )
})

it('creates a delivery order with recipient snapshot', async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(checkoutResponse))
  vi.stubGlobal('fetch', fetchMock)

  await checkoutApi.checkout(
    {
      fulfillmentType: 'Delivery',
      deliveryAddressId: 'address-1',
      recipientName: 'Nguyen Van A',
      recipientPhone: '0900000000',
      deliveryAddress: '12 Nguyen Trai, Quan 1, TP.HCM',
    },
    token
  )

  expect(fetchMock).toHaveBeenCalledWith(
    '/api/checkout',
    expect.objectContaining({
      body: JSON.stringify({
        fulfillmentType: 'Delivery',
        deliveryAddressId: 'address-1',
        recipientName: 'Nguyen Van A',
        recipientPhone: '0900000000',
        deliveryAddress: '12 Nguyen Trai, Quan 1, TP.HCM',
      }),
    })
  )
})

it.each(['VNPay', 'MoMo'] as const)('accepts %s payment method', async (method) => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(
      jsonResponse({
        paymentId: 'payment-1',
        method,
        status: 'Pending',
        checkoutUrl: 'https://sandbox.example/pay',
      })
    )
  )
  await checkoutApi.initiatePayment({ orderId: 'order-1', method }, token)
  expect(fetch).toHaveBeenCalledWith(
    '/api/checkout/payment',
    expect.objectContaining({
      body: JSON.stringify({ orderId: 'order-1', method }),
    })
  )
})

it('passes abort signal to checkout and payment requests', async () => {
  const fetchMock = vi.fn().mockImplementation(() =>
    Promise.resolve(
      jsonResponse({
        orderId: 'order-1',
        subtotal: 100000,
        discountAmount: 0,
        shippingFee: 0,
        totalAmount: 100000,
        status: 'Pending',
        payment: null,
      })
    )
  )
  vi.stubGlobal('fetch', fetchMock)
  const controller = new AbortController()

  await checkoutApi.checkout({ fulfillmentType: 'Pickup' }, token, controller.signal)
  expect(fetchMock).toHaveBeenCalledWith(
    '/api/checkout',
    expect.objectContaining({
      signal: controller.signal,
    })
  )

  await checkoutApi.initiatePayment(
    { orderId: 'order-1', method: 'COD' },
    token,
    controller.signal
  )
  expect(fetchMock).toHaveBeenCalledWith(
    '/api/checkout/payment',
    expect.objectContaining({
      signal: controller.signal,
    })
  )
})

