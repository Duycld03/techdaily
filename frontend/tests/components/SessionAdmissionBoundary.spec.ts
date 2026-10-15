import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { createSSRApp, defineComponent, h, ref } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import App from '~/app.vue'
import SessionAdmissionBoundary from '~/components/auth/SessionAdmissionBoundary.vue'
import { useClientAdmission } from '~/composables/useClientAdmission'
import { useAuthStore } from '~/stores/useAuthStore'
import middleware from '~/middleware/auth.global'
import { adminTestJwt, clearAuthStorage } from '../helpers/admin'

describe('Client session admission privacy', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    useClientAdmission().state.value = 'pending'
    vi.clearAllMocks()
    vi.stubGlobal('useRouter', () => ({ currentRoute: ref({ path: '/library', fullPath: '/library' }) }))
  })
  afterEach(() => { useClientAdmission().state.value = 'ready'; vi.unstubAllGlobals() })

  it('does not set up private learner/admin pages or navigation in SSR while admission is unresolved', async () => {
    const setupPrivate = vi.fn()
    const privatePage = defineComponent({ setup() { setupPrivate(); return () => h('div', 'PRIVATE_PAGE') } })
    const app = createSSRApp(App)
    app.use(createPinia())
    app.component('NuxtPage', privatePage)
    app.config.globalProperties.$t = (key: string) => key
    const html = await renderToString(app)
    expect(html).toContain('data-session-boundary')
    expect(html).not.toContain('PRIVATE_PAGE')
    expect(html).not.toContain('<header')
    expect(html).not.toContain('<aside')
    expect(setupPrivate).not.toHaveBeenCalled()
  })

  it('defers route checks and refresh while SSR/browser identity resolution is pending', async () => {
    const refresh = vi.spyOn(useAuthStore(), 'tryRefreshToken')
    await middleware({ path: '/library', fullPath: '/library' } as never)
    expect(refresh).not.toHaveBeenCalled()
    expect(navigateTo).not.toHaveBeenCalled()
  })

  it('finishes client admission only after a canonical session is ready', async () => {
    await useAuthStore().setSession(adminTestJwt('learner'), { id: 'learner', email: 'learner@example.test', name: 'Learner', preferredLocale: 'en' })
    const wrapper = mount(SessionAdmissionBoundary)
    await flushPromises()
    expect(useClientAdmission().state.value).toBe('ready')
    expect(navigateTo).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('keeps a neutral retry state on transient failure and redirects terminal rejection safely', async () => {
    const auth = useAuthStore()
    vi.spyOn(auth, 'tryRefreshToken').mockImplementation(async () => { auth.lastRefreshError = 503; return false })
    const wrapper = mount(SessionAdmissionBoundary)
    await flushPromises()
    expect(useClientAdmission().state.value).toBe('unavailable')
    expect(wrapper.find('button').exists()).toBe(true)
    expect(navigateTo).not.toHaveBeenCalled()
    vi.mocked(auth.tryRefreshToken).mockImplementation(async () => { auth.lastRefreshError = 401; return false })
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(navigateTo).toHaveBeenCalledWith({ path: '/login', query: { redirect: '/library' } })
    wrapper.unmount()
  })
})
