import { describe, it, expect, beforeEach, vi, type Mock } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useApiClient, _resetRedirectingToLogin } from '~/composables/useApiClient'
import { useAuthStore } from '~/stores/useAuthStore'
import { useToast } from '~/composables/useToast'

interface GlobalWithNavigateTo {
  navigateTo: Mock
}

describe('useApiClient 401 Interceptor', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    const toast = useToast()
    toast.clear()
    vi.clearAllMocks()
    _resetRedirectingToLogin()
    vi.clearAllMocks()
  })

  it('performs successful GET request', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      text: async () => JSON.stringify({ message: 'success' })
    })

    const api = useApiClient()
    const res = await api.get<{ message: string }>('/api/v1/health')
    expect(res.message).toBe('success')
  })

  it('intercepts 401 Unauthorized, purges session, triggers toast warning, and redirects to login', async () => {
    const auth = useAuthStore()
    auth.token = 'stale-expired-jwt'
    auth.user = { id: 'u-1', email: 'test@example.com', name: 'Test', preferredLocale: 'en' }
    localStorage.setItem('deeppace_token', 'stale-expired-jwt')

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      json: async () => ({ detail: 'Token expired', code: 'UNAUTHORIZED' })
    })

    const api = useApiClient()

    await expect(api.get('/api/v1/daily/today')).rejects.toThrow('Token expired')

    // 1. Session must be cleared
    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
    expect(localStorage.getItem('deeppace_token')).toBeNull()

    // 2. Toast warning must be emitted with i18n key
    const toast = useToast()
    expect(toast.toasts.value.length).toBeGreaterThan(0)
    expect(toast.toasts.value[0]?.type).toBe('warning')
    expect(toast.toasts.value[0]?.message).toBe('auth.session_expired')

    // 3. navigateTo must be called with redirect to login
    expect((globalThis as unknown as GlobalWithNavigateTo).navigateTo).toHaveBeenCalledWith(
      expect.objectContaining({
        path: '/login'
      })
    )
  })

  it('does not trigger session expiration redirect for login endpoint failures', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      json: async () => ({ detail: 'Invalid email or password.', code: 'AUTH_INVALID_CREDENTIALS' })
    })

    const api = useApiClient()

    await expect(api.post('/api/v1/auth/login', { email: 'a@b.com', password: 'wrong' })).rejects.toThrow('Invalid email or password.')

    const toast = useToast()
    expect(toast.toasts.value.length).toBe(0)
    expect((globalThis as unknown as GlobalWithNavigateTo).navigateTo).not.toHaveBeenCalled()
  })
  it('deduplicates concurrent 401 Unauthorized responses to trigger exactly one navigateTo call', async () => {
    _resetRedirectingToLogin()
    const auth = useAuthStore()
    auth.token = 'stale-expired-jwt'

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      json: async () => ({ detail: 'Token expired', code: 'UNAUTHORIZED' })
    })

    const api = useApiClient()

    // Dispatch 3 concurrent requests returning 401
    await Promise.allSettled([
      api.get('/api/v1/daily/today'),
      api.get('/api/v1/review/deck'),
      api.get('/api/v1/graph')
    ])

    // Exactly one navigateTo call should be triggered
    const navMock = (globalThis as unknown as GlobalWithNavigateTo).navigateTo
    expect(navMock).toHaveBeenCalledTimes(1)
    expect(navMock).toHaveBeenCalledWith(
      expect.objectContaining({
        path: '/login'
      })
    )
  })
  it('does not purge session credentials when token refresh encounters a network drop or 502/503 during server restart', async () => {
    _resetRedirectingToLogin()
    const auth = useAuthStore()
    auth.token = 'active-jwt-token'
    auth.user = { id: 'u-1', email: 'test@example.com', name: 'Test', preferredLocale: 'en' }
    localStorage.setItem('deeppace_token', 'active-jwt-token')

    // Initial request returns 401, but the subsequent refresh call throws a network error (server restarting)
    globalThis.fetch = vi.fn().mockImplementation((url: string) => {
      if (url.includes('/api/v1/auth/refresh')) {
        return Promise.reject(new TypeError('Failed to fetch'))
      }
      return Promise.resolve({
        ok: false,
        status: 401,
        json: async () => ({ detail: 'Token expired', code: 'UNAUTHORIZED' })
      })
    })

    const api = useApiClient()

    await expect(api.get('/api/v1/daily/today')).rejects.toThrow()
    // Session credentials must NOT be purged on network error
    expect(auth.token).toBe('active-jwt-token')
    expect(auth.user?.email).toBe('test@example.com')
    expect(localStorage.getItem('deeppace_token')).toBe('active-jwt-token')

    // Must NOT navigate to /login
    expect((globalThis as unknown as GlobalWithNavigateTo).navigateTo).not.toHaveBeenCalled()
  })
})

describe('useApiClient Base URL Resolution', () => {
  it('prepends runtimeConfig.public.apiBaseUrl to requests when configured', async () => {
    const originalRuntimeConfig = (globalThis as any).useRuntimeConfig
    ;(globalThis as any).useRuntimeConfig = () => ({
      public: { apiBaseUrl: 'https://api.techdaily.app' }
    })

    const fetchSpy = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      text: async () => JSON.stringify({ ok: true })
    })
    globalThis.fetch = fetchSpy

    try {
      const api = useApiClient()
      await api.get('/api/v1/health')
      expect(fetchSpy).toHaveBeenCalledWith('https://api.techdaily.app/api/v1/health', expect.any(Object))
    } finally {
      ;(globalThis as any).useRuntimeConfig = originalRuntimeConfig
    }
  })

  it('uses relative path when runtimeConfig.public.apiBaseUrl is empty in production', async () => {
    const originalRuntimeConfig = (globalThis as any).useRuntimeConfig
    ;(globalThis as any).useRuntimeConfig = () => ({
      public: { apiBaseUrl: '' }
    })

    const fetchSpy = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      text: async () => JSON.stringify({ ok: true })
    })
    globalThis.fetch = fetchSpy

    try {
      const api = useApiClient()
      await api.get('/api/v1/health')
      expect(fetchSpy).toHaveBeenCalledWith('/api/v1/health', expect.any(Object))
    } finally {
      ;(globalThis as any).useRuntimeConfig = originalRuntimeConfig
    }
  })
})
