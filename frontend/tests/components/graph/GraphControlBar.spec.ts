import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import GraphControlBar from '~/components/graph/GraphControlBar.vue'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

describe('GraphControlBar.vue', () => {
  function createTestStore() {
    setActivePinia(createPinia())
    const store = useKnowledgeGraphStore()
    return store
  }

  it('renders search input, fit screen button, and filter pills with whitespace-nowrap shrink-0', () => {
    createTestStore()
    const wrapper = mount(GraphControlBar)

    const searchInput = wrapper.find('input[type="text"]')
    expect(searchInput.exists()).toBe(true)

    // Fit Screen button
    const fitBtn = wrapper.findAll('button').find((b) => b.attributes('title') === 'graph.fitScreen' || b.text().includes('graph.fitScreen'))
    expect(fitBtn).toBeDefined()
    expect(fitBtn?.classes()).toContain('whitespace-nowrap')
    expect(fitBtn?.classes()).toContain('shrink-0')

    // Filter pills have whitespace-nowrap shrink-0
    const pills = wrapper.findAll('button.whitespace-nowrap.shrink-0')
    expect(pills.length).toBeGreaterThan(5)
  })

  it('updates category filter when a pillar pill is clicked', async () => {
    const store = createTestStore()
    const wrapper = mount(GraphControlBar)

    const dotnetBtn = wrapper.findAll('button').find((b) => b.text().includes('Backend (.NET)'))
    expect(dotnetBtn).toBeDefined()

    await dotnetBtn!.trigger('click')
    expect(store.selectedCategory).toBe('BackendDotNet')
  })

  it('updates node type filter when a type button is clicked', async () => {
    const store = createTestStore()
    const wrapper = mount(GraphControlBar)

    const bookBtn = wrapper.findAll('button').find((b) => b.text().includes('graph.filters.books'))
    expect(bookBtn).toBeDefined()

    await bookBtn!.trigger('click')
    expect(store.selectedNodeType).toBe('book')
  })

  it('updates mastery filter when a mastery status is clicked', async () => {
    const store = createTestStore()
    const wrapper = mount(GraphControlBar)

    const masteredBtn = wrapper.findAll('button').find((b) => b.text().includes('graph.filters.mastered'))
    expect(masteredBtn).toBeDefined()

    await masteredBtn!.trigger('click')
    expect(store.selectedMastery).toBe('mastered')
  })

  it('emits fit-screen event when Fit Screen button is clicked', async () => {
    createTestStore()
    const wrapper = mount(GraphControlBar)

    const fitBtn = wrapper.findAll('button').find((b) => b.attributes('title') === 'graph.fitScreen' || b.text().includes('graph.fitScreen'))
    expect(fitBtn).toBeDefined()

    await fitBtn!.trigger('click')
    expect(wrapper.emitted('fit-screen')).toBeTruthy()
  })

  it('updates store searchQuery on text input', async () => {
    const store = createTestStore()
    const wrapper = mount(GraphControlBar)

    const input = wrapper.find('input[type="text"]')
    await input.setValue('concurrency')
    await input.trigger('input')

    expect(store.searchQuery).toBe('concurrency')
  })

  it('resets filters when Reset Filters button is clicked', async () => {
    const store = createTestStore()
    store.selectedCategory = 'BackendDotNet'
    store.searchQuery = 'postgres'

    const wrapper = mount(GraphControlBar)
    const resetBtn = wrapper.findAll('button').find((b) => b.text().includes('graph.resetFilters'))
    expect(resetBtn).toBeDefined()

    await resetBtn!.trigger('click')
    expect(store.selectedCategory).toBe('all')
    expect(store.searchQuery).toBe('')
  })
})
