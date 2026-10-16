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
  it('correctly tracks canManageDocumentOperations capability and controls /admin/documents access', async () => {
    const access = useAdminAccessStore()
    get.mockResolvedValue({
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: false,
      canManageDocumentOperations: true
    })

    expect(await access.verify()).toBe(true)
    expect(access.canManageDocumentOperations).toBe(true)
    expect(access.canAccessDestination('/admin/documents')).toBe(true)
    expect(access.canAccessDestination('/admin/catalog')).toBe(false)
    expect(access.canAccessDestination('/admin/users')).toBe(false)
    expect(access.canAccessDestination('/admin/audit')).toBe(false)
  })

  it('preserves query and hash contexts when verifying destination access (F15)', async () => {
    const access = useAdminAccessStore()
    get.mockResolvedValue({
      canManageCatalog: true,
      canManageCategories: true,
      canManageDocumentOperations: true
    })
    await access.verify()

    expect(access.canAccessDestination('/admin/catalog?search=vue#section-1')).toBe(true)
    expect(access.canAccessDestination('/admin/categories?page=2#active')).toBe(true)
    expect(access.canAccessDestination('/admin/documents?status=Processing#details')).toBe(true)
    expect(access.canAccessDestination('/admin/users?query=test')).toBe(false)
  })
  it('tracks all 7 capabilities and hasAnyCapability getter', async () => {
    const access = useAdminAccessStore()
    get.mockResolvedValue({
      canViewAdminDashboard: true,
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: false,
      canManageDocumentOperations: false,
      canManageRoles: false,
      canManageCategories: false
    })

    expect(await access.verify()).toBe(true)
    expect(access.canViewAdminDashboard).toBe(true)
    expect(access.canManageCatalog).toBe(false)
    expect(access.canManageAccounts).toBe(false)
    expect(access.canReadAudit).toBe(false)
    expect(access.canManageDocumentOperations).toBe(false)
    expect(access.canManageRoles).toBe(false)
    expect(access.canManageCategories).toBe(false)
    expect(access.hasAnyCapability).toBe(true)
  })

  it('correctly tracks canManageCategories capability and controls /admin/categories access', async () => {
    const access = useAdminAccessStore()
    get.mockResolvedValue({
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: false,
      canManageDocumentOperations: false,
      canManageCategories: true
    })

    expect(await access.verify()).toBe(true)
    expect(access.canManageCategories).toBe(true)
    expect(access.canAccessDestination('/admin/categories')).toBe(true)
    expect(access.canAccessDestination('/admin/categories/')).toBe(true)
    expect(access.canAccessDestination('/admin/catalog')).toBe(false)
    expect(access.canAccessDestination('/admin/users')).toBe(false)
    expect(access.canAccessDestination('/admin/audit')).toBe(false)
  })
  it('denies access and invalidates admin session if all 6 capability flags are false', async () => {
    const auth = useAuthStore()
    auth.setSession(adminTestJwt(), { id: 'admin-1', email: 'admin@example.test', name: 'Admin', preferredLocale: 'en' })
    const access = useAdminAccessStore()
    const prevAdminGen = auth.adminGeneration

    get.mockResolvedValue({
      canViewAdminDashboard: false,
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: false,
      canManageDocumentOperations: false,
      canManageRoles: false,
      canManageCategories: false
    })
    expect(await access.verify()).toBe(false)
    expect(access.state).toBe('denied')
    expect(auth.adminGeneration).toBeGreaterThan(prevAdminGen)
  })

  it('ManageRoles alone does not grant access to /admin/users or any admin destination', async () => {
    const access = useAdminAccessStore()
    get.mockResolvedValue({
      canViewAdminDashboard: false,
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: false,
      canManageDocumentOperations: false,
      canManageRoles: true
    })

    expect(await access.verify()).toBe(true)
    expect(access.canManageRoles).toBe(true)
    expect(access.canAccessDestination('/admin')).toBe(false)
    expect(access.canAccessDestination('/admin/dashboard')).toBe(false)
    expect(access.canAccessDestination('/admin/catalog')).toBe(false)
    expect(access.canAccessDestination('/admin/users')).toBe(false)
    expect(access.canAccessDestination('/admin/documents')).toBe(false)
    expect(access.canAccessDestination('/admin/audit')).toBe(false)
    expect(access.getFirstAccessibleAdminRoute()).toBe('/login')
  })

  it('denies access to unknown destinations', async () => {
    const access = useAdminAccessStore()
    get.mockResolvedValue({
      canViewAdminDashboard: true,
      canManageCatalog: true,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: true,
      canManageRoles: true
    })

    expect(await access.verify()).toBe(true)
    expect(access.canAccessDestination('/admin/unknown')).toBe(false)
    expect(access.canAccessDestination('/admin/missing')).toBe(false)
    expect(access.canAccessDestination('/other')).toBe(false)
  })

  it('computes deterministic landing route in priority order', async () => {
    const access = useAdminAccessStore()

    // 1. Dashboard priority
    get.mockResolvedValueOnce({
      canViewAdminDashboard: true,
      canManageCatalog: true,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: true
    })
    await access.verify()
    expect(access.getFirstAccessibleAdminRoute()).toBe('/admin/dashboard')

    // 2. Catalog priority
    access.reset()
    get.mockResolvedValueOnce({
      canViewAdminDashboard: false,
      canManageCatalog: true,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: true
    })
    await access.verify()
    expect(access.getFirstAccessibleAdminRoute()).toBe('/admin/catalog')

    // 3. Categories priority
    access.reset()
    get.mockResolvedValueOnce({
      canViewAdminDashboard: false,
      canManageCatalog: false,
      canManageCategories: true,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: true
    })
    await access.verify()
    expect(access.getFirstAccessibleAdminRoute()).toBe('/admin/categories')

    // 4. Accounts priority
    access.reset()
    get.mockResolvedValueOnce({
      canViewAdminDashboard: false,
      canManageCatalog: false,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: true
    })
    await access.verify()
    expect(access.getFirstAccessibleAdminRoute()).toBe('/admin/users')

    // 4. Documents priority
    access.reset()
    get.mockResolvedValueOnce({
      canViewAdminDashboard: false,
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: true,
      canManageDocumentOperations: true
    })
    await access.verify()
    expect(access.getFirstAccessibleAdminRoute()).toBe('/admin/documents')

    // 5. Audit priority
    access.reset()
    get.mockResolvedValueOnce({
      canViewAdminDashboard: false,
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: true,
      canManageDocumentOperations: false
    })
    await access.verify()
    expect(access.getFirstAccessibleAdminRoute()).toBe('/admin/audit')

    // Fallback: /login
    access.reset()
    expect(access.getFirstAccessibleAdminRoute()).toBe('/login')
  })

  it('treats 401 and 403 as denied and invalidates admin access', async () => {
    const auth = useAuthStore()
    auth.setSession(adminTestJwt(), { id: 'admin-1', email: 'admin@example.test', name: 'Admin', preferredLocale: 'en' })
    const access = useAdminAccessStore()

    get.mockRejectedValueOnce(Object.assign(new Error('Unauthorized'), { status: 401 }))
    expect(await access.verify()).toBe(false)
    expect(access.state).toBe('denied')

    access.reset()
    get.mockRejectedValueOnce(Object.assign(new Error('Forbidden'), { status: 403 }))
    expect(await access.verify()).toBe(false)
    expect(access.state).toBe('denied')
  })
})
