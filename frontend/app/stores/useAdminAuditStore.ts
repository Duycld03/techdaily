import { defineStore } from 'pinia'
import { shallowRef, watch } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiClient } from '~/composables/useApiClient'
import type { PagedResult } from '~/stores/useAdminUsersStore'

export interface AdminAuditEventSummary {
  id: string
  occurredAt: string
  actorUserId: string
  action: string
  targetType: string
  targetId: string
  outcome: 'Succeeded' | 'Accepted' | 'Rejected' | 'Failed'
  reason: string | null
  errorCode: string | null
  correlationId: string | null
}

export interface AdminAuditEventDetail extends AdminAuditEventSummary {
  actorEmail: string | null
  changedFields: string[]
  statePayload: Record<string, unknown>
}

export interface AdminAuditFilters {
  actorUserId?: string
  action?: string
  targetType?: string
  targetId?: string
  outcome?: 'Succeeded' | 'Accepted' | 'Rejected' | 'Failed'
  from?: string
  to?: string
}

export const useAdminAuditStore = defineStore('admin-audit', () => {
  const auth = useAuthStore()
  const api = useApiClient()

  const events = shallowRef<AdminAuditEventSummary[]>([])
  const totalCount = shallowRef(0)
  const page = shallowRef(1)
  const pageSize = shallowRef(20)
  const filters = shallowRef<AdminAuditFilters>({})
  const selectedEvent = shallowRef<AdminAuditEventDetail | null>(null)

  const isLoading = shallowRef(false)
  const error = shallowRef<string | null>(null)

  let fetchGeneration = 0

  function reset() {
    fetchGeneration++
    events.value = []
    totalCount.value = 0
    page.value = 1
    filters.value = {}
    selectedEvent.value = null
    isLoading.value = false
    error.value = null
  }

  watch([() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration], reset, { flush: 'sync' })

  function resetFilters() {
    filters.value = {}
    page.value = 1
    fetchEvents()
  }

  async function fetchEvents(): Promise<void> {
    const currentGen = ++fetchGeneration
    isLoading.value = true
    error.value = null

    try {
      const queryParams: Record<string, string | number> = {
        page: page.value,
        pageSize: pageSize.value
      }

      if (filters.value.actorUserId?.trim()) queryParams.actorUserId = filters.value.actorUserId.trim()
      if (filters.value.action?.trim()) queryParams.action = filters.value.action.trim()
      if (filters.value.targetType?.trim()) queryParams.targetType = filters.value.targetType.trim()
      if (filters.value.targetId?.trim()) queryParams.targetId = filters.value.targetId.trim()
      if (filters.value.outcome) queryParams.outcome = filters.value.outcome
      if (filters.value.from?.trim()) queryParams.from = filters.value.from.trim()
      if (filters.value.to?.trim()) queryParams.to = filters.value.to.trim()

      const res = await api.get<PagedResult<AdminAuditEventSummary>>('/api/v1/admin/audit', {
        query: queryParams,
        cache: 'no-store'
      })

      if (currentGen !== fetchGeneration) return

      events.value = res.items
      totalCount.value = res.totalCount
      page.value = res.page
      pageSize.value = res.pageSize
    } catch (err: unknown) {
      if (currentGen !== fetchGeneration) return
      error.value = err instanceof Error ? err.message : 'Failed to load audit history'
    } finally {
      if (currentGen === fetchGeneration) {
        isLoading.value = false
      }
    }
  }

  async function selectEvent(id: string): Promise<AdminAuditEventDetail | null> {
    const currentGen = fetchGeneration
    error.value = null
    try {
      const detail = await api.get<AdminAuditEventDetail>(`/api/v1/admin/audit/${id}`, { cache: 'no-store' })
      if (currentGen !== fetchGeneration) return null
      selectedEvent.value = detail
      return detail
    } catch (err: unknown) {
      if (currentGen !== fetchGeneration) return null
      error.value = err instanceof Error ? err.message : 'Failed to load audit event details'
      return null
    }
  }

  return {
    events,
    totalCount,
    page,
    pageSize,
    filters,
    selectedEvent,
    isLoading,
    error,
    fetchEvents,
    selectEvent,
    resetFilters,
    reset
  }
})
