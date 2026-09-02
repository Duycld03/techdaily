import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useApiClient } from '~/composables/useApiClient'
import { useAuthStore } from '~/stores/useAuthStore'

export interface TechInsight {
  id: string
  slug: string
  title: string
  category: number // 0=Frontend, 1=BackendDotNet, 2=DatabaseStorage, 3=SystemDesign
  tags: string[]
  summaryMarkdown: string
  problemSnippet: string
  solutionSnippet: string
  underTheHoodMarkdown: string
  benchmarkStats: string
  sourceUrl?: string | null
  likesCount: number
  bookmarksCount: number
  isBookmarkedByUser?: boolean
}

export interface InsightsFeedResponse {
  insights: TechInsight[]
  totalCount: number
  page: number
  pageSize: number
  hasMore: boolean
}

export const useInsightsStore = defineStore('insights', () => {
  const insights = ref<TechInsight[]>([])
  const bookmarkedInsights = ref<TechInsight[]>([])
  const currentIndex = ref(0)
  const selectedCategory = ref<number | null>(null)
  const selectedTag = ref<string | null>(null)
  const onlyBookmarked = ref(false)
  const isLoading = ref(false)
  const isGenerating = ref(false)
  const isLoadingBookmarks = ref(false)
  const totalCount = ref(0)
  const error = ref<string | null>(null)

  const currentInsight = computed<TechInsight | null>(() => {
    if (insights.value.length === 0) return null
    return insights.value[currentIndex.value] || null
  })

  const hasNext = computed(() => currentIndex.value < insights.value.length - 1)
  const hasPrev = computed(() => currentIndex.value > 0)

  async function fetchFeed(category?: number | null, tag?: string | null, bookmarked?: boolean) {
    if (category !== undefined) selectedCategory.value = category
    if (tag !== undefined) selectedTag.value = tag
    if (bookmarked !== undefined) onlyBookmarked.value = bookmarked

    isLoading.value = true
    error.value = null

    try {
      const api = useApiClient()
      const params = new URLSearchParams()
      if (selectedCategory.value !== null) {
        params.append('category', selectedCategory.value.toString())
      }
      if (selectedTag.value) {
        params.append('tag', selectedTag.value)
      }
      if (onlyBookmarked.value) {
        params.append('onlyBookmarked', 'true')
      }
      params.append('page', '1')
      params.append('pageSize', '50')

      const url = `/api/v1/insights/feed?${params.toString()}`
      const response = await api.get<InsightsFeedResponse>(url)

      insights.value = response.insights || []
      totalCount.value = response.totalCount || 0
      currentIndex.value = 0
    } catch (err: any) {
      error.value = err?.message || 'Failed to fetch insights feed'
      insights.value = []
    } finally {
      isLoading.value = false
    }
  }

  async function fetchBookmarkedInsights() {
    isLoadingBookmarks.value = true
    try {
      const api = useApiClient()
      const response = await api.get<InsightsFeedResponse>('/api/v1/insights/feed?onlyBookmarked=true&pageSize=50')
      bookmarkedInsights.value = response.insights || []
      return bookmarkedInsights.value
    } catch (err) {
      bookmarkedInsights.value = []
      return []
    } finally {
      isLoadingBookmarks.value = false
    }
  }

  function nextInsight() {
    if (hasNext.value) {
      currentIndex.value++
    }
  }

  function prevInsight() {
    if (hasPrev.value) {
      currentIndex.value--
    }
  }

  function shuffle() {
    if (insights.value.length <= 1) return
    const array = [...insights.value]
    for (let i = array.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [array[i], array[j]] = [array[j], array[i]]
    }
    insights.value = array
    currentIndex.value = 0
  }

  async function generateWithAi(preferredTopic?: string, locale: string = 'en') {
    isGenerating.value = true
    error.value = null

    try {
      const api = useApiClient()
      const response = await api.post<TechInsight>('/api/v1/insights/generate', {
        preferredCategory: selectedCategory.value,
        preferredTopic: preferredTopic || null,
        locale
      })

      if (response && response.id) {
        insights.value.unshift(response)
        currentIndex.value = 0
        totalCount.value++
        return response
      }
    } catch (err: any) {
      error.value = err?.message || 'Failed to generate insight with AI'
      throw err
    } finally {
      isGenerating.value = false
    }
  }

  async function toggleBookmark(insightId: string) {
    const authStore = useAuthStore()
    if (!authStore.isAuthenticated) {
      throw new Error('UNAUTHENTICATED')
    }

    try {
      const api = useApiClient()
      const response = await api.post<{ isBookmarked: boolean; totalBookmarks: number }>(
        `/api/v1/insights/${insightId}/bookmark`
      )

      if (response) {
        const target = insights.value.find(i => i.id === insightId)
        if (target) {
          target.bookmarksCount = response.totalBookmarks
          target.isBookmarkedByUser = response.isBookmarked
        }

        // Also update bookmarked list
        if (!response.isBookmarked) {
          bookmarkedInsights.value = bookmarkedInsights.value.filter(i => i.id !== insightId)
          if (onlyBookmarked.value) {
            insights.value = insights.value.filter(i => i.id !== insightId)
            if (currentIndex.value >= insights.value.length) {
              currentIndex.value = Math.max(0, insights.value.length - 1)
            }
          }
        } else if (target && !bookmarkedInsights.value.some(i => i.id === insightId)) {
          bookmarkedInsights.value.unshift({ ...target, isBookmarkedByUser: true })
        }
      }
      return response
    } catch (err: any) {
      throw err
    }
  }

  return {
    insights,
    bookmarkedInsights,
    currentIndex,
    currentInsight,
    selectedCategory,
    selectedTag,
    onlyBookmarked,
    isLoading,
    isGenerating,
    isLoadingBookmarks,
    totalCount,
    error,
    hasNext,
    hasPrev,
    fetchFeed,
    fetchBookmarkedInsights,
    nextInsight,
    prevInsight,
    shuffle,
    generateWithAi,
    toggleBookmark
  }
})
