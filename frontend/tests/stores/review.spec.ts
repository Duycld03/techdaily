import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useReviewStore } from '~/stores/useReviewStore'

const mockCards = [
  {
    id: 'c-101',
    topicId: 't-1',
    topicTitle: 'Vue 3 Reactivity Engine',
    category: 0,
    difficulty: 1,
    topicSummary: 'Proxy vs Object.defineProperty',
    topicDeepDiveMarkdown: '### Deep Dive Content',
    repetitionCount: 1,
    easeFactor: 2.5,
    intervalDays: 1,
    nextReviewDate: '2026-08-31',
    status: 1
  },
  {
    id: 'c-102',
    topicId: 't-2',
    topicTitle: 'PostgreSQL MVCC & VACUUM',
    category: 2,
    difficulty: 2,
    topicSummary: 'Multi-version concurrency control mechanics',
    topicDeepDiveMarkdown: '### Deep Dive MVCC',
    repetitionCount: 2,
    easeFactor: 2.36,
    intervalDays: 6,
    nextReviewDate: '2026-08-31',
    status: 1
  }
]

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/deck')) return { dueCards: [...mockCards], totalCardsDue: 2 }
      throw new Error('Not found')
    }),
    post: vi.fn(async (url: string, body: any) => {
      if (url.includes('/grade')) {
        return {
          cardId: 'c-101',
          nextReviewDate: '2026-09-06',
          newIntervalDays: 6,
          newEaseFactor: 2.6,
          newRepetitionCount: 2
        }
      }
      throw new Error('Not found')
    })
  })
}))

describe('useReviewStore (SM-2 Spaced Repetition)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('fetches review deck correctly', async () => {
    const review = useReviewStore()
    expect(review.cards).toHaveLength(0)

    await review.fetchReviewDeck()
    expect(review.cards).toHaveLength(2)
    expect(review.cards[0].topicTitle).toBe('Vue 3 Reactivity Engine')
    expect(review.totalCardsDue).toBe(2)
  })

  it('removes graded card from active deck on grading', async () => {
    const review = useReviewStore()
    await review.fetchReviewDeck()
    expect(review.cards).toHaveLength(2)

    await review.gradeCard('c-101', 5)
    expect(review.cards).toHaveLength(1)
    expect(review.cards[0].id).toBe('c-102')
  })
})
