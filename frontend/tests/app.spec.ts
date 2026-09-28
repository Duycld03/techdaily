import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import { ref } from 'vue'
import App from '~/app.vue'

const currentRoute = ref({
  path: '/',
  fullPath: '/'
})

vi.stubGlobal('useRoute', () => currentRoute.value)

describe('app.vue shell isolation and page keying', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
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
    '/reset-password'
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
})
