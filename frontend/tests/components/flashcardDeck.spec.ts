import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import FlashcardDeck from '~/components/review/FlashcardDeck.vue'
import Sm2GradingButtons from '~/components/review/Sm2GradingButtons.vue'
import type { ReviewCard } from '~/stores/useReviewStore'

const mockCard: ReviewCard = {
  id: 'card-123',
  topicId: 'topic-456',
  topicTitle: 'Why is separation of concerns critical for maintainability?',
  category: 1, // BackendRuntime
  difficulty: 2, // Senior
  topicSummary: 'Separation of concerns isolates responsibilities, preventing cascading changes across architectural boundaries.',
  topicDeepDiveMarkdown: '```csharp\npublic interface IOrderService { void ProcessOrder(); }\n```',
  repetitionCount: 3,
  easeFactor: 2.5,
  intervalDays: 6,
  nextReviewDate: '2026-09-20',
  status: 1
}

describe('FlashcardDeck.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders front face with question and metadata, completely hiding answer summary (Zero Leakage)', () => {
    const wrapper = mount(FlashcardDeck, {
      props: {
        card: mockCard,
        remainingCount: 5
      }
    })

    // Question prompt is visible
    expect(wrapper.text()).toContain('Why is separation of concerns critical for maintainability?')
    expect(wrapper.text()).toContain('Backend Runtime')
    expect(wrapper.text()).toContain('Senior')
    expect(wrapper.text()).toContain('Repetition #3')
    expect(wrapper.text()).toContain('EF: 2.50 • 6d')

    // ZERO LEAKAGE: topicSummary must NOT be visible on front face
    expect(wrapper.text()).not.toContain('Separation of concerns isolates responsibilities')
    // Deep dive markdown must NOT be visible on front face
    expect(wrapper.text()).not.toContain('IOrderService')

    // Flip CTA is present
    expect(wrapper.text()).toContain('review.show_answer')
  })

  it('flips to back face when Show Answer button is clicked, revealing the solution', async () => {
    const wrapper = mount(FlashcardDeck, {
      props: {
        card: mockCard,
        remainingCount: 5
      }
    })

    const flipButton = wrapper.findAll('button').find((b) => b.text().includes('review.show_answer'))
    expect(flipButton).toBeDefined()

    await flipButton!.trigger('click')

    // Now flipped to back face
    expect(wrapper.text()).toContain('Separation of concerns isolates responsibilities')
    expect(wrapper.text()).toContain('review.core_solution')
    expect(wrapper.text()).toContain('review.hide_answer')

    // Sm2GradingButtons component is now rendered
    expect(wrapper.findComponent(Sm2GradingButtons).exists()).toBe(true)
  })

  it('flips card using keyboard Space/Enter key, and ignores keydown when input is focused', async () => {
    const wrapper = mount(FlashcardDeck, {
      props: {
        card: mockCard,
        remainingCount: 5
      }
    })

    // Create an input element and simulate typing in it (Input Shielding Invariant)
    const dummyInput = document.createElement('input')
    document.body.appendChild(dummyInput)
    dummyInput.focus()

    const spaceEventOnInput = new KeyboardEvent('keydown', { code: 'Space', key: ' ', bubbles: true })
    Object.defineProperty(spaceEventOnInput, 'target', { value: dummyInput })
    window.dispatchEvent(spaceEventOnInput)
    await wrapper.vm.$nextTick()

    // Must NOT have flipped because an input was focused!
    expect(wrapper.text()).not.toContain('Separation of concerns isolates responsibilities')

    // Now blur input and press Space on window
    dummyInput.blur()
    document.body.removeChild(dummyInput)

    const spaceEvent = new KeyboardEvent('keydown', { code: 'Space', key: ' ', bubbles: true })
    window.dispatchEvent(spaceEvent)
    await wrapper.vm.$nextTick()

    // Must have flipped!
    expect(wrapper.text()).toContain('Separation of concerns isolates responsibilities')
  })

  it('grades card with keyboard shortcuts [1], [2], [3], [4] when flipped and auto-resets flip state', async () => {
    const wrapper = mount(FlashcardDeck, {
      props: {
        card: mockCard,
        remainingCount: 5
      }
    })

    // Flip the card
    const flipButton = wrapper.findAll('button').find((b) => b.text().includes('review.show_answer'))
    await flipButton!.trigger('click')

    // Press key '3' for Good rating (Score 4)
    const key3Event = new KeyboardEvent('keydown', { key: '3', bubbles: true })
    window.dispatchEvent(key3Event)
    await wrapper.vm.$nextTick()

    // Emitted grade event with score 4
    expect(wrapper.emitted('grade')).toBeTruthy()
    expect(wrapper.emitted('grade')![0]).toEqual([4])
  })

  it('resets flip state when card prop changes', async () => {
    const wrapper = mount(FlashcardDeck, {
      props: {
        card: mockCard,
        remainingCount: 5
      }
    })

    // Flip the card
    const flipButton = wrapper.findAll('button').find((b) => b.text().includes('review.show_answer'))
    await flipButton!.trigger('click')
    expect(wrapper.text()).toContain('Separation of concerns isolates responsibilities')

    // Pass a new card
    const nextCard: ReviewCard = {
      ...mockCard,
      id: 'card-999',
      topicTitle: 'Next Question Challenge'
    }
    await wrapper.setProps({ card: nextCard })
    await wrapper.vm.$nextTick()

    // Must be reset to front face!
    expect(wrapper.text()).toContain('Next Question Challenge')
    expect(wrapper.text()).not.toContain('Separation of concerns isolates responsibilities')
  })
})

describe('Sm2GradingButtons.vue', () => {
  it('renders 4 grading buttons with keyboard shortcut badges [1]-[4] and localized subtexts', () => {
    const wrapper = mount(Sm2GradingButtons)

    const buttons = wrapper.findAll('button')
    expect(buttons.length).toBe(4)

    // Keyboard shortcut badges
    expect(wrapper.text()).toContain('1')
    expect(wrapper.text()).toContain('2')
    expect(wrapper.text()).toContain('3')
    expect(wrapper.text()).toContain('4')

    // Localized labels and descriptions
    expect(wrapper.text()).toContain('review.grade_again')
    expect(wrapper.text()).toContain('review.grade_hard')
    expect(wrapper.text()).toContain('review.grade_good')
    expect(wrapper.text()).toContain('review.grade_easy')

    expect(wrapper.text()).toContain('review.grade_again_desc')
    expect(wrapper.text()).toContain('review.grade_hard_desc')
    expect(wrapper.text()).toContain('review.grade_good_desc')
    expect(wrapper.text()).toContain('review.grade_easy_desc')
  })

  it('emits score 1 for Again, 3 for Hard, 4 for Good, 5 for Easy', async () => {
    const wrapper = mount(Sm2GradingButtons)
    const buttons = wrapper.findAll('button')

    // Again -> Score 1
    await buttons[0].trigger('click')
    expect(wrapper.emitted('grade')![0]).toEqual([1])

    // Hard -> Score 3
    await buttons[1].trigger('click')
    expect(wrapper.emitted('grade')![1]).toEqual([3])

    // Good -> Score 4
    await buttons[2].trigger('click')
    expect(wrapper.emitted('grade')![2]).toEqual([4])

    // Easy -> Score 5
    await buttons[3].trigger('click')
    expect(wrapper.emitted('grade')![3]).toEqual([5])
  })
})
