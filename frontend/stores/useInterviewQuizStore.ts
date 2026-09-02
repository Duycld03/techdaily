import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useApiClient } from '~/composables/useApiClient'
import { useToast } from '~/composables/useToast'

export interface QuizQuestion {
  id: string
  topic: string
  category: number
  level: number // 0=Fresher, 1=Junior, 2=Middle, 3=Senior
  questionText: string
  options: string[]
  correctOptionIndex: number
  explanationMarkdown: string
  tags: string[]
  isMastered: boolean
  lastSelectedOptionIndex?: number | null
  isLastAnswerCorrect?: boolean | null
  correctCount: number
  incorrectCount: number
}

export interface QuizSubmissionResult {
  isCorrect: boolean
  correctOptionIndex: number
  explanationMarkdown: string
  isMastered: boolean
  correctCount: number
  incorrectCount: number
}

export interface LevelStat {
  level: number
  answeredCount: number
  masteredCount: number
  accuracyRate: number
}

export interface TopicStat {
  topic: string
  answeredCount: number
  masteredCount: number
  accuracyRate: number
}

export interface QuizStats {
  totalAnswered: number
  masteredCount: number
  reviewQueueCount: number
  accuracyRate: number
  levelBreakdown: LevelStat[]
  topicBreakdown: TopicStat[]
}

