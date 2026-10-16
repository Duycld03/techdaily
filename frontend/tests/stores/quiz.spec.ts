import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useInterviewQuizStore } from '~/stores/useInterviewQuizStore'

const { category, inactive, get, post } = vi.hoisted(() => ({
  category: { id: '97e10b46-4a79-43f4-8b29-91c85582f899', slug: 'new-subject', nameEn: 'New subject', nameVi: 'Chủ đề mới', sortOrder: 8, isActive: true, version: 1 },
  inactive: { id: 'bb03c028-488c-472c-b1cf-0d8354879109', slug: 'retained', nameEn: 'Retained subject', nameVi: 'Chủ đề lưu giữ', sortOrder: 9, isActive: false, version: 2 },
  get: vi.fn(), post: vi.fn()
}))
vi.mock('~/composables/useToast', () => ({ useToast: () => ({ success: vi.fn(), error: vi.fn(), info: vi.fn() }) }))
vi.mock('~/composables/useApiClient', () => ({ useApiClient: () => ({ get, post }) }))

function question(id: string, descriptor = category) {
  return { id, topic: 'Source topic', questionText: `Actual question ${id}?`, categoryId: descriptor.id, category: descriptor, level: 3, options: ['A', 'B', 'C', 'D'], correctOptionIndex: 0, explanationMarkdown: 'Explanation', tags: [], isMastered: false, correctCount: 0, incorrectCount: 1 }
}

describe('useInterviewQuizStore UUID categories', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    get.mockImplementation(async (url: string) => {
      if (url === '/api/v1/categories') return [category, inactive]
      if (url.includes('/stats')) return { totalAnswered: 10, masteredCount: 8, reviewQueueCount: 2, accuracyRate: 80, levelBreakdown: [], topicBreakdown: [], categoryBreakdown: [{ categoryId: inactive.id, category: inactive, answeredCount: 2, masteredCount: 0, accuracyRate: 0 }] }
      const params = new URL('http://localhost' + url).searchParams
      return { questions: [question('retained', inactive)], totalCount: 1, page: Number(params.get('page')), pageSize: Number(params.get('pageSize')), totalPages: 1 }
    })
    post.mockImplementation(async (url: string, body: { selectedOptionIndex?: number }) => {
      if (url.includes('/submit')) return { isCorrect: body.selectedOptionIndex === 0, correctOptionIndex: 0, explanationMarkdown: 'Explanation', isMastered: body.selectedOptionIndex === 0, correctCount: body.selectedOptionIndex === 0 ? 1 : 0, incorrectCount: body.selectedOptionIndex === 0 ? 0 : 1 }
      return { questions: [question('one'), question('two')], topic: 'Source topic', level: 3, totalCount: 2 }
    })
  })

  it('initializes without an invented category or generated questions', () => {
    const store = useInterviewQuizStore()
    expect(store.currentCategoryId).toBeNull()
    expect(store.questions).toEqual([])
    expect(store.activeTab).toBe('generate')
  })

  it('blocks standalone generation without an explicit active category before invoking AI', async () => {
    const store = useInterviewQuizStore()
    await expect(store.generateQuiz('Topic', 3, 5, null)).rejects.toThrow()
    await expect(store.generateQuiz('Topic', 3, 5, inactive.id)).rejects.toThrow()
    expect(post).not.toHaveBeenCalled()
  })

  it('sends only categoryId, preserves questionText and remembers the Generate More assignment', async () => {
    const store = useInterviewQuizStore()
    await store.generateQuiz('Topic', 3, 5, category.id, 'vi')
    expect(post).toHaveBeenCalledWith('/api/v1/quiz/generate', { topic: 'Topic', level: 3, count: 5, categoryId: category.id, locale: 'vi', bookId: null, isGrounded: false })
    expect(store.currentQuestion?.questionText).toBe('Actual question one?')
    expect(store.currentCategoryId).toBe(category.id)
    expect(store.activeTab).toBe('arena')
    expect(store.progressPercentage).toBe(50)
  })

  it('lets book-grounded generation derive its retained inactive category from the server', async () => {
    const store = useInterviewQuizStore()
    post.mockResolvedValueOnce({ questions: [question('source', inactive)], totalCount: 1 })
    await store.generateQuiz('Topic', 3, 5, null, 'en', 'book-id', true)
    expect(post.mock.calls[0]![1]).toMatchObject({ categoryId: null, bookId: 'book-id', isGrounded: true })
    expect(store.currentQuestion?.category).toEqual(inactive)
  })

  it('submits correct and incorrect answers without changing category provenance', async () => {
    const store = useInterviewQuizStore()
    await store.generateQuiz('Topic', 3, 5, category.id)
    expect((await store.submitAnswer('one', 0))?.isMastered).toBe(true)
    expect((await store.submitAnswer('two', 1))?.isMastered).toBe(false)
    expect(store.questions[0]?.categoryId).toBe(category.id)
    expect(store.questions[1]?.incorrectCount).toBe(1)
  })

  it('navigates next and previous and enters summary at the end', async () => {
    const store = useInterviewQuizStore()
    await store.generateQuiz('Topic', 3, 5, category.id)
    store.nextQuestion()
    expect(store.currentIndex).toBe(1)
    store.nextQuestion()
    expect(store.activeTab).toBe('summary')
    store.prevQuestion()
    expect(store.currentIndex).toBe(0)
  })

  it('keeps inactive UUID review filters across pagination and preserves dynamic analytics', async () => {
    const store = useInterviewQuizStore()
    await store.fetchReviewQueue({ categoryId: inactive.id, level: 2, topic: 'Source', page: 2, pageSize: 10 })
    const params = new URL('http://localhost' + get.mock.calls.at(-1)![0]).searchParams
    expect(params.get('categoryId')).toBe(inactive.id)
    expect(params.has('category')).toBe(false)
    expect(params.get('level')).toBe('2')
    expect(store.reviewPage).toBe(2)
    expect(store.reviewQueue[0]?.category).toEqual(inactive)
    await store.fetchStats()
    expect(store.stats?.categoryBreakdown[0]?.categoryId).toBe(inactive.id)
  })
})
