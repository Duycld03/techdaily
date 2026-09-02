import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useInsightsStore } from '~/stores/useInsightsStore'

vi.mock('~/stores/useAuthStore', () => ({
  useAuthStore: () => ({
    isAuthenticated: true,
    user: { id: 'usr-1', email: 'test@techdaily.io' }
  })
}))

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/api/v1/insights/feed')) {
        return {
          insights: [
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
              bookmarksCount: 2
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
              bookmarksCount: 8
            }
          ],
          totalCount: 2,
          page: 1,
          pageSize: 50,
          hasMore: false
        }
      }
      throw new Error('Unknown endpoint')
    }),
    post: vi.fn(async (url: string, body: any) => {
      if (url.includes('/api/v1/insights/generate')) {
        return {
          id: 'ins-ai',
          slug: 'ai-generated-insight',
          title: 'AI Generated Architecture Pattern',
          category: 3,
          tags: ['architecture', 'ai'],
          summaryMarkdown: 'AI Summary',
          problemSnippet: 'AI Problem',
          solutionSnippet: 'AI Solution',
          underTheHoodMarkdown: 'AI Under The Hood',
          benchmarkStats: '⚡ 20x faster',
          likesCount: 0,
          bookmarksCount: 0
        }
      }
      if (url.includes('/bookmark')) {
        return {
          isBookmarked: true,
          totalBookmarks: 3
        }
      }
      throw new Error('Unknown endpoint')
    })
  })
}))

describe('useInsightsStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('fetches feed and navigates between insights next and prev', async () => {
    const store = useInsightsStore()
    await store.fetchFeed()

    expect(store.insights.length).toBe(2)
    expect(store.currentIndex).toBe(0)
    expect(store.currentInsight?.slug).toBe('dotnet-span-split')
    expect(store.hasNext).toBe(true)
    expect(store.hasPrev).toBe(false)

    store.nextInsight()
    expect(store.currentIndex).toBe(1)
    expect(store.currentInsight?.slug).toBe('postgres-hot-updates')
    expect(store.hasNext).toBe(false)
    expect(store.hasPrev).toBe(true)

    store.prevInsight()
    expect(store.currentIndex).toBe(0)
  })

  it('generates new insight with AI and prepends to feed', async () => {
    const store = useInsightsStore()
    await store.fetchFeed()

    const newInsight = await store.generateWithAi('Memory Optimization')
    expect(newInsight.slug).toBe('ai-generated-insight')
    expect(store.insights.length).toBe(3)
    expect(store.currentIndex).toBe(0)
    expect(store.currentInsight?.title).toBe('AI Generated Architecture Pattern')
  })

  it('bookmarks an insight', async () => {
    const store = useInsightsStore()
    await store.fetchFeed()

    await store.toggleBookmark('ins-1')
    expect(store.currentInsight?.bookmarksCount).toBe(3)
    expect(store.currentInsight?.isBookmarkedByUser).toBe(true)
  })
})
