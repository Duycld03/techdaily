import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '~/stores/useAuthStore'
import authMiddleware from '~/middleware/auth.global'

function createMockJwt(expOffsetSeconds: number): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
  const payload = btoa(JSON.stringify({
    nameid: 'u-100',
    email: 'test@techdaily.io',
    unique_name: 'Test Engineer',
    exp: Math.floor(Date.now() / 1000) + expOffsetSeconds
  }))
  return `${header}.${payload}.mockSignature`
}

describe('auth.global route middleware', () => {
  let authStore: ReturnType<typeof useAuthStore>

  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    useCookie('techdaily_token').value = null
    useCookie('techdaily_user').value = null
    authStore = useAuthStore()
    vi.clearAllMocks()
  })
  describe('unauthenticated visitor access', () => {
    it('redirects /library to /login with redirect parameter', async () => {
      const to = { path: '/library', fullPath: '/library' } as any
      await (authMiddleware as any)(to)

      expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
        path: '/login',
        query: { redirect: '/library' }
      })
    })

    it('redirects /read/[bookId] to /login preserving query params', async () => {
      const to = { path: '/read/book-123', fullPath: '/read/book-123?slice=4' } as any
      await (authMiddleware as any)(to)

      expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
        path: '/login',
        query: { redirect: '/read/book-123?slice=4' }
      })
    })

    it('redirects other protected routes such as /today and /notes', async () => {
      const to = { path: '/today', fullPath: '/today' } as any
      await (authMiddleware as any)(to)

      expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
        path: '/login',
        query: { redirect: '/today' }
      })
    })

    it('permits unauthenticated visitor on /playground and /showcase without redirect', async () => {
      const playgroundTo = { path: '/playground/phase-2', fullPath: '/playground/phase-2' } as any
      const showcaseTo = { path: '/showcase', fullPath: '/showcase' } as any

      await (authMiddleware as any)(playgroundTo)
      await (authMiddleware as any)(showcaseTo)

      expect((globalThis as any).navigateTo).not.toHaveBeenCalled()
    })
  })

  describe('authenticated user access', () => {
    beforeEach(() => {
      authStore.token = 'valid-token'
      authStore.user = { id: 'u1', email: 'test@techdaily.local', name: 'Test User' } as any
    })

    it('permits authenticated user on /library without redirect', async () => {
      const to = { path: '/library', fullPath: '/library' } as any
      const result = await (authMiddleware as any)(to)

      expect(result).toBeUndefined()
      expect((globalThis as any).navigateTo).not.toHaveBeenCalled()
    })

    it('permits authenticated user on /read/[bookId] without redirect', async () => {
      const to = { path: '/read/book-123', fullPath: '/read/book-123?slice=2' } as any
      const result = await (authMiddleware as any)(to)

      expect(result).toBeUndefined()
      expect((globalThis as any).navigateTo).not.toHaveBeenCalled()
    })

    it('redirects logged-in user away from /login to /', async () => {
      const to = { path: '/login', fullPath: '/login' } as any
      await (authMiddleware as any)(to)

      expect((globalThis as any).navigateTo).toHaveBeenCalledWith('/')
    })
  })

  describe('expired token refresh behavior', () => {
    it('attempts transparent refresh when access token is expired on protected route', async () => {
      const expiredToken = createMockJwt(-120)
      authStore.token = expiredToken
      const tryRefreshSpy = vi.spyOn(authStore, 'tryRefreshToken').mockImplementation(async () => {
        authStore.token = createMockJwt(3600)
        return true
      })

      const to = { path: '/library', fullPath: '/library' } as any
      const result = await (authMiddleware as any)(to)

      expect(tryRefreshSpy).toHaveBeenCalledTimes(1)
      expect((globalThis as any).navigateTo).not.toHaveBeenCalled()
      expect(result).toBeUndefined()
    })

    it('redirects to /login when transparent refresh fails for expired token', async () => {
      const expiredToken = createMockJwt(-120)
      authStore.token = expiredToken
      const tryRefreshSpy = vi.spyOn(authStore, 'tryRefreshToken').mockResolvedValue(false)

      const to = { path: '/review', fullPath: '/review' } as any
      await (authMiddleware as any)(to)

      expect(tryRefreshSpy).toHaveBeenCalledTimes(1)
      expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
        path: '/login',
        query: { redirect: '/review' }
      })
    })
  })
})
