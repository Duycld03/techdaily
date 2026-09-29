import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import FlashcardHeroCard from '~/components/review/FlashcardHeroCard.vue'
import MasteryGaugeCard from '~/components/review/MasteryGaugeCard.vue'
import ReviewForecastChart from '~/components/review/ReviewForecastChart.vue'
import AdvancedFilterModal from '~/components/review/AdvancedFilterModal.vue'
import FlashcardBentoCard from '~/components/review/FlashcardBentoCard.vue'
import AtRiskLeechCard from '~/components/review/AtRiskLeechCard.vue'
import SourceChannelRetentionCard from '~/components/review/SourceChannelRetentionCard.vue'
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

    // Assert source_topic does not exist
    const sourceTopicBtn = wrapper.findAll('button').find((b) => b.text().includes('review.source_topic'))
    expect(sourceTopicBtn).toBeUndefined()

    // Click on source chunk filter (review.source_chunk)
    const sourceChunkBtn = wrapper.findAll('button').find((b) => b.text().includes('review.source_chunk'))
    expect(sourceChunkBtn).toBeDefined()
    await sourceChunkBtn!.trigger('click')

    // Click apply button
    const applyBtn = wrapper.findAll('button').find((b) => b.text().includes('review.apply_filters'))
    expect(applyBtn).toBeDefined()
    await applyBtn!.trigger('click')

    expect(wrapper.emitted('apply')).toBeTruthy()
    const applied = wrapper.emitted('apply')![0]![0] as { sourceType: number }
    expect(applied.sourceType).toBe(3)
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

  it('renders filter buttons without checkmark icons', async () => {
    const wrapper = mount(AdvancedFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {
          status: 1,
          sourceType: 3,
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

    const selectedSourceBtn = wrapper.findAll('button').find((b) => b.text().includes('review.source_chunk'))
    expect(selectedSourceBtn?.exists()).toBe(true)

    const selectedStatusBtn = wrapper.findAll('button').find((b) => b.text().includes('review.status_reviewing'))
    expect(selectedStatusBtn?.exists()).toBe(true)

    const selectedUrgencyBtn = wrapper.findAll('button').find((b) => b.text().includes('review.urgency_due'))
    expect(selectedUrgencyBtn?.exists()).toBe(true)

    const selectedSortBtn = wrapper.findAll('button').find((b) => b.text().includes('review.sort_difficulty'))
    expect(selectedSortBtn?.exists()).toBe(true)

    const checkIcons = wrapper.findAll('.lucide-check')
    expect(checkIcons.length).toBe(0)
  })
})

describe('FlashcardBentoCard.vue', () => {
  const mockCard: ReviewCard = {
    id: 'bento-1',
    sourceType: 1, // Reading Highlight
    status: 1, // Reviewing
    category: 1,
    difficulty: 2,
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

    expect(wrapper.text()).toContain('Explain how MVCC works in PostgreSQL.')
    expect(wrapper.text()).toContain('PostgreSQL uses xmin/xmax tuple headers to determine visibility.')
    expect(wrapper.text()).toContain('review.card_urgency_due')
    expect(wrapper.text()).toContain('review.card_ef_interval')
    expect(wrapper.text()).toContain('review.card_source')
    expect(wrapper.text()).toContain('review.card_details_btn')
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

describe('AtRiskLeechCard.vue', () => {
  it('emits review when the at-risk CTA is clicked', async () => {
    const wrapper = mount(AtRiskLeechCard, {
      props: { overdueCount: 3, leechCount: 5, atRiskCount: 8 }
    })
    const btn = wrapper.find('button')
    expect(btn.exists()).toBe(true)
    await btn.trigger('click')
    expect(wrapper.emitted('review')).toBeTruthy()
  })

  it('renders overdue, leech, and at-risk counts', () => {
    const wrapper = mount(AtRiskLeechCard, {
      props: { overdueCount: 3, leechCount: 5, atRiskCount: 8 }
    })
    const text = wrapper.text()
    expect(text).toContain('3')
    expect(text).toContain('5')
    expect(text).toContain('8')
  })

  it('hides the CTA and shows the empty state when nothing is at risk', () => {
    const wrapper = mount(AtRiskLeechCard, {
      props: { overdueCount: 0, leechCount: 0, atRiskCount: 0 }
    })
    expect(wrapper.find('button').exists()).toBe(false)
    expect(wrapper.text()).toContain('review.atrisk_empty')
  })
})

describe('SourceChannelRetentionCard.vue', () => {
  const sources = [
    { sourceType: 'Highlight', total: 5, learning: 2, reviewing: 1, mastered: 2, averageEaseFactor: 2.31 },
    { sourceType: 'QuizMistake', total: 3, learning: 1, reviewing: 1, mastered: 1, averageEaseFactor: 1.85 },
    { sourceType: 'DocumentChunk', total: 2, learning: 1, reviewing: 1, mastered: 0, averageEaseFactor: 1.6 }
  ]

  it('renders mastered/total ratio and average ease per source channel', () => {
    const wrapper = mount(SourceChannelRetentionCard, { props: { sources } })
    const text = wrapper.text()
    expect(text).toContain('2/5')
    expect(text).toContain('1/3')
    expect(text).toContain('0/2')
    expect(text).toContain('2.31')
    expect(text).toContain('1.85')
    expect(text).toContain('1.60')
  })

  it('maps source type ids to the correct channel labels', () => {
    const wrapper = mount(SourceChannelRetentionCard, { props: { sources } })
    const text = wrapper.text()
    expect(text).toContain('review.source_highlight')
    expect(text).toContain('review.source_quiz')
    expect(text).toContain('review.source_drill')
  })

  it('sizes mastery bars proportional to the mastered share', () => {
    const wrapper = mount(SourceChannelRetentionCard, { props: { sources } })
    const styles = wrapper.findAll('div[style]').map((b) => b.attributes('style'))
    expect(styles).toEqual(
      expect.arrayContaining([
        expect.stringContaining('width: 40%'),
        expect.stringContaining('width: 33%')
      ])
    )
  })

  it('shows the empty state when there are no source channels', () => {
    const wrapper = mount(SourceChannelRetentionCard, { props: { sources: [] } })
    expect(wrapper.text()).toContain('review.source_empty')
    expect(wrapper.text()).not.toContain('/')
  })
})
