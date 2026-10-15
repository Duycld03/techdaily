import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AdminUsersPage from '../../app/pages/admin/users.vue'
import { useAdminAccessStore } from '../../app/stores/useAdminAccessStore'
import { useAdminUsersStore, type AdminAccountDetail, type AdminAccountSummary } from '../../app/stores/useAdminUsersStore'
import { useAuthStore, type AuthUser } from '../../app/stores/useAuthStore'

describe('AdminUsersPage (users.vue)', () => {
  const mockLearnerUser: AdminAccountSummary = {
    id: 'user-learner-1',
    email: 'learner1@example.com',
    name: 'Learner User',
    avatarUrl: null,
    status: 'Active',
    createdAt: '2026-10-15T00:00:00Z',
    role: {
      id: 'role-learner-id',
      key: 'learner',
      nameEn: 'Learner',
      nameVi: 'Học viên',
      isProtected: false
    }
  } as unknown as AdminAccountSummary

  const mockAdminUser: AdminAccountSummary = {
    id: 'user-admin-1',
    email: 'admin1@example.com',
    name: 'Admin User',
    avatarUrl: null,
    status: 'Active',
    createdAt: '2026-10-15T00:00:00Z',
    role: {
      id: 'role-admin-id',
      key: 'admin',
      nameEn: 'Administrator',
      nameVi: 'Quản trị viên',
      isProtected: true
    }
  } as unknown as AdminAccountSummary

  const mockDetail: AdminAccountDetail = {
    id: 'user-learner-1',
    email: 'learner1@example.com',
    name: 'Learner User',
    avatarUrl: null,
    status: 'Active',
    createdAt: '2026-10-15T00:00:00Z',
    preferredLocale: 'en',
    adminControlVersion: 2,
    hasPassword: true,
    hasGoogleAuth: false,
    suspendedByUserId: null,
    suspendedAt: null,
    suspensionReason: null,
    role: {
      id: 'role-learner-id',
      key: 'learner',
      nameEn: 'Learner',
      nameVi: 'Học viên',
      isProtected: false
    }
  } as unknown as AdminAccountDetail

  beforeEach(() => {
    setActivePinia(createPinia())
    const authStore = useAuthStore()
    authStore.user = {
      id: 'admin-caller-id',
      email: 'caller@example.com',
      name: 'Calling Admin'
    } as unknown as AuthUser

    const accessStore = useAdminAccessStore()
    accessStore.capabilities = {
      canViewAdminDashboard: true,
      canManageCatalog: true,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: true,
      canManageRoles: true
    }

    const usersStore = useAdminUsersStore()
    usersStore.users = [mockLearnerUser, mockAdminUser]
    usersStore.selectedUser = mockDetail
    usersStore.totalCount = 2
    usersStore.fetchUsers = vi.fn().mockImplementation(async () => {
      usersStore.users = [mockLearnerUser, mockAdminUser]
      usersStore.totalCount = 2
    })
    usersStore.selectUser = vi.fn().mockImplementation(async () => {
      usersStore.selectedUser = mockDetail
      return mockDetail
    })
    usersStore.fetchRoles = vi.fn().mockResolvedValue(undefined)
  })

  function createWrapper() {
    return mount(AdminUsersPage, {
      global: {
        stubs: {
          Teleport: true,
          NuxtLink: {
            template: '<a><slot /></a>'
          }
        }
      }
    })
  }

  it('displays role badge on user directory items', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    const roleBadges = wrapper.findAll('[data-testid="user-role-badge"]')
    expect(roleBadges.length).toBeGreaterThan(0)
    expect(roleBadges[0].text()).toContain('admin.roles.learner.name')
    wrapper.unmount()
  })

  it('displays role badge and description in the selected user detail panel', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    const detailRoleBadge = wrapper.find('[data-testid="detail-role-badge"]')
    expect(detailRoleBadge.exists()).toBe(true)
    expect(detailRoleBadge.text()).toContain('admin.roles.learner.name')
    expect(wrapper.text()).toContain('admin.roles.learner.description')
    wrapper.unmount()
  })

  it('displays Change Role button when accessStore.canManageRoles is true and selectedUser is not self', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    const changeRoleBtn = wrapper.find('[data-testid="change-role-btn"]')
    expect(changeRoleBtn.exists()).toBe(true)
    expect(changeRoleBtn.text()).toContain('admin.users.changeRole')
    wrapper.unmount()
  })

  it('hides Change Role button when accessStore.canManageRoles is false', async () => {
    const accessStore = useAdminAccessStore()
    accessStore.capabilities = {
      ...accessStore.capabilities,
      canManageRoles: false
    }

    const wrapper = createWrapper()
    await flushPromises()

    const changeRoleBtn = wrapper.find('[data-testid="change-role-btn"]')
    expect(changeRoleBtn.exists()).toBe(false)
    wrapper.unmount()
  })

  it('hides Change Role button when selectedUser is self', async () => {
    const authStore = useAuthStore()
    authStore.user = {
      id: 'user-learner-1',
      email: 'learner1@example.com',
      name: 'Self User'
    } as unknown as AuthUser

    const wrapper = createWrapper()
    await flushPromises()

    const changeRoleBtn = wrapper.find('[data-testid="change-role-btn"]')
    expect(changeRoleBtn.exists()).toBe(false)
    wrapper.unmount()
  })

  it('opens AdminRoleModal when clicking Change Role button', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    const changeRoleBtn = wrapper.find('[data-testid="change-role-btn"]')
    await changeRoleBtn.trigger('click')
    await wrapper.vm.$nextTick()

    const modal = wrapper.findComponent({ name: 'AdminRoleModal' })
    expect(modal.exists()).toBe(true)
    expect(modal.props('open')).toBe(true)
    wrapper.unmount()
  })
})
