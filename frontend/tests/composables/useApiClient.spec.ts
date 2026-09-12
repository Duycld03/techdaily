import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useApiClient } from '~/composables/useApiClient'
import { useAuthStore } from '~/stores/useAuthStore'
import { useToast } from '~/composables/useToast'

describe('useApiClient 401 Interceptor', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    const toast = useToast()
    toast.clear()
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
    localStorage.setItem('techdaily_token', 'stale-expired-jwt')

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      json: async () => ({ error: 'Token expired' })
    })

    const api = useApiClient()

    await expect(api.get('/api/v1/daily/today')).rejects.toThrow('Token expired')

    // 1. Session must be cleared
    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
    expect(localStorage.getItem('techdaily_token')).toBeNull()

    // 2. Toast warning must be emitted with i18n key
    const toast = useToast()
    expect(toast.toasts.value.length).toBeGreaterThan(0)
    expect(toast.toasts.value[0].type).toBe('warning')
    expect(toast.toasts.value[0].message).toBe('auth.session_expired')

    // 3. navigateTo must be called with redirect to login
    expect(globalThis.navigateTo).toHaveBeenCalledWith(
      expect.objectContaining({
        path: '/login'
      })
    )
  })

  it('does not trigger session expiration redirect for login endpoint failures', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      json: async () => ({ error: 'Invalid email or password.' })
    })

    const api = useApiClient()

    await expect(api.post('/api/v1/auth/login', { email: 'a@b.com', password: 'wrong' })).rejects.toThrow('Invalid email or password.')

    const toast = useToast()
    expect(toast.toasts.value.length).toBe(0)
    expect(globalThis.navigateTo).not.toHaveBeenCalled()
  })
})
