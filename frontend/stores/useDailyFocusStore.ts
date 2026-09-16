import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useApiClient } from '~/composables/useApiClient'
import { useToast } from '~/composables/useToast'
import { useApiError } from '~/composables/useApiError'

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

export interface DocumentChunk {
  id: string
  chunkOrder: number
  chapterTitle: string
  originalTextMarkdown: string
  summaryMarkdown: string
  keyTakeaways: string[]
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

export interface PacerBookSummary {
  id: string
  title: string
  progressPercentage: number
  totalChunks: number
  currentChunkOrder: number
  isActive: boolean
}

export interface PacerInfo {
  bookId: string
  bookTitle: string
  chapterTitle: string
  currentChunkOrder: number
  totalChunks: number
  progressPercentage: number
  hasPrevious: boolean
  hasNext: boolean
  availableBooks: PacerBookSummary[]
}

export interface TodayFocusResponse {
  topic?: Topic
  question: InterviewQuestion
  documentChunk?: DocumentChunk
  drill: DailyDrill
  currentStreak: number
  longestStreak: number
  freezeCreditsRemaining: number
  pacer?: PacerInfo
  isGeneratingQuestion?: boolean
}

export const useDailyFocusStore = defineStore('dailyFocus', () => {
  const data = ref<TodayFocusResponse | null>(null)
  const isLoading = ref(false)
  const isSubmitting = ref(false)
  const isSwitchingBook = ref(false)
  const isGeneratingQuestion = ref(false)
  const error = ref<string | null>(null)
  const { formatError } = useApiError()

  async function fetchTodayFocus(
    params?: { bookId?: string; chunkOrder?: number; dayOrder?: number; date?: string; locale?: string } | number,
    legacyDate?: string,
    legacyLocale: string = 'en'
  ) {
    let bookId: string | undefined
    let chunkOrder: number | undefined
    let dayOrder: number | undefined
    let date: string | undefined = legacyDate
    let locale: string = legacyLocale

    if (typeof params === 'number') {
      dayOrder = params
    } else if (params) {
      bookId = params.bookId
      chunkOrder = params.chunkOrder
      dayOrder = params.dayOrder
      date = params.date
      locale = params.locale ?? 'en'
    }

    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const query = new URLSearchParams()
      if (bookId) query.append('bookId', bookId)
      if (chunkOrder !== undefined && chunkOrder !== null) query.append('chunkOrder', chunkOrder.toString())
      if (dayOrder !== undefined && dayOrder !== null) query.append('dayOrder', dayOrder.toString())
      if (date) query.append('date', date)
      if (locale) query.append('locale', locale)

      const res = await api.get<TodayFocusResponse>(`/api/v1/daily/today?${query.toString()}`)
      data.value = res
      isGeneratingQuestion.value = res.isGeneratingQuestion ?? false

      if (res.isGeneratingQuestion && res.documentChunk?.id) {
        fetchChunkChallenge(res.documentChunk.id)
      }

      return res
    } catch (err: any) {
      error.value = formatError(err, 'today.error_load_failed')
    } finally {
      isLoading.value = false
    }
  }

  async function switchBook(bookId: string, locale: string = 'en') {
    isSwitchingBook.value = true
    try {
      const api = useApiClient()
      const pacer = await api.post<PacerInfo>('/api/v1/daily/switch-book', { bookId })
      await fetchTodayFocus({ bookId, chunkOrder: pacer.currentChunkOrder, locale })
      return pacer
    } catch (err: any) {
      const toast = useToast()
      toast.error(formatError(err, 'pacer.error_switch_failed'))
      throw err
    } finally {
      isSwitchingBook.value = false
    }
  }

  async function fetchChunkChallenge(chunkId: string) {
    try {
      const api = useApiClient()
      const question = await api.get<InterviewQuestion>(`/api/v1/daily/chunk-challenge/${chunkId}`)
      if (data.value && question && data.value.documentChunk?.id === chunkId) {
        data.value.question = question
        data.value.isGeneratingQuestion = false
        isGeneratingQuestion.value = false
      }
      return question
    } catch (err) {
      isGeneratingQuestion.value = false
    }
  }

  async function submitOption(selectedOptionIndex: number, locale: string = 'en') {
    if (!data.value?.drill) return null

    isSubmitting.value = true
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
      try {
        const toast = useToast()
        toast.error(formatError(err, 'today.error_submit_failed'))
      } catch {
        // ignore
      }
      throw err
    } finally {
      isSubmitting.value = false
    }
  }

  async function explainTerm(term: string, category: string, context: string, locale: string = 'en') {
    const api = useApiClient()
    return await api.post<{ term: string; explanation: string; locale: string; isFromCache?: boolean }>('/api/v1/daily/explain-term', {
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
    isSwitchingBook,
    isGeneratingQuestion,
    error,
    fetchTodayFocus,
    switchBook,
    fetchChunkChallenge,
    submitOption,
    explainTerm
  }
})
