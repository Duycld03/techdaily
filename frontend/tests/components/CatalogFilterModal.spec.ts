import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import FilterModal from '~/components/catalog/CatalogFilterModal.vue'
import { useCategoryStore } from '~/stores/useCategoryStore'

const category = { id: '92345678-1234-4234-8234-123456789012', slug: 'custom', nameEn: 'Custom topic', nameVi: 'Chu de', sortOrder: 1, isActive: false, version: 1 }
const prefix = 'catalog'
function render(currentFilters = {}) {
  return mount(FilterModal, { props: { isOpen: true, currentFilters }, global: { stubs: { Teleport: true }, mocks: { $t: (key: string) => key } } })
}

describe('CatalogFilterModal UUID filters', () => {
  beforeEach(() => { setActivePinia(createPinia()) })

  it('loads metadata on mount and allows filtering inactive content by UUID only', async () => {
    const store = useCategoryStore()
    const ensure = vi.spyOn(store, 'ensureCategories').mockImplementation(async () => { store.categories = [category]; return [category] })
    const wrapper = render()
    await flushPromises()
    expect(ensure).toHaveBeenCalledOnce()
    const option = wrapper.findAll('button').find(button => button.text().includes('Custom topic'))!
    expect(option.text()).toContain('categories.inactive')
    await option.trigger('click')
    await wrapper.get('[data-testid="' + prefix + '-filter-modal-apply-btn"]').trigger('click')
    expect(wrapper.emitted('apply')![0][0]).toEqual({ categoryId: category.id, sourceType: undefined, language: undefined })
    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('preserves the current UUID and clears only filters on reset', async () => {
    const store = useCategoryStore()
    vi.spyOn(store, 'ensureCategories').mockResolvedValue([category])
    store.categories = [category]
    const wrapper = render({ categoryId: category.id, language: 'vi' })
    await wrapper.get('[data-testid="' + prefix + '-filter-modal-apply-btn"]').trigger('click')
    expect(wrapper.emitted('apply')![0][0]).toEqual({ categoryId: category.id, sourceType: undefined, language: 'vi' })
    await wrapper.get('[data-testid="' + prefix + '-filter-modal-reset-btn"]').trigger('click')
    await wrapper.get('[data-testid="' + prefix + '-filter-modal-apply-btn"]').trigger('click')
    expect(wrapper.emitted('reset')).toHaveLength(1)
    expect(wrapper.emitted('apply')![1][0]).toEqual({ categoryId: undefined, sourceType: undefined, language: undefined })
  })

  it('shows a localized metadata failure and retries without seed fallback options', async () => {
    const store = useCategoryStore()
    store.error = 'network unavailable'
    const ensure = vi.spyOn(store, 'ensureCategories').mockResolvedValue([])
    const wrapper = render()
    await flushPromises()
    expect(wrapper.get('[role="alert"]').text()).toContain('categories.metadataUnavailable')
    expect(wrapper.text()).not.toContain('library.categories.frontend')
    const retry = wrapper.findAll('button').find(button => button.text().includes('categories.retry'))!
    await retry.trigger('click')
    expect(ensure).toHaveBeenCalledTimes(2)
  })

  it('hides the dialog when closed and emits close for cancel', async () => {
    vi.spyOn(useCategoryStore(), 'ensureCategories').mockResolvedValue([])
    const wrapper = render()
    await wrapper.get('[data-testid="' + prefix + '-filter-modal-cancel-btn"]').trigger('click')
    expect(wrapper.emitted('close')).toHaveLength(1)
    await wrapper.setProps({ isOpen: false })
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })
})
