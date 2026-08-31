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
})
