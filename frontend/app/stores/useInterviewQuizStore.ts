import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useApiClient } from '~/composables/useApiClient'
import { useToast } from '~/composables/useToast'
import { useApiError } from '~/composables/useApiError'
import { useCategoryStore, type CategoryDescriptor } from '~/stores/useCategoryStore'

export interface QuizQuestion {
  id: string
  topic: string
  categoryId: string
  category: CategoryDescriptor
  questionText: string
  level: number // 0=Fresher, 1=Junior, 2=Middle, 3=Senior
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
  level: number | string
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

export interface CategoryStat {
  categoryId: string
  category: CategoryDescriptor
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
  categoryBreakdown: CategoryStat[]
}

export const useInterviewQuizStore = defineStore('interviewQuiz', () => {
  const api = useApiClient()
  const toast = useToast()
  const { formatError, t } = useApiError()
  const categoryStore = useCategoryStore()

  const questions = ref<QuizQuestion[]>([])
  const currentIndex = ref(0)
  const userAnswers = ref<Record<string, number>>({})
  const submissions = ref<Record<string, QuizSubmissionResult>>({})
  const reviewQueue = ref<QuizQuestion[]>([])
  const reviewQueueTotal = ref(0)
  const reviewPage = ref(1)
  const reviewPageSize = ref(10)
  const reviewTotalCount = ref(0)
  const reviewTotalPages = ref(0)
  const stats = ref<QuizStats | null>(null)
  const reviewCategoryId = ref<string | null>(null)
  const reviewLevel = ref<number | null>(null)
  const reviewTopic = ref<string | null>(null)

  const activeTab = ref<'generate' | 'arena' | 'review' | 'stats' | 'summary'>('generate')
  const currentTopic = ref('.NET 10 Internals')
  const currentLevel = ref(3) // 3 = Senior
  const currentCount = ref(5)
  const currentCategoryId = ref<string | null>(null)

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
    categoryId: string | null = null,
    locale: string = 'en',
    bookId?: string | null,
    isGrounded?: boolean
  ) {
    if (!bookId) {
      await categoryStore.ensureCategories()
      if (!categoryId || !categoryStore.getCategoryById(categoryId)?.isActive || categoryStore.error) {
        throw new Error(t('categories.selectCategory'))
      }
    }
    isGenerating.value = true
    error.value = null
    currentTopic.value = topic
    currentLevel.value = level
    currentCount.value = count
    currentCategoryId.value = categoryId

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
        categoryId,
        locale,
        bookId: bookId || null,
        isGrounded: !!isGrounded
      })

      questions.value = response.questions || []
      currentIndex.value = 0
      userAnswers.value = {}
      submissions.value = {}
      activeTab.value = 'arena'

      if (questions.value.length === 0) {
        toast.info(t('quiz.no_questions_generated'))
      }
      return response
    } catch (err: any) {
      const formatted = formatError(err, 'quiz.generate_failed')
      await categoryStore.fetchCategories(true)
      error.value = formatted
      toast.error(formatted)
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
      const targetQuestion = questions.value.find(q => q.id === questionId)
      if (targetQuestion) {
        targetQuestion.isMastered = result.isMastered
        targetQuestion.lastSelectedOptionIndex = selectedOptionIndex
        targetQuestion.isLastAnswerCorrect = result.isCorrect
        targetQuestion.correctCount = result.correctCount
        targetQuestion.incorrectCount = result.incorrectCount
      }

      // If in review queue and now mastered, remove from queue
      if (result.isMastered) {
        reviewQueue.value = reviewQueue.value.filter(q => q.id !== questionId)
        if (reviewQueueTotal.value > 0) reviewQueueTotal.value--
      }

      return result
    } catch (err: any) {
      toast.error(formatError(err, 'quiz.submit_failed'))
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

  async function fetchReviewQueue(params: {
    categoryId?: string | null
    level?: number | null
    topic?: string | null
    page?: number
    pageSize?: number
  } = {}) {
    if (params.categoryId !== undefined) reviewCategoryId.value = params.categoryId
    if (params.level !== undefined) reviewLevel.value = params.level
    if (params.topic !== undefined) reviewTopic.value = params.topic
    const { page, pageSize } = params

    const targetPage = page ?? reviewPage.value
    const targetPageSize = pageSize ?? reviewPageSize.value

    isLoading.value = true
    try {
      const queryParams = new URLSearchParams()
      if (reviewCategoryId.value) queryParams.set('categoryId', reviewCategoryId.value)
      if (reviewLevel.value !== null) queryParams.set('level', reviewLevel.value.toString())
      if (reviewTopic.value) queryParams.set('topic', reviewTopic.value)
      queryParams.set('page', targetPage.toString())
      queryParams.set('pageSize', targetPageSize.toString())

      const response = await api.get<{
        questions: QuizQuestion[]
        totalCount?: number
        page?: number
        pageSize?: number
        totalPages?: number
      }>(`/api/v1/quiz/review-queue?${queryParams.toString()}`)

      reviewQueue.value = response.questions || []
      reviewTotalCount.value = response.totalCount ?? reviewQueue.value.length
      reviewQueueTotal.value = reviewTotalCount.value
      reviewPage.value = response.page ?? targetPage
      reviewPageSize.value = response.pageSize ?? targetPageSize
      reviewTotalPages.value =
        response.totalPages ??
        (reviewTotalCount.value > 0 ? Math.ceil(reviewTotalCount.value / reviewPageSize.value) : 0)

      return response
    } catch (err: unknown) {
      toast.error(formatError(err, 'quiz.review_queue_failed'))
    } finally {
      isLoading.value = false
    }
  }

  function startReviewSession(customQuestions?: QuizQuestion[]) {
    const listToReview = customQuestions || reviewQueue.value
    if (listToReview.length === 0) {
      toast.info(t('quiz.no_review_questions'))
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
      toast.error(formatError(err, 'quiz.stats_failed'))
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
    reviewCategoryId,
    reviewLevel,
    reviewTopic,
    stats,
    activeTab,
    currentTopic,
    currentLevel,
    currentCount,
    currentCategoryId,
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
    reviewPage,
    reviewPageSize,
    reviewTotalCount,
    reviewTotalPages,
    fetchReviewQueue,
    startReviewSession,
    fetchStats,
    resetSession
  }
})
