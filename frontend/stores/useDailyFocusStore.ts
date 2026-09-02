import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

export interface Topic {
  id: string
  slug: string
  title: string
  category: number
  difficulty: number
  dayOrder: number
  summary: string
  deepDiveMarkdown: string
  benchmarkSnippet?: string
}

export interface InterviewQuestion {
  id: string
  questionText: string
  options: string[]
  correctOptionIndex?: number
  explanationMarkdown?: string
  expectedKeyPoints: string[]
  modelAnswerMarkdown: string
  difficulty: number
}

export interface MicroQuiz {
  question: string
  options: string[]
  answerIndex: number
  explanation: string
}

export interface DocumentChunk {
  id: string
  chunkOrder: number
  chapterTitle: string
  originalTextMarkdown: string
  summaryMarkdown: string
  keyTakeaways: string[]
  microQuiz: MicroQuiz
  language: string
  estimatedReadMinutes: number
}

export interface DailyDrill {
  id: string
  scheduledDate: string
  status: number // 0=Pending, 1=Submitted, 2=Reviewed
  selectedOptionIndex?: number
  isCorrect?: boolean
  score?: number
  attemptCount: number
  submittedAt?: string
}

export interface TodayFocusResponse {
  topic: Topic
  question: InterviewQuestion
  documentChunk?: DocumentChunk
  drill: DailyDrill
  currentStreak: number
  longestStreak: number
  freezeCreditsRemaining: number
}

export const useDailyFocusStore = defineStore('dailyFocus', () => {
  const data = ref<TodayFocusResponse | null>(null)
  const isLoading = ref(false)
  const isSubmitting = ref(false)
  const error = ref<string | null>(null)

  async function fetchTodayFocus(dayOrder?: number, date?: string, locale: string = 'en') {
    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const query = new URLSearchParams()
      if (dayOrder !== undefined && dayOrder !== null) query.append('dayOrder', dayOrder.toString())
      if (date) query.append('date', date)
      if (locale) query.append('locale', locale)

      const res = await api.get<TodayFocusResponse>(`/api/v1/daily/today?${query.toString()}`)
      data.value = res
      return res
    } catch (err: any) {
      error.value = err.message || 'Failed to load daily focus.'
    } finally {
      isLoading.value = false
    }
  }

  async function submitOption(selectedOptionIndex: number, locale: string = 'en') {
    if (!data.value?.drill) return null

    isSubmitting.value = true
    error.value = null
    try {
      const api = useApiClient()
      const res = await api.post<{
        isCorrect: boolean
        selectedOptionIndex: number
        correctOptionIndex: number
        score: number
        explanationMarkdown: string
        currentStreak: number
        longestStreak: number
        totalDrillsCompleted: number
        averageScore: number
      }>(`/api/v1/daily/drills/${data.value.drill.id}/submit`, {
        selectedOptionIndex,
        locale
      })

      if (data.value) {
        data.value.drill.status = 2 // Reviewed
        data.value.drill.selectedOptionIndex = selectedOptionIndex
        data.value.drill.isCorrect = res.isCorrect
        data.value.drill.score = res.score
        data.value.question.correctOptionIndex = res.correctOptionIndex
        data.value.question.explanationMarkdown = res.explanationMarkdown
        data.value.currentStreak = res.currentStreak
        data.value.longestStreak = res.longestStreak
      }

      return res
    } catch (err: any) {
      error.value = err.message || 'Failed to submit scenario option.'
      throw err
    } finally {
      isSubmitting.value = false
    }
  }

  async function explainTerm(term: string, category: string, context: string, locale: string = 'en') {
    const api = useApiClient()
    return await api.post<{ term: string; explanation: string; locale: string }>('/api/v1/daily/explain-term', {
      term,
      category,
      context,
      locale
    })
  }

  return {
    data,
    isLoading,
    isSubmitting,
    error,
    fetchTodayFocus,
    submitOption,
    explainTerm
  }
})
