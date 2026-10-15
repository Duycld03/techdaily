import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAdminAuditStore } from '~/stores/useAdminAuditStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { clearAuthStorage } from '../helpers/admin'
import type { AdminAuditEventDetail, AdminAuditEventSummary } from '~/stores/useAdminAuditStore'

const { get } = vi.hoisted(() => ({
  get: vi.fn()
}))

vi.mock('~/composables/useApiClient', async importOriginal => ({
  ...await importOriginal<typeof import('~/composables/useApiClient')>(),
  useApiClient: () => ({ get })
}))

describe('useAdminAuditStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    get.mockReset()
  })

  it('initializes with default state', () => {
    const store = useAdminAuditStore()
    expect(store.events).toEqual([])
    expect(store.totalCount).toBe(0)
    expect(store.page).toBe(1)
    expect(store.pageSize).toBe(20)
    expect(store.filters).toEqual({})
    expect(store.selectedEvent).toBeNull()
  })

  it('fetchEvents applies filters and queries endpoint', async () => {
    const store = useAdminAuditStore()
    const mockSummary: AdminAuditEventSummary = {
      id: 'e-1',
      occurredAt: '2026-10-15T10:00:00Z',
      actorUserId: 'a-1',
      action: 'User.Suspend',
      targetType: 'User',
      targetId: 'u-1',
      outcome: 'Succeeded',
      reason: 'Violation',
      errorCode: null,
      correlationId: 'c-1'
    }

    get.mockResolvedValueOnce({
      items: [mockSummary],
      totalCount: 1,
      page: 1,
      pageSize: 20
    })

    store.filters = {
      action: 'User.Suspend',
      outcome: 'Succeeded'
    }

    await store.fetchEvents()

    expect(get).toHaveBeenCalledWith('/api/v1/admin/audit', {
      query: {
        page: 1,
        pageSize: 20,
        action: 'User.Suspend',
        outcome: 'Succeeded'
      },
      cache: 'no-store'
    })

    expect(store.events).toHaveLength(1)
    expect(store.events[0].id).toBe('e-1')
  })

  it('selectEvent loads detailed event projection', async () => {
    const store = useAdminAuditStore()
    const mockDetail: AdminAuditEventDetail = {
      id: 'e-1',
      occurredAt: '2026-10-15T10:00:00Z',
      actorUserId: 'a-1',
      actorEmail: 'admin@example.com',
      action: 'User.Suspend',
      targetType: 'User',
      targetId: 'u-1',
      outcome: 'Succeeded',
      reason: 'Violation',
      errorCode: null,
      correlationId: 'c-1',
      changedFields: ['Status'],
      statePayload: { Status: 'Suspended' }
    }

    get.mockResolvedValueOnce(mockDetail)

    const detail = await store.selectEvent('e-1')

    expect(get).toHaveBeenCalledWith('/api/v1/admin/audit/e-1', { cache: 'no-store' })
    expect(detail).not.toBeNull()
    expect(store.selectedEvent?.actorEmail).toBe('admin@example.com')
    expect(store.selectedEvent?.changedFields).toEqual(['Status'])
  })

  it('resets state on auth session change', () => {
    const store = useAdminAuditStore()
    store.events = [{
      id: 'e-1',
      occurredAt: '2026-10-15T10:00:00Z',
      actorUserId: 'a-1',
      action: 'User.Suspend',
      targetType: 'User',
      targetId: 'u-1',
      outcome: 'Succeeded',
      reason: null,
      errorCode: null,
      correlationId: null
    }]

    const auth = useAuthStore()
    auth.sessionGeneration++

    expect(store.events).toEqual([])
    expect(store.selectedEvent).toBeNull()
  })
})
