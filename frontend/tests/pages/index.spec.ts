import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import IndexPage from '~/pages/index.vue'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'

describe('pages/index.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('fetches today focus data on mount if not already loaded', async () => {
    const focusStore = useDailyFocusStore()
    const fetchSpy = vi.spyOn(focusStore, 'fetchTodayFocus').mockResolvedValue({} as any)

    mount(IndexPage, {
      global: {
        stubs: {
          HomeBentoDashboard: {
            template: '<div class="bento-dashboard-stub">Dashboard Loaded</div>'
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(fetchSpy).toHaveBeenCalled()
  })

  it('renders HomeBentoDashboard component when data exists', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: {
        id: 'c-1',
        chapterTitle: 'System Design Patterns',
        chunkOrder: 1,
        summaryMarkdown: 'Core patterns'
      }
    } as any

    const wrapper = mount(IndexPage, {
      global: {
        stubs: {
          HomeBentoDashboard: {
            template: '<div class="bento-dashboard-stub">Dashboard Content</div>'
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()
    expect(wrapper.find('.bento-dashboard-stub').exists()).toBe(true)
    expect(wrapper.text()).toContain('Dashboard Content')
  })

  it('renders loading spinner when loading and data is null', () => {
    const focusStore = useDailyFocusStore()
    focusStore.isLoading = true
    focusStore.data = null

    const wrapper = mount(IndexPage, {
      global: {
        stubs: {
          HomeBentoDashboard: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.find('.animate-spin').exists()).toBe(true)
  })
  it('triggers background revalidation on mount when data is already cached without showing spinner', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: { id: 'c-1', chapterTitle: 'System Design Patterns' }
    } as any
    const fetchSpy = vi.spyOn(focusStore, 'fetchTodayFocus').mockResolvedValue({} as any)

    const wrapper = mount(IndexPage, {
      global: {
        stubs: {
          HomeBentoDashboard: {
            template: '<div class="bento-dashboard-stub">Dashboard Cached Content</div>'
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(fetchSpy).toHaveBeenCalled()
    expect(wrapper.find('.animate-spin').exists()).toBe(false)
    expect(wrapper.find('.bento-dashboard-stub').exists()).toBe(true)
  })

  it('revalidates focus data on popstate history event', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = { id: 'existing' } as any
    const fetchSpy = vi.spyOn(focusStore, 'fetchTodayFocus').mockResolvedValue({} as any)

    mount(IndexPage, {
      global: {
        stubs: {
          HomeBentoDashboard: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    fetchSpy.mockClear()
    window.dispatchEvent(new PopStateEvent('popstate'))
    await flushPromises()

    expect(fetchSpy).toHaveBeenCalled()
  })

  it('revalidates focus data on pageshow bfcache restoration', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = { id: 'existing' } as any
    const fetchSpy = vi.spyOn(focusStore, 'fetchTodayFocus').mockResolvedValue({} as any)

    mount(IndexPage, {
      global: {
        stubs: {
          HomeBentoDashboard: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    fetchSpy.mockClear()
    window.dispatchEvent(new PageTransitionEvent('pageshow', { persisted: true }))
    await flushPromises()

    expect(fetchSpy).toHaveBeenCalled()
  })
})
