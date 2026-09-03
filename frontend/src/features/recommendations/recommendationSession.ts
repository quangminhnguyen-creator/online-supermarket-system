const ANONYMOUS_SESSION_KEY = 'os_anonymous_session_id'
const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

function createGuid(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (char) => {
    const random = (Math.random() * 16) | 0
    const value = char === 'x' ? random : (random & 0x3) | 0x8
    return value.toString(16)
  })
}

export function isAnonymousSessionId(value: string | null | undefined): value is string {
  return typeof value === 'string' && GUID_PATTERN.test(value)
}

export function getOrCreateAnonymousSessionId(): string {
  const existing = window.localStorage.getItem(ANONYMOUS_SESSION_KEY)
  if (isAnonymousSessionId(existing)) return existing
  const fresh = createGuid()
  window.localStorage.setItem(ANONYMOUS_SESSION_KEY, fresh)
  return fresh
}

export function rotateAnonymousSessionId(): string {
  const fresh = createGuid()
  window.localStorage.setItem(ANONYMOUS_SESSION_KEY, fresh)
  return fresh
}