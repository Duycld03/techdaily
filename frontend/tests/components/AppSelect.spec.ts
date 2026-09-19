import { describe, it, expect, vi, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AppSelect from '~/components/common/AppSelect.vue'

describe('AppSelect.vue', () => {
  const sampleOptions = [
    { value: 'senior', label: 'Senior Software Engineer' },
    { value: 'staff', label: 'Staff Software Engineer' },
    { value: 'principal', label: 'Principal Software Architect' }
  ]

  const defaultMount = (props = {}, options = {}) => {
    return mount(AppSelect, {
      props: {
        modelValue: 'senior',
        options: sampleOptions,
        ...props
      },
      global: {
        stubs: {
          Teleport: true
        }
      },
      ...options
    })
  }

  afterEach(() => {
    // Clean up any teleported elements in document.body
    document.body.innerHTML = ''
    vi.restoreAllMocks()
  })

  it('renders trigger with selected option label', () => {
    const wrapper = defaultMount({ modelValue: 'staff' })

    const trigger = wrapper.find('[data-testid="app-select-trigger"]')
    expect(trigger.exists()).toBe(true)
    expect(trigger.text()).toContain('Staff Software Engineer')
  })

  it('renders placeholder when modelValue does not match any option', () => {
    const wrapper = defaultMount({
      modelValue: null,
      placeholder: 'Select a target role...'
    })

    expect(wrapper.text()).toContain('Select a target role...')
  })

  it('toggles listbox visibility on trigger click', async () => {
    const wrapper = defaultMount()

    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(false)

    // Click trigger to open
    await wrapper.find('[data-testid="app-select-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(true)

    // Click trigger to close
    await wrapper.find('[data-testid="app-select-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(false)
  })

  it('emits update:modelValue and change when an option is selected', async () => {
    const wrapper = defaultMount()

    await wrapper.find('[data-testid="app-select-trigger"]').trigger('click')
    const option = wrapper.find('[data-testid="app-select-option-principal"]')
    expect(option.exists()).toBe(true)

    await option.trigger('click')
    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['principal'])
    expect(wrapper.emitted('change')).toBeTruthy()
    expect(wrapper.emitted('change')?.[0]).toEqual(['principal'])
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(false)
  })

  it('does not open when disabled', async () => {
    const wrapper = defaultMount({ disabled: true })

    await wrapper.find('[data-testid="app-select-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(false)
  })

  it('handles keyboard navigation: ArrowDown and Enter to select', async () => {
    const wrapper = defaultMount(
      { modelValue: 'senior' },
      { attachTo: document.body }
    )

    const trigger = wrapper.find('[data-testid="app-select-trigger"]')

    // Press ArrowDown to open
    await trigger.trigger('keydown', { key: 'ArrowDown' })
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(true)

    // Press ArrowDown again to advance highlight to 'staff'
    await trigger.trigger('keydown', { key: 'ArrowDown' })

    // Press Enter to select
    await trigger.trigger('keydown', { key: 'Enter' })
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['staff'])
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(false)

    wrapper.unmount()
  })

  it('closes on Escape key without emitting selection', async () => {
    const wrapper = defaultMount()

    await wrapper.find('[data-testid="app-select-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(true)

    await wrapper.find('[data-testid="app-select-trigger"]').trigger('keydown', { key: 'Escape' })
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(false)
    expect(wrapper.emitted('update:modelValue')).toBeFalsy()
  })

  it('closes dropdown when clicking outside', async () => {
    const wrapper = defaultMount({}, { attachTo: document.body })

    await wrapper.find('[data-testid="app-select-trigger"]').trigger('click')
    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(true)

    const outsideEl = document.createElement('div')
    document.body.appendChild(outsideEl)

    const mouseEvent = new MouseEvent('mousedown', {
      bubbles: true,
      cancelable: true
    })
    outsideEl.dispatchEvent(mouseEvent)
    await wrapper.vm.$nextTick()

    expect(wrapper.find('[data-testid="app-select-listbox"]').exists()).toBe(false)

    wrapper.unmount()
    outsideEl.remove()
  })

  it('teleports listbox to document.body by default', async () => {
    const wrapper = mount(AppSelect, {
      props: {
        modelValue: 'senior',
        options: sampleOptions
      },
      attachTo: document.body
    })

    await wrapper.find('[data-testid="app-select-trigger"]').trigger('click')

    // Teleported listbox should exist in document.body
    const bodyListbox = document.body.querySelector('[data-testid="app-select-listbox"]')
    expect(bodyListbox).not.toBeNull()
    expect(bodyListbox?.getAttribute('role')).toBe('listbox')

    wrapper.unmount()
  })

  it('computes auto-flip upwards when bottom clearance is insufficient', async () => {
    const wrapper = mount(AppSelect, {
      props: {
        modelValue: 'senior',
        options: sampleOptions
      },
      attachTo: document.body
    })

    const triggerEl = wrapper.find('[data-testid="app-select-trigger"]').element as HTMLButtonElement
    // Mock getBoundingClientRect near bottom of viewport: top=700, bottom=740, innerHeight=800 -> spaceBelow=60 < 250, spaceAbove=700 > 60
    vi.spyOn(triggerEl, 'getBoundingClientRect').mockReturnValue({
      top: 700,
      bottom: 740,
      left: 100,
      right: 340,
      width: 240,
      height: 40,
      x: 100,
      y: 700,
      toJSON: () => {}
    })

    await wrapper.find('[data-testid="app-select-trigger"]').trigger('click')

    const bodyListbox = document.body.querySelector('[data-testid="app-select-listbox"]') as HTMLElement
    expect(bodyListbox).not.toBeNull()
    expect(bodyListbox.style.position).toBe('fixed')
    expect(bodyListbox.style.top).toBe('auto')
    expect(bodyListbox.style.bottom).not.toBe('auto')

    wrapper.unmount()
  })
})
