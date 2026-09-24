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

  it('switches to register mode and displays name, confirm password, and eye toggles', async () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    // Initially in login mode: only email and password inputs
    expect(wrapper.findAll('input[type="text"]').length).toBe(0)
    expect(wrapper.findAll('input[type="password"]').length).toBe(1)

    // Click Register tab
    const buttons = wrapper.findAll('button')
    const registerTab = buttons.find(b => b.text().includes('Register') || b.text().includes('auth.register_tab'))
    expect(registerTab).toBeDefined()
    await registerTab!.trigger('click')

    // Now in register mode: name and confirm password inputs are visible
    const textInputs = wrapper.findAll('input[type="text"]')
    expect(textInputs.length).toBe(1) // Name input
    expect(textInputs[0].attributes('placeholder')).toBeTruthy()

    const passwordInputs = wrapper.findAll('input[type="password"]')
    expect(passwordInputs.length).toBe(2) // Password and Confirm Password inputs
    expect(passwordInputs[0].attributes('placeholder')).toBeTruthy()
    expect(passwordInputs[1].attributes('placeholder')).toBeTruthy()
  })

  it('shows password mismatch warning when confirm password does not match', async () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    // Switch to register mode
    const buttons = wrapper.findAll('button')
    const registerTab = buttons.find(b => b.text().includes('Register') || b.text().includes('auth.register_tab'))
    await registerTab!.trigger('click')

    const passwordInputs = wrapper.findAll('input[type="password"]')
    await passwordInputs[0].setValue('Password123!')
    await passwordInputs[1].setValue('DifferentPassword!')

    expect(wrapper.text()).toContain('passwords_mismatch')
  })

  it('toggles password visibility with eye toggle button', async () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    // Initially password input has type="password"
    const initialPasswordInput = wrapper.find('input[type="password"]')
    expect(initialPasswordInput.exists()).toBe(true)

    // Find eye toggle button
    const eyeToggle = wrapper.find('button[aria-label="Show password"]')
    expect(eyeToggle.exists()).toBe(true)
    await eyeToggle.trigger('click')

    // Input type becomes "text"
    expect(wrapper.find('input[type="text"]').exists()).toBe(true)
    expect(wrapper.find('button[aria-label="Hide password"]').exists()).toBe(true)
  })
})
