import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import middleware from '~/middleware/auth.global'
import { useAuthStore } from '~/stores/useAuthStore'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { adminTestJwt, clearAuthStorage } from '../helpers/admin'
const run = (path: string, query = {}) => middleware({ path, fullPath: path, query } as never, {} as never)
const environment = process as typeof process & { server?: boolean }

describe('administrator route admission', () => {
  beforeEach(() => {
    setActivePinia(createPinia()); clearAuthStorage(); vi.clearAllMocks(); environment.server = false
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ canManageCatalog: true, canManageAccounts: true, canReadAudit: true }))))
  })
  afterEach(() => { environment.server = false; vi.unstubAllGlobals() })
  function signIn() { useAuthStore().setSession(adminTestJwt(), { id: 'admin-1', name: 'Admin', email: 'admin@example.test', preferredLocale: 'en' }) }
  it.each(['/admin', '/admin/catalog', '/admin/documents', '/admin/missing'])('redirects unauthenticated SSR request %s to administrator login', async path => {
    environment.server = true
    await run(path)
    expect(navigateTo).toHaveBeenCalledWith({ path: '/admin/login', query: { redirect: path } })
    expect(fetch).not.toHaveBeenCalled()
  })
  it('verifies administrator permission on SSR before permitting protected rendering', async () => {
    signIn(); environment.server = true
    const token = useAuthStore().token
    await run('/admin/catalog')
    expect(fetch).toHaveBeenCalledWith(expect.stringMatching(/\/api\/v1\/(admin|catalog)\/capabilities/), expect.objectContaining({ headers: expect.objectContaining({ Authorization: `Bearer ${token}` }) }))
    expect(useAdminAccessStore().isAuthorized).toBe(true)
    expect(navigateTo).not.toHaveBeenCalled()
  })
  it('keeps SSR neutral when only a refresh cookie can restore the session', async () => {
    environment.server = true
    useCookie('refreshToken').value = 'opaque-refresh-cookie'
    await run('/admin')
    expect(useAdminAccessStore().isAuthorized).toBe(false)
    expect(useAdminAccessStore().state).toBe('pending')
    expect(fetch).not.toHaveBeenCalled()
    expect(navigateTo).not.toHaveBeenCalled()
  })
  it('preserves neutral SSR markup during hydration and lets the mounted boundary renew the session', async () => {
    vi.stubGlobal('useNuxtApp', () => ({ isHydrating: true, payload: { serverRendered: true } }))
    useCookie('refreshToken').value = 'opaque-refresh-cookie'
    await run('/admin')
    expect(useAdminAccessStore().state).toBe('pending')
    expect(fetch).not.toHaveBeenCalled()
    expect(navigateTo).not.toHaveBeenCalled()
  })
  it('reuses the fresh SSR permission while hydrating and verifies it on subsequent navigation', async () => {
    signIn(); useAdminAccessStore().state = 'authorized'
    const app = { isHydrating: true, payload: { serverRendered: true } }
    vi.stubGlobal('useNuxtApp', () => app)
    await run('/admin')
    expect(fetch).not.toHaveBeenCalled()
    app.isHydrating = false
    await run('/admin/catalog')
    expect(fetch).toHaveBeenCalledOnce()
  })
  it('redirects learners from protected routes but keeps admin login reachable', async () => {
    signIn()
    vi.mocked(fetch).mockImplementation(async () => new Response(JSON.stringify({ canManageCatalog: false })))
    await run('/admin/catalog')
    expect(navigateTo).toHaveBeenCalledWith({ path: '/admin/login', query: { redirect: '/admin/catalog' } })
    vi.mocked(navigateTo).mockClear()
    await run('/admin/login/')
    expect(navigateTo).not.toHaveBeenCalled()
    expect(useAuthStore().isLoggedIn).toBe(true)
  })
  it('retains valid credentials and withholds access after transient verification failure', async () => {
    signIn(); environment.server = true
    vi.mocked(fetch).mockRejectedValue(new TypeError('Connection unavailable'))
    await run('/admin/catalog')
    expect(useAdminAccessStore().state).toBe('unavailable')
    expect(useAdminAccessStore().isAuthorized).toBe(false)
    expect(useAuthStore().isLoggedIn).toBe(true)
    expect(navigateTo).not.toHaveBeenCalled()
  })
  it('checks fresh server permission instead of trusting a cached client capability', async () => {
    signIn(); useAdminAccessStore().state = 'authorized'
    vi.mocked(fetch).mockImplementation(async () => new Response(JSON.stringify({ canManageCatalog: false })))
    await run('/admin')
    expect(useAdminAccessStore().isAuthorized).toBe(false)
    expect(navigateTo).toHaveBeenCalled()
  })
  it('rejects unsafe administrator return paths for an already authorized account', async () => {
    signIn()
    await run('/admin/login', { redirect: '//example.test/admin' })
    expect(navigateTo).toHaveBeenCalledWith('/admin')
  })
  it('redirects from /admin/documents when canManageDocumentOperations is false', async () => {
    signIn()
    vi.mocked(fetch).mockImplementation(async () => new Response(JSON.stringify({
      canManageCatalog: true,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: false
    })))
    await run('/admin/documents')
    expect(navigateTo).toHaveBeenCalledWith({ path: '/admin/login', query: { redirect: '/admin/documents' } })
  })
})
