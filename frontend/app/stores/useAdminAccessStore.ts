import { defineStore } from 'pinia'
import { computed, shallowRef, watch } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiClient } from '~/composables/useApiClient'
import { normalizeRoutePath } from '~/utils/adminRoutes'

export type AdminAccessState = 'pending' | 'checking' | 'authorized' | 'denied' | 'unavailable'

export interface AdminCapabilities {
  canViewAdminDashboard: boolean
  canManageCatalog: boolean
  canManageAccounts: boolean
  canReadAudit: boolean
  canManageDocumentOperations: boolean
  canManageRoles: boolean
}

export const useAdminAccessStore = defineStore('admin-access', () => {
  const auth = useAuthStore()
  const api = useApiClient()
  const state = shallowRef<AdminAccessState>('pending')
  const capabilities = shallowRef<AdminCapabilities>({
    canViewAdminDashboard: false,
    canManageCatalog: false,
    canManageAccounts: false,
    canReadAudit: false,
    canManageDocumentOperations: false,
    canManageRoles: false
  })

  const canViewAdminDashboard = computed(() => capabilities.value.canViewAdminDashboard)
  const canManageCatalog = computed(() => capabilities.value.canManageCatalog)
  const canManageAccounts = computed(() => capabilities.value.canManageAccounts)
  const canReadAudit = computed(() => capabilities.value.canReadAudit)
  const canManageDocumentOperations = computed(() => capabilities.value.canManageDocumentOperations)
  const canManageRoles = computed(() => capabilities.value.canManageRoles)
  const hasAnyCapability = computed(() =>
    capabilities.value.canViewAdminDashboard ||
    capabilities.value.canManageCatalog ||
    capabilities.value.canManageAccounts ||
    capabilities.value.canReadAudit ||
    capabilities.value.canManageDocumentOperations ||
    capabilities.value.canManageRoles
  )

  const isAuthorized = computed(() => state.value === 'authorized')
  let flight: Promise<boolean> | null = null
  let generation = 0

  function reset() {
    generation++
    flight = null
    state.value = 'pending'
    capabilities.value = {
      canViewAdminDashboard: false,
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: false,
      canManageDocumentOperations: false,
      canManageRoles: false
    }
  }

  watch([() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration], reset, { flush: 'sync' })

  function canAccessDestination(path: string): boolean {
    const norm = normalizeRoutePath(path)
    if (norm === '/admin' || norm === '/admin/dashboard' || norm.startsWith('/admin/dashboard/')) {
      return capabilities.value.canViewAdminDashboard
    }
    if (norm === '/admin/catalog' || norm.startsWith('/admin/catalog/')) {
      return capabilities.value.canManageCatalog
    }
    if (norm === '/admin/users' || norm.startsWith('/admin/users/')) {
      return capabilities.value.canManageAccounts
    }
    if (norm === '/admin/audit' || norm.startsWith('/admin/audit/')) {
      return capabilities.value.canReadAudit
    }
    if (norm === '/admin/documents' || norm.startsWith('/admin/documents/')) {
      return capabilities.value.canManageDocumentOperations
    }
    return false
  }

  function getFirstAccessibleAdminRoute(): string {
    if (capabilities.value.canViewAdminDashboard) return '/admin/dashboard'
    if (capabilities.value.canManageCatalog) return '/admin/catalog'
    if (capabilities.value.canManageAccounts) return '/admin/users'
    if (capabilities.value.canManageDocumentOperations) return '/admin/documents'
    if (capabilities.value.canReadAudit) return '/admin/audit'
    return '/login'
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
          canViewAdminDashboard: Boolean(res.canViewAdminDashboard),
          canManageCatalog: Boolean(res.canManageCatalog),
          canManageAccounts: Boolean(res.canManageAccounts),
          canReadAudit: Boolean(res.canReadAudit),
          canManageDocumentOperations: Boolean(res.canManageDocumentOperations),
          canManageRoles: Boolean(res.canManageRoles)
        }
        capabilities.value = newCaps

        if (
          !newCaps.canViewAdminDashboard &&
          !newCaps.canManageCatalog &&
          !newCaps.canManageAccounts &&
          !newCaps.canReadAudit &&
          !newCaps.canManageDocumentOperations &&
          !newCaps.canManageRoles
        ) {
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
    canViewAdminDashboard,
    canManageCatalog,
    canManageAccounts,
    canReadAudit,
    canManageDocumentOperations,
    canManageRoles,
    hasAnyCapability,
    isAuthorized,
    canAccessDestination,
    getFirstAccessibleAdminRoute,
    verify,
    reset,
    setUnavailable
  }
})
