import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '~/stores/useAuthStore'

// Mock useApiClient composable
vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    post: vi.fn(async (url: string, body: any) => {
      if (url.includes('/login') || url.includes('/register') || url.includes('/google')) {
        return {
          token: 'mock-jwt-token-123',
          user: {
            id: 'u-1',
            email: body.email || 'engineer@techdaily.local',
            name: body.name || 'Senior Engineer',
            preferredLocale: body.locale || 'en'
          }
        }
      }
      throw new Error('Unknown endpoint')
    })
  })
}))

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('initializes with logged out state', () => {
    const auth = useAuthStore()
    expect(auth.isLoggedIn).toBe(false)
    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
  })

  it('successfully logs in and stores session in localStorage', async () => {
    const auth = useAuthStore()
    const res = await auth.login('engineer@techdaily.local', 'password123')

    expect(res.token).toBe('mock-jwt-token-123')
    expect(auth.isLoggedIn).toBe(true)
    expect(auth.user?.email).toBe('engineer@techdaily.local')
    expect(localStorage.getItem('techdaily_token')).toBe('mock-jwt-token-123')
  })

  it('successfully registers new user', async () => {
    const auth = useAuthStore()
    const res = await auth.register('newuser@techdaily.local', 'password123', 'New Architect', 'vi')

    expect(res.token).toBe('mock-jwt-token-123')
    expect(auth.isLoggedIn).toBe(true)
    expect(auth.user?.name).toBe('New Architect')
    expect(auth.user?.preferredLocale).toBe('vi')
  })

  it('clears token and user on logout', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@techdaily.local', 'password123')
    expect(auth.isLoggedIn).toBe(true)

    auth.logout()
    expect(auth.isLoggedIn).toBe(false)
    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
    expect(localStorage.getItem('techdaily_token')).toBeNull()
  })

  describe('JWT Expiration and Proactive Cleanup', () => {
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

    it('correctly identifies expired vs active tokens', () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-3600) // 1 hour ago
      const activeToken = createMockJwt(3600) // 1 hour in future

      expect(auth.isTokenExpired(expiredToken)).toBe(true)
      expect(auth.isTokenExpired(activeToken)).toBe(false)
      expect(auth.isTokenExpired(null)).toBe(true)
    })

    it('reports isLoggedIn as false when token is expired', () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-60)
      auth.token = expiredToken

      expect(auth.isLoggedIn).toBe(false)
      expect(auth.isAuthenticated).toBe(false)
    })

    it('reports isLoggedIn as true when token is valid', () => {
      const auth = useAuthStore()
      const activeToken = createMockJwt(3600)
      auth.token = activeToken

      expect(auth.isLoggedIn).toBe(true)
      expect(auth.isAuthenticated).toBe(true)
    })

    it('proactively purges expired token on init()', () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-120)

      localStorage.setItem('techdaily_token', expiredToken)
      localStorage.setItem('techdaily_user', JSON.stringify({ id: 'u-100', email: 'test@techdaily.io', name: 'Test' }))

      auth.init()

      expect(auth.isLoggedIn).toBe(false)
      expect(auth.token).toBeNull()
      expect(auth.user).toBeNull()
      expect(localStorage.getItem('techdaily_token')).toBeNull()
      expect(localStorage.getItem('techdaily_user')).toBeNull()
    })

    it('preserves valid token on init()', () => {
      const auth = useAuthStore()
      const activeToken = createMockJwt(7200)

      localStorage.setItem('techdaily_token', activeToken)
      localStorage.setItem('techdaily_user', JSON.stringify({ id: 'u-100', email: 'test@techdaily.io', name: 'Test' }))

      auth.init()

      expect(auth.isLoggedIn).toBe(true)
      expect(auth.token).toBe(activeToken)
      expect(auth.user?.email).toBe('test@techdaily.io')
    })
  })
})
