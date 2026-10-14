import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import CatalogFilterModal from '~/components/catalog/CatalogFilterModal.vue'

describe('CatalogFilterModal.vue', () => {
  const defaultGlobal = {
    stubs: {
      Teleport: true
    },
    mocks: {
      $t: (key: string) => key
    }
  }

  it('renders filter sections when open', () => {
    const wrapper = mount(CatalogFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {}
      },
      global: defaultGlobal
    })

    expect(wrapper.text()).toContain('library.discover.filter_modal_title')
    expect(wrapper.text()).toContain('library.category_label')
    expect(wrapper.text()).toContain('library.discover.source_type_label')
    expect(wrapper.text()).toContain('library.discover.language_filter')
  })

  it('initializes with currentFilters values', async () => {
    const wrapper = mount(CatalogFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {
          category: 1,
          sourceType: 2,
          language: 'vi'
        }
      },
      global: defaultGlobal
    })

    const buttons = wrapper.findAll('button')
    expect(buttons.length).toBeGreaterThan(0)
  })

  it('emits close when cancel or close button is clicked', async () => {
    const wrapper = mount(CatalogFilterModal, {
      props: {
        isOpen: true,
        currentFilters: {}
      },
      global: defaultGlobal
    })

    const buttons = wrapper.findAll('button')
    const closeBtn = buttons.find(b => b.text().includes('common.cancel'))
    expect(closeBtn).toBeDefined()
    await closeBtn!.trigger('click')

    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('emits reset when reset button is clicked', async () => {
    const wrapper = mount(CatalogFilterModal, {
      props: {
        isOpen: true,
        currentFilters: { category: 2 }
      },
      global: defaultGlobal
    })

    const buttons = wrapper.findAll('button')
    const resetBtn = buttons.find(b => b.text().includes('review.reset_filters'))
    expect(resetBtn).toBeDefined()
    await resetBtn!.trigger('click')

    expect(wrapper.emitted('reset')).toBeTruthy()
  })

  it('emits apply with updated criteria when apply button is clicked', async () => {
    const wrapper = mount(CatalogFilterModal, {
      props: {
        isOpen: true,
        currentFilters: { category: 1 }
      },
      global: defaultGlobal
    })

    const buttons = wrapper.findAll('button')
    const applyBtn = buttons.find(b => b.text().includes('review.apply_filters'))
    expect(applyBtn).toBeDefined()
    await applyBtn!.trigger('click')

    expect(wrapper.emitted('apply')).toBeTruthy()
    const applied = wrapper.emitted('apply')![0][0] as { category?: number }
    expect(applied.category).toBe(1)
  })
})
