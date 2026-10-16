import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import QuizPage, { formatSeniorityLevel, seniorityLevels } from '~/pages/quiz.vue'
declare module '~/pages/quiz.vue' {
  export const seniorityLevels: Array<{ id: number; key: string; label: string; desc: string }>
  export function formatSeniorityLevel(level: string | number): { id: number; key: string; label: string; desc: string }
}
import { useAuthStore } from '~/stores/useAuthStore'
import { useInterviewQuizStore } from '~/stores/useInterviewQuizStore'
import { useCategoryStore } from '~/stores/useCategoryStore'

const category = { id: '97e10b46-4a79-43f4-8b29-91c85582f899', slug: 'new-subject', nameEn: 'New subject', nameVi: 'Chủ đề mới', sortOrder: 8, isActive: true, version: 1 }
const inactive = { id: 'bb03c028-488c-472c-b1cf-0d8354879109', slug: 'retained', nameEn: 'Retained subject', nameVi: 'Chủ đề lưu giữ', sortOrder: 9, isActive: false, version: 2 }
const offPageBookId = '11ee7e16-2892-4524-ae92-8ce69d034e50'

const mockQuizStats = {
  totalAnswered: 40,
  masteredCount: 30,
  reviewQueueCount: 4,
  accuracyRate: 75,
  levelBreakdown: [
    { level: 'Fresher', answeredCount: 10, masteredCount: 8, accuracyRate: 80 },
    { level: 'Junior', answeredCount: 10, masteredCount: 7, accuracyRate: 70 },
    { level: 'Middle', answeredCount: 10, masteredCount: 6, accuracyRate: 60 },
    { level: 'Senior', answeredCount: 10, masteredCount: 9, accuracyRate: 90 }
  ],
  topicBreakdown: [
    { topic: 'C# 13 Runtime', answeredCount: 10, masteredCount: 8, accuracyRate: 80 },
    { topic: 'PostgreSQL 17 Storage Engine', answeredCount: 10, masteredCount: 7, accuracyRate: 70 },
    { topic: 'Distributed Systems', answeredCount: 10, masteredCount: 4, accuracyRate: 40 }
  ],
  categoryBreakdown: [
    { categoryId: inactive.id, category: inactive, answeredCount: 2, masteredCount: 0, accuracyRate: 0 }
  ]
}

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url === '/api/v1/categories') return [category, inactive]
      if (url.includes('/api/v1/quiz/stats')) {
        return { ...mockQuizStats }
      }
      if (url.includes('/api/v1/quiz/review-queue')) {
        return {
          questions: [
            {
              id: 'q-1',
              topic: '.NET 10',
              categoryId: inactive.id,
              category: inactive,
              level: 3,
              questionText: 'What is dynamic PGO?',
              options: ['A', 'B'],
              correctOptionIndex: 0,
              explanationMarkdown: 'Profile guided optimization',
              tags: ['dotnet'],
              isMastered: false,
              correctCount: 0,
              incorrectCount: 1
            }
          ],
          totalCount: 15,
          page: 1,
          pageSize: 10,
          totalPages: 2
        }
      }
      if (url === `/api/v1/library/books/${offPageBookId}`) {
        return { book: { id: offPageBookId, title: 'Owned document beyond page one', slug: 'off-page', sourceType: 1, categoryId: inactive.id, category: inactive, totalChunks: 1, isPublished: false, createdAt: '2026-10-16T00:00:00Z', chunks: [] } }
      }
      if (url.includes('/api/v1/library/books')) {
        return { books: [] }
      }
      return {}
    }),
    post: vi.fn(async () => ({ success: true }))
  })
}))

