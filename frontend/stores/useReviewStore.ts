import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

export interface ReviewCard {
  id: string
  topicId?: string
  sourceType?: number
  frontMarkdown?: string
  backMarkdown?: string
  sourceHighlightId?: string
  sourceQuizQuestionId?: string
  topicTitle: string
  category: number
  difficulty: number
  topicSummary: string
  topicDeepDiveMarkdown: string
  repetitionCount: number
  easeFactor: number
  intervalDays: number
  nextReviewDate: string
  status: number
}

export interface DeckStatistics {
  totalCards: number
  learningCount: number
  reviewingCount: number
  masteredCount: number
}

export interface ReviewFilterState {
  status: number | null
  sourceType: number | null
  urgency: string | null
  sortBy: string | null
}

export interface ForecastDayBin {
  date: string
  dayLabel: string
  count: number
  isToday: boolean
}

export const useReviewStore = defineStore('review', () => {
  const cards = ref<ReviewCard[]>([])
  const totalCardsDue = ref(0)
  const currentCardIndex = ref(0)
  const isLoading = ref(false)
  const isGrading = ref(false)
  const error = ref<string | null>(null)

  // Deck Management State
  const deckCards = ref<ReviewCard[]>([])
  const deckTotalCount = ref(0)
  const deckCurrentPage = ref(1)
  const deckPageSize = ref(20)
  const deckTotalPages = ref(0)
  const deckStatistics = ref<DeckStatistics>({
    totalCards: 0,
    learningCount: 0,
    reviewingCount: 0,
    masteredCount: 0
  })
  const isDeckLoading = ref(false)

  async function fetchReviewDeck(date?: string) {
    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const query = date ? `?date=${date}` : ''
      const res = await api.get<{ dueCards: ReviewCard[]; totalCardsDue: number }>(`/api/v1/review/deck${query}`)
      cards.value = res.dueCards
      totalCardsDue.value = res.totalCardsDue
      currentCardIndex.value = 0
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch review cards.'
    } finally {
      isLoading.value = false
    }
  }

  async function gradeCard(cardId: string, qualityGrade: number) {
    isGrading.value = true
    try {
      const api = useApiClient()
      await api.post(`/api/v1/review/cards/${cardId}/grade`, { qualityGrade })
      // Move to next card
      cards.value = cards.value.filter((c) => c.id !== cardId)
      totalCardsDue.value = cards.value.length
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to grade card.'
      throw err
    } finally {
      isGrading.value = false
    }
  }

  async function createCardFromHighlight(highlightId: string, locale = 'en') {
    const api = useApiClient()
    const res = await api.post<{ cardId: string; front: string; back: string }>(
      '/api/v1/review/cards/from-highlight',
      { highlightId, locale }
    )
    return res
  }

  async function createCardFromQuizMistake(questionId: string) {
    const api = useApiClient()
    const res = await api.post<{ cardId: string }>(
      '/api/v1/review/cards/from-quiz-mistake',
      { questionId }
    )
    return res
  }

  async function fetchDeckCards(params?: {
    search?: string
    status?: number
    sourceType?: number
    page?: number
    pageSize?: number
  }) {
    isDeckLoading.value = true
    try {
      const api = useApiClient()
      const queryParts: string[] = []
      if (params?.page) queryParts.push(`page=${params.page}`)
      if (params?.pageSize) queryParts.push(`pageSize=${params.pageSize}`)
      if (params?.search) queryParts.push(`search=${encodeURIComponent(params.search)}`)
      if (params?.status !== undefined && params?.status !== null) queryParts.push(`status=${params.status}`)
      if (params?.sourceType !== undefined && params?.sourceType !== null) queryParts.push(`sourceType=${params.sourceType}`)
      const query = queryParts.length > 0 ? `?${queryParts.join('&')}` : ''

      const res = await api.get<{
        cards: ReviewCard[]
        totalCount: number
        page: number
        pageSize: number
        totalPages?: number
        statistics?: {
          totalCards: number
          learningCards?: number
          learningCount?: number
          reviewingCards?: number
          reviewingCount?: number
          masteredCards?: number
          masteredCount?: number
        }
      }>(`/api/v1/review/cards${query}`)

      deckCards.value = res.cards || []
      deckTotalCount.value = res.totalCount ?? 0
      deckCurrentPage.value = res.page ?? (params?.page || 1)
      deckPageSize.value = res.pageSize ?? (params?.pageSize || 20)
      deckTotalPages.value =
        res.totalPages ??
        (deckTotalCount.value > 0 ? Math.ceil(deckTotalCount.value / deckPageSize.value) : 0)
      if (res.statistics) {
        deckStatistics.value = {
          totalCards: res.statistics.totalCards ?? 0,
          learningCount: res.statistics.learningCount ?? res.statistics.learningCards ?? 0,
          reviewingCount: res.statistics.reviewingCount ?? res.statistics.reviewingCards ?? 0,
          masteredCount: res.statistics.masteredCount ?? res.statistics.masteredCards ?? 0
        }
      }
      return res
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch deck cards.'
      throw err
    } finally {
      isDeckLoading.value = false
    }
  }

  async function updateCard(cardId: string, payload: { frontMarkdown: string; backMarkdown: string }) {
    const api = useApiClient()
    const res = await api.put<{ card: ReviewCard }>(`/api/v1/review/cards/${cardId}`, payload)
    const updated = res.card || (res as unknown as ReviewCard)
    const idx = deckCards.value.findIndex((c) => c.id === cardId)
    if (idx !== -1) {
      deckCards.value[idx] = { ...deckCards.value[idx], ...updated }
    }
    return updated
  }

  async function deleteCard(cardId: string) {
    const api = useApiClient()
    await api.delete(`/api/v1/review/cards/${cardId}`)
    deckCards.value = deckCards.value.filter((c) => c.id !== cardId)
    deckTotalCount.value = Math.max(0, deckTotalCount.value - 1)
  }

  async function resetCardProgress(cardId: string) {
    const api = useApiClient()
    const res = await api.post<{ card: ReviewCard }>(`/api/v1/review/cards/${cardId}/reset`)
    const updated = res.card || (res as unknown as ReviewCard)
    const idx = deckCards.value.findIndex((c) => c.id === cardId)
    if (idx !== -1) {
      deckCards.value[idx] = { ...deckCards.value[idx], ...updated }
    }
    return updated
  }

  return {
    cards,
    totalCardsDue,
    currentCardIndex,
    isLoading,
    isGrading,
    error,
    deckCards,
    deckTotalCount,
    deckCurrentPage,
    deckPageSize,
    deckTotalPages,
    deckStatistics,
    isDeckLoading,
    fetchReviewDeck,
    gradeCard,
    createCardFromHighlight,
    createCardFromQuizMistake,
    fetchDeckCards,
    updateCard,
    deleteCard,
    resetCardProgress
  }
})
