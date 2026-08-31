import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface ReviewCard {
  id: string
  topicId: string
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

export const useReviewStore = defineStore('review', () => {
  const cards = ref<ReviewCard[]>([])
  const totalCardsDue = ref(0)
  const currentCardIndex = ref(0)
  const isLoading = ref(false)
  const isGrading = ref(false)
  const error = ref<string | null>(null)

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
    } catch (err: any) {
      error.value = err.message || 'Failed to fetch review cards.'
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
    } catch (err: any) {
      error.value = err.message || 'Failed to grade card.'
      throw err
    } finally {
      isGrading.value = false
    }
  }

  return {
    cards,
    totalCardsDue,
    currentCardIndex,
    isLoading,
    isGrading,
    error,
    fetchReviewDeck,
    gradeCard
  }
})
