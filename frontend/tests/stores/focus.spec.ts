import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'

const mockFocusData = {
  topic: {
    id: 't-1',
    slug: 'vue3-reactivity',
    title: 'Vue 3 Reactivity Engine',
    category: 0,
    difficulty: 1,
    dayOrder: 1,
    summary: 'Deep dive into Proxy and Reflect',
    deepDiveMarkdown: '### Proxy Mechanisms'
  },
  question: {
    id: 'q-1',
    questionText: 'When does destructuring reactive() lose reactivity?',
    options: [
      'Wrap with reactive()',
      'Use shallowRef() avoiding deep Proxy traversal',
      'Individual ref() properties',
      'Disable reactivity'
    ],
    expectedKeyPoints: ['Proxy getter', 'Loss of track()'],
    modelAnswerMarkdown: 'Destructuring breaks Proxy reference...',
    difficulty: 1
  },
  documentChunk: {
    id: 'c-1',
    chunkOrder: 1,
    chapterTitle: 'Reactivity Engine',
    originalTextMarkdown: 'Vue 3 Reactivity uses ES6 Proxy...',
    summaryMarkdown: 'Summary of Reactivity...',
    keyTakeaways: ['Proxy tracks get/set'],
    microQuiz: {
      question: 'What triggers track()?',
      options: ['Property access', 'Destructure', 'Console log'],
      answerIndex: 0,
      explanation: 'Property access invokes Proxy get trap.'
    },
    language: 'en',
    estimatedReadMinutes: 3
  },
  drill: {
    id: 'd-1',
    scheduledDate: '2026-08-31',
    status: 0,
    attemptCount: 0
  },
  currentStreak: 5,
  longestStreak: 12,
  freezeCreditsRemaining: 2
}

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/today')) return JSON.parse(JSON.stringify(mockFocusData))
      throw new Error('Not found')
    }),
    post: vi.fn(async (url: string, body: any) => {
      if (url.includes('/submit')) {
        if (body.selectedOptionIndex !== undefined) {
          return {
            isCorrect: body.selectedOptionIndex === 1,
            selectedOptionIndex: body.selectedOptionIndex,
            correctOptionIndex: 1,
            score: body.selectedOptionIndex === 1 ? 10 : 0,
            explanationMarkdown: '### Architectural Breakdown\nshallowRef avoids deep proxy wrapping.',
            currentStreak: 6,
            longestStreak: 12,
            totalDrillsCompleted: 6,
            averageScore: 9.5
          }
        }
        return {
          review: {
            score: 9,
            summaryFeedback: 'Excellent architecture answer',
            strengths: ['Accurate Proxy mechanism breakdown'],
            missingPoints: [],
            improvedAnswerMarkdown: 'Principal level answer...'
          },
          currentStreak: 6,
          longestStreak: 12,
          totalDrillsCompleted: 6,
          averageScore: 9.0
        }
      }
      throw new Error('Not found')
    })
  })
}))

describe('useDailyFocusStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('fetches today focus curriculum and stores it in state', async () => {
    const focus = useDailyFocusStore()
    expect(focus.data).toBeNull()

    await focus.fetchTodayFocus()
    expect(focus.data).not.toBeNull()
    expect(focus.data?.topic.title).toBe('Vue 3 Reactivity Engine')
    expect(focus.data?.question.options).toHaveLength(4)
    expect(focus.data?.currentStreak).toBe(5)
    expect(focus.data?.documentChunk?.microQuiz?.answerIndex).toBe(0)
  })

  it('submits scenario option and updates state with score and explanation', async () => {
    const focus = useDailyFocusStore()
    await focus.fetchTodayFocus()

    const result = await focus.submitOption(1, 'en')

    expect(result.isCorrect).toBe(true)
    expect(result.score).toBe(10)
    expect(result.correctOptionIndex).toBe(1)
    expect(focus.data?.drill.status).toBe(2)
    expect(focus.data?.drill.selectedOptionIndex).toBe(1)
    expect(focus.data?.drill.isCorrect).toBe(true)
    expect(focus.data?.question.correctOptionIndex).toBe(1)
    expect(focus.data?.question.explanationMarkdown).toContain('shallowRef avoids deep proxy wrapping')
    expect(focus.data?.currentStreak).toBe(6)
  })

  it('submits legacy drill answer and updates review score in state', async () => {
    const focus = useDailyFocusStore()
    await focus.fetchTodayFocus()

    const result = await focus.submitDrill({
      answerText: 'Destructuring breaks the Proxy getter interception.',
      locale: 'en'
    })

    expect(result?.review.score).toBe(9)
    expect(focus.data?.drill.status).toBe(2)
    expect(focus.data?.drill.aiReview?.score).toBe(9)
    expect(focus.data?.currentStreak).toBe(6)
  })
})
