import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import SettingsPage from '~/pages/settings.vue'
import AppSelect from '~/components/common/AppSelect.vue'
import { useProfileStore } from '~/stores/useProfileStore'
import { useToast } from '~/composables/useToast'
import { ApiError } from '~/composables/useApiClient'
import { ref } from 'vue'

const mockSendTestPush = vi.fn()
const mockSubscribeUser = vi.fn()
const mockUnsubscribeUser = vi.fn()
const mockCheckSubscriptionStatus = vi.fn()
const mockIsSubscribed = ref(true)

vi.mock('~/composables/useApiClient', () => ({
  ApiError: class ApiError extends Error {
    code?: string
    status: number
    details?: unknown
    data?: unknown
    constructor(message: string, status: number, code?: string, details?: unknown, data?: unknown) {
      super(message)
      this.name = 'ApiError'
      this.status = status
      this.code = code
      this.details = details
      this.data = data
    }
  },
  useApiClient: () => ({
    get: vi.fn(async () => ({ user: { preferredStudyTime: '08:00', streakAlertTime: '20:00', timeZone: 'UTC' } })),
    post: vi.fn(),
    put: vi.fn()
  })
}))

vi.mock('~/composables/useWebPush', () => ({
  useWebPush: () => ({
    isPushSupported: ref(true),
    isSubscribed: mockIsSubscribed,
    isLoading: ref(false),
    checkSubscriptionStatus: mockCheckSubscriptionStatus,
    subscribeUser: mockSubscribeUser,
    unsubscribeUser: mockUnsubscribeUser,
    sendTestPush: mockSendTestPush
  })
}))

describe('SettingsPage handleSendTestPush', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    const toast = useToast()
    toast.clear()
    vi.clearAllMocks()
    mockIsSubscribed.value = true
  })

  it('shows success toast when test push succeeds with sent > 0', async () => {
    mockSendTestPush.mockResolvedValueOnce({ success: true, sent: 1, total: 1, stalePurged: 0 })

    const wrapper = mount(SettingsPage, {
      global: {
        stubs: {
          ThemeToggle: true,
          LocaleSelector: true
        }
      }
    })
    const notifTab = wrapper.findAll('button').find(b => b.text().includes('settings.tab_notifications'))
    expect(notifTab).toBeDefined()
    await notifTab?.trigger('click')
    await flushPromises()

    const testBtn = wrapper.findAll('button').find(b => b.text().includes('settings.web_push_test_btn'))
    expect(testBtn).toBeDefined()
    await testBtn!.trigger('click')
    await flushPromises()

    const toast = useToast()
    const lastToast = toast.toasts.value[toast.toasts.value.length - 1]
    expect(lastToast).toBeDefined()
    expect(lastToast?.type).toBe('success')
    expect(lastToast?.message).toBe('settings.web_push_test_success')
  })

  it('shows warning toast when test push succeeds with sent === 0', async () => {
    mockSendTestPush.mockResolvedValueOnce({ success: true, sent: 0, total: 1, stalePurged: 0 })

    const wrapper = mount(SettingsPage, {
      global: {
        stubs: {
          ThemeToggle: true,
          LocaleSelector: true
        }
      }
    })
    const notifTab = wrapper.findAll('button').find(b => b.text().includes('settings.tab_notifications'))
    expect(notifTab).toBeDefined()
    await notifTab?.trigger('click')
    await flushPromises()

    const testBtn = wrapper.findAll('button').find(b => b.text().includes('settings.web_push_test_btn'))
    await testBtn!.trigger('click')
    await flushPromises()

    const toast = useToast()
    const lastToast = toast.toasts.value[toast.toasts.value.length - 1]
    expect(lastToast).toBeDefined()
    expect(lastToast?.type).toBe('warning')
    expect(lastToast?.message).toBe('settings.web_push_test_zero_sent')
  })

  it('handles PUSH_SUBSCRIPTION_EXPIRED ApiError by setting isSubscribed false and 8000ms error toast', async () => {
    const error = new ApiError(
      'Push notification subscription has expired.',
      400,
      'PUSH_SUBSCRIPTION_EXPIRED',
      null,
      { code: 'PUSH_SUBSCRIPTION_EXPIRED' }
    )
    mockSendTestPush.mockRejectedValueOnce(error)

    const wrapper = mount(SettingsPage, {
      global: {
        stubs: {
          ThemeToggle: true,
          LocaleSelector: true
        }
      }
    })
    const notifTab = wrapper.findAll('button').find(b => b.text().includes('settings.tab_notifications'))
    expect(notifTab).toBeDefined()
    await notifTab?.trigger('click')
    await flushPromises()

    const testBtn = wrapper.findAll('button').find(b => b.text().includes('settings.web_push_test_btn'))
    await testBtn!.trigger('click')
    await flushPromises()

    expect(mockIsSubscribed.value).toBe(false)
    const toast = useToast()
    const lastToast = toast.toasts.value[toast.toasts.value.length - 1]
    expect(lastToast).toBeDefined()
    expect(lastToast?.type).toBe('error')
    expect(lastToast?.message).toBe('settings.web_push_test_expired')
    expect(lastToast?.duration).toBe(8000)
  })

  it('handles other errors using formatError and fallback key', async () => {
    const error = new ApiError(
      'Failed to deliver test notification.',
      400,
      'PUSH_DELIVERY_FAILED',
      null,
      { code: 'PUSH_DELIVERY_FAILED' }
    )
    mockSendTestPush.mockRejectedValueOnce(error)

    const wrapper = mount(SettingsPage, {
      global: {
        stubs: {
          ThemeToggle: true,
          LocaleSelector: true
        }
      }
    })
    const notifTab = wrapper.findAll('button').find(b => b.text().includes('settings.tab_notifications'))
    expect(notifTab).toBeDefined()
    await notifTab?.trigger('click')
    await flushPromises()

    const testBtn = wrapper.findAll('button').find(b => b.text().includes('settings.web_push_test_btn'))
    await testBtn!.trigger('click')
    await flushPromises()

    const toast = useToast()
    const lastToast = toast.toasts.value[toast.toasts.value.length - 1]
    expect(lastToast).toBeDefined()
    expect(lastToast?.type).toBe('error')
    expect(lastToast?.message).toBe('api_errors.PUSH_DELIVERY_FAILED')
  })
})

