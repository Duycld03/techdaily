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
  ]
}

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/api/v1/quiz/stats')) {
        return { ...mockQuizStats }
      }
      if (url.includes('/api/v1/quiz/review-queue')) {
        return {
          questions: [
            {
              id: 'q-1',
              topic: '.NET 10',
              category: 1,
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
  beforeEach(() => {
    setActivePinia(createPinia())
    const authStore = useAuthStore()
    authStore.token = 'mock-jwt-token'
    authStore.user = {
      id: 'usr-1',
      email: 'staff.dev@techdaily.dev',
      name: 'Taylor TechLead',
      preferredLocale: 'en'
    }
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
    expect(wrapper.text()).toContain('Fresher / Entry')
    expect(wrapper.text()).toContain('Junior')
    expect(wrapper.text()).toContain('Mid-Level')
    expect(wrapper.text()).toContain('Senior / Staff')

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
})
