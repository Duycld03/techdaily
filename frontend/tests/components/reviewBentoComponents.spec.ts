import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import FlashcardHeroCard from '~/components/review/FlashcardHeroCard.vue'
import MasteryGaugeCard from '~/components/review/MasteryGaugeCard.vue'
import ReviewForecastChart from '~/components/review/ReviewForecastChart.vue'
import AdvancedFilterModal from '~/components/review/AdvancedFilterModal.vue'
import FlashcardBentoCard from '~/components/review/FlashcardBentoCard.vue'
import type { ReviewCard } from '~/stores/useReviewStore'

describe('FlashcardHeroCard.vue', () => {
  it('displays due count and calculates dynamic study duration', () => {
    const wrapper = mount(FlashcardHeroCard, {
      props: {
        dueCount: 10
      }
    })

    expect(wrapper.text()).toContain('10')
    // 10 * 0.5 = 5 min
    expect(wrapper.text()).toContain('5 min')
  })

  it('calculates minimum 1 minute when due count is 1', () => {
    const wrapper = mount(FlashcardHeroCard, {
      props: {
        dueCount: 1
      }
    })

    expect(wrapper.text()).toContain('1')
    expect(wrapper.text()).toContain('1 min')
  })

  it('emits startReview when CTA button is clicked', async () => {
    const wrapper = mount(FlashcardHeroCard, {
      props: {
        dueCount: 5
      }
    })

    const button = wrapper.find('button')
    await button.trigger('click')

    expect(wrapper.emitted('startReview')).toBeTruthy()
    expect(wrapper.emitted('startReview')!.length).toBe(1)
  })
})

describe('MasteryGaugeCard.vue', () => {
  it('calculates mastery rate of 0% when total is 0', () => {
    const wrapper = mount(MasteryGaugeCard, {
      props: {
        masteredCount: 0,
        totalCount: 0
      }
    })

    expect(wrapper.text()).toContain('0%')
    expect(wrapper.text()).toContain('review.tier_starting')
  })

  it('calculates 50% mastery rate and shows building reflexes tier', () => {
    const wrapper = mount(MasteryGaugeCard, {
      props: {
        masteredCount: 5,
        totalCount: 10
      }
    })

    expect(wrapper.text()).toContain('50%')
    expect(wrapper.text()).toContain('review.tier_building')
  })

  it('calculates 70% mastery rate and shows solid progress tier', () => {
    const wrapper = mount(MasteryGaugeCard, {
      props: {
        masteredCount: 7,
        totalCount: 10
      }
    })

    expect(wrapper.text()).toContain('70%')
    expect(wrapper.text()).toContain('review.tier_solid')
  })

  it('calculates 100% mastery rate and shows mastery excellence tier', () => {
    const wrapper = mount(MasteryGaugeCard, {
      props: {
        masteredCount: 10,
        totalCount: 10
      }
    })

    expect(wrapper.text()).toContain('100%')
    expect(wrapper.text()).toContain('review.tier_mastered')
  })

  it('renders SVG semi-circular arc with stroke-dashoffset', () => {
    const wrapper = mount(MasteryGaugeCard, {
      props: {
        masteredCount: 5,
        totalCount: 10
      }
    })
    const gaugeSvg = wrapper.find('svg[aria-label="Mastery rate gauge"]')
    const paths = gaugeSvg.findAll('path')
    expect(paths.length).toBe(2) // background arc and value arc
    const valueArc = paths[1]
    expect(valueArc).toBeDefined()
    expect(valueArc!.attributes('stroke-dasharray')).toBe('141.37')
    // 50% of 141.37 ≈ 70.685
    const offset = parseFloat(valueArc!.attributes('stroke-dashoffset') || '0')
    expect(Math.round(offset)).toBe(71)
  })
})

describe('ReviewForecastChart.vue', () => {
  it('computes 7-day forecast correctly from card dates', () => {
    const todayStr = new Date().toISOString().slice(0, 10)
    const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10)

    const mockCards: ReviewCard[] = [
      {
        id: '1',
        topicTitle: 'Topic 1',
        category: 0,
        difficulty: 1,
        topicSummary: 'Summary 1',
        topicDeepDiveMarkdown: 'Deep dive 1',
        repetitionCount: 1,
        easeFactor: 2.5,
        intervalDays: 1,
        nextReviewDate: todayStr,
        status: 1
      },
      {
        id: '2',
        topicTitle: 'Topic 2',
        category: 0,
        difficulty: 1,
        topicSummary: 'Summary 2',
        topicDeepDiveMarkdown: 'Deep dive 2',
        repetitionCount: 2,
        easeFactor: 2.6,
        intervalDays: 2,
        nextReviewDate: tomorrow,
        status: 1
      }
    ]

    const wrapper = mount(ReviewForecastChart, {
      props: {
        cards: mockCards
      }
    })

    expect(wrapper.text()).toContain('review.forecast_title')
    expect(wrapper.text()).toContain('2') // total upcoming
    expect(wrapper.text()).toContain('review.today_badge')
  })
})

