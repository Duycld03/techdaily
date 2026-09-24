import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import GraphLegend from '~/components/graph/GraphLegend.vue'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

describe('GraphLegend.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('renders expanded legend card by default on desktop', () => {
    const wrapper = mount(GraphLegend)

    expect(wrapper.find('[data-testid="legend-card"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="legend-expand-btn"]').exists()).toBe(false)

    // Check entity items exist
    expect(wrapper.find('[data-testid="legend-item-pillar"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="legend-item-topic"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="legend-item-book"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="legend-item-highlight"]').exists()).toBe(true)

    // Check mastery items exist
    expect(wrapper.find('[data-testid="legend-item-learning"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="legend-item-reviewing"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="legend-item-mastered"]').exists()).toBe(true)
  })

  it('toggles collapse and expand state and persists to localStorage', async () => {
    const wrapper = mount(GraphLegend)

    // Initially expanded
    expect(wrapper.find('[data-testid="legend-card"]').exists()).toBe(true)

    // Click collapse button
    const collapseBtn = wrapper.find('[data-testid="legend-collapse-btn"]')
    await collapseBtn.trigger('click')

    // Now collapsed
    expect(wrapper.find('[data-testid="legend-card"]').exists()).toBe(false)
    const expandBtn = wrapper.find('[data-testid="legend-expand-btn"]')
    expect(expandBtn.exists()).toBe(true)
    expect(localStorage.getItem('techdaily_graph_legend_collapsed')).toBe('true')

    // Click expand button
    await expandBtn.trigger('click')
    expect(wrapper.find('[data-testid="legend-card"]').exists()).toBe(true)
    expect(localStorage.getItem('techdaily_graph_legend_collapsed')).toBe('false')
  })

  it('triggers store.setHoveredLegendType on mouseenter and mouseleave', async () => {
    const store = useKnowledgeGraphStore()
    const wrapper = mount(GraphLegend)

    const pillarItem = wrapper.find('[data-testid="legend-item-pillar"]')
    await pillarItem.trigger('mouseenter')
    expect(store.hoveredLegendType).toBe('pillar')

    await pillarItem.trigger('mouseleave')
    expect(store.hoveredLegendType).toBeNull()

    const masteredItem = wrapper.find('[data-testid="legend-item-mastered"]')
    await masteredItem.trigger('mouseenter')
    expect(store.hoveredLegendType).toBe('mastered')

    await masteredItem.trigger('mouseleave')
    expect(store.hoveredLegendType).toBeNull()
  })

  it('restores collapsed state from localStorage on mount', () => {
    localStorage.setItem('techdaily_graph_legend_collapsed', 'true')
    const wrapper = mount(GraphLegend)

    expect(wrapper.find('[data-testid="legend-card"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="legend-expand-btn"]').exists()).toBe(true)
  })

  it('renders legend card with node type indicators', () => {
    const wrapper = mount(GraphLegend)
    const card = wrapper.find('[data-testid="legend-card"]')
    expect(card.exists()).toBe(true)
    expect(wrapper.text()).toContain('graph.legend.topic')
  })

  it('defaults to collapsed on tablet/mobile screens (< 1024px) when no localStorage is set', () => {
    const originalInnerWidth = window.innerWidth
    try {
      window.innerWidth = 768
      const wrapper = mount(GraphLegend)
      expect(wrapper.find('[data-testid="legend-card"]').exists()).toBe(false)
      expect(wrapper.find('[data-testid="legend-expand-btn"]').exists()).toBe(true)
    } finally {
      window.innerWidth = originalInnerWidth
    }
  })
})
