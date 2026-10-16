import { describe, it, expect, beforeEach, vi, type Mock } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import { ref } from 'vue'
import AdminAuthorizationBoundary from '~/components/admin/AdminAuthorizationBoundary.vue'
import { useAdminAccessStore } from '../../app/stores/useAdminAccessStore'
import { useAuthStore } from '../../app/stores/useAuthStore'
import { adminTestJwt, clearAuthStorage } from '../helpers/admin'

const currentRoute = ref({
  path: '/admin/catalog',
  fullPath: '/admin/catalog'
})

vi.stubGlobal('useRoute', () => currentRoute.value)

describe('AdminAuthorizationBoundary.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    vi.clearAllMocks()
    vi.stubGlobal('useRouter', () => ({ currentRoute }))
    currentRoute.value = { path: '/admin/catalog', fullPath: '/admin/catalog' }

    const auth = useAuthStore()
    auth.setSession(adminTestJwt(), { id: 'admin-1', email: 'admin@example.test', name: 'Admin', preferredLocale: 'en' })
  })

  it('renders checking indicator when state is pending and executes retry', async () => {
    const access = useAdminAccessStore()
    access.state = 'pending'
    const verifySpy = vi.spyOn(access, 'verify').mockResolvedValue(true)

    const wrapper = mount(AdminAuthorizationBoundary)
    await flushPromises()

    expect(wrapper.find('[data-admin-boundary]').exists()).toBe(true)
    expect(verifySpy).toHaveBeenCalled()
  })

  it('renders retry button when state is unavailable and retries on click', async () => {
    const access = useAdminAccessStore()
    access.state = 'unavailable'

    const wrapper = mount(AdminAuthorizationBoundary)
    const retryBtn = wrapper.find('button')
    expect(retryBtn.exists()).toBe(true)

    const verifySpy = vi.spyOn(access, 'verify').mockResolvedValue(true)
    await retryBtn.trigger('click')
    await flushPromises()

    expect(verifySpy).toHaveBeenCalled()
  })

  it('navigates to login redirect when capability verification results in denial', async () => {
    const access = useAdminAccessStore()
    access.state = 'pending'
    vi.spyOn(access, 'verify').mockImplementation(async () => {
      access.state = 'denied'
      return false
    })

    mount(AdminAuthorizationBoundary)
    await flushPromises()

    const nav = navigateTo as unknown as Mock
    expect(nav).toHaveBeenCalledWith({
      path: '/admin/login',
      query: { redirect: '/admin/catalog' }
    })
  })

  it('redirects to first accessible admin route if authorized but destination is forbidden (F11)', async () => {
    currentRoute.value = { path: '/admin/catalog', fullPath: '/admin/catalog' }
    const access = useAdminAccessStore()
    access.state = 'pending'
    vi.spyOn(access, 'verify').mockImplementation(async () => {
      access.state = 'authorized'
      access.capabilities = {
        canViewAdminDashboard: false,
        canManageCatalog: false,
        canManageAccounts: false,
        canReadAudit: true,
        canManageDocumentOperations: false,
        canManageRoles: false,
        canManageCategories: false
      }
      return true
    })

    mount(AdminAuthorizationBoundary)
    await flushPromises()

    // Audit-only admin cannot access /admin/catalog -> redirected to /admin/audit
    const nav = navigateTo as unknown as Mock
    expect(nav).toHaveBeenCalledWith('/admin/audit')
  })
})
