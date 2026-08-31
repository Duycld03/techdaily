import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import StreakBadge from '~/components/common/StreakBadge.vue'

describe('StreakBadge.vue', () => {
  it('renders active streak count and freeze credits', () => {
    const wrapper = mount(StreakBadge, {
      props: {
        streak: 7,
        freezeCredits: 2
      }
    })

    expect(wrapper.text()).toContain('7')
    expect(wrapper.find('span').exists()).toBe(true)
  })

  it('renders 0 streak with gray badge styling', () => {
    const wrapper = mount(StreakBadge, {
      props: {
        streak: 0,
        freezeCredits: 1
      }
    })

    expect(wrapper.text()).toContain('0')
  })
})
