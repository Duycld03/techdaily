import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AdminLogin from '~/pages/admin/login.vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { adminTestJwt, clearAuthStorage } from '../helpers/admin'

const render = () => mount(AdminLogin, { global: { stubs: {
  StudioLayout: { template: '<div><slot name="main"/><slot name="dock"/><slot name="footer"/></div>' },
  NuxtLink: { props: ['to'], template: '<a :href="to"><slot /></a>' }, ThemeToggle: true, LocaleSelector: true
} } })

describe('administrator sign-in orchestration', () => {
  beforeEach(() => {
    setActivePinia(createPinia()); clearAuthStorage(); vi.clearAllMocks()
    vi.stubGlobal('useRoute', () => ({ path: '/admin/login', query: { redirect: '/admin/catalog?search=vue' } }))
    useAdminAccessStore().state = 'denied'
  })

  it('replaces a regular session and enters the safe administrator destination only after verification', async () => {
    const auth = useAuthStore()
    auth.setSession(adminTestJwt('learner'), { id: 'learner', email: 'learner@example.test', name: 'Learner', preferredLocale: 'en' })
    const access = useAdminAccessStore()
    access.state = 'denied'
    const login = vi.spyOn(auth, 'login').mockImplementation(async () => {
      auth.setSession(adminTestJwt(), { id: 'admin-1', email: 'admin@example.test', name: 'Admin', preferredLocale: 'en' })
    })
    const verify = vi.spyOn(access, 'verify').mockResolvedValue(true)
    const wrapper = render()
    await wrapper.get('#admin-email').setValue('admin@example.test')
    await wrapper.get('#admin-password').setValue('password')
    await wrapper.get('[name="rememberMe"]').setValue(false)
    await wrapper.get('form').trigger('submit'); await flushPromises()
    expect(login).toHaveBeenCalledWith('admin@example.test', 'password', false)
    expect(verify).toHaveBeenCalledOnce()
    expect(navigateTo).toHaveBeenCalledWith('/admin/catalog?search=vue')
    expect(auth.user?.id).toBe('admin-1')
  })

  it('falls back to first accessible admin route if requested contextual destination is forbidden (F15)', async () => {
    vi.stubGlobal('useRoute', () => ({ path: '/admin/login', query: { redirect: '/admin/users?page=2' } }))
    const auth = useAuthStore()
    const access = useAdminAccessStore()
    vi.spyOn(auth, 'login').mockImplementation(async () => {
      auth.setSession(adminTestJwt(), { id: 'admin-1', email: 'admin@example.test', name: 'Admin', preferredLocale: 'en' })
    })
    vi.spyOn(access, 'verify').mockImplementation(async () => {
      access.state = 'authorized'
      access.capabilities = {
        canViewAdminDashboard: false,
        canManageCatalog: true,
        canManageAccounts: false,
        canReadAudit: false,
        canManageDocumentOperations: false,
        canManageRoles: false,
        canManageCategories: false
      }
      return true
    })

    const wrapper = render()
    await wrapper.get('#admin-email').setValue('admin@example.test')
    await wrapper.get('#admin-password').setValue('password')
    await wrapper.get('form').trigger('submit'); await flushPromises()

    expect(navigateTo).toHaveBeenCalledWith('/admin/catalog')
  })

  it('keeps a successfully signed-in regular account on the accessible admin login page', async () => {
    const auth = useAuthStore()
    vi.spyOn(auth, 'login').mockImplementation(async () => {
      auth.setSession(adminTestJwt('learner'), { id: 'learner', email: 'learner@example.test', name: 'Learner', preferredLocale: 'en' })
    })
    vi.spyOn(useAdminAccessStore(), 'verify').mockImplementation(async () => { useAdminAccessStore().state = 'denied'; return false })
    const wrapper = render()
    await wrapper.get('#admin-email').setValue('learner@example.test')
    await wrapper.get('#admin-password').setValue('password')
    await wrapper.get('form').trigger('submit'); await flushPromises()
    expect(wrapper.get('[role="alert"]').text()).toBe('admin.login.denied')
    expect(auth.isLoggedIn).toBe(true)
    expect(navigateTo).not.toHaveBeenCalled()
  })

  it('shows invalid credentials and allows retry without running capability discovery', async () => {
    useAdminAccessStore().state = 'pending'
    vi.spyOn(useAuthStore(), 'login').mockRejectedValue({ status: 400, code: 'AUTH_INVALID_CREDENTIALS', message: 'Invalid credentials' })
    const verify = vi.spyOn(useAdminAccessStore(), 'verify')
    const wrapper = render()
    await wrapper.get('#admin-email').setValue('admin@example.test')
    await wrapper.get('#admin-password').setValue('wrong')
    await wrapper.get('form').trigger('submit'); await flushPromises()
    expect(wrapper.find('[role="alert"]').exists()).toBe(true)
    expect(wrapper.get('button[type="submit"]').attributes('disabled')).toBeUndefined()
    expect(verify).not.toHaveBeenCalled()
    expect(navigateTo).not.toHaveBeenCalled()
  })
})