export const useInterviewQuizStore = defineStore('interviewQuiz', () => {
  const api = useApiClient()
  const toast = useToast()

  const questions = ref<QuizQuestion[]>([])
  const currentIndex = ref(0)
  const userAnswers = ref<Record<string, number>>({})
  const submissions = ref<Record<string, QuizSubmissionResult>>({})
  const reviewQueue = ref<QuizQuestion[]>([])
  const reviewQueueTotal = ref(0)
  const stats = ref<QuizStats | null>(null)

  const activeTab = ref<'generate' | 'arena' | 'review' | 'stats' | 'summary'>('generate')
  const currentTopic = ref('.NET 10 Internals')
  const currentLevel = ref(3) // 3 = Senior
  const currentCount = ref(5)

  const isLoading = ref(false)
  const isGenerating = ref(false)
  const isSubmitting = ref(false)
  const error = ref<string | null>(null)

  const currentQuestion = computed<QuizQuestion | null>(() => {
    if (questions.value.length === 0) return null
    return questions.value[currentIndex.value] || null
  })

  const currentSubmission = computed<QuizSubmissionResult | null>(() => {
    if (!currentQuestion.value) return null
    return submissions.value[currentQuestion.value.id] || null
  })

  const isCurrentAnswered = computed(() => {
    if (!currentQuestion.value) return false
    return !!submissions.value[currentQuestion.value.id]
  })

  const progressPercentage = computed(() => {
    if (questions.value.length === 0) return 0
    return Math.round(((currentIndex.value + 1) / questions.value.length) * 100)
  })

  const sessionScore = computed(() => {
    const answeredCount = Object.keys(submissions.value).length
    if (answeredCount === 0) return { correct: 0, total: questions.value.length, percentage: 0 }
    const correctCount = Object.values(submissions.value).filter(s => s.isCorrect).length
    return {
      correct: correctCount,
      total: questions.value.length,
      percentage: Math.round((correctCount / questions.value.length) * 100)
    }
  })

  async function generateQuiz(
    topic: string,
    level: number = 3,
    count: number = 5,
    category?: number | null,
    locale: string = 'en'
  ) {
    isGenerating.value = true
    error.value = null
    currentTopic.value = topic
    currentLevel.value = level
    currentCount.value = count

    try {
      const response = await api.post<{
        questions: QuizQuestion[]
        topic: string
        level: number
        totalCount: number
      }>('/api/v1/quiz/generate', {
        topic,
        level,
        count,
        category: category ?? null,
        locale
      })

      questions.value = response.questions || []
      currentIndex.value = 0
      userAnswers.value = {}
      submissions.value = {}
      activeTab.value = 'arena'

      if (questions.value.length === 0) {
        toast.info('No questions generated. Please try a different topic.')
      }
      return response
    } catch (err: any) {
      error.value = err.message || 'Failed to generate quiz questions.'
      toast.error(error.value || 'Error generating quiz')
      throw err
    } finally {
      isGenerating.value = false
    }
  }

  async function submitAnswer(questionId: string, selectedOptionIndex: number) {
    if (submissions.value[questionId] || isSubmitting.value) return

    isSubmitting.value = true
    userAnswers.value[questionId] = selectedOptionIndex

    try {
      const result = await api.post<QuizSubmissionResult>('/api/v1/quiz/submit', {
        questionId,
        selectedOptionIndex
      })

      submissions.value[questionId] = result

      // Update question state
      const qIndex = questions.value.findIndex(q => q.id === questionId)
      if (qIndex !== -1) {
        questions.value[qIndex].isMastered = result.isMastered
        questions.value[qIndex].lastSelectedOptionIndex = selectedOptionIndex
        questions.value[qIndex].isLastAnswerCorrect = result.isCorrect
        questions.value[qIndex].correctCount = result.correctCount
        questions.value[qIndex].incorrectCount = result.incorrectCount
      }

      // If in review queue and now mastered, remove from queue
      if (result.isMastered) {
        reviewQueue.value = reviewQueue.value.filter(q => q.id !== questionId)
        if (reviewQueueTotal.value > 0) reviewQueueTotal.value--
      }

      return result
    } catch (err: any) {
      toast.error(err.message || 'Failed to submit answer.')
      throw err
    } finally {
      isSubmitting.value = false
    }
  }

  function nextQuestion() {
    if (currentIndex.value < questions.value.length - 1) {
      currentIndex.value++
    } else {
      activeTab.value = 'summary'
    }
  }

  function prevQuestion() {
    if (currentIndex.value > 0) {
      currentIndex.value--
    }
  }

  function jumpToQuestion(index: number) {
    if (index >= 0 && index < questions.value.length) {
      currentIndex.value = index
    }
  }

  async function fetchReviewQueue(
    category?: number | null,
    level?: number | null,
    topic?: string | null,
    page: number = 1,
    pageSize: number = 20
  ) {
    isLoading.value = true
    try {
      const queryParams = new URLSearchParams()
      if (category !== undefined && category !== null) queryParams.set('category', category.toString())
      if (level !== undefined && level !== null) queryParams.set('level', level.toString())
      if (topic) queryParams.set('topic', topic)
      queryParams.set('page', page.toString())
      queryParams.set('pageSize', pageSize.toString())

      const response = await api.get<{
        questions: QuizQuestion[]
        totalCount: number
        page: number
        pageSize: number
      }>(`/api/v1/quiz/review-queue?${queryParams.toString()}`)

      reviewQueue.value = response.questions || []
      reviewQueueTotal.value = response.totalCount || 0
      return response
    } catch (err: any) {
      toast.error(err.message || 'Failed to load review queue.')
    } finally {
      isLoading.value = false
    }
  }

  function startReviewSession(customQuestions?: QuizQuestion[]) {
    const listToReview = customQuestions || reviewQueue.value
    if (listToReview.length === 0) {
      toast.info('No unmastered questions in the review queue.')
      return
    }
    questions.value = [...listToReview]
    currentIndex.value = 0
    userAnswers.value = {}
    submissions.value = {}
    activeTab.value = 'arena'
  }

  async function fetchStats() {
    isLoading.value = true
    try {
      const response = await api.get<QuizStats>('/api/v1/quiz/stats')
      stats.value = response
      return response
    } catch (err: any) {
      toast.error(err.message || 'Failed to load quiz statistics.')
    } finally {
      isLoading.value = false
    }
  }

  function resetSession() {
    activeTab.value = 'generate'
    questions.value = []
    currentIndex.value = 0
    userAnswers.value = {}
    submissions.value = {}
  }

  return {
    questions,
    currentIndex,
    userAnswers,
    submissions,
    reviewQueue,
    reviewQueueTotal,
    stats,
    activeTab,
    currentTopic,
    currentLevel,
    currentCount,
    isLoading,
    isGenerating,
    isSubmitting,
    error,
    currentQuestion,
    currentSubmission,
    isCurrentAnswered,
    progressPercentage,
    sessionScore,
    generateQuiz,
    submitAnswer,
    nextQuestion,
    prevQuestion,
    jumpToQuestion,
    fetchReviewQueue,
    startReviewSession,
    fetchStats,
    resetSession
  }
})
