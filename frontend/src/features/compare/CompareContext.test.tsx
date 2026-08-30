import { act, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CompareProvider, useCompare, type CompareProduct } from './CompareContext'

const phoneA: CompareProduct = {
  id: 'phone-a',
  categoryId: 'phone-leaf',
  categoryName: 'Điện thoại',
  categorySlug: 'dien-thoai',
}
const phoneB: CompareProduct = {
  id: 'phone-b',
  categoryId: 'phone-leaf',
  categoryName: 'Điện thoại',
  categorySlug: 'dien-thoai',
}
const tablet: CompareProduct = {
  id: 'tablet-a',
  categoryId: 'tablet-leaf',
  categoryName: 'Máy tính bảng',
  categorySlug: 'may-tinh-bang',
}
const unknown: CompareProduct = {
  id: 'unknown-a',
  categoryId: 'uncategorized-id',
  categoryName: 'Chưa phân loại',
  categorySlug: 'uncategorized',
}

function Harness() {
  const ctx = useCompare()
  return (
    <div>
      <span data-testid="count">{ctx.compareProducts.length}</span>
      <span data-testid="canAddMore">{String(ctx.canAddMore)}</span>
      <span data-testid="hasProduct">{String(ctx.hasProduct)}</span>
      <span data-testid="modalOpen">{String(ctx.isModalOpen)}</span>
      <button onClick={() => ctx.addToCompare(phoneA)}>add-phone-a</button>
      <button onClick={() => ctx.addToCompare(phoneB)}>add-phone-b</button>
      <button onClick={() => ctx.addToCompare(tablet)}>add-tablet</button>
      <button onClick={() => ctx.addToCompare(unknown)}>add-unknown</button>
      <button onClick={() => ctx.removeFromCompare(phoneA.id)}>remove-phone-a</button>
      <button onClick={() => ctx.clearCompare()}>clear</button>
      <button onClick={ctx.openModal}>open</button>
      <button onClick={ctx.closeModal}>close</button>
      <span data-testid="warning">{ctx.getDifferentCategoryWarning(tablet)}</span>
      <span data-testid="unknown-warning">{ctx.getDifferentCategoryWarning(unknown)}</span>
    </div>
  )
}

function renderHarness() {
  return render(
    <CompareProvider>
      <Harness />
    </CompareProvider>
  )
}

describe('CompareContext', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('allows adding up to 2 products of the same leaf category', () => {
    renderHarness()
    act(() => {
      screen.getByRole('button', { name: 'add-phone-a' }).click()
      screen.getByRole('button', { name: 'add-phone-b' }).click()
    })
    expect(screen.getByTestId('count')).toHaveTextContent('2')
    expect(screen.getByTestId('canAddMore')).toHaveTextContent('false')
    expect(screen.getByTestId('hasProduct')).toHaveTextContent('true')
  })

  it('rejects a third product when the list is full', () => {
    renderHarness()
    act(() => {
      screen.getByRole('button', { name: 'add-phone-a' }).click()
      screen.getByRole('button', { name: 'add-phone-b' }).click()
    })
    act(() => {
      screen.getByRole('button', { name: 'add-tablet' }).click()
    })
    expect(screen.getByTestId('count')).toHaveTextContent('2')
  })

  it('rejects products from different leaf categories', () => {
    renderHarness()
    act(() => {
      screen.getByRole('button', { name: 'add-phone-a' }).click()
    })
    act(() => {
      screen.getByRole('button', { name: 'add-tablet' }).click()
    })
    expect(screen.getByTestId('count')).toHaveTextContent('1')
    expect(screen.getByTestId('warning')).toHaveTextContent(
      'Chỉ có thể so sánh các sản phẩm cùng loại.',
    )
  })

  it('rejects an uncategorized product even when compare list is empty', () => {
    renderHarness()
    act(() => {
      screen.getByRole('button', { name: 'add-unknown' }).click()
    })
    expect(screen.getByTestId('count')).toHaveTextContent('0')
    expect(screen.getByTestId('unknown-warning')).toHaveTextContent(
      'Sản phẩm chưa được phân loại nên chưa thể so sánh.',
    )
  })

  it('removes a single product', () => {
    renderHarness()
    act(() => {
      screen.getByRole('button', { name: 'add-phone-a' }).click()
      screen.getByRole('button', { name: 'add-phone-b' }).click()
      screen.getByRole('button', { name: 'remove-phone-a' }).click()
    })
    expect(screen.getByTestId('count')).toHaveTextContent('1')
    expect(screen.getByTestId('canAddMore')).toHaveTextContent('true')
  })

  it('clears all products', () => {
    renderHarness()
    act(() => {
      screen.getByRole('button', { name: 'add-phone-a' }).click()
      screen.getByRole('button', { name: 'add-phone-b' }).click()
      screen.getByRole('button', { name: 'clear' }).click()
    })
    expect(screen.getByTestId('count')).toHaveTextContent('0')
    expect(screen.getByTestId('hasProduct')).toHaveTextContent('false')
  })

  it('toggles the modal via openModal and closeModal', () => {
    renderHarness()
    act(() => {
      screen.getByRole('button', { name: 'open' }).click()
    })
    expect(screen.getByTestId('modalOpen')).toHaveTextContent('true')
    act(() => {
      screen.getByRole('button', { name: 'close' }).click()
    })
    expect(screen.getByTestId('modalOpen')).toHaveTextContent('false')
  })

  it('opens the modal on the global open-compare-modal event', () => {
    renderHarness()
    act(() => {
      window.dispatchEvent(new Event('open-compare-modal'))
    })
    expect(screen.getByTestId('modalOpen')).toHaveTextContent('true')
  })
})