describe('SettingsPage Timezone Auto-Persist', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    const toast = useToast()
    toast.toasts.value = []
  })

  it('automatically persists timezone to profileStore on selection change', async () => {
    const wrapper = mount(SettingsPage, {
      global: {
        stubs: {
          ThemeToggle: true,
          LocaleSelector: true,
          AppSelect: true
        }
      }
    })
    await flushPromises()

    const profileStore = useProfileStore()
    const updateSpy = vi.spyOn(profileStore, 'updateProfile').mockResolvedValueOnce({
      id: 'user-1',
      email: 'test@example.com',
      name: 'Test User',
      preferredLocale: 'en',
      targetRole: 'Fullstack',
      dailyGoalMinutes: 30,
      timeZone: 'Asia/Tokyo'
    })
    // Timezone selector is directly available on the General tab
    await flushPromises()

    const appSelects = wrapper.findAllComponents(AppSelect)
    const tzSelect = appSelects.find(c => {
      const opts = c.props('options') as Array<{ value: string }> | undefined
      return opts?.some(o => o.value === 'UTC' || o.value === 'Asia/Ho_Chi_Minh')
    }) ?? appSelects[2]
    expect(tzSelect).toBeDefined()

    await tzSelect!.vm.$emit('update:modelValue', 'Asia/Tokyo')
    await flushPromises()

    expect(updateSpy).toHaveBeenCalledWith({ timeZone: 'Asia/Tokyo' })
    const toast = useToast()
    const lastToast = toast.toasts.value[toast.toasts.value.length - 1]
    expect(lastToast).toBeDefined()
    expect(lastToast?.type).toBe('success')
  })
})

describe('SettingsPage Unified Navigation & Account Tabs', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    const toast = useToast()
    toast.toasts.value = []
  })

  it('renders consolidated navigation rail tabs', async () => {
    const wrapper = mount(SettingsPage, {
      global: {
        stubs: {
          ThemeToggle: true,
          LocaleSelector: true,
          AppSelect: true,
          AppTimePicker: true,
        }
      }
    })
    await flushPromises()

    const buttons = wrapper.findAll('button')
    expect(buttons.some(b => b.text().includes('settings.tab_general'))).toBe(true)
    expect(buttons.some(b => b.text().includes('settings.tab_notifications'))).toBe(true)
    expect(buttons.some(b => b.text().includes('settings.tab_security'))).toBe(true)
  })

  it('submits profile updates on profile tab', async () => {
    const wrapper = mount(SettingsPage, {
      global: {
        stubs: {
          ThemeToggle: true,
          LocaleSelector: true,
          AppSelect: true,
          AppTimePicker: true,
        }
      }
    })
    await flushPromises()

    const profileStore = useProfileStore()
    const updateSpy = vi.spyOn(profileStore, 'updateProfile').mockResolvedValueOnce({
      id: 'usr-1',
      email: 'test@techdaily.dev',
      name: 'Alex Principal',
      targetRole: 'Principal Architect',
      dailyGoalMinutes: 15,
      preferredLocale: 'en'
    })

    // Profile tab is active by default
    const nameInput = wrapper.find('input[type="text"]')
    expect(nameInput.exists()).toBe(true)
    await nameInput.setValue('Alex Principal')

    const form = wrapper.find('form')
    await form.trigger('submit.prevent')
    await flushPromises()

    expect(updateSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Alex Principal'
      })
    )
    const toast = useToast()
    const lastToast = toast.toasts.value[toast.toasts.value.length - 1]
    expect(lastToast?.type).toBe('success')
  })

  it('submits password change on security tab', async () => {
    const wrapper = mount(SettingsPage, {
      global: {
        stubs: {
          ThemeToggle: true,
          LocaleSelector: true,
          AppSelect: true,
          AppTimePicker: true,
        }
      }
    })
    await flushPromises()

    const profileStore = useProfileStore()
    if (profileStore.profile) {
      profileStore.profile.hasPassword = true
    }

    const secTab = wrapper.findAll('button').find(b => b.text().includes('settings.tab_security'))
    await secTab?.trigger('click')
    await flushPromises()

    const changePwdSpy = vi.spyOn(profileStore, 'changePassword').mockResolvedValueOnce()

    const pwdInputs = wrapper.findAll('input[type="password"]')
    expect(pwdInputs.length).toBeGreaterThanOrEqual(2)

    // Enter current, new, and confirm passwords
    await pwdInputs[0]?.setValue('OldSecret123!')
    await pwdInputs[1]?.setValue('NewSecret123!')
    if (pwdInputs[2]) {
      await pwdInputs[2].setValue('NewSecret123!')
    }

    const form = wrapper.find('form')
    await form.trigger('submit.prevent')
    await flushPromises()

    expect(changePwdSpy).toHaveBeenCalled()
  })
})
