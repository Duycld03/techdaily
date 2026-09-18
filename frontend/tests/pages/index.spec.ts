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
      topic: {
        id: 't-1',
        title: 'System Design Patterns',
        dayOrder: 1,
        category: 'Architecture',
        summary: 'Core patterns'
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
})
