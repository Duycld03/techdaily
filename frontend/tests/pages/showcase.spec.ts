import { describe, it, expect, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import ShowcasePage from '~/pages/showcase.vue'
import LayoutArchetypesShowcase from '~/components/showcase/LayoutArchetypesShowcase.vue'
import PaginationShowcase from '~/components/showcase/PaginationShowcase.vue'
import Sm2AssessmentShowcase from '~/components/showcase/Sm2AssessmentShowcase.vue'
import RetentionAnalyticsShowcase from '~/components/showcase/RetentionAnalyticsShowcase.vue'

describe('Design System Showcase Page & Archetypes', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders standardized page header banner and quick-jump navigation', () => {
    const wrapper = mount(ShowcasePage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true,
          PrimitivesShowcase: true,
          PaginationShowcase: true,
          Sm2AssessmentShowcase: true,
          RetentionAnalyticsShowcase: true,
          LayoutArchetypesShowcase: true,
          IconShowcase: true
        }
      }
    })

    // Header title and version badge
    expect(wrapper.text()).toContain('showcase.title')
    expect(wrapper.text()).toContain('showcase.version_badge')

    // Quick-jump anchors
    const anchorHrefs = wrapper.findAll('nav a').map((a) => a.attributes('href'))
    expect(anchorHrefs).toContain('#primitives')
    expect(anchorHrefs).toContain('#pagination')
    expect(anchorHrefs).toContain('#sm2')
    expect(anchorHrefs).toContain('#retention')
    expect(anchorHrefs).toContain('#archetypes')
    expect(anchorHrefs).toContain('#feedback')
    expect(anchorHrefs).toContain('#icons')
  })

  it('renders PaginationShowcase with BasePagination and updates active page', async () => {
    const wrapper = mount(PaginationShowcase, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('Standard 1-Based Pagination')
    expect(wrapper.text()).toContain('Compact Variant')
    expect(wrapper.findAllComponents({ name: 'BasePagination' }).length).toBeGreaterThanOrEqual(2)
  })

  it('renders Sm2AssessmentShowcase with Sm2GradingButtons and FlashcardBentoCards', () => {
    const wrapper = mount(Sm2AssessmentShowcase, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('Sm2GradingButtons')
    expect(wrapper.text()).toContain('Flashcard Inventory Cards')
    expect(wrapper.findComponent({ name: 'Sm2GradingButtons' }).exists()).toBe(true)
    expect(wrapper.findAllComponents({ name: 'FlashcardBentoCard' }).length).toBe(3)
  })

  it('renders RetentionAnalyticsShowcase with all 5 bento retention cards', () => {
    const wrapper = mount(RetentionAnalyticsShowcase, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.findComponent({ name: 'MasteryGaugeCard' }).exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'ReviewForecastChart' }).exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'AtRiskLeechCard' }).exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'SourceChannelRetentionCard' }).exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'EaseFactorDistributionCard' }).exists()).toBe(true)
  })

  it('supports BentoDashboardLayout in LayoutArchetypesShowcase', async () => {
    const wrapper = mount(LayoutArchetypesShowcase, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    // Archetype switcher should include Bento Dashboard button
    const bentoBtn = wrapper.findAll('button').find((b) => b.text().includes('Bento Dashboard'))
    expect(bentoBtn).toBeDefined()

    // Click Bento Dashboard button
    await bentoBtn!.trigger('click')
    await flushPromises()

    expect(wrapper.findComponent({ name: 'BentoDashboardLayout' }).exists()).toBe(true)
    expect(wrapper.text()).toContain('Executive Cockpit')
    expect(wrapper.text()).toContain('Primary Action Stage (2 Cols)')
    expect(wrapper.text()).toContain('Telemetry Dock (1 Col)')
  })
})
