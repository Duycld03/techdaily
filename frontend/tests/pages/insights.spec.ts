import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import InsightsPage from '~/pages/insights.vue'
vi.mock('~/stores/useAuthStore', () => ({
  useAuthStore: () => ({
    isAuthenticated: true,
    isLoggedIn: true,
    init: vi.fn(),
    user: { id: 'usr-1', email: 'dev@deeppace.test', name: 'Dev', preferredLocale: 'en' }
  })
}))
import { useInsightsStore } from '~/stores/useInsightsStore'
import { useCategoryStore } from '~/stores/useCategoryStore'

const category = { id: '97e10b46-4a79-43f4-8b29-91c85582f899', slug: 'new-subject', nameEn: 'New subject', nameVi: 'Chủ đề mới', sortOrder: 8, isActive: true, version: 1 }
const inactive = { id: 'bb03c028-488c-472c-b1cf-0d8354879109', slug: 'retained', nameEn: 'Retained subject', nameVi: 'Chủ đề lưu giữ', sortOrder: 9, isActive: false, version: 2 }

const mockInsights = [
  {
    id: 'ins-1',
    slug: 'dotnet-span-split',
    title: 'Span Split Optimization',
    categoryId: category.id,
    category,
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
    categoryId: inactive.id,
    category: inactive,
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
let bookmarkedFeedMock = [mockInsights[1]!]

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url === '/api/v1/categories') return [category, inactive]
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
          categories: [{ categoryId: category.id, category, count: 5 }, { categoryId: inactive.id, category: inactive, count: 8 }],
          suggestedTopics: { [category.id]: ['Source-derived topic'], [inactive.id]: [] }
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
    bookmarkedFeedMock = [mockInsights[1]!]
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

    const backendBtn = wrapper.findAll('button').find((b) => b.text().includes('New subject'))
    expect(backendBtn).toBeDefined()
    await backendBtn!.trigger('click')
    await flushPromises()

    // In explore mode, onlySaved is false
    expect(fetchFeedSpy).toHaveBeenCalledWith(category.id, null, false)

    // Switch to saved mode
    const savedBtn = wrapper.findAll('button').find((b) => b.text().includes('insights.view_saved'))
    await savedBtn!.trigger('click')
    await flushPromises()

    // Click category again in saved mode
    await backendBtn!.trigger('click')
    await flushPromises()

    // In saved mode, onlySaved is true
    expect(fetchFeedSpy).toHaveBeenCalledWith(category.id, null, true)
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
        ...mockInsights[0]!,
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

  it('loads shared metadata on direct entry and keeps inactive history filterable', async () => {
    const wrapper = mount(InsightsPage)
    await flushPromises()
    const retained = wrapper.findAll('button').find(button => button.text().includes(inactive.nameEn))
    expect(retained).toBeDefined()
    await retained!.trigger('click')
    expect(useInsightsStore().selectedCategoryId).toBe(inactive.id)
    expect(retained!.attributes('aria-pressed')).toBe('true')
    expect(wrapper.text()).toContain('categories.inactive')
  })

  it('requires an explicit active generation choice and never invents topic suggestions', async () => {
    const wrapper = mount(InsightsPage, { global: { stubs: { Teleport: true } } })
    await flushPromises()
    const open = wrapper.findAll('button').find(button => button.text().includes('insights.generate_ai'))
    await open!.trigger('click')
    const submit = wrapper.find('[data-testid="generate-insight-submit"]')
    expect(submit.attributes('disabled')).toBeDefined()
    const select = wrapper.find('[data-testid="insight-category-select"]')
    await select.find('[role="combobox"]').trigger('click')
    expect(select.find(`[data-testid="app-select-option-${inactive.id}"]`).exists()).toBe(false)
    await select.find(`[data-testid="app-select-option-${category.id}"]`).trigger('click')
    await wrapper.find('input[placeholder="insights.generate_topic_placeholder"]').setValue('Source-derived topic')
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[data-testid="generate-insight-submit"]').attributes('disabled')).toBeUndefined()
  })

  it('preserves a failed generation draft and blocks a category deactivated after selection', async () => {
    const wrapper = mount(InsightsPage, { global: { stubs: { Teleport: true } } })
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text().includes('insights.generate_ai'))!.trigger('click')
    const select = wrapper.find('[data-testid="insight-category-select"]')
    await select.find('[role="combobox"]').trigger('click')
    await select.find(`[data-testid="app-select-option-${category.id}"]`).trigger('click')
    const input = wrapper.find('input[placeholder="insights.generate_topic_placeholder"]')
    await input.setValue('Retain my source topic')
    const metadata = useCategoryStore()
    metadata.categories = [{ ...category, isActive: false }, inactive]
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[data-testid="generate-insight-submit"]').attributes('disabled')).toBeDefined()
    expect((input.element as HTMLInputElement).value).toBe('Retain my source topic')
  })
})
