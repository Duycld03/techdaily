import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '~/stores/useAuthStore'
import authMiddleware from '~/middleware/auth.global'

describe('auth.global route middleware', () => {
  let authStore: ReturnType<typeof useAuthStore>

  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    authStore = useAuthStore()
    vi.clearAllMocks()
  })

  describe('unauthenticated visitor access', () => {
    it('redirects /library to /login with redirect parameter', () => {
      const to = { path: '/library', fullPath: '/library' } as any
      ;(authMiddleware as any)(to)

      expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
        path: '/login',
        query: { redirect: '/library' }
      })
    })

    it('redirects /read/[bookId] to /login preserving query params', () => {
      const to = { path: '/read/book-123', fullPath: '/read/book-123?slice=4' } as any
      ;(authMiddleware as any)(to)

      expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
        path: '/login',
        query: { redirect: '/read/book-123?slice=4' }
      })
    })

    it('redirects other protected routes such as /today and /notes', () => {
      const to = { path: '/today', fullPath: '/today' } as any
      (authMiddleware as any)(to)

      expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
        path: '/login',
        query: { redirect: '/today' }
      })
    })

    it('permits unauthenticated visitor on /playground and /showcase without redirect', () => {
      const playgroundTo = { path: '/playground/phase-2', fullPath: '/playground/phase-2' } as any
      const showcaseTo = { path: '/showcase', fullPath: '/showcase' } as any

      ;(authMiddleware as any)(playgroundTo)
      ;(authMiddleware as any)(showcaseTo)

      expect((globalThis as any).navigateTo).not.toHaveBeenCalled()
    })
  })

  describe('authenticated user access', () => {
    beforeEach(() => {
      authStore.token = 'valid-token'
      authStore.user = { id: 'u1', email: 'test@techdaily.local', name: 'Test User' } as any
    })

    it('permits authenticated user on /library without redirect', () => {
      const to = { path: '/library', fullPath: '/library' } as any
      const result = (authMiddleware as any)(to)

      expect(result).toBeUndefined()
      expect((globalThis as any).navigateTo).not.toHaveBeenCalled()
    })

    it('permits authenticated user on /read/[bookId] without redirect', () => {
      const to = { path: '/read/book-123', fullPath: '/read/book-123?slice=2' } as any
      const result = (authMiddleware as any)(to)

      expect(result).toBeUndefined()
      expect((globalThis as any).navigateTo).not.toHaveBeenCalled()
    })

    it('redirects logged-in user away from /login to /', () => {
      const to = { path: '/login', fullPath: '/login' } as any
      (authMiddleware as any)(to)

      expect((globalThis as any).navigateTo).toHaveBeenCalledWith('/')
    })
  })
})
