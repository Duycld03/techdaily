import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AdminRoleModal from '../../app/components/admin/AdminRoleModal.vue'
import { useAdminUsersStore } from '../../app/stores/useAdminUsersStore'
import { useAuthStore, type AuthUser } from '../../app/stores/useAuthStore'

describe('AdminRoleModal.vue', () => {
  const mockAdminRole = {
    id: 'role-admin-id',
    key: 'admin',
    nameEn: 'Administrator',
    nameVi: 'Quản trị viên',
    isProtected: true,
    supportedPermissions: ['ViewAdminDashboard', 'ManageCatalog', 'ManageAccounts', 'ReadAudit', 'ManageDocumentOperations', 'ManageRoles']
  }

  const mockLearnerRole = {
    id: 'role-learner-id',
    key: 'learner',
    nameEn: 'Learner',
    nameVi: 'Học viên',
    isProtected: false,
    supportedPermissions: []
  }

  const mockUserDetail = {
    id: 'user-target-1',
    email: 'learner@example.com',
    name: 'Target Learner',
    avatarUrl: null,
    status: 'Active' as const,
    createdAt: '2026-10-15T00:00:00Z',
    preferredLocale: 'en',
    adminControlVersion: 3,
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
  }

  beforeEach(() => {
    setActivePinia(createPinia())
    const authStore = useAuthStore()
    authStore.user = {
      id: 'admin-caller-id',
      email: 'admin@example.com',
      name: 'Admin Caller'
    } as unknown as AuthUser

    const usersStore = useAdminUsersStore()
    usersStore.roles = [mockLearnerRole, mockAdminRole] as unknown as typeof usersStore.roles
    usersStore.isLoadingRoles = false
    usersStore.assignRole = vi.fn().mockResolvedValue({ success: true }) as unknown as typeof usersStore.assignRole
    usersStore.selectUser = vi.fn().mockResolvedValue({
      ...mockUserDetail,
      adminControlVersion: 4
    }) as unknown as typeof usersStore.selectUser
  })

  function createWrapper(props = {}) {
    return mount(AdminRoleModal, {
      props: {
        open: true,
        user: mockUserDetail,
        ...props
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })
  }

  it('renders modal when open is true with target user details and current role', async () => {
    const wrapper = createWrapper()

    expect(wrapper.find('[role="dialog"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Target Learner')
    expect(wrapper.text()).toContain('learner@example.com')
    expect(wrapper.text()).toContain('admin.users.currentRole')
    wrapper.unmount()
  })

  it('renders loading skeleton when usersStore.isLoadingRoles is true', async () => {
    const usersStore = useAdminUsersStore()
    usersStore.isLoadingRoles = true

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="role-loading-skeleton"]').exists()).toBe(true)
    wrapper.unmount()
  })

  it('disables submit when no new role is selected', async () => {
    const wrapper = createWrapper()

    const submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    expect(submitBtn.attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })

  it('disables submit and shows error when selecting identical current role', async () => {
    const wrapper = createWrapper()

    // Select learner role (same as current)
    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-learner-id')
    await wrapper.vm.$nextTick()

    const submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    expect(submitBtn.attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('admin.users.roleSameError')
    wrapper.unmount()
  })

  it('displays supported permissions when a privileged role is selected', async () => {
    const wrapper = createWrapper()

    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-admin-id')
    await wrapper.vm.$nextTick()

    const permissionsBlock = wrapper.find('[data-testid="supported-permissions-block"]')
    expect(permissionsBlock.exists()).toBe(true)
    expect(permissionsBlock.text()).toContain('admin.permissions.ManageRoles')
    expect(permissionsBlock.text()).toContain('admin.permissions.ManageAccounts')
    wrapper.unmount()
  })

  it('disables submit when reason is empty or exceeds 500 characters', async () => {
    const wrapper = createWrapper()

    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-admin-id')
    await wrapper.vm.$nextTick()

    const submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    // Empty reason
    expect(submitBtn.attributes('disabled')).toBeDefined()

    // Exceeds 500 characters
    const textarea = wrapper.find('[data-testid="admin-role-reason"]')
    await textarea.setValue('A'.repeat(501))
    await wrapper.vm.$nextTick()

    expect(submitBtn.attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })

  it('submits role assignment with exact payload on valid form', async () => {
    const usersStore = useAdminUsersStore()
    const wrapper = createWrapper()

    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-admin-id')

    const textarea = wrapper.find('[data-testid="admin-role-reason"]')
    await textarea.setValue('Promoting to administrator for system ops')
    await wrapper.vm.$nextTick()

    const submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    expect(submitBtn.attributes('disabled')).toBeUndefined()

    await submitBtn.trigger('click')
    await flushPromises()

    expect(usersStore.assignRole).toHaveBeenCalledWith(
      'user-target-1',
      'role-admin-id',
      'Promoting to administrator for system ops',
      3
    )
    expect(wrapper.emitted('success')).toBeTruthy()
    expect(wrapper.emitted('close')).toBeTruthy()
    wrapper.unmount()
  })

  it('handles concurrency conflict by preserving draft inputs and providing reload action', async () => {
    const usersStore = useAdminUsersStore()
    usersStore.assignRole = vi.fn().mockResolvedValue({
      success: false,
      errorCode: 'ACCOUNT_CONCURRENCY_CONFLICT',
      errorMessage: 'Account was modified by another administrator.'
    }) as unknown as typeof usersStore.assignRole

    const wrapper = createWrapper()

    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-admin-id')

    const textarea = wrapper.find('[data-testid="admin-role-reason"]')
    await textarea.setValue('Operational promotion reason')
    await wrapper.vm.$nextTick()

    const submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    await submitBtn.trigger('click')
    await flushPromises()

    // Conflict error displayed
    expect(wrapper.find('[data-testid="role-form-error"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('admin.users.roleConcurrencyError')

    // Draft reason and selected role preserved
    expect((wrapper.find('[data-testid="admin-role-reason"]').element as HTMLTextAreaElement).value).toBe('Operational promotion reason')

    // Reload button offered
    const reloadBtn = wrapper.find('[data-testid="reload-account-btn"]')
    expect(reloadBtn.exists()).toBe(true)

    // Click Reload Account
    await reloadBtn.trigger('click')
    await flushPromises()

    expect(usersStore.selectUser).toHaveBeenCalledWith('user-target-1')
    // Conflict error cleared, ready to resubmit
    expect(wrapper.find('[data-testid="reload-account-btn"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('displays protection error when last admin assignment is rejected', async () => {
    const usersStore = useAdminUsersStore()
    usersStore.assignRole = vi.fn().mockResolvedValue({
      success: false,
      errorCode: 'ACCOUNT_LAST_ADMIN_PROTECTED',
      errorMessage: 'Cannot reassign role: at least one active administrator must remain'
    }) as unknown as typeof usersStore.assignRole

    const adminUser = {
      ...mockUserDetail,
      role: { id: 'role-admin-id', key: 'admin', nameEn: 'Administrator', nameVi: 'Quản trị viên', isProtected: true }
    }

    const wrapper = createWrapper({ user: adminUser })

    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-learner-id')

    const textarea = wrapper.find('[data-testid="admin-role-reason"]')
    await textarea.setValue('Demotion test')
    await wrapper.vm.$nextTick()

    const submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    await submitBtn.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('admin.users.roleLastAdminError')
    wrapper.unmount()
  })

  it('prevents self role change if target user is caller', async () => {
    const authStore = useAuthStore()
    authStore.user = { id: 'user-target-1', email: 'self@example.com' } as unknown as AuthUser

    const wrapper = createWrapper()

    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-admin-id')

    const textarea = wrapper.find('[data-testid="admin-role-reason"]')
    await textarea.setValue('Self promotion')
    await wrapper.vm.$nextTick()

    const submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    expect(submitBtn.attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })

  it('closes modal on Escape key and emits close', async () => {
    const wrapper = createWrapper()

    const dialog = wrapper.find('[role="dialog"]')
    await dialog.trigger('keydown', { key: 'Escape' })

    expect(wrapper.emitted('close')).toBeTruthy()
    wrapper.unmount()
  })
})
