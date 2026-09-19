import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ConcentricMetricCard from '~/components/today/ConcentricMetricCard.vue'

describe('ConcentricMetricCard.vue', () => {
  it('renders default metrics when no props are provided', () => {
    const wrapper = mount(ConcentricMetricCard)

    expect(wrapper.find('svg').exists()).toBe(true)
    expect(wrapper.text()).toContain('0 / 10m')
    expect(wrapper.text()).toContain('100%')
  })

  it('renders customized daily goal and SM-2 retention metrics', () => {
    const wrapper = mount(ConcentricMetricCard, {
      props: {
        actualMinutes: 5,
        goalMinutes: 10,
        masteredCards: 18,
        totalCards: 20,
        dueCards: 4
      }
    })

    expect(wrapper.text()).toContain('5 / 10m')
    expect(wrapper.text()).toContain('18 / 20')
    expect(wrapper.text()).toContain('90%')
    expect(wrapper.text()).toContain('4')
  })

  it('computes correct SVG stroke-dashoffset values', () => {
    const wrapper = mount(ConcentricMetricCard, {
      props: {
        actualMinutes: 10,
        goalMinutes: 10,
        masteredCards: 20,
        totalCards: 20
      }
    })

    const circles = wrapper.find('svg.-rotate-90').findAll('circle')
    // Outer and inner animated circles (index 1 and index 3)
    expect(circles.length).toBe(4)

    // At 100% progress, dashoffset should be 0
    const outerProgressCircle = circles[1]
    expect(outerProgressCircle?.attributes('stroke-dashoffset')).toBe('0')

    const innerProgressCircle = circles[3]
    expect(innerProgressCircle?.attributes('stroke-dashoffset')).toBe('0')
  })

  it('handles zero goal and zero cards without NaN errors', () => {
    const wrapper = mount(ConcentricMetricCard, {
      props: {
        actualMinutes: 0,
        goalMinutes: 0,
        masteredCards: 0,
        totalCards: 0,
        dueCards: 0
      }
    })

    expect(wrapper.text()).toContain('100%')
    expect(wrapper.text()).not.toContain('NaN')
  })

  it('renders exactly one review deck navigation link in the footer without duplicate header link', () => {
    const wrapper = mount(ConcentricMetricCard, {
      props: {
        dueCards: 0
      },
      global: {
        stubs: {
          NuxtLink: {
            template: '<a :href="to" class="nuxt-link-stub"><slot /></a>',
            props: ['to']
          }
        }
      }
    })

    const links = wrapper.findAll('a[href="/review"]')
    expect(links.length).toBe(1)
    expect(links[0]?.text()).toContain('dashboard.view_deck')
  })

  it('renders review_now action button in footer when cards are due', () => {
    const wrapper = mount(ConcentricMetricCard, {
      props: {
        dueCards: 5
      },
      global: {
        stubs: {
          NuxtLink: {
            template: '<a :href="to" class="nuxt-link-stub"><slot /></a>',
            props: ['to']
          }
        }
      }
    })

    const links = wrapper.findAll('a[href="/review"]')
    expect(links.length).toBe(1)
    expect(links[0]?.text()).toContain('dashboard.review_now')
  })
})
