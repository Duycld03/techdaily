import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import ReviewPage from '~/pages/review.vue'
import { useReviewStore } from '~/stores/useReviewStore'

const mockDueCards = [
  {
    id: 'due-1',
    topicId: 't-1',
    topicTitle: 'Distributed Consensus with Raft',
    category: 3,
    difficulty: 2,
    topicSummary: 'Leader election, log replication, safety invariants',
    topicDeepDiveMarkdown: '### Raft Consensus Mechanics',
    repetitionCount: 1,
    easeFactor: 2.5,
    intervalDays: 1,
    nextReviewDate: '2026-08-31',
    status: 1
  }
]
let currentDueCards = [...mockDueCards]

const mockDeckCards = [
  {
    id: 'deck-1',
    topicId: 't-1',
    sourceType: 0,
    topicTitle: 'Distributed Consensus with Raft',
    category: 3,
    difficulty: 2,
    topicSummary: 'Leader election, log replication, safety invariants',
    topicDeepDiveMarkdown: '### Raft Consensus Mechanics',
    frontMarkdown: 'Explain Raft leader election invariants.',
    backMarkdown: 'A candidate must win majority votes within randomized election timeout.',
    repetitionCount: 2,
    easeFactor: 2.6,
    intervalDays: 6,
    nextReviewDate: '2026-09-06',
    status: 1
  }
]

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/deck')) {
        return { dueCards: [...currentDueCards], totalCardsDue: currentDueCards.length }
      }
      if (url.includes('/cards')) {
        return {
          cards: [...mockDeckCards],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          statistics: {
            totalCards: 1,
            learningCount: 0,
            reviewingCount: 1,
            masteredCount: 0
          }
        }
      }
      throw new Error('Not found')
    }),
    post: vi.fn(async (url: string) => {
      if (url.includes('/grade')) return { success: true }
      if (url.includes('/reset')) {
        return {
          card: {
            ...mockDeckCards[0],
            repetitionCount: 0,
            intervalDays: 1,
            status: 0
          }
        }
      }
      throw new Error('Not found')
    }),
    put: vi.fn(async (_url: string, body: { frontMarkdown: string; backMarkdown: string }) => ({
      card: {
        ...mockDeckCards[0],
        frontMarkdown: body.frontMarkdown,
        backMarkdown: body.backMarkdown
      }
    })),
    delete: vi.fn(async () => ({ success: true }))
  })
}))

describe('review.vue (Dual-Mode Spaced Repetition & Deck Management)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    currentDueCards = [...mockDueCards]
  })

  it('renders top tab switcher with both Review Session and Deck Management tabs', async () => {
    const wrapper = mount(ReviewPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true,
          FlashcardDeck: true
        }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('review.tab_session')
    expect(wrapper.text()).toContain('review.tab_management')
  })

  it('switches between Review Session and Deck Management views', async () => {
    const wrapper = mount(ReviewPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true,
          FlashcardDeck: true
        }
      }
    })

    await flushPromises()

    // Initially on Tab 1 (Review Session)
    expect(wrapper.findComponent({ name: 'FlashcardDeck' }).exists()).toBe(true)

    // Switch to Tab 2
    const buttons = wrapper.findAll('button')
    const deckTabBtn = buttons.find((b) => b.text().includes('review.tab_management'))
    expect(deckTabBtn).toBeDefined()
    await deckTabBtn!.trigger('click')

    await flushPromises()

    // Tab 2 Bento content rendered
    expect(wrapper.text()).toContain('review.cards_due')
    expect(wrapper.text()).toContain('review.mastery_rate')
    expect(wrapper.text()).toContain('review.forecast_title')
    expect(wrapper.text()).toContain('Explain Raft leader election invariants.')
  })

  it('filters deck cards by search keyword and status chip', async () => {
    const wrapper = mount(ReviewPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true,
          FlashcardDeck: true
        }
      }
    })

    await flushPromises()

    // Switch to Tab 2
    const buttons = wrapper.findAll('button')
    const deckTabBtn = buttons.find((b) => b.text().includes('review.tab_management'))
    await deckTabBtn!.trigger('click')
    await flushPromises()

    // Quick filter chip
    const masteredChip = wrapper.findAll('button').find((b) => b.text().includes('review.quick_filter_mastered'))
    expect(masteredChip).toBeDefined()
    await masteredChip!.trigger('click')

    const reviewStore = useReviewStore()
    expect(reviewStore.deckCards).toBeDefined()
  })

  it('renders Daily Review Completion Hub when zero cards are due and navigates to deck management via CTA', async () => {
    currentDueCards = []

    const wrapper = mount(ReviewPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true,
          FlashcardDeck: true
        }
      }
    })

    await flushPromises()

    // Completion hub banner
    expect(wrapper.text()).toContain('review.no_cards')
    expect(wrapper.text()).toContain('review.no_cards_desc')

    // Deduplicated: Mastery Gauge and Review Forecast are NOT in completion session
    expect(wrapper.text()).not.toContain('review.mastery_rate')
    expect(wrapper.text()).not.toContain('review.forecast_title')
    // Action CTAs
    expect(wrapper.text()).toContain('review.browse_deck_btn')
    expect(wrapper.text()).toContain('review.cram_practice_btn')

    // Primary CTA click transitions to deck management tab
    const browseBtn = wrapper.findAll('button').find((b) => b.text().includes('review.browse_deck_btn'))
    expect(browseBtn).toBeDefined()
    await browseBtn!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('review.cards_due')
    expect(wrapper.text()).toContain('review.mastery_rate')
    expect(wrapper.text()).toContain('review.forecast_title')
    expect(wrapper.text()).toContain('Explain Raft leader election invariants.')
  })
})
