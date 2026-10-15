import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { useAdminDashboardStore } from '~/stores/useAdminDashboardStore'
import { adminTestJwt, clearAuthStorage, deferred } from '../helpers/admin'

const { get } = vi.hoisted(() => ({ get: vi.fn() }))
vi.mock('~/composables/useApiClient', async importOriginal => ({ ...await importOriginal<typeof import('~/composables/useApiClient')>(), useApiClient: () => ({ get }) }))

describe('server-confirmed administrator admission', () => {
  beforeEach(() => { setActivePinia(createPinia()); clearAuthStorage(); get.mockReset() })
  it('starts withheld and admits only an explicit server capability', async () => {
    const access = useAdminAccessStore()
    expect(access.isAuthorized).toBe(false)
    get.mockResolvedValue({ canManageCatalog: true, canManageAccounts: true, canReadAudit: true })
    expect(await access.verify()).toBe(true)
    expect(get).toHaveBeenCalledWith('/api/v1/admin/capabilities', { cache: 'no-store' })
    get.mockResolvedValue({ canManageCatalog: false, canManageAccounts: false, canReadAudit: false })
    expect(await access.verify()).toBe(false)
    expect(access.state).toBe('denied')
  })
  it('deduplicates checks and distinguishes transient failure from denial', async () => {
    const pending = deferred<{ canManageCatalog: boolean }>()
    get.mockReturnValue(pending.promise)
    const access = useAdminAccessStore()
    const first = access.verify()
    const second = access.verify()
    expect(get).toHaveBeenCalledTimes(1)
    pending.reject(Object.assign(new Error('Unavailable'), { status: 503 }))
    await Promise.all([first, second])
    expect(access.state).toBe('unavailable')
    expect(access.isAuthorized).toBe(false)
  })
  it('discards a previous account permission response after account replacement', async () => {
    const auth = useAuthStore()
    auth.setSession(adminTestJwt(), { id: 'admin-1', email: 'admin@example.test', name: 'Admin', preferredLocale: 'en' })
    const pending = deferred<{ canManageCatalog: boolean }>()
    get.mockReturnValue(pending.promise)
    const access = useAdminAccessStore()
    const check = access.verify()
    auth.setSession(adminTestJwt('learner-2'), { id: 'learner-2', email: 'learner@example.test', name: 'Learner', preferredLocale: 'en' })
    pending.resolve({ canManageCatalog: true, canManageAccounts: true, canReadAudit: true })
    expect(await check).toBe(false)
    expect(access.isAuthorized).toBe(false)
  })
  it('clears prior privileged observations after a definitive capability denial while retaining the learner session', async () => {
    const auth = useAuthStore()
    auth.setSession(adminTestJwt(), { id: 'admin-1', email: 'admin@example.test', name: 'Admin', preferredLocale: 'en' })
    const access = useAdminAccessStore()
    access.state = 'authorized'
    const dashboard = useAdminDashboardStore()
    dashboard.aiProbe = { checkedAt: '2026-10-14T10:00:00Z' } as never
    get.mockResolvedValue({ canManageCatalog: false, canManageAccounts: false, canReadAudit: false })
    expect(await access.verify()).toBe(false)
    expect(access.state).toBe('denied')
    expect(dashboard.aiProbe).toBeNull()
    expect(auth.isLoggedIn).toBe(true)
    expect(auth.user?.id).toBe('admin-1')
  })
})
