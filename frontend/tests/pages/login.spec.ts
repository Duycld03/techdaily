import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import type { Mock } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import LoginPage from '~/pages/login.vue'
import App from '~/app.vue'
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
interface GlobalWithRoute {
  useRoute: () => {
    path: string
    params: Record<string, string>
    query: Record<string, string>
  }
}


describe('pages/login.vue', () => {
  const win = window as unknown as WindowWithGoogle
  const globalObj = globalThis as unknown as GlobalWithRuntimeConfig
  const globalRoute = globalThis as unknown as GlobalWithRoute
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

  it('renders Studio Auth desktop branding stage and auth card', () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    // Brand stage
    expect(wrapper.text()).toContain('TechDaily')
    expect(wrapper.text()).toContain('TECHDAILY')
    // Right interactive auth card
    expect(wrapper.find('.glass-panel').exists()).toBe(true)
  })

  it('renders inputs for name, email, and dual passwords in register mode', async () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    const buttons = wrapper.findAll('button')
    const registerTab = buttons.find(b => b.text().includes('Register') || b.text().includes('auth.register_tab'))
    await registerTab!.trigger('click')

    // Col 1 (Row 1): Name, Col 2 (Row 1): Email
    const nameInput = wrapper.find('input[type="text"]')
    const emailInput = wrapper.find('input[type="email"]')
    const passwordInputs = wrapper.findAll('input[type="password"]')
    expect(nameInput.exists()).toBe(true)
    expect(emailInput.exists()).toBe(true)
    expect(passwordInputs.length).toBe(2)
  })

  it('submits login credentials when form is valid', async () => {
    const authStore = useAuthStore()
    const mockAuthResponse = {
      token: 'mock-token',
      user: { id: 'u1', email: 'user@example.com', name: 'User', preferredLocale: 'en' }
    }
    const loginSpy = vi.spyOn(authStore, 'login').mockResolvedValue(mockAuthResponse)

    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    await wrapper.find('input[type="email"]').setValue('user@example.com')
    await wrapper.find('input[type="password"]').setValue('password123')
    await wrapper.find('form').trigger('submit.prevent')

    expect(loginSpy).toHaveBeenCalledWith('user@example.com', 'password123')
  })

  it('submits register credentials when form is valid', async () => {
    const authStore = useAuthStore()
    const mockAuthResponse = {
      token: 'mock-token',
      user: { id: 'u1', email: 'alex@example.com', name: 'Alex Morgan', preferredLocale: 'en' }
    }
    const registerSpy = vi.spyOn(authStore, 'register').mockResolvedValue(mockAuthResponse)

    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    const buttons = wrapper.findAll('button')
    const registerTab = buttons.find(b => b.text().includes('Register') || b.text().includes('auth.register_tab'))
    await registerTab!.trigger('click')

    await wrapper.find('input[type="text"]').setValue('Alex Morgan')
    await wrapper.find('input[type="email"]').setValue('alex@example.com')
    const passwordInputs = wrapper.findAll('input[type="password"]')
    await passwordInputs[0].setValue('SecurePass123!')
    await passwordInputs[1].setValue('SecurePass123!')

    await wrapper.find('form').trigger('submit.prevent')

    expect(registerSpy).toHaveBeenCalledWith('alex@example.com', 'SecurePass123!', 'Alex Morgan', 'en')
  })

  it('validates required fields before submitting register', async () => {
    const authStore = useAuthStore()
    const mockAuthResponse = {
      token: 'mock-token',
      user: { id: 'u1', email: 'alex@example.com', name: 'Alex Morgan', preferredLocale: 'en' }
    }
    const registerSpy = vi.spyOn(authStore, 'register').mockResolvedValue(mockAuthResponse)
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    const buttons = wrapper.findAll('button')
    const registerTab = buttons.find(b => b.text().includes('Register') || b.text().includes('auth.register_tab'))
    await registerTab!.trigger('click')

    // Missing email / password
    await wrapper.find('form').trigger('submit.prevent')
    expect(registerSpy).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('auth.toast_enter_credentials')

    // Provide email and password, but missing name
    await wrapper.find('input[type="email"]').setValue('alex@example.com')
    const passwordInputs = wrapper.findAll('input[type="password"]')
    await passwordInputs[0].setValue('SecurePass123!')
    await passwordInputs[1].setValue('SecurePass123!')

    await wrapper.find('form').trigger('submit.prevent')
    expect(registerSpy).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('auth.toast_name_required')

    // Short password (< 8 chars)
    await wrapper.find('input[type="text"]').setValue('Alex Morgan')
    await passwordInputs[0].setValue('short')
    await passwordInputs[1].setValue('short')

    await wrapper.find('form').trigger('submit.prevent')
    expect(registerSpy).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('api_errors.AUTH_PASSWORD_TOO_SHORT')
  })

  it('renders absolute centered OAuth divider without layout blowout', () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })

    const dividerContainer = wrapper.find('.relative.my-4, .relative.my-5')
    expect(dividerContainer.exists()).toBe(true)

    const absoluteLine = dividerContainer.find('.absolute.inset-0.flex.items-center')
    expect(absoluteLine.exists()).toBe(true)
    expect(absoluteLine.find('.border-t').exists()).toBe(true)

    const dividerBadge = dividerContainer.find('.relative.flex.justify-center')
    expect(dividerBadge.exists()).toBe(true)
    expect(dividerBadge.text()).toContain('auth.or_continue_with')
  })

  it('renders top ambient utility bar with emblem, LocaleSelector, and ThemeToggle', () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          LocaleSelector: { template: '<div data-testid="locale-selector">Locale</div>' },
          ThemeToggle: { template: '<div data-testid="theme-toggle">Theme</div>' }
        }
      }
    })

    const header = wrapper.find('header')
    expect(header.exists()).toBe(true)
    expect(header.text()).toContain('TechDaily')
    expect(wrapper.find('[data-testid="locale-selector"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="theme-toggle"]').exists()).toBe(true)
  })

  it('switches to forgot-password mode and submits email for password reset', async () => {
    const wrapper = mount(LoginPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          LocaleSelector: true,
          ThemeToggle: true
        }
      }
    })

    // Click "Forgot password?"
    const forgotLink = wrapper.findAll('button').find(b => b.text().includes('auth.forgot_password_link'))
    expect(forgotLink).toBeDefined()
    await forgotLink!.trigger('click')
    await flushPromises()

    // Header updates to recovery title
    expect(wrapper.text()).toContain('auth.recover_cockpit_title')
    expect(wrapper.text()).toContain('auth.recover_cockpit_subtitle')
    // Submit with email
    const emailInput = wrapper.find('input[type="email"]')
    await emailInput.setValue('developer@techdaily.io')

    const form = wrapper.find('form')
    await form.trigger('submit.prevent')
    await flushPromises()

    // Mode returns to login after reset link is sent
    expect(wrapper.text()).toContain('auth.welcome_title')
  })

  describe('app shell isolation', () => {
    it('suppresses AppHeader on /login route', () => {
      const origUseRoute = globalRoute.useRoute
      globalRoute.useRoute = () => ({ path: '/login', params: {}, query: {} })

      const wrapper = mount(App, {
        global: {
          stubs: {
            AppHeader: { template: '<header data-testid="header">Header</header>' },
            AppSidebar: true,
            AppCommandPalette: true,
            AppToastContainer: true,
            NuxtPage: true
          }
        }
      })

      expect(wrapper.find('[data-testid="header"]').exists()).toBe(false)
      globalRoute.useRoute = origUseRoute
    })

    it('suppresses AppSidebar on /login route', () => {
      const origUseRoute = globalRoute.useRoute
      globalRoute.useRoute = () => ({ path: '/login', params: {}, query: {} })

      const wrapper = mount(App, {
        global: {
          stubs: {
            AppHeader: true,
            AppSidebar: { template: '<aside data-testid="sidebar">Sidebar</aside>' },
            AppCommandPalette: true,
            AppToastContainer: true,
            NuxtPage: true
          }
        }
      })

      expect(wrapper.find('[data-testid="sidebar"]').exists()).toBe(false)
      globalRoute.useRoute = origUseRoute
    })

    it('renders AppHeader and AppSidebar on non-auth route', () => {
      const origUseRoute = globalRoute.useRoute
      globalRoute.useRoute = () => ({ path: '/today', params: {}, query: {} })

      const wrapper = mount(App, {
        global: {
          stubs: {
            AppHeader: { template: '<header data-testid="header">Header</header>' },
            AppSidebar: { template: '<aside data-testid="sidebar">Sidebar</aside>' },
            AppCommandPalette: true,
            AppToastContainer: true,
            NuxtPage: true
          }
        }
      })

      expect(wrapper.find('[data-testid="header"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="sidebar"]').exists()).toBe(true)
      globalRoute.useRoute = origUseRoute
    })
  })

  describe('Studio Cockpit Layout & Telemetry Architecture', () => {
    it('renders clean top header branding without telemetry clutter or version pill', () => {
      const wrapper = mount(LoginPage, {
        global: {
          stubs: {
            NuxtLink: { template: '<a><slot /></a>' },
            LocaleSelector: { template: '<div data-testid="locale-selector">Locale</div>' },
            ThemeToggle: { template: '<div data-testid="theme-toggle">Theme</div>' }
          }
        }
      })

      expect(wrapper.text()).toContain('TechDaily')
      expect(wrapper.text()).not.toContain('IDE')
      expect(wrapper.text()).not.toContain('v2.5.0-sys')
      expect(wrapper.text()).not.toContain('PING 18ms')
      expect(wrapper.text()).not.toContain('auth.status_operational')
      expect(wrapper.text()).not.toContain('auth.system_invariant')
      expect(wrapper.find('[data-testid="locale-selector"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="theme-toggle"]').exists()).toBe(true)
    })

    it('renders curriculum telemetry showcase with gauges and live code showcase', () => {
      const wrapper = mount(LoginPage, {
        global: {
          stubs: {
            NuxtLink: { template: '<a><slot /></a>' }
          }
        }
      })

      // Left showcase elements
      expect(wrapper.text()).toContain('TECHDAILY')
      expect(wrapper.text()).toContain('SM-2 ACTIVE RECALL')
      expect(wrapper.text()).toContain('auth.cockpit_title')
      expect(wrapper.text()).toContain('auth.session_interval_target')
      expect(wrapper.text()).toContain('auth.session_interval_hit')
      expect(wrapper.text()).toContain('auth.sm2_spaced_decay')
      expect(wrapper.text()).toContain('auth.sm2_decay_value')

      // Live code card
      expect(wrapper.text()).toContain('auth.file_consensus')
      expect(wrapper.text()).toContain('techDaily.getDailySlice')
    })

    it('renders interactive auth cockpit card with shortcut badges and session persistence', () => {
      globalObj.useRuntimeConfig().public.googleClientId = ''
      const wrapper = mount(LoginPage, {
        global: {
          stubs: {
            NuxtLink: { template: '<a><slot /></a>' }
          }
        }
      })

      // Google OAuth button present, GitHub OAuth removed
      expect(wrapper.text()).toContain('⌘L')
      expect(wrapper.text()).not.toContain('⌘G')

      // Monospace labels
      expect(wrapper.text()).toContain('auth.dev_handle_label')
      expect(wrapper.text()).toContain('auth.secret_token_label')
      // Session persistence and submit shortcut
      expect(wrapper.text()).toContain('auth.remember_session')
      expect(wrapper.text()).toContain('↵ RETURN')
    })

    it('does not render bottom compliance telemetry bar or redundant status lines', () => {
      const wrapper = mount(LoginPage, {
        global: {
          stubs: {
            NuxtLink: { template: '<a><slot /></a>' }
          }
        }
      })

      expect(wrapper.text()).not.toContain('auth.compliance_footer')
      expect(wrapper.text()).not.toContain('auth.node_secure')
      expect(wrapper.text()).not.toContain('auth.tls_badge')
      expect(wrapper.find('footer').exists()).toBe(false)
    })
  })
  describe('Studio Cockpit Account Recovery & 3-Tab Switcher', () => {
    it('switches to recovery mode via top segmented recovery tab', async () => {
      const wrapper = mount(LoginPage, {
        global: {
          stubs: {
            NuxtLink: { template: '<a><slot /></a>' }
          }
        }
      })

      // Find recovery tab in top segmented switcher
      const buttons = wrapper.findAll('button')
      const recoveryTab = buttons.find(b => b.text().includes('auth.recovery_tab_title'))
      expect(recoveryTab).toBeDefined()
      await recoveryTab!.trigger('click')
      await flushPromises()

      // Assert recovery header and advisory notice
      expect(wrapper.text()).toContain('auth.recover_cockpit_title')
      expect(wrapper.text()).toContain('auth.recover_cockpit_subtitle')
      expect(wrapper.text()).toContain('auth.account_email_label')
      expect(wrapper.text()).toContain('auth.oauth_bypass_notice')
      expect(wrapper.text()).toContain('auth.send_recovery_link_btn')
      expect(wrapper.text()).toContain('auth.back_to_signin_btn')

      // Click "Back to Sign In"
      const backBtn = wrapper.findAll('button').find(b => b.text().includes('auth.back_to_signin_btn'))
      expect(backBtn).toBeDefined()
      await backBtn!.trigger('click')
      await flushPromises()

      // Returns to login mode
      expect(wrapper.text()).toContain('auth.welcome_title')
    })

    it('renders zero-knowledge security footnote and legal links in card footer', () => {
      const wrapper = mount(LoginPage, {
        global: {
          stubs: {
            NuxtLink: { template: '<a><slot /></a>' }
          }
        }
      })

      expect(wrapper.text()).toContain('auth.zero_knowledge_badge')
      expect(wrapper.text()).toContain('auth.terms_link')
      expect(wrapper.text()).toContain('auth.privacy_link')
    })
  })
})
