import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useInterviewQuizStore } from '~/stores/useInterviewQuizStore'

vi.mock('~/composables/useToast', () => ({
  useToast: () => ({
    success: vi.fn(),
    error: vi.fn(),
    info: vi.fn()
  })
}))

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/api/v1/quiz/review-queue')) {
        return {
          questions: [
            {
              id: 'q-review-1',
              topic: '.NET Memory',
              category: 1,
              level: 3,
              questionText: 'Explain GC Gen 2 latency',
              options: ['A. Span', 'B. Heap', 'C. Lock', 'D. None'],
              correctOptionIndex: 0,
              explanationMarkdown: 'Deep breakdown',
              tags: ['dotnet'],
              isMastered: false,
              correctCount: 0,
              incorrectCount: 2
            }
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20
        }
      }
      if (url.includes('/api/v1/quiz/stats')) {
        return {
          totalAnswered: 10,
          masteredCount: 8,
          reviewQueueCount: 2,
          accuracyRate: 80.0,
          levelBreakdown: [
            { level: 3, answeredCount: 10, masteredCount: 8, accuracyRate: 80.0 }
          ],
          topicBreakdown: [
            { topic: '.NET Memory', answeredCount: 10, masteredCount: 8, accuracyRate: 80.0 }
          ]
        }
      }
      throw new Error('Unknown endpoint')
    }),
    post: vi.fn(async (url: string, body: any) => {
      if (url.includes('/api/v1/quiz/generate')) {
        return {
          questions: [
            {
              id: 'q-1',
              topic: body.topic || '.NET Memory',
              category: 1,
              level: body.level || 3,
              questionText: 'How to eliminate GC Gen 2 pauses?',
              options: [
                'A. Use ArrayPool and stack-allocated Spans',
                'B. Allocate repeatedly in tight loops',
                'C. Use a global sync lock',
                'D. Disable garbage collection'
              ],
              correctOptionIndex: 0,
              explanationMarkdown: 'ArrayPool avoids Gen 2 promotions.',
              tags: ['csharp', 'memory'],
              isMastered: false,
              correctCount: 0,
              incorrectCount: 0
            },
            {
              id: 'q-2',
              topic: body.topic || '.NET Memory',
              category: 1,
              level: body.level || 3,
              questionText: 'What is the Large Object Heap threshold?',
              options: [
                'A. 85,000 bytes',
                'B. 1,024 bytes',
                'C. 64 KB',
                'D. 1 MB'
              ],
              correctOptionIndex: 0,
              explanationMarkdown: '85,000 bytes is the default LOH threshold.',
              tags: ['csharp', 'loh'],
              isMastered: false,
              correctCount: 0,
              incorrectCount: 0
            }
          ],
          topic: body.topic || '.NET Memory',
          level: body.level || 3,
          totalCount: 2
        }
      }
      if (url.includes('/api/v1/quiz/submit')) {
        const isCorrect = body.selectedOptionIndex === 0
        return {
          isCorrect,
          correctOptionIndex: 0,
          explanationMarkdown: 'Detailed evaluation explanation',
          isMastered: isCorrect,
          correctCount: isCorrect ? 1 : 0,
          incorrectCount: isCorrect ? 0 : 1
        }
      }
      throw new Error('Unknown endpoint')
    })
  })
}))

describe('useInterviewQuizStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('initializes with default state', () => {
    const store = useInterviewQuizStore()
    expect(store.questions.length).toBe(0)
    expect(store.activeTab).toBe('generate')
    expect(store.currentIndex).toBe(0)
    expect(store.currentQuestion).toBeNull()
  })

  it('generates quiz successfully and transitions to arena tab', async () => {
    const store = useInterviewQuizStore()
    await store.generateQuiz('.NET Memory', 3, 2)

    expect(store.questions.length).toBe(2)
    expect(store.activeTab).toBe('arena')
    expect(store.currentIndex).toBe(0)
    expect(store.currentQuestion?.id).toBe('q-1')
    expect(store.progressPercentage).toBe(50)
  })

  it('submits correct answer and marks question as mastered', async () => {
    const store = useInterviewQuizStore()
    await store.generateQuiz('.NET Memory', 3, 2)

    const result = await store.submitAnswer('q-1', 0)
    expect(result?.isCorrect).toBe(true)
    expect(result?.isMastered).toBe(true)
    expect(store.isCurrentAnswered).toBe(true)
    expect(store.questions[0].isMastered).toBe(true)
  })

  it('submits incorrect answer and keeps question unmastered', async () => {
    const store = useInterviewQuizStore()
    await store.generateQuiz('.NET Memory', 3, 2)

    const result = await store.submitAnswer('q-1', 1)
    expect(result?.isCorrect).toBe(false)
    expect(result?.isMastered).toBe(false)
    expect(store.questions[0].isMastered).toBe(false)
    expect(store.questions[0].incorrectCount).toBe(1)
  })

  it('navigates next and prev question, transitions to summary on finish', async () => {
    const store = useInterviewQuizStore()
    await store.generateQuiz('.NET Memory', 3, 2)

    expect(store.currentIndex).toBe(0)
    store.nextQuestion()
    expect(store.currentIndex).toBe(1)

    store.nextQuestion()
    expect(store.activeTab).toBe('summary')

    store.prevQuestion()
    expect(store.currentIndex).toBe(0)
  })

  it('fetches review queue and statistics', async () => {
    const store = useInterviewQuizStore()
    await store.fetchReviewQueue()
    expect(store.reviewQueue.length).toBe(1)
    expect(store.reviewQueueTotal).toBe(1)

    await store.fetchStats()
    expect(store.stats?.totalAnswered).toBe(10)
    expect(store.stats?.accuracyRate).toBe(80.0)
  })
})