describe('formatSeniorityLevel helper', () => {
  it('maps numeric IDs correctly', () => {
    expect(formatSeniorityLevel(0)).toEqual(seniorityLevels[0])
    expect(formatSeniorityLevel(1)).toEqual(seniorityLevels[1])
    expect(formatSeniorityLevel(2)).toEqual(seniorityLevels[2])
    expect(formatSeniorityLevel(3)).toEqual(seniorityLevels[3])
    // Unknown number defaults to senior (level 3)
    expect(formatSeniorityLevel(99)).toEqual(seniorityLevels[3])
  })

  it('maps string enum values case-insensitively and handles aliases', () => {
    expect(formatSeniorityLevel('Fresher')).toEqual(seniorityLevels[0])
    expect(formatSeniorityLevel('fresher')).toEqual(seniorityLevels[0])
    expect(formatSeniorityLevel('0')).toEqual(seniorityLevels[0])

    expect(formatSeniorityLevel('Junior')).toEqual(seniorityLevels[1])
    expect(formatSeniorityLevel('junior')).toEqual(seniorityLevels[1])
    expect(formatSeniorityLevel('1')).toEqual(seniorityLevels[1])

    expect(formatSeniorityLevel('Middle')).toEqual(seniorityLevels[2])
    expect(formatSeniorityLevel('middle')).toEqual(seniorityLevels[2])
    expect(formatSeniorityLevel('mid')).toEqual(seniorityLevels[2])
    expect(formatSeniorityLevel('2')).toEqual(seniorityLevels[2])

    expect(formatSeniorityLevel('Senior')).toEqual(seniorityLevels[3])
    expect(formatSeniorityLevel('senior')).toEqual(seniorityLevels[3])
    expect(formatSeniorityLevel('3')).toEqual(seniorityLevels[3])

    // Fallback defaults to senior
    expect(formatSeniorityLevel('Principal')).toEqual(seniorityLevels[3])
  })
})

