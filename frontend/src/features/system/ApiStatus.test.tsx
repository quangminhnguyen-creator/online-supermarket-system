import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as systemApi from '../../api/systemApi'
import { ApiStatus } from './ApiStatus'

describe('ApiStatus', () => {
  afterEach(() => vi.restoreAllMocks())

  it('shows API ready after a successful health check', async () => {
    vi.spyOn(systemApi, 'getHealth').mockResolvedValue({ status: 'ok' })

    render(<ApiStatus />)

    expect(await screen.findByText('API đã sẵn sàng')).toBeInTheDocument()
  })

  it('shows a recoverable error when the API is unavailable', async () => {
    vi.spyOn(systemApi, 'getHealth').mockRejectedValue(new Error('offline'))

    render(<ApiStatus />)

    expect(await screen.findByText('Không thể kết nối API')).toBeInTheDocument()
  })
})
