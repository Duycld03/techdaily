import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ThemeToggle from '~/components/common/ThemeToggle.vue'

describe('ThemeToggle.vue', () => {
  it('renders theme toggle button with accessibility attributes', () => {
    const wrapper = mount(ThemeToggle)
    expect(wrapper.find('button').exists()).toBe(true)
    expect(wrapper.attributes('aria-label')).toBe('Toggle Color Theme')
  })

  it('triggers theme toggle on click', async () => {
    const wrapper = mount(ThemeToggle)
    await wrapper.trigger('click')
    expect(wrapper.find('button').exists()).toBe(true)
  })
})