describe('quiz.vue (Bento Grid Dashboard in Stats Tab)', () => {
  beforeEach(async () => {
    setActivePinia(createPinia())
    const authStore = useAuthStore()
    const token = `e30.${btoa(JSON.stringify({ sub: 'usr-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.signature`
    await authStore.setSession(token, {
      id: 'usr-1',
      email: 'staff.dev@deeppace.test',
      name: 'Taylor TechLead',
      preferredLocale: 'en'
    })
  })

  it('renders 4-card Bento Grid Dashboard with distinct seniority levels', async () => {
    const wrapper = mount(QuizPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true
        }
      }
    })

    await flushPromises()

    const quizStore = useInterviewQuizStore()
    quizStore.activeTab = 'stats'
    await wrapper.vm.$nextTick()

    // 1. Hero Performance Card
    expect(wrapper.text()).toContain('quiz.bento_hero_title')
    expect(wrapper.text()).toContain('75%')
    expect(wrapper.text()).toContain('quiz.readiness_ready')
    expect(wrapper.text()).toContain('quiz.btn_review_mistakes')

    // 2. Spaced Mastery Gauge Card
    expect(wrapper.text()).toContain('quiz.bento_mastery_title')
    expect(wrapper.find('svg[aria-label="Quiz mastery rate gauge"]').exists()).toBe(true)

    // 3. Seniority Matrix Card (resolves legacy bug: all 4 distinct levels render!)
    expect(wrapper.text()).toContain('quiz.bento_seniority_title')
    expect(wrapper.text()).toContain('Foundation')
    expect(wrapper.text()).toContain('Applied')
    expect(wrapper.text()).toContain('Advanced')
    expect(wrapper.text()).toContain('Mastery')

    // 4. Topic Strengths & Weaknesses Card
    expect(wrapper.text()).toContain('quiz.bento_topic_title')
    expect(wrapper.text()).toContain('C# 13 Runtime')
    expect(wrapper.text()).toContain('PostgreSQL 17 Storage Engine')
    expect(wrapper.text()).toContain('Distributed Systems')

    // Primary CTA click transitions to review tab
    const reviewMistakesBtn = wrapper.findAll('button').find((b) => b.text().includes('quiz.btn_review_mistakes'))
    expect(reviewMistakesBtn).toBeDefined()
    await reviewMistakesBtn!.trigger('click')
    expect(quizStore.activeTab).toBe('review')
  })

  it('renders dual review triggers and BasePagination in review tab', async () => {
    const wrapper = mount(QuizPage, {
      global: {
        stubs: {
          ShikiCodeBlock: true,
          Teleport: true
        }
      }
    })
    await flushPromises()

    const quizStore = useInterviewQuizStore()
    quizStore.activeTab = 'review'
    await quizStore.fetchReviewQueue()
    await wrapper.vm.$nextTick()
    await flushPromises()

    expect(wrapper.text()).toContain('quiz.tab_review_queue')
    expect(wrapper.text()).toContain('quiz.practice_current_batch')
    expect(wrapper.text()).toContain('quiz.practice_all_mistakes')
    expect(wrapper.find('nav').exists()).toBe(true)
    expect(wrapper.find('nav').attributes('role')).toBe('navigation')
  })

  it('renders 2-column Bento Generation Studio with clean typographic levels and segmented count pills', async () => {
    const wrapper = mount(QuizPage, {
      global: {
        stubs: {
          ShikiCodeBlock: true,
          Teleport: true
        }
      }
    })
    await flushPromises()

    const quizStore = useInterviewQuizStore()
    quizStore.activeTab = 'generate'
    await wrapper.vm.$nextTick()

    // Studio Header & Subtitle
    expect(wrapper.text()).toContain('quiz.title')
    expect(wrapper.text()).toContain('quiz.subtitle')

    // Standard studio container max-w-6xl
    expect(wrapper.find('.max-w-6xl').exists()).toBe(true)

    // Balanced 50/50 Bento Grid with dynamic alignment (items-stretch when unselected)
    expect(wrapper.find('.lg\\:grid-cols-2').exists()).toBe(true)
    expect(wrapper.find('.items-stretch').exists()).toBe(true)
    expect(wrapper.find('.justify-between').exists()).toBe(true)

    // Generate CTA button
    const generateBtn = wrapper.find('[data-testid="generate-quiz-btn"]')
    expect(generateBtn.exists()).toBe(true)
    expect(generateBtn.text()).toContain('quiz.btn_generate')

    // Seniority levels (4 typographic cards)
    expect(wrapper.text()).toContain('quiz.level_fresher')
    expect(wrapper.text()).toContain('quiz.level_junior')
    expect(wrapper.text()).toContain('quiz.level_middle')
    expect(wrapper.text()).toContain('quiz.level_senior')

    // Segmented question count pills (3 / 5 / 10)
    expect(wrapper.text()).toContain('quiz.count_3')
    expect(wrapper.text()).toContain('quiz.count_5')
    expect(wrapper.text()).toContain('quiz.count_10')

    // A topic alone must never silently assign a default category.
    const topicInput = wrapper.find('input[type="text"]')
    await topicInput.setValue('New subject topic')
    expect(generateBtn.attributes('disabled')).toBeDefined()
    const categorySelect = wrapper.find('[data-testid="quiz-category-select"]')
    await categorySelect.find('[role="combobox"]').trigger('click')
    expect(categorySelect.find(`[data-testid="app-select-option-${inactive.id}"]`).exists()).toBe(false)
    await categorySelect.find(`[data-testid="app-select-option-${category.id}"]`).trigger('click')
    expect(generateBtn.attributes('disabled')).toBeUndefined()
  })

  it('renders unboxed direct-canvas arena studio and handles keyboard shortcuts', async () => {
    const quizStore = useInterviewQuizStore()
    quizStore.questions = [
      {
        id: 'q-1',
        topic: 'ASP.NET Core Performance',
        categoryId: category.id,
        category,
        level: 3,
        questionText: 'Which collection pool avoids LOH allocation?',
        options: ['List<T>', 'ArrayPool<T>', 'Queue<T>', 'Stack<T>'],
        correctOptionIndex: 1,
        explanationMarkdown: 'ArrayPool reduces GC pressure.',
        tags: ['Performance'],
        isMastered: false,
        correctCount: 2,
        incorrectCount: 1
      }
    ]
    quizStore.currentIndex = 0
    quizStore.activeTab = 'arena'

    const wrapper = mount(QuizPage)
    await flushPromises()

    // Renders topic and question text
    expect(wrapper.text()).toContain('ASP.NET Core Performance')
    expect(wrapper.text()).toContain('Which collection pool avoids LOH allocation?')

    // 4 Option cards rendered
    const optionCards = wrapper.findAllComponents({ name: 'OptionCard' })
    expect(optionCards.length).toBe(4)

    // Test keyboard selection via '2' or 'b'
    window.dispatchEvent(new KeyboardEvent('keydown', { key: '2' }))
    await wrapper.vm.$nextTick()

    // Second option card should now have state selected
    expect(optionCards[1]!.props('state')).toBe('selected')

    // Test keyboard selection via 'c' (third option)
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'c' }))
    await wrapper.vm.$nextTick()
    expect(optionCards[2]!.props('state')).toBe('selected')
  })
  it('retains inactive review choices and renders UUID category analytics', async () => {
    const wrapper = mount(QuizPage, { global: { stubs: { Teleport: true } } })
    await flushPromises()
    const store = useInterviewQuizStore()
    store.activeTab = 'review'
    await wrapper.vm.$nextTick()
    const filter = wrapper.find('[data-testid="quiz-review-category-select"]')
    await filter.find('[role="combobox"]').trigger('click')
    expect(filter.find(`[data-testid="app-select-option-${inactive.id}"]`).exists()).toBe(true)
    await filter.find(`[data-testid="app-select-option-${inactive.id}"]`).trigger('click')
    await flushPromises()
    expect(store.reviewCategoryId).toBe(inactive.id)
    expect(store.reviewPage).toBe(1)
    store.activeTab = 'stats'
    await wrapper.vm.$nextTick()
    expect(wrapper.text()).toContain(inactive.nameEn)
    expect(wrapper.text()).toContain('categories.inactive')
  })
  it('blocks a stale generation selection without erasing the topic or historical mistakes', async () => {
    const wrapper = mount(QuizPage, { global: { stubs: { Teleport: true } } })
    await flushPromises()
    await wrapper.find('input[type="text"]').setValue('Retain my quiz topic')
    const select = wrapper.find('[data-testid="quiz-category-select"]')
    await select.find('[role="combobox"]').trigger('click')
    await select.find(`[data-testid="app-select-option-${category.id}"]`).trigger('click')
    useCategoryStore().categories = [{ ...category, isActive: false }, inactive]
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[data-testid="generate-quiz-btn"]').attributes('disabled')).toBeDefined()
    expect((wrapper.find('input[type="text"]').element as HTMLInputElement).value).toBe('Retain my quiz topic')
    expect(useInterviewQuizStore().reviewQueue[0]?.categoryId).toBe(inactive.id)
  })
  it('resolves a grounded reader deep link outside the current library page and retains its inactive assignment', async () => {
    const previousRoute = Reflect.get(globalThis, 'useRoute')
    vi.stubGlobal('useRoute', () => ({ query: { bookId: offPageBookId, topic: 'Owned source topic' } }))
    try {
      const store = useInterviewQuizStore()
      const generate = vi.spyOn(store, 'generateQuiz').mockResolvedValue(undefined)
      const wrapper = mount(QuizPage)
      await flushPromises()
      expect(wrapper.text()).toContain(inactive.nameEn)
      const submit = wrapper.find('[data-testid="generate-quiz-btn"]')
      expect(submit.attributes('disabled')).toBeUndefined()
      await submit.trigger('click')
      expect(generate).toHaveBeenCalledWith('Owned source topic', 3, 5, null, 'en', offPageBookId, true)
      wrapper.unmount()
    } finally {
      vi.stubGlobal('useRoute', previousRoute)
    }
  })
  it('loads an inactive UUID review filter directly from the URL without discarding other preferences', async () => {
    const previousRoute = Reflect.get(globalThis, 'useRoute')
    vi.stubGlobal('useRoute', () => ({ query: { tab: 'review', page: '3', categoryId: inactive.id, level: '2', topic: 'Retained topic', reader: 'compact' } }))
    try {
      const store = useInterviewQuizStore()
      const fetch = vi.spyOn(store, 'fetchReviewQueue')
      mount(QuizPage)
      await flushPromises()
      expect(fetch).toHaveBeenCalledWith({ categoryId: inactive.id, level: 2, topic: 'Retained topic', page: 3 })
    } finally {
      vi.stubGlobal('useRoute', previousRoute)
    }
  })

  it('clears only an obsolete numeric category URL filter', async () => {
    const previousRoute = Reflect.get(globalThis, 'useRoute')
    const previousRouter = Reflect.get(globalThis, 'useRouter')
    const replace = vi.fn()
    vi.stubGlobal('useRoute', () => ({ query: { tab: 'review', category: '1', reader: 'compact', topic: 'Retained topic', level: '2' } }))
    vi.stubGlobal('useRouter', () => ({ replace, push: vi.fn() }))
    try {
      mount(QuizPage)
      await flushPromises()
      expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ category: undefined, reader: 'compact', topic: 'Retained topic', level: '2' }) })
    } finally {
      vi.stubGlobal('useRoute', previousRoute)
      vi.stubGlobal('useRouter', previousRouter)
    }
  })

  it('does not start a partial Practice All session when a later page fails', async () => {
    const wrapper = mount(QuizPage)
    await flushPromises()
    const store = useInterviewQuizStore()
    store.activeTab = 'review'
    store.reviewTotalPages = 2
    const fetch = vi.spyOn(store, 'fetchReviewQueue')
    fetch.mockResolvedValueOnce({ questions: [...store.reviewQueue], totalCount: 101, page: 1, pageSize: 100, totalPages: 2 })
    fetch.mockResolvedValueOnce(undefined)
    const start = vi.spyOn(store, 'startReviewSession')
    await wrapper.vm.$nextTick()
    await wrapper.findAll('button').find(button => button.text().includes('quiz.practice_all_mistakes'))!.trigger('click')
    await flushPromises()
    expect(start).not.toHaveBeenCalled()
  })
})
