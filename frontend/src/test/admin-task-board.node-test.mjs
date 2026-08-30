// Run explicitly with Node's built-in test runner; Vitest must not collect this file.
import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'
import { JSDOM } from 'jsdom'

const boardUrl = new URL('../../../docs/tasks/admin-catalog-task-board.html', import.meta.url)
const storageKey = 'adminCatalogTaskBoard:v1'

async function openBoard(savedState) {
  const html = await readFile(boardUrl, 'utf8')
  const dom = new JSDOM(html, {
    runScripts: 'dangerously',
    url: 'https://aptechmart.local/admin-catalog-task-board',
    beforeParse(window) {
      if (savedState) window.localStorage.setItem(storageKey, savedState)
    },
  })
  return dom
}

test('renders all nine sequential implementation tasks', async () => {
  const dom = await openBoard()
  const document = dom.window.document

  const cards = [...document.querySelectorAll('[data-task-card]')]
  assert.equal(cards.length, 9)
  assert.deepEqual(
    cards.map((card) => card.getAttribute('data-task-id')),
    ['1', '2', '3', '4', '5', '6', '7', '8', '9']
  )
  assert.match(cards[0].textContent, /Catalog domain behavior/)
  assert.match(cards[8].textContent, /Final verification/)
})

test('persists task status and restores it on the next open', async () => {
  const first = await openBoard()
  const select = first.window.document.querySelector('[data-task-id="1"] [data-task-status]')
  select.value = 'done'
  select.dispatchEvent(new first.window.Event('change', { bubbles: true }))

  assert.match(first.window.document.querySelector('[data-progress-label]').textContent, /1\/9 task/)
  const savedState = first.window.localStorage.getItem(storageKey)
  assert.ok(savedState)

  const second = await openBoard(savedState)
  assert.equal(
    second.window.document.querySelector('[data-task-id="1"] [data-task-status]').value,
    'done'
  )
})

test('filters task cards by status', async () => {
  const dom = await openBoard()
  const document = dom.window.document
  const firstStatus = document.querySelector('[data-task-id="1"] [data-task-status]')
  firstStatus.value = 'doing'
  firstStatus.dispatchEvent(new dom.window.Event('change', { bubbles: true }))

  const filter = document.querySelector('[data-status-filter]')
  filter.value = 'doing'
  filter.dispatchEvent(new dom.window.Event('change', { bubbles: true }))

  const visibleCards = [...document.querySelectorAll('[data-task-card]')]
    .filter((card) => !card.hidden)
  assert.equal(visibleCards.length, 1)
  assert.equal(visibleCards[0].getAttribute('data-task-id'), '1')
})

test('reset clears saved progress and returns every task to not started', async () => {
  const dom = await openBoard()
  const document = dom.window.document
  const firstStatus = document.querySelector('[data-task-id="1"] [data-task-status]')
  firstStatus.value = 'done'
  firstStatus.dispatchEvent(new dom.window.Event('change', { bubbles: true }))

  document.querySelector('[data-reset-board]').click()

  assert.equal(dom.window.localStorage.getItem(storageKey), null)
  assert.ok([...document.querySelectorAll('[data-task-status]')].every((select) => select.value === 'todo'))
  assert.match(document.querySelector('[data-progress-label]').textContent, /0\/9 task/)
})
