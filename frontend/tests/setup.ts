import { vi } from 'vitest'

// Global Nuxt mock composables for Vitest
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
  t: (key: string) => key
})

;(globalThis as any).useColorMode = () => ({
  value: 'dark',
  preference: 'dark'
})

;(globalThis as any).navigateTo = vi.fn()

const mockCookies = new Map<string, any>()
;(globalThis as any).useCookie = (name: string) => {
  if (!mockCookies.has(name)) {
    mockCookies.set(name, { value: null })
  }
  return mockCookies.get(name)
}
