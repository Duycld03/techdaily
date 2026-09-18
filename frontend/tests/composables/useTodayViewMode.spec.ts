import { describe, it, expect, beforeEach } from 'vitest'
import { useTodayViewMode, TODAY_VIEW_MODE_STORAGE_KEY } from '~/composables/useTodayViewMode'

describe('useTodayViewMode.ts', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  it('defaults to bento mode when localStorage is empty', () => {
    const { viewMode } = useTodayViewMode()
    expect(viewMode.value).toBe('bento')
  })

  it('sets viewMode and persists to localStorage', () => {
    const { viewMode, setViewMode } = useTodayViewMode()

    setViewMode('split')
    expect(viewMode.value).toBe('split')
    expect(window.localStorage.getItem(TODAY_VIEW_MODE_STORAGE_KEY)).toBe('split')

    setViewMode('bento')
    expect(viewMode.value).toBe('bento')
    expect(window.localStorage.getItem(TODAY_VIEW_MODE_STORAGE_KEY)).toBe('bento')
  })

  it('ignores invalid view modes', () => {
    const { viewMode, setViewMode } = useTodayViewMode()
    expect(viewMode.value).toBe('bento')

    const unsafeSet = setViewMode as (mode: unknown) => void
    unsafeSet('invalid_mode')
    expect(viewMode.value).toBe('bento')
  })

  it('restores stored mode from localStorage on initialization', () => {
    window.localStorage.setItem(TODAY_VIEW_MODE_STORAGE_KEY, 'split')
    const { viewMode } = useTodayViewMode()
    expect(viewMode.value).toBe('split')
  })
})
