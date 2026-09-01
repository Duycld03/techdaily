import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import InterviewChallengePane from '~/components/today/InterviewChallengePane.vue'

vi.mock('canvas-confetti', () => ({
  default: vi.fn()
}))

vi.mock('~/composables/useMarkdownRenderer', () => ({
  useMarkdownRenderer: () => ({
    render: vi.fn((md: string) => `<div class="rendered-markdown">${md}</div>`),
    isHighlighterReady: ref(true)
  })
}))

const mockQuestion = {
  id: 'q-101',
  questionText: 'When designing Redis cache for concurrent bursts, which approach avoids race conditions?',
  options: [
    'Use GET then SET with TTL',
    'Use Redis SETNX (When.NotExists) with TTL before execution',
    'Execute handler first then SET',
    'Acquire table lock'
  ],
  expectedKeyPoints: [],
  modelAnswerMarkdown: '',
  difficulty: 2
}

const mockPendingDrill = {
  id: 'd-101',
  scheduledDate: '2026-09-01',
  status: 0,
  attemptCount: 0
}

const mockReviewedDrill = {
  id: 'd-101',
  scheduledDate: '2026-09-01',
  status: 2,
  selectedOptionIndex: 1,
  isCorrect: true,
  score: 10,
  attemptCount: 1
}

describe('InterviewChallengePane.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders question and 4 scenario options when pending', () => {
    const wrapper = mount(InterviewChallengePane, {
      props: {
        question: mockQuestion,
        drill: mockPendingDrill
      },
      global: {
        mocks: {
          $t: (key: string) => key
        },
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('When designing Redis cache for concurrent bursts')
    expect(wrapper.text()).toContain('Use Redis SETNX (When.NotExists)')
    expect(wrapper.findAll('button[type="button"]').length).toBeGreaterThanOrEqual(4)
  })

  it('highlights option when clicked', async () => {
    const wrapper = mount(InterviewChallengePane, {
      props: {
        question: mockQuestion,
        drill: mockPendingDrill
      },
      global: {
        mocks: {
          $t: (key: string) => key
        },
        stubs: {
          NuxtLink: true
        }
      }
    })

    const optionButtons = wrapper.findAll('button[type="button"]')
    await optionButtons[1].trigger('click')

    expect(wrapper.find('.ring-brand-500\\/30').exists()).toBe(true)
  })

  it('renders review state and explanation when drill is reviewed', () => {
    const reviewedQuestion = {
      ...mockQuestion,
      correctOptionIndex: 1,
      explanationMarkdown: '### Architectural Breakdown\nSETNX provides atomic lock.'
    }

    const wrapper = mount(InterviewChallengePane, {
      props: {
        question: reviewedQuestion,
        drill: mockReviewedDrill
      },
      global: {
        mocks: {
          $t: (key: string) => key
        },
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('+10 Pts')
    expect(wrapper.text()).toContain('today.optimal_choice')
    expect(wrapper.text()).toContain('SETNX provides atomic lock')
  })
})
