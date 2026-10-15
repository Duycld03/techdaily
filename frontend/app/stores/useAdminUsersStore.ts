import { defineStore } from 'pinia'
import { shallowRef, watch } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiClient } from '~/composables/useApiClient'

export interface AdminAccountSummary {
  id: string
  email: string
  name: string
  avatarUrl: string | null
  status: 'Active' | 'Suspended'
  createdAt: string
}

export interface AdminAccountDetail extends AdminAccountSummary {
  preferredLocale: string
  adminControlVersion: number
  hasPassword: boolean
  hasGoogleAuth: boolean
  suspendedByUserId: string | null
  suspendedAt: string | null
  suspensionReason: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export const useAdminUsersStore = defineStore('admin-users', () => {
  const auth = useAuthStore()
  const api = useApiClient()

  const users = shallowRef<AdminAccountSummary[]>([])
  const totalCount = shallowRef(0)
  const page = shallowRef(1)
  const pageSize = shallowRef(20)
  const search = shallowRef('')
  const statusFilter = shallowRef<'Active' | 'Suspended' | null>(null)
  const selectedUser = shallowRef<AdminAccountDetail | null>(null)

  const isLoading = shallowRef(false)
  const isActionInFlight = shallowRef(false)
  const error = shallowRef<string | null>(null)

  let fetchGeneration = 0

  function reset() {
    fetchGeneration++
    users.value = []
    totalCount.value = 0
    page.value = 1
    search.value = ''
    statusFilter.value = null
    selectedUser.value = null
    isLoading.value = false
    isActionInFlight.value = false
    error.value = null
  }

  watch([() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration], reset, { flush: 'sync' })

  async function fetchUsers(): Promise<void> {
    const currentGen = ++fetchGeneration
    isLoading.value = true
    error.value = null

    try {
      const queryParams: Record<string, string | number> = {
        page: page.value,
        pageSize: pageSize.value
      }
      if (search.value.trim()) {
        queryParams.search = search.value.trim()
      }
      if (statusFilter.value) {
        queryParams.status = statusFilter.value
      }

      const res = await api.get<PagedResult<AdminAccountSummary>>('/api/v1/admin/users', {
        query: queryParams,
        cache: 'no-store'
      })

      if (currentGen !== fetchGeneration) return

      users.value = res.items
      totalCount.value = res.totalCount
      page.value = res.page
      pageSize.value = res.pageSize
    } catch (err: unknown) {
      if (currentGen !== fetchGeneration) return
      error.value = err instanceof Error ? err.message : 'Failed to load accounts'
    } finally {
      if (currentGen === fetchGeneration) {
        isLoading.value = false
      }
    }
  }

  async function selectUser(id: string): Promise<AdminAccountDetail | null> {
    const currentGen = fetchGeneration
    error.value = null
    try {
      const detail = await api.get<AdminAccountDetail>(`/api/v1/admin/users/${id}`, { cache: 'no-store' })
      if (currentGen !== fetchGeneration) return null
      selectedUser.value = detail
      return detail
    } catch (err: unknown) {
      if (currentGen !== fetchGeneration) return null
      error.value = err instanceof Error ? err.message : 'Failed to load account details'
      return null
    }
  }

  async function suspendUser(id: string, reason: string, expectedVersion: number): Promise<boolean> {
    if (isActionInFlight.value) return false
    isActionInFlight.value = true
    error.value = null
    try {
      const updated = await api.post<AdminAccountDetail>(`/api/v1/admin/users/${id}/suspend`, {
        reason,
        expectedVersion
      })
      selectedUser.value = updated
      await fetchUsers()
      return true
    } catch (err: unknown) {
      const message = err && typeof err === 'object' && 'message' in err ? String(err.message) : 'Suspension failed'
      error.value = message
      return false
    } finally {
      isActionInFlight.value = false
    }
  }

  async function reactivateUser(id: string, reason: string, expectedVersion: number): Promise<boolean> {
    if (isActionInFlight.value) return false
    isActionInFlight.value = true
    error.value = null
    try {
      const updated = await api.post<AdminAccountDetail>(`/api/v1/admin/users/${id}/reactivate`, {
        reason,
        expectedVersion
      })
      selectedUser.value = updated
      await fetchUsers()
      return true
    } catch (err: unknown) {
      const message = err && typeof err === 'object' && 'message' in err ? String(err.message) : 'Reactivation failed'
      error.value = message
      return false
    } finally {
      isActionInFlight.value = false
    }
  }

  async function revokeSessions(id: string, reason: string, expectedVersion: number): Promise<boolean> {
    if (isActionInFlight.value) return false
    isActionInFlight.value = true
    error.value = null
    try {
      const updated = await api.post<AdminAccountDetail>(`/api/v1/admin/users/${id}/revoke-sessions`, {
        reason,
        expectedVersion
      })
      selectedUser.value = updated
      await fetchUsers()
      return true
    } catch (err: unknown) {
      const message = err && typeof err === 'object' && 'message' in err ? String(err.message) : 'Revocation failed'
      error.value = message
      return false
    } finally {
      isActionInFlight.value = false
    }
  }

  return {
    users,
    totalCount,
    page,
    pageSize,
    search,
    statusFilter,
    selectedUser,
    isLoading,
    isActionInFlight,
    error,
    fetchUsers,
    selectUser,
    suspendUser,
    reactivateUser,
    revokeSessions,
    reset
  }
})
