import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAdminUsersStore } from '~/stores/useAdminUsersStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { clearAuthStorage } from '../helpers/admin'

const { get, post } = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn()
}))

vi.mock('~/composables/useApiClient', async importOriginal => ({
  ...await importOriginal<typeof import('~/composables/useApiClient')>(),
  useApiClient: () => ({ get, post })
}))

describe('useAdminUsersStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    get.mockReset()
    post.mockReset()
  })

  it('initializes with default state', () => {
    const store = useAdminUsersStore()
    expect(store.users).toEqual([])
    expect(store.totalCount).toBe(0)
    expect(store.page).toBe(1)
    expect(store.pageSize).toBe(20)
    expect(store.selectedUser).toBeNull()
    expect(store.isLoading).toBe(false)
  })

  it('fetchUsers queries /api/v1/admin/users with parameters and updates state', async () => {
    const store = useAdminUsersStore()
    get.mockResolvedValueOnce({
      items: [
        {
          id: 'u-1',
          email: 'user1@example.com',
          name: 'User 1',
          avatarUrl: null,
          status: 'Active',
          createdAt: '2026-10-15T10:00:00Z'
        }
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20
    })

    store.search = 'user1'
    store.statusFilter = 'Active'

    await store.fetchUsers()

    expect(get).toHaveBeenCalledWith('/api/v1/admin/users', {
      query: {
        page: 1,
        pageSize: 20,
        search: 'user1',
        status: 'Active'
      },
      cache: 'no-store'
    })

    expect(store.users).toHaveLength(1)
    expect(store.users[0].id).toBe('u-1')
    expect(store.totalCount).toBe(1)
    expect(store.isLoading).toBe(false)
  })

  it('selectUser loads account detail projection', async () => {
    const store = useAdminUsersStore()
    get.mockResolvedValueOnce({
      id: 'u-1',
      email: 'user1@example.com',
      name: 'User 1',
      avatarUrl: null,
      status: 'Active',
      createdAt: '2026-10-15T10:00:00Z',
      preferredLocale: 'en',
      adminControlVersion: 1,
      hasPassword: true,
      hasGoogleAuth: false,
      suspendedByUserId: null,
      suspendedAt: null,
      suspensionReason: null
    })

    const detail = await store.selectUser('u-1')

    expect(get).toHaveBeenCalledWith('/api/v1/admin/users/u-1', { cache: 'no-store' })
    expect(detail).not.toBeNull()
    expect(store.selectedUser?.id).toBe('u-1')
    expect(store.selectedUser?.hasPassword).toBe(true)
  })

  it('suspendUser posts reason and expectedVersion then refreshes', async () => {
    const store = useAdminUsersStore()
    post.mockResolvedValueOnce({
      id: 'u-1',
      email: 'user1@example.com',
      name: 'User 1',
      avatarUrl: null,
      status: 'Suspended',
      createdAt: '2026-10-15T10:00:00Z',
      preferredLocale: 'en',
      adminControlVersion: 2,
      hasPassword: true,
      hasGoogleAuth: false,
      suspendedByUserId: 'admin-id',
      suspendedAt: '2026-10-15T12:00:00Z',
      suspensionReason: 'Violation of Terms'
    })
    get.mockResolvedValueOnce({ items: [], totalCount: 0, page: 1, pageSize: 20 })

    const success = await store.suspendUser('u-1', 'Violation of Terms', 1)

    expect(success).toBe(true)
    expect(post).toHaveBeenCalledWith('/api/v1/admin/users/u-1/suspend', {
      reason: 'Violation of Terms',
      expectedVersion: 1
    })
    expect(store.selectedUser?.status).toBe('Suspended')
    expect(store.selectedUser?.adminControlVersion).toBe(2)
  })

  it('resets state on auth session change', () => {
    const store = useAdminUsersStore()
    store.users = [{
      id: '1',
      email: '1@example.com',
      name: 'User 1',
      avatarUrl: null,
      status: 'Active',
      createdAt: '2026-10-15T10:00:00Z'
    }]
    store.selectedUser = {
      id: '1',
      email: '1@example.com',
      name: 'User 1',
      avatarUrl: null,
      status: 'Active',
      createdAt: '2026-10-15T10:00:00Z',
      preferredLocale: 'en',
      adminControlVersion: 1,
      hasPassword: true,
      hasGoogleAuth: false,
      suspendedByUserId: null,
      suspendedAt: null,
      suspensionReason: null
    }

    const auth = useAuthStore()
    auth.sessionGeneration++

    expect(store.users).toEqual([])
    expect(store.selectedUser).toBeNull()
  })
})
