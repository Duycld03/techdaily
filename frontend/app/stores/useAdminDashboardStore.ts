import { defineStore } from 'pinia'
import { shallowRef, watch } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiClient } from '~/composables/useApiClient'
import type { AdminOverview, AdminStatus, AdminAiProbe } from '~/types/adminDashboard'

export const useAdminDashboardStore = defineStore('admin-dashboard', () => {
  const auth = useAuthStore()
  const api = useApiClient()
  const overview = shallowRef<AdminOverview | null>(null)
  const status = shallowRef<AdminStatus | null>(null)
  const aiProbe = shallowRef<AdminAiProbe | null>(null)
  const overviewError = shallowRef<string | null>(null)
  const statusError = shallowRef<string | null>(null)
  const probeError = shallowRef<string | null>(null)
  const isOverviewLoading = shallowRef(false)
  const isStatusLoading = shallowRef(false)
  const isProbing = shallowRef(false)
  const lastProbeStartedAt = shallowRef<number | null>(null)
  let generation = 0
  let overviewFlight: Promise<void> | null = null
  let statusFlight: Promise<void> | null = null
  let probeFlight: Promise<void> | null = null

  function reset() {
    generation++
    overview.value = null
    status.value = null
    aiProbe.value = null
    overviewError.value = statusError.value = probeError.value = null
    isOverviewLoading.value = isStatusLoading.value = isProbing.value = false
    lastProbeStartedAt.value = null
    overviewFlight = statusFlight = probeFlight = null
  }
  watch([() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration], reset, { flush: 'sync' })

  function fetchOverview(): Promise<void> {
    if (overviewFlight) return overviewFlight
    const current = generation
    isOverviewLoading.value = true
    overviewError.value = null
    overviewFlight = (async () => {
      try {
        const data = await api.get<AdminOverview>('/api/v1/admin/dashboard/overview', { cache: 'no-store' })
        if (current === generation) overview.value = data
      } catch {
        if (current === generation) overviewError.value = 'ADMIN_DASHBOARD_UNAVAILABLE'
      } finally {
        if (current === generation) { isOverviewLoading.value = false; overviewFlight = null }
      }
    })()
    return overviewFlight
  }

  function fetchStatus(): Promise<void> {
    if (statusFlight) return statusFlight
    const current = generation
    isStatusLoading.value = true
    statusError.value = null
    statusFlight = (async () => {
      try {
        const data = await api.get<AdminStatus>('/api/v1/admin/dashboard/status', { cache: 'no-store' })
        if (current === generation) status.value = data
      } catch {
        if (current === generation) statusError.value = 'SERVICE_UNAVAILABLE'
      } finally {
        if (current === generation) { isStatusLoading.value = false; statusFlight = null }
      }
    })()
    return statusFlight
  }

  function probeAi(): Promise<void> {
    if (probeFlight) return probeFlight
    const current = generation
    isProbing.value = true
    probeError.value = null
    lastProbeStartedAt.value = Date.now()
    probeFlight = (async () => {
      try {
        const data = await api.post<AdminAiProbe>('/api/v1/admin/dashboard/ai-probe')
        if (current === generation) aiProbe.value = data
      } catch (error: unknown) {
        if (current !== generation) return
        const failure = error as { status?: number; data?: AdminAiProbe }
        const data = failure.data
        if (data?.textGeneration && data?.embeddings && typeof data.checkedAt === 'string') aiProbe.value = data
        probeError.value = failure.status === 429 ? 'RATE_LIMITED' : 'AI_PROBE_FAILED'
      } finally {
        if (current === generation) { isProbing.value = false; probeFlight = null }
      }
    })()
    return probeFlight
  }

  async function refresh() {
    await Promise.allSettled([fetchOverview(), fetchStatus()])
  }

  return { overview, status, aiProbe, overviewError, statusError, probeError, isOverviewLoading,
    isStatusLoading, isProbing, lastProbeStartedAt, fetchOverview, fetchStatus, probeAi, refresh, reset }
})
