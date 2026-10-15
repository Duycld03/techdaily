import { defineStore } from 'pinia'
import { computed, shallowRef, watch } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiClient } from '~/composables/useApiClient'
import { normalizeRoutePath } from '~/utils/adminRoutes'

export type AdminAccessState = 'pending' | 'checking' | 'authorized' | 'denied' | 'unavailable'

export interface AdminCapabilities {
  canManageCatalog: boolean
  canManageAccounts: boolean
  canReadAudit: boolean
}

export const useAdminAccessStore = defineStore('admin-access', () => {
  const auth = useAuthStore()
  const api = useApiClient()
  const state = shallowRef<AdminAccessState>('pending')
  const capabilities = shallowRef<AdminCapabilities>({
    canManageCatalog: false,
    canManageAccounts: false,
    canReadAudit: false
  })

  const canManageCatalog = computed(() => capabilities.value.canManageCatalog)
  const canManageAccounts = computed(() => capabilities.value.canManageAccounts)
  const canReadAudit = computed(() => capabilities.value.canReadAudit)
  const hasAnyCapability = computed(() =>
    capabilities.value.canManageCatalog ||
    capabilities.value.canManageAccounts ||
    capabilities.value.canReadAudit
  )

  const isAuthorized = computed(() => state.value === 'authorized')
  let flight: Promise<boolean> | null = null
  let generation = 0

  function reset() {
    generation++
    flight = null
    state.value = 'pending'
    capabilities.value = {
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: false
    }
  }

  watch([() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration], reset, { flush: 'sync' })

  function canAccessDestination(path: string): boolean {
    const norm = normalizeRoutePath(path)
    if (norm === '/admin/catalog' || norm.startsWith('/admin/catalog/')) {
      return capabilities.value.canManageCatalog
    }
    if (norm === '/admin/users' || norm.startsWith('/admin/users/')) {
      return capabilities.value.canManageAccounts
    }
    if (norm === '/admin/audit' || norm.startsWith('/admin/audit/')) {
      return capabilities.value.canReadAudit
    }
    return isAuthorized.value
  }

  function verify(): Promise<boolean> {
    if (flight) return flight
    const requestGeneration = generation
    if (state.value !== 'authorized') {
      state.value = 'checking'
    }
    const request = (async () => {
      try {
        const res = await api.get<Partial<AdminCapabilities>>('/api/v1/admin/capabilities', { cache: 'no-store' })
        if (requestGeneration !== generation) return false

        const newCaps: AdminCapabilities = {
          canManageCatalog: Boolean(res.canManageCatalog),
          canManageAccounts: Boolean(res.canManageAccounts),
          canReadAudit: Boolean(res.canReadAudit)
        }
        capabilities.value = newCaps

        if (!newCaps.canManageCatalog && !newCaps.canManageAccounts && !newCaps.canReadAudit) {
          auth.invalidateAdminAccess()
          state.value = 'denied'
          return false
        }

        state.value = 'authorized'
        return true
      } catch (error: unknown) {
        if (requestGeneration !== generation) return false
        const status = error && typeof error === 'object' && 'status' in error ? error.status : null
        if (status === 401 || status === 403) {
          auth.invalidateAdminAccess()
          state.value = 'denied'
        } else {
          state.value = 'unavailable'
        }
        return false
      } finally {
        if (requestGeneration === generation) flight = null
      }
    })()
    flight = request
    return request
  }

  function setUnavailable() {
    state.value = 'unavailable'
  }

  return {
    state,
    capabilities,
    canManageCatalog,
    canManageAccounts,
    canReadAudit,
    hasAnyCapability,
    isAuthorized,
    canAccessDestination,
    verify,
    reset,
    setUnavailable
  }
})
