import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import { ref } from 'vue'
import App from '~/app.vue'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { clearAuthStorage } from './helpers/admin'

const currentRoute = ref({
  path: '/',
  fullPath: '/'
})

vi.stubGlobal('useRoute', () => currentRoute.value)

describe('app.vue shell isolation and page keying', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    vi.clearAllMocks()
    vi.stubGlobal('useRouter', () => ({}))
    currentRoute.value = { path: '/', fullPath: '/' }
  })

  it('renders AppHeader and AppSidebar for regular authenticated routes', () => {
    currentRoute.value = { path: '/', fullPath: '/' }
    const wrapper = mount(App, {
      global: {
        stubs: {
          AppHeader: { template: '<header class="stub-header">Header</header>' },
          AppSidebar: { template: '<aside class="stub-sidebar">Sidebar</aside>' },
          AppCommandPalette: true,
          AppToastContainer: true,
          NuxtPage: { template: '<div class="stub-nuxt-page">Page</div>' }
        }
      }
    })

    expect(wrapper.find('.stub-header').exists()).toBe(true)
    expect(wrapper.find('.stub-sidebar').exists()).toBe(true)
    expect(wrapper.find('.stub-nuxt-page').exists()).toBe(true)
  })

  it.each([
    '/login',
    '/login/',
    '/register',
    '/register/',
    '/forgot-password',
    '/reset-password',
    '/admin/login',
    '/admin/login/'
  ])('hides AppHeader and AppSidebar on auth route %s', (path) => {
    currentRoute.value = { path, fullPath: `${path}?redirect=/` }
    const wrapper = mount(App, {
      global: {
        stubs: {
          AppHeader: { template: '<header class="stub-header">Header</header>' },
          AppSidebar: { template: '<aside class="stub-sidebar">Sidebar</aside>' },
          AppCommandPalette: true,
          AppToastContainer: true,
          NuxtPage: { template: '<div class="stub-nuxt-page">Page</div>' }
        }
      }
    })

    expect(wrapper.find('.stub-header').exists()).toBe(false)
    expect(wrapper.find('.stub-sidebar').exists()).toBe(false)
    expect(wrapper.find('.stub-nuxt-page').exists()).toBe(true)
  })

  it.each(['pending', 'unavailable', 'denied'] as const)('withholds the protected page and chrome when admin permission is %s', (state) => {
    currentRoute.value = { path: '/admin', fullPath: '/admin' }
    useAdminAccessStore().state = state
    const wrapper = mount(App, { global: { stubs: {
      AdminShell: { template: '<section data-admin-shell><slot /></section>' },
      AdminAuthorizationBoundary: { template: '<section data-neutral-boundary />' },
      AppHeader: true, AppSidebar: true, AppCommandPalette: true, AppToastContainer: true,
      NuxtPage: { template: '<div data-protected-page />' }
    } } })
    expect(wrapper.find('[data-neutral-boundary]').exists()).toBe(true)
    expect(wrapper.find('[data-protected-page]').exists()).toBe(false)
    expect(wrapper.find('[data-admin-shell]').exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'AppHeader' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'AppSidebar' }).exists()).toBe(false)
  })

  it('selects only administrator chrome for authorized admin routes', () => {
    currentRoute.value = { path: '/admin/catalog', fullPath: '/admin/catalog' }
    useAdminAccessStore().state = 'authorized'
    const wrapper = mount(App, { global: { stubs: {
      AdminShell: { template: '<section data-admin-shell><slot /></section>' },
      AppHeader: true, AppSidebar: true, AppCommandPalette: true, AppToastContainer: true,
      NuxtPage: { template: '<div data-protected-page />' }
    } } })
    expect(wrapper.find('[data-admin-shell]').exists()).toBe(true)
    expect(wrapper.find('[data-protected-page]').exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'AppHeader' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'AppSidebar' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'AppCommandPalette' }).exists()).toBe(false)
  })

  it('opens the login page after denial even before Nuxt finishes the previous protected page', () => {
    currentRoute.value = { path: '/admin', fullPath: '/admin' }
    const committed = ref({ path: '/admin/login', fullPath: '/admin/login?redirect=/admin' })
    vi.stubGlobal('useRouter', () => ({ currentRoute: committed }))
    useAdminAccessStore().state = 'denied'
    const wrapper = mount(App, { global: { stubs: {
      AdminShell: true, AdminAuthorizationBoundary: true, AppToastContainer: true,
      NuxtPage: { template: '<div data-login-page />' }
    } } })
    expect(wrapper.find('[data-login-page]').exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'AdminAuthorizationBoundary' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'AdminShell' }).exists()).toBe(false)
  })

  it('keeps reader mode free of both learner and administrator chrome', () => {
    currentRoute.value = { path: '/read/book-1', fullPath: '/read/book-1' }
    const wrapper = mount(App, { global: { stubs: { AppHeader: true, AppSidebar: true, AppToastContainer: true, AppCommandPalette: true, NuxtPage: true } } })
    expect(wrapper.findComponent({ name: 'AppHeader' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'AppSidebar' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'AdminShell' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'NuxtPage' }).exists()).toBe(true)
  })
})