describe('AdvancedFilterModal.vue', () => {
  it('emits apply when filters are selected and apply clicked', async () => {
    const wrapper = mount(AdvancedFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {
          status: null,
          sourceType: null,
          urgency: null,
          sortBy: null
        }
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })

    // Click on a source filter (e.g. source_topic)
    const sourceTopicBtn = wrapper.findAll('button').find((b) => b.text().includes('review.source_topic'))
    expect(sourceTopicBtn).toBeDefined()
    await sourceTopicBtn!.trigger('click')

    // Click apply button
    const applyBtn = wrapper.findAll('button').find((b) => b.text().includes('review.apply_filters'))
    expect(applyBtn).toBeDefined()
    await applyBtn!.trigger('click')

    expect(wrapper.emitted('apply')).toBeTruthy()
    const applied = wrapper.emitted('apply')![0]![0] as { sourceType: number }
    expect(applied.sourceType).toBe(0)
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('emits reset when reset button is clicked', async () => {
    const wrapper = mount(AdvancedFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {
          status: 1,
          sourceType: 0,
          urgency: 'due',
          sortBy: 'difficulty'
        }
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })

    const resetBtn = wrapper.findAll('button').find((b) => b.text().includes('review.reset_filters'))
    expect(resetBtn).toBeDefined()
    await resetBtn!.trigger('click')

    expect(wrapper.emitted('reset')).toBeTruthy()
  })

  it('applies uniform brand active styling, whitespace-nowrap, and no checkmark icons on filter buttons', async () => {
    const wrapper = mount(AdvancedFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {
          status: 1,
          sourceType: 0,
          urgency: 'due',
          sortBy: 'difficulty'
        }
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })

    const selectedSourceBtn = wrapper.findAll('button').find((b) => b.text().includes('review.source_topic'))
    expect(selectedSourceBtn?.classes()).toContain('bg-brand-600')
    expect(selectedSourceBtn?.classes()).toContain('font-bold')
    expect(selectedSourceBtn?.classes()).toContain('whitespace-nowrap')

    const selectedStatusBtn = wrapper.findAll('button').find((b) => b.text().includes('review.status_reviewing'))
    expect(selectedStatusBtn?.classes()).toContain('bg-brand-600')
    expect(selectedStatusBtn?.classes()).toContain('font-bold')
    expect(selectedStatusBtn?.classes()).toContain('whitespace-nowrap')

    const selectedUrgencyBtn = wrapper.findAll('button').find((b) => b.text().includes('review.urgency_due'))
    expect(selectedUrgencyBtn?.classes()).toContain('bg-brand-600')
    expect(selectedUrgencyBtn?.classes()).toContain('font-bold')
    expect(selectedUrgencyBtn?.classes()).toContain('whitespace-nowrap')

    const selectedSortBtn = wrapper.findAll('button').find((b) => b.text().includes('review.sort_difficulty'))
    expect(selectedSortBtn?.classes()).toContain('bg-brand-600')
    expect(selectedSortBtn?.classes()).toContain('font-bold')
    expect(selectedSortBtn?.classes()).toContain('whitespace-nowrap')

    const checkIcons = wrapper.findAll('.lucide-check')
    expect(checkIcons.length).toBe(0)
  })
})

describe('FlashcardBentoCard.vue', () => {
  const mockCard: ReviewCard = {
    id: 'bento-1',
    sourceType: 1, // Reading Highlight
    status: 1, // Reviewing
    topicTitle: 'PostgreSQL MVCC & VACUUM',
    category: 1,
    difficulty: 2,
    topicSummary: 'How PostgreSQL handles multi-version concurrency control.',
    topicDeepDiveMarkdown: 'Deep dive markdown',
    frontMarkdown: 'Explain how MVCC works in PostgreSQL.',
    backMarkdown: 'PostgreSQL uses xmin/xmax tuple headers to determine visibility.',
    repetitionCount: 3,
    easeFactor: 2.65,
    intervalDays: 7,
    nextReviewDate: new Date().toISOString().slice(0, 10)
  }

  it('renders badges, question prompt, and SM-2 metrics', () => {
    const wrapper = mount(FlashcardBentoCard, {
      props: {
        card: mockCard
      }
    })

    expect(wrapper.text()).toContain('review.source_highlight')
    expect(wrapper.text()).toContain('review.status_reviewing')
    expect(wrapper.text()).toContain('review.due_today')
    expect(wrapper.text()).toContain('Explain how MVCC works in PostgreSQL.')
    expect(wrapper.text()).toContain('review.repetitions')
    expect(wrapper.text()).toContain('review.interval_days')
    expect(wrapper.text()).toContain('review.ease_factor')
  })

  it('toggles accordion answer on button click', async () => {
    const wrapper = mount(FlashcardBentoCard, {
      props: {
        card: mockCard
      }
    })

    expect(wrapper.text()).toContain('review.show_answer')
    expect(wrapper.text()).not.toContain('PostgreSQL uses xmin/xmax tuple headers')

    // Click show answer
    const toggleBtn = wrapper.findAll('button').find((b) => b.text().includes('review.show_answer'))
    expect(toggleBtn).toBeDefined()
    await toggleBtn!.trigger('click')

    expect(wrapper.text()).toContain('review.hide_answer')
    expect(wrapper.text()).toContain('PostgreSQL uses xmin/xmax tuple headers')
  })

  it('emits edit, reset, and delete when action buttons are clicked', async () => {
    const wrapper = mount(FlashcardBentoCard, {
      props: {
        card: mockCard
      }
    })

    const buttons = wrapper.findAll('button')
    // Action buttons are pencil (edit), rotateCcw (reset), trash2 (delete)
    const editBtn = buttons.find((b) => b.attributes('title') === 'review.edit_card')
    const resetBtn = buttons.find((b) => b.attributes('title') === 'review.reset_progress')
    const deleteBtn = buttons.find((b) => b.attributes('title') === 'review.delete_card')

    expect(editBtn).toBeDefined()
    expect(resetBtn).toBeDefined()
    expect(deleteBtn).toBeDefined()

    await editBtn!.trigger('click')
    expect(wrapper.emitted('edit')?.[0]?.[0]).toEqual(mockCard)

    await resetBtn!.trigger('click')
    expect(wrapper.emitted('reset')?.[0]?.[0]).toEqual(mockCard)

    await deleteBtn!.trigger('click')
    expect(wrapper.emitted('delete')?.[0]?.[0]).toEqual(mockCard)
  })
})
