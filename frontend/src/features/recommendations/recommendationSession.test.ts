import { afterEach, describe, expect, it } from 'vitest'
import {
  getOrCreateAnonymousSessionId,
  isAnonymousSessionId,
  rotateAnonymousSessionId,
} from './recommendationSession'

afterEach(() => window.localStorage.clear())

describe('recommendationSession', () => {
  it('creates and reuses a valid anonymous guid', () => {
    const first = getOrCreateAnonymousSessionId()
    const second = getOrCreateAnonymousSessionId()

    expect(isAnonymousSessionId(first)).toBe(true)
    expect(second).toBe(first)
  })

  it('ignores a corrupted stored value and creates a fresh guid', () => {
    window.localStorage.setItem('os_anonymous_session_id', 'not-a-guid')

    const fresh = getOrCreateAnonymousSessionId()

    expect(isAnonymousSessionId(fresh)).toBe(true)
    expect(fresh).not.toBe('not-a-guid')
  })

  it('rotateAnonymousSessionId replaces with a new valid guid', () => {
    const first = getOrCreateAnonymousSessionId()
    const rotated = rotateAnonymousSessionId()

    expect(isAnonymousSessionId(rotated)).toBe(true)
    expect(rotated).not.toBe(first)
  })
})