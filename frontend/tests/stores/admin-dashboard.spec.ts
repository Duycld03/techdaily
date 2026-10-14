import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAdminDashboardStore } from '~/stores/useAdminDashboardStore'
import { useAuthStore } from '~/stores/useAuthStore'
import type { AdminOverview } from '~/types/adminDashboard'
import { adminTestJwt, clearAuthStorage, deferred } from '../helpers/admin'
const { get, post } = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }))
vi.mock('~/composables/useApiClient', async importOriginal => ({ ...await importOriginal<typeof import('~/composables/useApiClient')>(), useApiClient: () => ({ get, post }) }))
const overview: AdminOverview = { accountCount: 0, personalDocuments: { total: 0, pending: 0, processing: 0, ready: 0, failed: 0 }, catalogDocuments: { total: 1, draft: 0, published: 1, hidden: 0, processing: { total: 1, pending: 0, processing: 0, ready: 1, failed: 0 } }, observedAt: '2026-10-14T10:00:00Z' }

describe('administrator dashboard data lifetime', () => {
  beforeEach(() => { setActivePinia(createPinia()); clearAuthStorage(); get.mockReset(); post.mockReset() })
  it('loads independent inventory/status sections without any automatic AI call', async () => {
    get.mockImplementation((url: string) => url.endsWith('/overview') ? Promise.resolve(overview) : Promise.reject(new Error('Unavailable')))
    const store = useAdminDashboardStore()
    expect(post).not.toHaveBeenCalled()
    await store.refresh()
    expect(store.overview?.accountCount).toBe(0)
    expect(store.status).toBeNull()
    expect(store.statusError).toBe('SERVICE_UNAVAILABLE')
    expect(post).not.toHaveBeenCalled()
    expect(get).toHaveBeenCalledTimes(2)
  })
  it('keeps a prior successful observation with an error and does not fabricate missing zeros', async () => {
    const store = useAdminDashboardStore()
    get.mockRejectedValueOnce(new Error('Unavailable'))
    await store.fetchOverview()
    expect(store.overview).toBeNull()
    get.mockResolvedValueOnce(overview)
    await store.fetchOverview()
    get.mockRejectedValueOnce(new Error('Unavailable'))
    await store.fetchOverview()
    expect(store.overview).toEqual(overview)
    expect(store.overviewError).toBe('ADMIN_DASHBOARD_UNAVAILABLE')
  })
  it('suppresses repeated refresh clicks and discards late results after logout', async () => {
    const pending = deferred<AdminOverview>()
    get.mockReturnValue(pending.promise)
    const store = useAdminDashboardStore()
    const request = store.fetchOverview()
    const duplicate = store.fetchOverview()
    expect(get).toHaveBeenCalledTimes(1)
    useAuthStore().clearSession()
    pending.resolve(overview)
    await Promise.all([request, duplicate])
    expect(store.overview).toBeNull()
    expect(store.isOverviewLoading).toBe(false)
  })
  it('makes an explicit single probe and preserves partial failed observations', async () => {
    const payload = { textGeneration: { status: 'unavailable', errorCode: 'AI_TIMEOUT' }, embeddings: { status: 'healthy' }, checkedAt: '2026-10-14T10:00:00Z' }
    post.mockRejectedValue({ status: 503, data: payload })
    const store = useAdminDashboardStore()
    await store.probeAi()
    expect(post).toHaveBeenCalledWith('/api/v1/admin/dashboard/ai-probe')
    expect(store.aiProbe).toEqual(payload)
    expect(store.probeError).toBe('AI_PROBE_FAILED')
    expect(store.isProbing).toBe(false)
  })
  it('clears privileged observations when permission is revoked, preserving learner identity', () => {
    const auth = useAuthStore()
    auth.setSession(adminTestJwt(), { id: 'admin-1', name: 'Admin', email: 'admin@example.test', preferredLocale: 'en' })
    const store = useAdminDashboardStore()
    store.overview = overview
    auth.invalidateAdminAccess()
    expect(store.overview).toBeNull()
    expect(auth.user?.id).toBe('admin-1')
    expect(auth.token).toBeTruthy()
  })
})
