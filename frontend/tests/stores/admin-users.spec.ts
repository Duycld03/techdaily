import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAdminUsersStore } from '~/stores/useAdminUsersStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { clearAuthStorage } from '../helpers/admin'

const { get, post, put } = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn()
}))

vi.mock('~/composables/useApiClient', async importOriginal => ({
  ...await importOriginal<typeof import('~/composables/useApiClient')>(),
  useApiClient: () => ({ get, post, put })
}))

describe('useAdminUsersStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    get.mockReset()
    post.mockReset()
    put.mockReset()
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
          createdAt: '2026-10-15T10:00:00Z',
          role: { id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', isProtected: false }
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
      suspensionReason: null,
      role: { id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', isProtected: false }
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
      suspensionReason: 'Violation of Terms',
      role: { id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', isProtected: false }
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
      createdAt: '2026-10-15T10:00:00Z',
      role: { id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', isProtected: false }
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
      suspensionReason: null,
      role: { id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', isProtected: false }
    }
    store.roles = [{
      id: 'r-1',
      key: 'Learner',
      nameEn: 'Learner',
      nameVi: 'Học viên',
      permissionKeys: []
    }]
    const auth = useAuthStore()
    auth.sessionGeneration++

    expect(store.users).toEqual([])
    expect(store.selectedUser).toBeNull()
    expect(store.roles).toEqual([])
  })

  it('fetchRoles loads available admin roles with canonical permissionKeys', async () => {
    const store = useAdminUsersStore()
    const roleList = [
      { id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', permissionKeys: [] },
      { id: 'r-2', key: 'Admin', nameEn: 'Admin', nameVi: 'Quản trị viên', permissionKeys: ['ManageAccounts', 'ManageRoles', 'ManageCategories'] }
    ]
    get.mockResolvedValueOnce({ items: roleList, totalCount: 2, page: 1, pageSize: 100 })

    await store.fetchRoles()

    expect(get).toHaveBeenCalledWith('/api/v1/admin/roles?page=1&pageSize=100', { cache: 'no-store' })
    expect(store.roles).toEqual(roleList)
    expect(store.isLoadingRoles).toBe(false)
    expect(store.rolesError).toBeNull()
  })

  it('role discovery ownership is independent of directory requests (search, filter, page, fetchUsers)', async () => {
    const store = useAdminUsersStore()
    const roleList = [
      { id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', permissionKeys: [] },
      { id: 'r-2', key: 'Admin', nameEn: 'Admin', nameVi: 'Quản trị viên', permissionKeys: ['ManageAccounts', 'ManageRoles'] }
    ]

    const { promise: deferredRoles, resolve: resolveRoles } = Promise.withResolvers<unknown>()
    get.mockImplementation((url: string) => {
      if (url.includes('/api/v1/admin/roles')) return deferredRoles
      return Promise.resolve({ items: [], totalCount: 0, page: 1, pageSize: 20 })
    })

    // 1. Role request starts and enters loading state
    const rolePromise = store.fetchRoles()
    expect(store.isLoadingRoles).toBe(true)

    // 2. While role request is in flight, directory operations occur
    store.search = 'test'
    store.statusFilter = 'Active'
    store.page = 2
    await store.fetchUsers()

    // 3. Deferred role request settles
    resolveRoles({ items: roleList, totalCount: 2, page: 1, pageSize: 100 })
    await rolePromise

    // 4. Role options settle without being dropped by directory operations
    expect(store.roles).toEqual(roleList)
    expect(store.isLoadingRoles).toBe(false)
    expect(store.rolesError).toBeNull()
  })

  it('role discovery handles failure, sets error, and recovers on retry', async () => {
    const store = useAdminUsersStore()
    const roleList = [
      { id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', permissionKeys: [] }
    ]

    // First attempt fails
    get.mockRejectedValueOnce(new Error('Network error loading roles'))
    await store.fetchRoles()
    expect(store.roles).toEqual([])
    expect(store.isLoadingRoles).toBe(false)
    expect(store.rolesError).toBe('Network error loading roles')

    // Retry succeeds
    get.mockResolvedValueOnce({ items: roleList, totalCount: 1, page: 1, pageSize: 100 })
    await store.fetchRoles()
    expect(store.roles).toEqual(roleList)
    expect(store.isLoadingRoles).toBe(false)
    expect(store.rolesError).toBeNull()
  })

  it('superseded role requests cannot publish or clear newer loading', async () => {
    const store = useAdminUsersStore()
    const oldRoles = [{ id: 'old-1', key: 'Old', nameEn: 'Old', nameVi: 'Cũ', permissionKeys: [] }]
    const newRoles = [{ id: 'new-1', key: 'New', nameEn: 'New', nameVi: 'Mới', permissionKeys: [] }]

    const { promise: deferredOld, resolve: resolveOld } = Promise.withResolvers<unknown>()
    const { promise: deferredNew, resolve: resolveNew } = Promise.withResolvers<unknown>()

    let callCount = 0
    get.mockImplementation((url: string) => {
      if (url.includes('/api/v1/admin/roles')) {
        callCount++
        return callCount === 1 ? deferredOld : deferredNew
      }
      return Promise.resolve({ items: [] })
    })

    // Start request 1 (slow)
    const req1 = store.fetchRoles()
    // Start request 2 (supersedes request 1)
    const req2 = store.fetchRoles()

    // Resolve request 2 first
    resolveNew({ items: newRoles, totalCount: 1, page: 1, pageSize: 100 })
    await req2
    expect(store.roles).toEqual(newRoles)
    expect(store.isLoadingRoles).toBe(false)

    // Stale request 1 settles later
    resolveOld({ items: oldRoles, totalCount: 1, page: 1, pageSize: 100 })
    await req1

    // Stale request 1 did NOT overwrite request 2
    expect(store.roles).toEqual(newRoles)
  })

  it('session and admin reset fence invalidates outstanding role requests', async () => {
    const store = useAdminUsersStore()
    const auth = useAuthStore()
    const roleList = [{ id: 'r-1', key: 'Learner', nameEn: 'Learner', nameVi: 'Học viên', permissionKeys: [] }]

    const { promise: deferredRoles, resolve: resolveRoles } = Promise.withResolvers<unknown>()
    get.mockImplementation(() => deferredRoles)

    const rolePromise = store.fetchRoles()
    expect(store.isLoadingRoles).toBe(true)

    // Session change triggers reset
    auth.sessionGeneration++
    expect(store.roles).toEqual([])
    expect(store.isLoadingRoles).toBe(false)

    // Outstanding request resolves after session change
    resolveRoles({ items: roleList, totalCount: 1, page: 1, pageSize: 100 })
    await rolePromise

    // Discarded; does not publish to the reset store
    expect(store.roles).toEqual([])
    expect(store.isLoadingRoles).toBe(false)
  })
  it('assignRole calls PUT endpoint with exact payload and updates user', async () => {
    const store = useAdminUsersStore()
    const updatedDetail = {
      id: 'u-1',
      email: 'user1@example.com',
      name: 'User 1',
      avatarUrl: null,
      status: 'Active' as const,
      createdAt: '2026-10-15T10:00:00Z',
      preferredLocale: 'en',
      adminControlVersion: 2,
      hasPassword: true,
      hasGoogleAuth: false,
      suspendedByUserId: null,
      suspendedAt: null,
      suspensionReason: null,
      role: { id: 'r-2', key: 'Admin', nameEn: 'Admin', nameVi: 'Quản trị viên', isProtected: true }
    }
    put.mockResolvedValueOnce(updatedDetail)
    get.mockResolvedValueOnce({ items: [updatedDetail], totalCount: 1, page: 1, pageSize: 20 })

    const result = await store.assignRole('u-1', 'r-2', 'Promotion to Admin', 1)

    expect(result).toEqual({ success: true })
    expect(put).toHaveBeenCalledWith('/api/v1/admin/users/u-1/role', {
      roleId: 'r-2',
      reason: 'Promotion to Admin',
      expectedVersion: 1
    })
    expect(store.selectedUser).toEqual(updatedDetail)
    expect(get).toHaveBeenCalledWith('/api/v1/admin/users', expect.any(Object))
  })

  it('assignRole extracts RFC 7807 error detail and code on failure', async () => {
    const store = useAdminUsersStore()
    put.mockRejectedValueOnce({
      status: 409,
      code: 'VERSION_CONFLICT',
      data: {
        code: 'VERSION_CONFLICT',
        detail: 'The account was modified by another administrator.'
      }
    })

    const result = await store.assignRole('u-1', 'r-2', 'Promotion', 1)

    expect(result.success).toBe(false)
    expect(result.errorCode).toBe('VERSION_CONFLICT')
    expect(result.errorMessage).toBe('The account was modified by another administrator.')
    expect(store.error).toBe('The account was modified by another administrator.')
    expect(store.isActionInFlight).toBe(false)
  })

  it('discards late fetchRoles response after session reset', async () => {
    const store = useAdminUsersStore()
    const auth = useAuthStore()
    const { promise, resolve } = Promise.withResolvers<unknown>()
    get.mockReturnValueOnce(promise)

    const call = store.fetchRoles()
    auth.sessionGeneration++
    resolve({ items: [{ id: 'r-1', key: 'Learner' }] })
    await call

    expect(store.roles).toEqual([])
  })
})
