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

  it('renders search input, fit screen button, and category filter buttons', () => {
    createTestStore()
    const wrapper = mount(GraphControlBar)

    const searchInput = wrapper.find('input[type="text"]')
    expect(searchInput.exists()).toBe(true)

    // Fit Screen button
    const fitBtn = wrapper.findAll('button').find((b) => b.attributes('title') === 'graph.fitScreen' || b.text().includes('graph.fitScreen'))
    expect(fitBtn).toBeDefined()

    const pillarButtons = wrapper.findAll('button')

    // Verify all 6 category pills have explicit translation keys
    const expectedKeys = [
      'graph.filters.allPillars',
      'graph.filters.backendRuntime',
      'graph.filters.databaseStorage',
      'graph.filters.systemDesign',
      'graph.filters.frontendWeb',
      'graph.filters.engineeringCraft'
    ]
    expectedKeys.forEach((key) => {
      const found = pillarButtons.some((b) => b.text().includes(key))
      expect(found).toBe(true)
    })
  })

  it('updates category filter when a pillar pill is clicked', async () => {
    const store = createTestStore()
    const wrapper = mount(GraphControlBar)

    const backendBtn = wrapper.findAll('button').find((b) => b.text().includes('Backend & Runtime') || b.text().includes('graph.filters.backendRuntime'))
    expect(backendBtn).toBeDefined()

    await backendBtn!.trigger('click')
    expect(store.selectedCategory).toBe('BackendRuntime')
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

  it('switches between 2D and 3D viewMode when engine switcher buttons are clicked', async () => {
    const store = createTestStore()
    const wrapper = mount(GraphControlBar)

    expect(store.viewMode).toBe('2d')

    const btn3D = wrapper.findAll('button').find((b) => b.text().includes('3D'))
    expect(btn3D).toBeDefined()
    await btn3D!.trigger('click')
    expect(store.viewMode).toBe('3d')

    const btn2D = wrapper.findAll('button').find((b) => b.text().includes('2D'))
    expect(btn2D).toBeDefined()
    await btn2D!.trigger('click')
    expect(store.viewMode).toBe('2d')
  })
})
