import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import InsightsPage from '~/pages/insights.vue'
vi.mock('~/stores/useAuthStore', () => ({
  useAuthStore: () => ({
    isAuthenticated: true,
    isLoggedIn: true,
    user: { id: 'usr-1', email: 'dev@techdaily.io', name: 'Dev', preferredLocale: 'en' }
  })
}))
import { useInsightsStore } from '~/stores/useInsightsStore'

const mockInsights = [
  {
    id: 'ins-1',
    slug: 'dotnet-span-split',
    title: 'Span Split Optimization',
    category: 1,
    tags: ['csharp', 'span'],
    summaryMarkdown: 'Summary 1',
    problemSnippet: 'Problem 1',
    solutionSnippet: 'Solution 1',
    underTheHoodMarkdown: 'Under The Hood 1',
    benchmarkStats: '⚡ 10x faster',
    likesCount: 5,
    bookmarksCount: 2,
    isBookmarkedByUser: false
  },
  {
    id: 'ins-2',
    slug: 'postgres-hot-updates',
    title: 'Postgres HOT Updates',
    category: 2,
    tags: ['postgres', 'mvcc'],
    summaryMarkdown: 'Summary 2',
    problemSnippet: 'Problem 2',
    solutionSnippet: 'Solution 2',
    underTheHoodMarkdown: 'Under The Hood 2',
    benchmarkStats: '⚡ 5x faster',
    likesCount: 12,
    bookmarksCount: 8,
    isBookmarkedByUser: true
  }
]

let feedMock = [...mockInsights]
let bookmarkedFeedMock = [mockInsights[1]]

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/api/v1/insights/feed')) {
        if (url.includes('onlyBookmarked=true')) {
          return {
            insights: [...bookmarkedFeedMock],
            totalCount: bookmarkedFeedMock.length,
            page: 1,
            pageSize: 50,
            hasMore: false
          }
        }
        return {
          insights: [...feedMock],
          totalCount: feedMock.length,
          page: 1,
          pageSize: 50,
          hasMore: false
        }
      }
      if (url.includes('/api/v1/insights/meta')) {
        return {
          categories: [
            { id: 0, key: 'FrontendWeb', labelEn: 'Frontend & Vue', labelVi: 'Frontend & Vue', count: 5 },
            { id: 1, key: 'BackendRuntime', labelEn: 'Backend & Runtime', labelVi: 'Hệ Thống Backend & Runtime', count: 12 },
            { id: 2, key: 'DatabaseStorage', labelEn: 'Database & Storage', labelVi: 'Cơ Sở Dữ Liệu & Lưu Trữ', count: 8 }
          ],
          suggestedTopics: {
            0: ['Vue Reactivity', 'Vite SSR'],
            1: ['Span<T>', 'Kestrel Sockets']
          }
        }
      }
      throw new Error(`Unexpected url: ${url}`)
    }),
    post: vi.fn(async (url: string) => {
      if (url.includes('/bookmark')) {
        return { isBookmarked: true, totalBookmarks: 3 }
      }
      throw new Error(`Unexpected url: ${url}`)
    })
  })
}))

describe('insights.vue (Dedicated View Mode Switcher & Category Filtering)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    feedMock = [...mockInsights]
    bookmarkedFeedMock = [mockInsights[1]]
  })

  it('renders View Mode Switcher with Explore and Saved (N) buttons', async () => {
    const wrapper = mount(InsightsPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true
        }
      }
    })
    await flushPromises()

    expect(wrapper.text()).toContain('insights.view_explore')
    const savedBtn = wrapper.findAll('button').find((b) => b.text().includes('insights.view_saved'))
    expect(savedBtn).toBeDefined()
    const insightsStore = useInsightsStore()
    expect(insightsStore.bookmarkedInsights.length).toBe(1)
  })

  it('toggles between Explore and Saved view modes', async () => {
    const wrapper = mount(InsightsPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true
        }
      }
    })
    await flushPromises()

    const insightsStore = useInsightsStore()
    const savedBtn = wrapper.findAll('button').find((b) => b.text().includes('insights.view_saved'))
    await savedBtn!.trigger('click')
    await flushPromises()

    expect(insightsStore.onlyBookmarked).toBe(true)

    const exploreBtn = wrapper.findAll('button').find((b) => b.text().includes('insights.view_explore'))
    await exploreBtn!.trigger('click')
    await flushPromises()

    expect(insightsStore.onlyBookmarked).toBe(false)
  })

  it('filters independently by category within active view mode', async () => {
    const wrapper = mount(InsightsPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true
        }
      }
    })
    await flushPromises()

    const insightsStore = useInsightsStore()
    const fetchFeedSpy = vi.spyOn(insightsStore, 'fetchFeed')

    // Find category button for Backend & Runtime
    const backendBtn = wrapper.findAll('button').find((b) => b.text().includes('Backend & Runtime') || b.text().includes('insights.cat_backend'))
    expect(backendBtn).toBeDefined()
    await backendBtn!.trigger('click')
    await flushPromises()

    // In explore mode, onlySaved is false
    expect(fetchFeedSpy).toHaveBeenCalledWith(1, null, false)

    // Switch to saved mode
    const savedBtn = wrapper.findAll('button').find((b) => b.text().includes('insights.view_saved'))
    await savedBtn!.trigger('click')
    await flushPromises()

    // Click category again in saved mode
    await backendBtn!.trigger('click')
    await flushPromises()

    // In saved mode, onlySaved is true
    expect(fetchFeedSpy).toHaveBeenCalledWith(1, null, true)
  })

  it('renders tailored empty state when in Saved mode with zero bookmarks', async () => {
    bookmarkedFeedMock = []
    feedMock = []

    const wrapper = mount(InsightsPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true
        }
      }
    })
    await flushPromises()

    const savedBtn = wrapper.findAll('button').find((b) => b.text().includes('insights.view_saved'))
    await savedBtn!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('insights.saved_empty_title')
    expect(wrapper.text()).toContain('insights.saved_empty_desc')
    expect(wrapper.text()).toContain('insights.saved_empty_cta')

    // Click CTA to return to explore mode
    const ctaBtn = wrapper.findAll('button').find((b) => b.text().includes('insights.saved_empty_cta'))
    expect(ctaBtn).toBeDefined()
    await ctaBtn!.trigger('click')
    await flushPromises()

    const insightsStore = useInsightsStore()
    expect(insightsStore.onlyBookmarked).toBe(false)
  })

  it('parses multi-metric benchmarkStats, strips leading emojis, and renders individual chips', async () => {
    feedMock = [
      {
        ...mockInsights[0],
        benchmarkStats: '⚡ Latency: 4.2ms -> 0.3ms (14x faster) | 🔥 Heap Fetches: 15k -> 0'
      }
    ]

    const wrapper = mount(InsightsPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true
        }
      }
    })
    await flushPromises()

    const text = wrapper.text()
    expect(text).toContain('Latency: 4.2ms -> 0.3ms (14x faster)')
    expect(text).toContain('Heap Fetches: 15k -> 0')
    expect(text).not.toContain('⚡')
    expect(text).not.toContain('🔥')
  })
})
