import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useApiClient, _resetRedirectingToLogin } from '~/composables/useApiClient'
import { useAuthStore } from '~/stores/useAuthStore'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useAdminDashboardStore } from '~/stores/useAdminDashboardStore'
import { adminTestJwt, clearAuthStorage, deferred } from '../helpers/admin'

function signIn(id = 'admin-1', lifetime = 3600) {
  useAuthStore().setSession(adminTestJwt(id, lifetime), { id, name: 'Admin', email: `${id}@example.test`, preferredLocale: 'en' })
}
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status })

describe('administrator API session boundaries', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    vi.clearAllMocks()
    _resetRedirectingToLogin()
    window.history.replaceState({}, '', '/admin/catalog?search=vue')
  })
  afterEach(() => { window.history.replaceState({}, '', '/') })

  it('redirects concurrent terminal 401s to administrator login once and ends the shared session', async () => {
    signIn()
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() => Promise.resolve(json({ detail: 'Session rejected' }, 401))))
    const api = useApiClient()
    await Promise.allSettled([api.get('/api/v1/admin/dashboard/overview'), api.get('/api/v1/admin/dashboard/status')])
    expect(useAuthStore().user).toBeNull()
    expect(navigateTo).toHaveBeenCalledTimes(1)
    expect(navigateTo).toHaveBeenCalledWith({ path: '/admin/login', query: { redirect: '/admin/catalog?search=vue' } })
  })

  it('clears privileged state on 403 while preserving the learner identity and credentials', async () => {
    signIn()
    const auth = useAuthStore()
    const access = useAdminAccessStore()
    access.state = 'authorized'
    const dashboard = useAdminDashboardStore()
    dashboard.aiProbe = { checkedAt: '2026-10-14T10:00:00Z' } as never
    const token = auth.token
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ detail: 'Forbidden' }, 403)))
    await expect(useApiClient().get('/api/v1/admin/dashboard/overview')).rejects.toMatchObject({ status: 403 })
    expect(access.isAuthorized).toBe(false)
    expect(dashboard.aiProbe).toBeNull()
    expect(auth.token).toBe(token)
    expect(auth.user?.id).toBe('admin-1')
    expect(navigateTo).toHaveBeenCalledWith(expect.objectContaining({ path: '/admin/login' }))
  })

  it('renews a short-lived session once for concurrent requests and keeps administrator access', async () => {
    signIn('admin-1', 30)
    const access = useAdminAccessStore()
    access.state = 'authorized'
    const dashboard = useAdminDashboardStore()
    const observation = { checkedAt: '2026-10-14T10:00:00Z' } as never
    dashboard.aiProbe = observation
    const renewed = adminTestJwt('admin-1', 7200)
    const fetchMock = vi.fn().mockImplementation(async (url: string) => url.endsWith('/auth/refresh')
      ? json({ token: renewed, user: useAuthStore().user }) : json({ ok: true }))
    vi.stubGlobal('fetch', fetchMock)
    const api = useApiClient()
    await Promise.all([api.get('/api/v1/admin/dashboard/status'), api.get('/api/v1/admin/dashboard/overview')])
    expect(fetchMock.mock.calls.filter(([url]) => String(url).endsWith('/auth/refresh'))).toHaveLength(1)
    expect(useAuthStore().token).toBe(renewed)
    expect(access.isAuthorized).toBe(true)
    expect(dashboard.aiProbe).toEqual(observation)
    expect(navigateTo).not.toHaveBeenCalled()
  })

  it('aborts a privileged request and discards its response after account replacement', async () => {
    signIn()
    const pending = deferred<Response>()
    const fetchMock = vi.fn().mockReturnValue(pending.promise)
    vi.stubGlobal('fetch', fetchMock)
    const request = useApiClient().get('/api/v1/admin/catalog/documents')
    const rejected = expect(request).rejects.toMatchObject({ code: 'SESSION_CHANGED' })
    signIn('admin-2')
    expect(fetchMock.mock.calls[0]?.[1].signal.aborted).toBe(true)
    pending.resolve(json({ privateResult: 'old-account' }))
    await rejected
    expect(useAuthStore().user?.id).toBe('admin-2')
  })

  it('discards an old refresh response instead of restoring the logged-out account', async () => {
    signIn()
    const pending = deferred<Response>()
    vi.stubGlobal('fetch', vi.fn().mockReturnValue(pending.promise))
    const request = useApiClient().refreshAuthToken()
    const rejected = expect(request).rejects.toMatchObject({ code: 'SESSION_CHANGED' })
    useAuthStore().clearSession()
    pending.resolve(json({ token: adminTestJwt(), user: { id: 'admin-1' } }))
    await rejected
    expect(useAuthStore().token).toBeNull()
  })

  it('retains credentials and reports transient refresh failure without a login redirect', async () => {
    signIn()
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url: string) => json({}, url.endsWith('/auth/refresh') ? 503 : 401)))
    await expect(useApiClient().get('/api/v1/admin/dashboard/status')).rejects.toMatchObject({ status: 503 })
    expect(useAuthStore().isLoggedIn).toBe(true)
    expect(navigateTo).not.toHaveBeenCalled()
  })
})
