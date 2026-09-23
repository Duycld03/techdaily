import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import type { Mock } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import LoginPage from '~/pages/login.vue'
import { useAuthStore } from '~/stores/useAuthStore'

interface GoogleAccountsIdMock {
  initialize: Mock
  renderButton: Mock
}

interface WindowWithGoogle extends Window {
  google?: {
    accounts?: {
      id?: GoogleAccountsIdMock
    }
  }
}

interface MockPublicConfig {
  apiBaseUrl: string
  googleClientId: string
}

interface GlobalWithRuntimeConfig {
  useRuntimeConfig: () => {
    public: MockPublicConfig
  }
  navigateTo: Mock
}

describe('pages/login.vue', () => {
  const win = window as unknown as WindowWithGoogle
  const globalObj = globalThis as unknown as GlobalWithRuntimeConfig
  let originalGoogle: unknown

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    originalGoogle = win.google
    globalObj.useRuntimeConfig().public.googleClientId = 'mock-google-client-id'
  })

  afterEach(() => {
    win.google = originalGoogle as WindowWithGoogle['google']
    globalObj.useRuntimeConfig().public.googleClientId = 'mock-google-client-id'
  })

  it('renders login form properly', () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    expect(wrapper.find('input[type="email"]').exists()).toBe(true)
    expect(wrapper.find('input[type="password"]').exists()).toBe(true)
  })

  it('does not initialize or render Google Identity Services when googleClientId is missing', async () => {
    globalObj.useRuntimeConfig().public.googleClientId = ''

    const initializeSpy = vi.fn()
    const renderButtonSpy = vi.fn()
    win.google = {
      accounts: {
        id: {
          initialize: initializeSpy,
          renderButton: renderButtonSpy
        }
      }
    }

    const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {})

    mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    await vi.waitFor(() => {
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        '[TechDaily Auth] Google Client ID is not configured. Google Sign-In is disabled.'
      )
    }, { timeout: 1000 })

    expect(initializeSpy).not.toHaveBeenCalled()
    expect(renderButtonSpy).not.toHaveBeenCalled()
  })

  it('initializes and renders Google Sign-In button when googleClientId is configured', async () => {
    globalObj.useRuntimeConfig().public.googleClientId = 'test-google-client-id.apps.googleusercontent.com'

    const initializeSpy = vi.fn()
    const renderButtonSpy = vi.fn()
    win.google = {
      accounts: {
        id: {
          initialize: initializeSpy,
          renderButton: renderButtonSpy
        }
      }
    }

    mount(LoginPage, {
      attachTo: document.body,
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    await vi.waitFor(() => {
      expect(initializeSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          client_id: 'test-google-client-id.apps.googleusercontent.com'
        })
      )
    }, { timeout: 1000 })

    expect(renderButtonSpy).toHaveBeenCalled()
  })

  it('redirects to /today when user is already logged in on mount', async () => {
    const authStore = useAuthStore()
    authStore.token = 'existing-token'
    authStore.user = { id: 'u1', email: 'test@example.com', name: 'Tester', preferredLocale: 'en' }

    mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    await flushPromises()
    expect(globalObj.navigateTo).toHaveBeenCalledWith('/today')
  })
})
