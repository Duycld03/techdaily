import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import GraphPage from '~/pages/graph.vue'
import {
  useKnowledgeGraphStore,
  type KnowledgeGraphResponse
} from '~/stores/useKnowledgeGraphStore'

// The store's fetchGraph early-returns when rawData is already set, so the API is not hit;
// the mock is a safety net against accidental network access.
vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async () => emptyGraph())
  })
}))

function emptyGraph(): KnowledgeGraphResponse {
  return {
    nodes: [],
    edges: [],
    stats: {
      totalNodes: 0,
      totalEdges: 0,
      nodeTypeCounts: {},
      pillarCounts: {},
      masteredCardsCount: 0
    }
  }
}

function mountGraph() {
  return mount(GraphPage, {
    global: {
      stubs: {
        GraphCanvas: true,
        GraphControlBar: true,
        GraphMinimap: true,
        GraphDetailDrawer: true,
        GraphCanvas3D: true,
        GraphLegend: true,
        ClientOnly: { template: '<div><slot /></div>' },
        NuxtLink: { props: ['to'], template: '<a :href="to"><slot /></a>' }
      }
    }
  })
}

describe('pages/graph.vue empty states', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('shows the start-learning CTA routing to /library when the user has no learned artifacts', async () => {
    const store = useKnowledgeGraphStore()
    store.rawData = emptyGraph()

    const wrapper = mountGraph()
    await flushPromises()

    expect(store.hasAnyNodes).toBe(false)
    expect(store.filteredNodes).toHaveLength(0)

    const cta = wrapper.find('[data-testid="graph-empty-cta"]')
    expect(cta.exists()).toBe(true)
    expect(cta.attributes('href')).toBe('/library')
  })

  it('shows the reset-filters CTA (not the start-learning CTA) when filters hide existing nodes', async () => {
    const store = useKnowledgeGraphStore()
    store.rawData = {
      nodes: [
        {
          id: 'topic_1',
          label: 'Async I/O',
          type: 'topic',
          category: 'BackendRuntime'
        }
      ],
      edges: [],
      stats: {
        totalNodes: 1,
        totalEdges: 0,
        nodeTypeCounts: { topic: 1 },
        pillarCounts: { BackendRuntime: 1 },
        masteredCardsCount: 0
      }
    }
    // Filter to a category the node does not belong to, hiding every node.
    store.setCategory('FrontendWeb')

    const wrapper = mountGraph()
    await flushPromises()

    expect(store.hasAnyNodes).toBe(true)
    expect(store.filteredNodes).toHaveLength(0)

    expect(wrapper.find('[data-testid="graph-empty-cta"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('graph.resetFilters')
  })
})
