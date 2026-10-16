import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { ref } from 'vue'
import FlashcardDeck from '~/components/review/FlashcardDeck.vue'
import type { ReviewCard } from '~/stores/useReviewStore'
import FlashcardBentoCard from '~/components/review/FlashcardBentoCard.vue'

const metadata = vi.hoisted(() => ({ nameEn: 'Novel Biology', nameVi: 'Sinh học mới', id: '8f326e3a-9401-4385-b287-737487a2c717', slug: 'biology', sortOrder: 8, isActive: false, version: 2 }))
vi.mock('~/stores/useCategoryStore', () => ({ useCategoryStore: () => ({
  ensureCategories: vi.fn().mockResolvedValue(undefined),
  getCategoryName: (value: unknown, locale: string) => value ? (locale === 'vi' ? metadata.nameVi : metadata.nameEn) : ''
}) }))
vi.mock('~/composables/useMarkdownRenderer', () => ({ useMarkdownRenderer: () => ({ render: (text: string) => text, isHighlighterReady: ref(true) }) }))
const card: ReviewCard = { id: 'card', categoryId: metadata.id, category: metadata, difficulty: 1, frontMarkdown: 'Explain cellular transport', backMarkdown: 'Answer', repetitionCount: 2, easeFactor: 2.5, intervalDays: 4, nextReviewDate: '2026-10-17', status: 1 }
describe('review category provenance', () => {
  beforeEach(() => setActivePinia(createPinia()))
  afterEach(() => vi.unstubAllGlobals())
  it('shows a novel retained inactive category rather than a seeded subject', () => {
    const wrapper = mount(FlashcardDeck, { props: { card, remainingCount: 1 } })
    expect(wrapper.text()).toContain('Novel Biology')
    expect(wrapper.text()).not.toContain('Frontend & Browser')
  })
  it('shows localized Unclassified for null provenance without replacing the identity', () => {
    const orphan = { ...card, sourceType: null, categoryId: null, category: null }
    const wrapper = mount(FlashcardDeck, { props: { card: orphan, remainingCount: 1 } })
    expect(wrapper.text()).toContain('review.unclassified')
    expect(wrapper.props('card').categoryId).toBeNull()
    expect(wrapper.text()).not.toContain('Frontend & Browser')
    expect(wrapper.props('card').sourceType).toBeNull()
    expect(wrapper.text()).not.toContain('review.source_monograph')
  })
  it('shows category provenance in management cards too', () => {
    const wrapper = mount(FlashcardBentoCard, { props: { card } })
    expect(wrapper.text()).toContain('Novel Biology')
  })
  it('renders the current Vietnamese descriptor without changing category identity', () => {
    vi.stubGlobal('useI18n', () => ({ locale: ref('vi'), t: (key: string) => key }))
    const wrapper = mount(FlashcardDeck, { props: { card, remainingCount: 1 } })
    expect(wrapper.text()).toContain('Sinh học mới')
    expect(wrapper.props('card').categoryId).toBe(metadata.id)
  })
})
