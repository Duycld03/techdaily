import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useInsightsStore } from '~/stores/useInsightsStore'

const { category, inactive, get, post } = vi.hoisted(() => ({
  category: { id: '97e10b46-4a79-43f4-8b29-91c85582f899', slug: 'new-subject', nameEn: 'New subject', nameVi: 'Chủ đề mới', sortOrder: 8, isActive: true, version: 1 },
  inactive: { id: 'bb03c028-488c-472c-b1cf-0d8354879109', slug: 'retained', nameEn: 'Retained subject', nameVi: 'Chủ đề lưu giữ', sortOrder: 9, isActive: false, version: 2 },
  get: vi.fn(), post: vi.fn()
}))
vi.mock('~/stores/useAuthStore', () => ({ useAuthStore: () => ({ init: vi.fn(), isAuthenticated: true, user: { id: 'usr-1' } }) }))
vi.mock('~/composables/useApiClient', () => ({ useApiClient: () => ({ get, post }) }))

function insight(id: string, descriptor = category) {
  return { id, slug: id, title: id, categoryId: descriptor.id, category: descriptor, tags: ['source-tag'], summaryMarkdown: 'Summary', problemSnippet: 'Problem', solutionSnippet: 'Solution', underTheHoodMarkdown: 'Mechanism', benchmarkStats: '10x', likesCount: 0, bookmarksCount: 0 }
}

describe('useInsightsStore UUID categories', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    get.mockImplementation(async (url: string) => {
      if (url === '/api/v1/categories') return [category, inactive]
      if (url.includes('/meta')) return { categories: [{ categoryId: category.id, category, count: 2 }], suggestedTopics: { [category.id]: ['Source title'], [inactive.id]: [] } }
      return { insights: [insight('one'), insight('two', inactive)], totalCount: 2 }
    })
    post.mockImplementation(async (url: string) => url.includes('/bookmark') ? { isBookmarked: true, totalBookmarks: 3 } : insight('generated'))
  })

  it('fetches UUID-filtered feeds, clears only the category filter for All, and retains saved mode', async () => {
    const store = useInsightsStore()
    await store.fetchFeed(category.id, 'source-tag', true)
    const query = new URL('http://localhost' + get.mock.calls.at(-1)![0]).searchParams
    expect(query.get('categoryId')).toBe(category.id)
    expect(query.has('category')).toBe(false)
    expect(query.get('onlyBookmarked')).toBe('true')
    await store.fetchFeed(null)
    expect(store.selectedCategoryId).toBeNull()
    expect(store.selectedTag).toBe('source-tag')
    expect(store.onlyBookmarked).toBe(true)
    expect(get.mock.calls.at(-1)![0]).not.toContain('categoryId=')
  })

  it('navigates next and previous while retaining inactive historical descriptors', async () => {
    const store = useInsightsStore()
    await store.fetchFeed(inactive.id)
    expect(store.hasNext).toBe(true)
    expect(store.hasPrev).toBe(false)
    store.nextInsight()
    expect(store.currentInsight?.category).toEqual(inactive)
    expect(store.hasNext).toBe(false)
    store.prevInsight()
    expect(store.currentIndex).toBe(0)
  })

  it('rejects unselected or inactive standalone generation without invoking AI', async () => {
    const store = useInsightsStore()
    await expect(store.generateWithAi(null, 'Topic')).rejects.toThrow()
    await expect(store.generateWithAi(inactive.id, 'Topic')).rejects.toThrow()
    expect(post).not.toHaveBeenCalled()
  })

  it('generates with the explicitly selected UUID only and prepends the real response', async () => {
    const store = useInsightsStore()
    await store.fetchFeed()
    await store.generateWithAi(category.id, 'Source title', 'vi')
    expect(post).toHaveBeenCalledWith('/api/v1/insights/generate', { categoryId: category.id, preferredTopic: 'Source title', locale: 'vi' })
    expect(store.currentInsight?.id).toBe('generated')
    expect(store.insights).toHaveLength(3)
  })

  it('bookmarks an insight and updates the saved count', async () => {
    const store = useInsightsStore()
    await store.fetchFeed()
    await store.toggleBookmark('one')
    expect(store.currentInsight?.bookmarksCount).toBe(3)
    expect(store.bookmarkedInsights).toHaveLength(1)
  })

  it('loads descriptor/count metadata and UUID-keyed source topics without curriculum fallbacks', async () => {
    const store = useInsightsStore()
    await store.fetchMetadata()
    expect(store.categoryMetadata).toEqual([{ categoryId: category.id, category, count: 2 }])
    expect(store.suggestedTopics[category.id]).toEqual(['Source title'])
    expect(store.suggestedTopics[inactive.id]).toEqual([])
  })
})
