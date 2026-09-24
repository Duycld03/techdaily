import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import OptionCard from '~/components/ui/OptionCard.vue'

describe('OptionCard.vue', () => {
  it('renders default state with letter and text', () => {
    const wrapper = mount(OptionCard, {
      props: {
        letter: 'A',
        text: 'First option text'
      }
    })

    expect(wrapper.text()).toContain('A')
    expect(wrapper.text()).toContain('First option text')
    expect(wrapper.find('[data-testid="quiz-option"]').attributes('aria-pressed')).toBe('false')
  })

  it('converts numeric letter index to character badge', () => {
    const wrapper = mount(OptionCard, {
      props: {
        letter: 1, // 0-based -> B
        text: 'Second option'
      }
    })

    expect(wrapper.text()).toContain('B')
  })

  it('emits select when clicked in enabled state', async () => {
    const wrapper = mount(OptionCard, {
      props: {
        letter: 'C',
        text: 'Clickable option'
      }
    })

    await wrapper.find('button').trigger('click')
    expect(wrapper.emitted('select')).toHaveLength(1)
  })

  it('does not emit select and has disabled attribute when disabled', async () => {
    const wrapper = mount(OptionCard, {
      props: {
        letter: 'D',
        text: 'Disabled option',
        disabled: true
      }
    })

    const button = wrapper.find('button')
    expect(button.attributes('disabled')).toBeDefined()
    await button.trigger('click')
    expect(wrapper.emitted('select')).toBeUndefined()
  })

  it('renders selected state with aria-pressed true', () => {
    const wrapper = mount(OptionCard, {
      props: {
        letter: 'B',
        text: 'Selected option',
        state: 'selected'
      }
    })

    expect(wrapper.find('button').attributes('aria-pressed')).toBe('true')
  })

  it('renders correct state with Check icon', () => {
    const wrapper = mount(OptionCard, {
      props: {
        letter: 'B',
        text: 'Correct answer',
        state: 'correct'
      }
    })

    // Check icon is rendered inside badge
    expect(wrapper.find('svg').exists()).toBe(true)
  })

  it('renders incorrect state with X icon', () => {
    const wrapper = mount(OptionCard, {
      props: {
        letter: 'C',
        text: 'Incorrect answer',
        state: 'incorrect'
      }
    })

    expect(wrapper.find('svg').exists()).toBe(true)
  })
})
