import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AdminRoleModal from '../../app/components/admin/AdminRoleModal.vue'
import { useAdminUsersStore, type AdminAccountDetail } from '../../app/stores/useAdminUsersStore'
import { useAuthStore, type AuthUser } from '../../app/stores/useAuthStore'

describe('AdminRoleModal.vue', () => {
  const mockAdminRole = {
    id: 'role-admin-id',
    key: 'admin',
    nameEn: 'Administrator',
    nameVi: 'Quản trị viên',
    permissionKeys: ['ViewAdminDashboard', 'ManageCatalog', 'ManageAccounts', 'ReadAudit', 'ManageDocumentOperations', 'ManageRoles', 'ManageCategories']
  }

  const mockLearnerRole = {
    id: 'role-learner-id',
    key: 'learner',
    nameEn: 'Learner',
    nameVi: 'Học viên',
    permissionKeys: []
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
    authStore.isInitialized = true
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

  function deferredReload() {
    let resolve!: (value: AdminAccountDetail | null) => void
    let reject!: (reason: Error) => void
    const promise = new Promise<AdminAccountDetail | null>((resolvePromise, rejectPromise) => {
      resolve = resolvePromise
      reject = rejectPromise
    })
    return { promise, resolve, reject }
  }

  async function enterConflict(wrapper: VueWrapper, draftReason = 'Preserved draft reason') {
    const usersStore = useAdminUsersStore()
    usersStore.assignRole = vi.fn().mockResolvedValue({
      success: false,
      errorCode: 'ACCOUNT_CONCURRENCY_CONFLICT',
      errorMessage: 'Account was modified by another administrator.'
    })
    wrapper.findComponent({ name: 'AppSelect' }).vm.$emit('update:modelValue', mockAdminRole.id)
    await wrapper.find('[data-testid="admin-role-reason"]').setValue(draftReason)
    await wrapper.find('[data-testid="submit-role-assign"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="reload-account-btn"]').exists()).toBe(true)
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
    expect(permissionsBlock.text()).toContain('admin.permissions.ManageCategories')
    wrapper.unmount()
  })

  it('displays empty grant indicator when a role with no permissions is selected', async () => {
    const adminUser = {
      ...mockUserDetail,
      role: { id: 'role-admin-id', key: 'admin', nameEn: 'Administrator', nameVi: 'Quản trị viên', isProtected: true }
    }
    const wrapper = createWrapper({ user: adminUser })

    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-learner-id')
    await wrapper.vm.$nextTick()

    const emptyBlock = wrapper.find('[data-testid="empty-permissions-block"]')
    expect(emptyBlock.exists()).toBe(true)
    expect(emptyBlock.text()).toContain('admin.users.noPermissions')
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

    let submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
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
    // Reload does NOT auto-submit; assignRole called only once initially
    expect(usersStore.assignRole).toHaveBeenCalledTimes(1)

    // Explicit reconfirmation uses refreshed version
    submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    expect(submitBtn.attributes('disabled')).toBeUndefined()
    usersStore.assignRole = vi.fn().mockResolvedValue({ success: true }) as unknown as typeof usersStore.assignRole
    await submitBtn.trigger('click')
    await flushPromises()
    expect(usersStore.assignRole).toHaveBeenCalledWith(
      'user-target-1',
      'role-admin-id',
      'Operational promotion reason',
      4
    )
    wrapper.unmount()
  })

  it('retains conflict gate and draft inputs when reload fails or returns null', async () => {
    const usersStore = useAdminUsersStore()
    usersStore.assignRole = vi.fn().mockResolvedValue({
      success: false,
      errorCode: 'ACCOUNT_CONCURRENCY_CONFLICT',
      errorMessage: 'Account was modified by another administrator.'
    }) as unknown as typeof usersStore.assignRole
    usersStore.selectUser = vi.fn().mockResolvedValue(null) as unknown as typeof usersStore.selectUser

    const wrapper = createWrapper()
    const roleSelect = wrapper.findComponent({ name: 'AppSelect' })
    roleSelect.vm.$emit('update:modelValue', 'role-admin-id')
    const textarea = wrapper.find('[data-testid="admin-role-reason"]')
    await textarea.setValue('Keep my draft reason')
    await wrapper.vm.$nextTick()

    await wrapper.find('[data-testid="submit-role-assign"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="reload-account-btn"]').exists()).toBe(true)

    // Reload fails (returns null)
    await wrapper.find('[data-testid="reload-account-btn"]').trigger('click')
    await flushPromises()

    // Conflict gate remains active
    expect(wrapper.find('[data-testid="reload-account-btn"]').exists()).toBe(true)
    const submitBtn = wrapper.find('[data-testid="submit-role-assign"]')
    expect(submitBtn.attributes('disabled')).toBeDefined()
    expect((wrapper.find('[data-testid="admin-role-reason"]').element as HTMLTextAreaElement).value).toBe('Keep my draft reason')
    wrapper.unmount()
  })

  it('preserves conflict gate and draft inputs across modal close and reopen for same target', async () => {
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
    await textarea.setValue('Preserved draft reason')
    await wrapper.vm.$nextTick()

    await wrapper.find('[data-testid="submit-role-assign"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="reload-account-btn"]').exists()).toBe(true)

    // Close modal
    await wrapper.setProps({ open: false })
    await wrapper.vm.$nextTick()

    // Reopen modal for same user
    await wrapper.setProps({ open: true })
    await wrapper.vm.$nextTick()

    // Conflict gate and draft inputs are still preserved
    expect(wrapper.find('[data-testid="reload-account-btn"]').exists()).toBe(true)
    expect((wrapper.find('[data-testid="admin-role-reason"]').element as HTMLTextAreaElement).value).toBe('Preserved draft reason')
    expect(wrapper.find('[data-testid="submit-role-assign"]').attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })

  it('resets dialog state when target user is replaced', async () => {
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
    await textarea.setValue('First user reason')
    await wrapper.vm.$nextTick()

    await wrapper.find('[data-testid="submit-role-assign"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="reload-account-btn"]').exists()).toBe(true)

    // Replace with a different target user
    const secondUser = {
      ...mockUserDetail,
      id: 'user-target-2',
      email: 'learner2@example.com',
      name: 'Second Learner'
    }
    await wrapper.setProps({ user: secondUser, open: true })
    await wrapper.vm.$nextTick()

    // Conflict gate is cleared and inputs reset for the new user
    expect(wrapper.find('[data-testid="reload-account-btn"]').exists()).toBe(false)
    expect((wrapper.find('[data-testid="admin-role-reason"]').element as HTMLTextAreaElement).value).toBe('')
    wrapper.unmount()
  })

  it.each(['null', 'rejection'] as const)('ignores an old %s reload after replacing the target while the new target reloads', async (outcome) => {
    const usersStore = useAdminUsersStore()
    const oldReload = deferredReload()
    const currentReload = deferredReload()
    usersStore.selectUser = vi.fn()
      .mockReturnValueOnce(oldReload.promise)
      .mockReturnValueOnce(currentReload.promise)
    const wrapper = createWrapper()

    await enterConflict(wrapper)
    await wrapper.find('[data-testid="reload-account-btn"]').trigger('click')
    const secondUser = { ...mockUserDetail, id: 'user-target-2', name: 'Second Learner' }
    await wrapper.setProps({ user: secondUser })
    await enterConflict(wrapper, 'Second account draft')
    expect(wrapper.find('[data-testid="reload-account-btn"]').attributes('disabled')).toBeUndefined()
    await wrapper.find('[data-testid="reload-account-btn"]').trigger('click')
    expect(usersStore.selectUser).toHaveBeenNthCalledWith(2, secondUser.id)

    if (outcome === 'null') oldReload.resolve(null)
    else oldReload.reject(new Error('Old account reload failed'))
    await flushPromises()

    expect(wrapper.find('[data-testid="role-form-error"]').text()).toContain('admin.users.roleConcurrencyError')
    expect(wrapper.find('[data-testid="reload-account-btn"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[data-testid="submit-role-assign"]').attributes('disabled')).toBeDefined()
    expect((wrapper.find('[data-testid="admin-role-reason"]').element as HTMLTextAreaElement).value).toBe('Second account draft')

    currentReload.resolve({ ...secondUser, adminControlVersion: 8 })
    await flushPromises()
    expect(wrapper.find('[data-testid="role-form-error"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="submit-role-assign"]').attributes('disabled')).toBeUndefined()
    usersStore.assignRole = vi.fn().mockResolvedValue({ success: true })
    await wrapper.find('[data-testid="submit-role-assign"]').trigger('click')
    await flushPromises()
    expect(usersStore.assignRole).toHaveBeenCalledWith(secondUser.id, mockAdminRole.id, 'Second account draft', 8)
    wrapper.unmount()
  })

  it.each(['success', 'null', 'rejection'] as const)('ignores an old %s reload after closing and reopening the same conflict', async (outcome) => {
    const usersStore = useAdminUsersStore()
    const oldReload = deferredReload()
    const currentReload = deferredReload()
    usersStore.selectUser = vi.fn()
      .mockReturnValueOnce(oldReload.promise)
      .mockReturnValueOnce(currentReload.promise)
    const wrapper = createWrapper()
    await enterConflict(wrapper)
    await wrapper.find('[data-testid="reload-account-btn"]').trigger('click')

    await wrapper.setProps({ open: false })
    await wrapper.setProps({ open: true })
    expect(wrapper.find('[data-testid="reload-account-btn"]').attributes('disabled')).toBeUndefined()
    expect(wrapper.findComponent({ name: 'AppSelect' }).props('modelValue')).toBe(mockAdminRole.id)
    expect((wrapper.find('[data-testid="admin-role-reason"]').element as HTMLTextAreaElement).value).toBe('Preserved draft reason')
    await wrapper.find('[data-testid="reload-account-btn"]').trigger('click')
    expect(usersStore.selectUser).toHaveBeenCalledTimes(2)

    if (outcome === 'success') oldReload.resolve({ ...mockUserDetail, adminControlVersion: 5 })
    else if (outcome === 'null') oldReload.resolve(null)
    else oldReload.reject(new Error('Closed dialog reload failed'))
    await flushPromises()

    expect(wrapper.find('[data-testid="role-form-error"]').text()).toContain('admin.users.roleConcurrencyError')
    expect(wrapper.find('[data-testid="reload-account-btn"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[data-testid="submit-role-assign"]').attributes('disabled')).toBeDefined()

    currentReload.resolve({ ...mockUserDetail, adminControlVersion: 6 })
    await flushPromises()
    expect(wrapper.find('[data-testid="role-form-error"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="submit-role-assign"]').attributes('disabled')).toBeUndefined()
    expect(usersStore.assignRole).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it.each([
    ['actor', 'success'],
    ['actor', 'null'],
    ['actor', 'rejection'],
    ['session', 'success'],
    ['session', 'null'],
    ['session', 'rejection'],
    ['authorization', 'success'],
    ['authorization', 'null'],
    ['authorization', 'rejection']
  ] as const)('ignores an old %s context reload completing with %s', async (context, outcome) => {
    const usersStore = useAdminUsersStore()
    const authStore = useAuthStore()
    const oldReload = deferredReload()
    const currentReload = deferredReload()
    usersStore.selectUser = vi.fn()
      .mockReturnValueOnce(oldReload.promise)
      .mockReturnValueOnce(currentReload.promise)
    const wrapper = createWrapper()
    await enterConflict(wrapper)
    await wrapper.find('[data-testid="reload-account-btn"]').trigger('click')

    if (context === 'actor') authStore.user = { ...authStore.user!, id: 'replacement-admin' }
    else if (context === 'session') authStore.sessionGeneration++
    else authStore.adminGeneration++
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[data-testid="reload-account-btn"]').attributes('disabled')).toBeUndefined()
    await wrapper.find('[data-testid="reload-account-btn"]').trigger('click')
    expect(usersStore.selectUser).toHaveBeenCalledTimes(2)

    if (outcome === 'success') oldReload.resolve({ ...mockUserDetail, adminControlVersion: 5 })
    else if (outcome === 'null') oldReload.resolve(null)
    else oldReload.reject(new Error('Previous session reload failed'))
    await flushPromises()

    expect(wrapper.find('[data-testid="role-form-error"]').text()).toContain('admin.users.roleConcurrencyError')
    expect(wrapper.find('[data-testid="reload-account-btn"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[data-testid="submit-role-assign"]').attributes('disabled')).toBeDefined()
    currentReload.resolve({ ...mockUserDetail, adminControlVersion: 6 })
    await flushPromises()
    expect(wrapper.find('[data-testid="role-form-error"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="submit-role-assign"]').attributes('disabled')).toBeUndefined()
    wrapper.unmount()
  })

  it.each(['null', 'rejection', 'different target'] as const)('settles the current reload without unlocking a conflict on %s', async (outcome) => {
    const usersStore = useAdminUsersStore()
    const reload = deferredReload()
    usersStore.selectUser = vi.fn().mockReturnValue(reload.promise)
    const wrapper = createWrapper()
    await enterConflict(wrapper)
    await wrapper.find('[data-testid="reload-account-btn"]').trigger('click')
    expect(wrapper.find('[data-testid="reload-account-btn"]').attributes('disabled')).toBeDefined()

    if (outcome === 'null') reload.resolve(null)
    else if (outcome === 'rejection') reload.reject(new Error('Current reload failed'))
    else reload.resolve({ ...mockUserDetail, id: 'different-account' })
    await flushPromises()

    expect(wrapper.find('[data-testid="reload-account-btn"]').attributes('disabled')).toBeUndefined()
    expect(wrapper.find('[data-testid="submit-role-assign"]').attributes('disabled')).toBeDefined()
    const expectedError = outcome === 'null'
      ? 'admin.users.reloadFailed'
      : outcome === 'rejection' ? 'Current reload failed' : 'admin.users.roleConcurrencyError'
    expect(wrapper.find('[data-testid="role-form-error"]').text()).toContain(expectedError)
    expect((wrapper.find('[data-testid="admin-role-reason"]').element as HTMLTextAreaElement).value).toBe('Preserved draft reason')
    wrapper.unmount()
  })

  it('does not move focus when selecting an account while closed but restores the captured trigger after actual close', async () => {
    const trigger = document.createElement('button')
    const otherControl = document.createElement('button')
    document.body.append(trigger, otherControl)
    otherControl.focus()
    const wrapper = createWrapper({ open: false, user: null, initiatingButton: trigger })

    try {
      await flushPromises()
      expect(document.activeElement).toBe(otherControl)
      await wrapper.setProps({ user: mockUserDetail })
      await flushPromises()
      expect(document.activeElement).toBe(otherControl)

      trigger.focus()
      await wrapper.setProps({ open: true })
      await wrapper.find('[role="dialog"]').trigger('keydown', { key: 'Escape' })
      expect(wrapper.emitted('close')).toBeTruthy()
      otherControl.focus()
      await wrapper.setProps({ open: false })
      await flushPromises()
      expect(document.activeElement).toBe(trigger)

      otherControl.focus()
      await wrapper.setProps({ user: { ...mockUserDetail, id: 'user-target-2' } })
      await flushPromises()
      expect(document.activeElement).toBe(otherControl)
    } finally {
      wrapper.unmount()
      trigger.remove()
      otherControl.remove()
    }
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
    authStore.isInitialized = true
    authStore.user = { id: 'user-target-1', email: 'self@example.com' } as unknown as AuthUser
    const usersStore = useAdminUsersStore()
    usersStore.roles = [mockLearnerRole, mockAdminRole] as unknown as typeof usersStore.roles

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
