import { ref } from 'vue'
import { vi } from 'vitest'
import { config } from '@vue/test-utils'
import { useApiError } from '~/composables/useApiError'
import { useToast } from '~/composables/useToast'
config.global.mocks = {
  ...config.global.mocks,
  $t: (key: string, params?: Record<string, unknown>) => {
    if (params) {
      let result = key
      for (const [k, v] of Object.entries(params)) {
        result = result.replace(new RegExp(`{${k}}`, 'g'), String(v))
      }
      return result
    }
    return key
  }
}
// Global Nuxt mock composables for Vitest
;(globalThis as any).useApiError = useApiError
;(globalThis as any).useToast = useToast
;(globalThis as any).useRuntimeConfig = () => ({
  public: {
    apiBaseUrl: 'http://localhost:5000',
    googleClientId: 'mock-google-client-id'
  }
})

;(globalThis as any).useRoute = () => ({
  path: '/today',
  params: {},
  query: {}
})

;(globalThis as any).useRouter = () => ({
  push: vi.fn(),
  replace: vi.fn()
})

;(globalThis as any).useI18n = () => ({
  locale: { value: 'en' },
  t: (key: string, params?: Record<string, any>) => {
    if (params) {
      let result = key
      for (const [k, v] of Object.entries(params)) {
        result = result.replace(new RegExp(`{${k}}`, 'g'), String(v))
      }
      return result
    }
    return key
  },
  te: (key: string) => true
})

;(globalThis as any).useColorMode = () => ({
  value: 'dark',
  preference: 'dark'
})

;(globalThis as any).navigateTo = vi.fn()
Reflect.set(globalThis, 'clearError', vi.fn())


const mockCookies = new Map<string, any>()
;(globalThis as any).useCookie = (name: string) => {
  if (!mockCookies.has(name)) {
    mockCookies.set(name, { value: null })
  }
  return mockCookies.get(name)
}
const mockStates = new Map<string, unknown>()
Reflect.set(globalThis, 'useState', (key: string, init?: () => unknown) => {
  if (!mockStates.has(key)) {
    mockStates.set(key, ref(init ? init() : undefined))
  }
  return mockStates.get(key)
})
