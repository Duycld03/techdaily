import { defineStore } from 'pinia'
import { computed, shallowRef, watch } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiClient } from '~/composables/useApiClient'

export type AdminAccessState = 'pending' | 'checking' | 'authorized' | 'denied' | 'unavailable'

export const useAdminAccessStore = defineStore('admin-access', () => {
  const auth = useAuthStore()
  const api = useApiClient()
  const state = shallowRef<AdminAccessState>('pending')
  const isAuthorized = computed(() => state.value === 'authorized')
  let flight: Promise<boolean> | null = null
  let generation = 0

  function reset() {
    generation++
    flight = null
    state.value = 'pending'
  }

  watch([() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration], reset, { flush: 'sync' })

  function verify(): Promise<boolean> {
    if (flight) return flight
    const requestGeneration = generation
    if (state.value !== 'authorized') {
      state.value = 'checking'
    }
    const request = (async () => {
      try {
        const capability = await api.get<{ canManageCatalog: boolean }>('/api/v1/catalog/capabilities', { cache: 'no-store' })
        if (requestGeneration !== generation) return false
        if (capability.canManageCatalog !== true) {
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
        } else state.value = 'unavailable'
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

  return { state, isAuthorized, verify, reset, setUnavailable }
})
