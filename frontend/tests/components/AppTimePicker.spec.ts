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
      global: {
        stubs: {
          Teleport: true
        },
        mocks: {
          $t: (key: string) => key
        }
      },
      ...options
    })
  }

  afterEach(() => {
    document.body.innerHTML = ''
    vi.restoreAllMocks()
  })

  it('renders trigger with formatted 12-hour display time for morning hour', () => {
    const wrapper = defaultMount({ modelValue: '08:00' })

    const trigger = wrapper.find('[data-testid="app-time-picker-trigger"]')
    expect(trigger.exists()).toBe(true)
    expect(trigger.text()).toContain('08:00 AM')
  })

  it('renders trigger with formatted 12-hour display time for evening hour', () => {
    const wrapper = defaultMount({ modelValue: '20:30' })

    const trigger = wrapper.find('[data-testid="app-time-picker-trigger"]')
    expect(trigger.text()).toContain('08:30 PM')
  })

  it('handles midnight 00:00 as 12:00 AM', () => {
    const wrapper = defaultMount({ modelValue: '00:15' })

    const trigger = wrapper.find('[data-testid="app-time-picker-trigger"]')
    expect(trigger.text()).toContain('12:15 AM')
  })

  it('handles noon 12:00 as 12:00 PM', () => {
    const wrapper = defaultMount({ modelValue: '12:00' })

    const trigger = wrapper.find('[data-testid="app-time-picker-trigger"]')
    expect(trigger.text()).toContain('12:00 PM')
  })

  it('toggles popover visibility on trigger click', async () => {
    const wrapper = defaultMount()

    expect(wrapper.find('[data-testid="app-time-picker-popover"]').exists()).toBe(false)

    // Click trigger to open
    await wrapper.find('[data-testid="app-time-picker-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-time-picker-popover"]').exists()).toBe(true)

    // Click trigger to close
    await wrapper.find('[data-testid="app-time-picker-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-time-picker-popover"]').exists()).toBe(false)
  })

  it('closes popover on Escape keydown', async () => {
    const wrapper = defaultMount()

    await wrapper.find('[data-testid="app-time-picker-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-time-picker-popover"]').exists()).toBe(true)

    await wrapper.find('[data-testid="app-time-picker-trigger"]').trigger('keydown', { key: 'Escape' })
    expect(wrapper.find('[data-testid="app-time-picker-popover"]').exists()).toBe(false)
  })

  it('closes popover when Done button is clicked', async () => {
    const wrapper = defaultMount({ modelValue: '08:00' })

    await wrapper.find('[data-testid="app-time-picker-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-time-picker-popover"]').exists()).toBe(true)

    const doneBtn = wrapper.find('[data-testid="time-picker-done-btn"]')
    expect(doneBtn.exists()).toBe(true)
    await doneBtn.trigger('click')

    expect(wrapper.find('[data-testid="app-time-picker-popover"]').exists()).toBe(false)
  })

  it('emits updated 24h time when hour, minute, or period is selected', async () => {
    const wrapper = defaultMount({ modelValue: '08:00' })

    await wrapper.find('[data-testid="app-time-picker-trigger"]').trigger('click')

    // Change hour to 9
    const hour9Btn = wrapper.find('[data-testid="time-hour-9"]')
    expect(hour9Btn.exists()).toBe(true)
    await hour9Btn.trigger('click')

    expect(wrapper.emitted('update:modelValue')![0]).toEqual(['09:00'])

    // Change minute to 30
    const min30Btn = wrapper.find('[data-testid="time-minute-30"]')
    expect(min30Btn.exists()).toBe(true)
    await min30Btn.trigger('click')

    expect(wrapper.emitted('update:modelValue')![1]).toEqual(['08:30'])

    // Change period to PM
    const pmBtn = wrapper.find('[data-testid="time-period-pm"]')
    expect(pmBtn.exists()).toBe(true)
    await pmBtn.trigger('click')

    expect(wrapper.emitted('update:modelValue')![2]).toEqual(['20:00'])
  })

  it('does not open popover when disabled', async () => {
    const wrapper = defaultMount({ disabled: true })

    await wrapper.find('[data-testid="app-time-picker-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-time-picker-popover"]').exists()).toBe(false)
  })
})
