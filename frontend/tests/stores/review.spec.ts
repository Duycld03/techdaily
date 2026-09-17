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
      if (url.includes('/cards')) {
        return {
          cards: [...mockCards],
          totalCount: 2,
          page: 1,
          pageSize: 20,
          totalPages: 1,
          statistics: {
            totalCards: 2,
            learningCount: 1,
            reviewingCount: 1,
            masteredCount: 0
          }
        }
      }
      throw new Error('Not found')
    }),
    post: vi.fn(async (url: string, _body?: unknown) => {
      if (url.includes('/grade')) {
        return {
          cardId: 'c-101',
          nextReviewDate: '2026-09-06',
          newIntervalDays: 6,
          newEaseFactor: 2.6,
          newRepetitionCount: 2
        }
      }
      if (url.includes('/from-highlight')) {
        return {
          id: 'c-h1',
          cardId: 'c-h1',
          front: 'Synthesized Question?',
          back: 'Synthesized Explanation'
        }
      }
      if (url.includes('/from-quiz-mistake')) {
        return {
          id: 'c-q1',
          cardId: 'c-q1'
        }
      }
      if (url.includes('/reset')) {
        return {
          card: {
            ...mockCards[0],
            repetitionCount: 0,
            intervalDays: 1,
            easeFactor: 2.5,
            status: 0
          }
        }
      }
      throw new Error('Not found')
    }),
    put: vi.fn(async (url: string, body: { frontMarkdown: string; backMarkdown: string }) => {
      if (url.includes('/cards/')) {
        return {
          card: {
            ...mockCards[0],
            frontMarkdown: body.frontMarkdown,
            backMarkdown: body.backMarkdown
          }
        }
      }
      throw new Error('Not found')
    }),
    delete: vi.fn(async (_url: string) => {
      return { success: true }
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
    expect(review.cards[0]?.topicTitle).toBe('Vue 3 Reactivity Engine')
    expect(review.totalCardsDue).toBe(2)
  })

  it('removes graded card from active deck on grading', async () => {
    const review = useReviewStore()
    await review.fetchReviewDeck()
    expect(review.cards).toHaveLength(2)

    await review.gradeCard('c-101', 5)
    expect(review.cards).toHaveLength(1)
    expect(review.cards[0]?.id).toBe('c-102')
  })

  it('creates flashcard from user highlight', async () => {
    const review = useReviewStore()
    const result = await review.createCardFromHighlight('h-123', 'en')
    expect(result).toBeDefined()
    expect(result.cardId).toBe('c-h1')
  })

  it('creates flashcard from quiz mistake', async () => {
    const review = useReviewStore()
    const result = await review.createCardFromQuizMistake('q-456')
    expect(result).toBeDefined()
    expect(result.cardId).toBe('c-q1')
  })

  it('fetches deck cards and statistics', async () => {
    const review = useReviewStore()
    expect(review.deckCards).toHaveLength(0)
    expect(review.deckStatistics.totalCards).toBe(0)

    await review.fetchDeckCards({ page: 1, pageSize: 20 })
    expect(review.deckCards).toHaveLength(2)
    expect(review.deckTotalCount).toBe(2)
    expect(review.deckTotalPages).toBe(1)
    expect(review.deckStatistics.totalCards).toBe(2)
    expect(review.deckStatistics.learningCount).toBe(1)
    expect(review.deckStatistics.reviewingCount).toBe(1)
  })

  it('updates card markdown content', async () => {
    const review = useReviewStore()
    await review.fetchDeckCards()

    const updated = await review.updateCard('c-101', {
      frontMarkdown: 'Updated Front Question',
      backMarkdown: 'Updated Back Answer'
    })

    expect(updated.frontMarkdown).toBe('Updated Front Question')
    expect(updated.backMarkdown).toBe('Updated Back Answer')
    expect(review.deckCards[0]?.frontMarkdown).toBe('Updated Front Question')
  })

  it('deletes card from deck', async () => {
    const review = useReviewStore()
    await review.fetchDeckCards()
    expect(review.deckCards).toHaveLength(2)

    await review.deleteCard('c-101')
    expect(review.deckCards).toHaveLength(1)
    expect(review.deckCards[0]?.id).toBe('c-102')
    expect(review.deckTotalCount).toBe(1)
  })

  it('resets card progression to day 1', async () => {
    const review = useReviewStore()
    await review.fetchDeckCards()

    const reset = await review.resetCardProgress('c-101')
    expect(reset.repetitionCount).toBe(0)
    expect(reset.intervalDays).toBe(1)
    expect(reset.status).toBe(0)
    expect(review.deckCards[0]?.repetitionCount).toBe(0)
  })
})
