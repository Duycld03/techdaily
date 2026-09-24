import { describe, it, expect, vi, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AppTimePicker from '~/components/common/AppTimePicker.vue'

describe('AppTimePicker.vue', () => {
  const defaultMount = (props = {}, options = {}) => {
    return mount(AppTimePicker, {
      props: {
        modelValue: '08:00',
        ...props
      },
      ...options
    })
  }

  afterEach(() => {
    document.body.innerHTML = ''
    vi.restoreAllMocks()
  })

  it('renders native input with type="time" and formatted 24h time', () => {
    const wrapper = defaultMount({ modelValue: '08:00' })

    const input = wrapper.find<HTMLInputElement>('[data-testid="app-time-picker-input"]')
    expect(input.exists()).toBe(true)
    expect(input.attributes('type')).toBe('time')
    expect(input.element.value).toBe('08:00')
  })

  it('normalizes seconds format HH:mm:ss to HH:mm for HTML5 time input', () => {
    const wrapper = defaultMount({ modelValue: '08:30:00' })

    const input = wrapper.find<HTMLInputElement>('[data-testid="app-time-picker-input"]')
    expect(input.element.value).toBe('08:30')
  })

  it('handles midnight 00:00 correctly', () => {
    const wrapper = defaultMount({ modelValue: '00:00' })

    const input = wrapper.find<HTMLInputElement>('[data-testid="app-time-picker-input"]')
    expect(input.element.value).toBe('00:00')
  })

  it('handles evening hour 20:45 correctly', () => {
    const wrapper = defaultMount({ modelValue: '20:45' })

    const input = wrapper.find<HTMLInputElement>('[data-testid="app-time-picker-input"]')
    expect(input.element.value).toBe('20:45')
  })

  it('emits update:modelValue and change events when user changes the time', async () => {
    const wrapper = defaultMount({ modelValue: '08:00' })

    const input = wrapper.find<HTMLInputElement>('[data-testid="app-time-picker-input"]')
    await input.setValue('09:15')

    expect(wrapper.emitted('update:modelValue')![0]).toEqual(['09:15'])
    expect(wrapper.emitted('change')![0]).toEqual(['09:15'])
  })

  it('renders disabled attribute when disabled prop is true', () => {
    const wrapper = defaultMount({ disabled: true })

    const input = wrapper.find('[data-testid="app-time-picker-input"]')
    expect(input.attributes('disabled')).toBeDefined()
  })

  it('renders clock icon with pointer-events-none inside input wrapper', () => {
    const wrapper = defaultMount()

    const iconWrapper = wrapper.find('.pointer-events-none')
    expect(iconWrapper.exists()).toBe(true)
  })
})
