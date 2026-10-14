import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import LibraryFilterModal from '~/components/library/LibraryFilterModal.vue'

describe('LibraryFilterModal.vue', () => {
  it('renders modal with category and language options when isOpen is true', () => {
    const wrapper = mount(LibraryFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {
          category: undefined,
          language: undefined
        }
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })

    expect(wrapper.find('[role="dialog"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="library-filter-modal-close-btn"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="library-filter-modal-reset-btn"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="library-filter-modal-apply-btn"]').exists()).toBe(true)

    // Check all categories are rendered (All + 7 categories = 8 buttons in section 1)
    const categoryButtons = wrapper.findAll('.space-y-6 > div:nth-child(2) button')
    expect(categoryButtons.length).toBe(8)
  })

  it('does not render modal content when isOpen is false', () => {
    const wrapper = mount(LibraryFilterModal, {
      props: {
        isOpen: false,
        currentFilters: {}
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })

    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })

  it('emits apply event with selected category and language', async () => {
    const wrapper = mount(LibraryFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {
          category: undefined,
          language: undefined
        }
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })

    // Click Category 1 (Backend)
    const categoryButtons = wrapper.findAll('.space-y-6 > div:nth-child(2) button')
    await categoryButtons[2].trigger('click') // index 2 is Backend (0 is All, 1 is Frontend, 2 is Backend)

    // Click Language 'vi'
    const languageButtons = wrapper.findAll('.space-y-6 > div:nth-child(3) button')
    await languageButtons[2].trigger('click') // index 2 is 'vi'

    // Click Apply
    await wrapper.find('[data-testid="library-filter-modal-apply-btn"]').trigger('click')

    expect(wrapper.emitted('apply')).toBeTruthy()
    expect(wrapper.emitted('apply')![0][0]).toEqual({
      category: 1,
      language: 'vi'
    })
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('emits reset event when reset button is clicked', async () => {
    const wrapper = mount(LibraryFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {
          category: 2,
          language: 'en'
        }
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })

    await wrapper.find('[data-testid="library-filter-modal-reset-btn"]').trigger('click')

    expect(wrapper.emitted('reset')).toBeTruthy()
  })

  it('emits close event when close or cancel button is clicked', async () => {
    const wrapper = mount(LibraryFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {}
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    })

    await wrapper.find('[data-testid="library-filter-modal-close-btn"]').trigger('click')
    expect(wrapper.emitted('close')).toHaveLength(1)

    await wrapper.find('[data-testid="library-filter-modal-cancel-btn"]').trigger('click')
    expect(wrapper.emitted('close')).toHaveLength(2)
  })
})
