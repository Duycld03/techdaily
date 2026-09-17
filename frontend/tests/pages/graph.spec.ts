import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import GraphPage from '~/pages/graph.vue'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'
vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async () => ({
      nodes: [],
      edges: [],
      stats: { totalNodes: 0, totalEdges: 0, nodeTypeCounts: {}, pillarCounts: {}, masteredCardsCount: 0 }
    }))
  })
}))

const GraphCanvasStub = {
  name: 'GraphCanvas',
  template: '<div data-testid="graph-canvas"></div>',
  methods: {
    fitScreen: vi.fn()
  }
}

const GraphControlBarStub = {
  name: 'GraphControlBar',
  template: '<div data-testid="graph-control-bar"></div>',
  emits: ['fit-screen']
}

const GraphMinimapStub = {
  name: 'GraphMinimap',
  template: '<div data-testid="graph-minimap"></div>',
  props: ['cy']
}

const GraphDetailDrawerStub = {
  name: 'GraphDetailDrawer',
  template: '<div data-testid="graph-detail-drawer"></div>'
}

describe('pages/graph.vue', () => {
  function createTestStore() {
    setActivePinia(createPinia())
    const store = useKnowledgeGraphStore()
    return store
  }

  it('calls fetchGraph on mounted', () => {
    const store = createTestStore()
    const fetchSpy = vi.spyOn(store, 'fetchGraph')

    mount(GraphPage, {
      global: {
        stubs: {
          GraphCanvas: GraphCanvasStub,
          GraphControlBar: GraphControlBarStub,
          GraphMinimap: GraphMinimapStub,
          GraphDetailDrawer: GraphDetailDrawerStub
        }
      }
    })

    expect(fetchSpy).toHaveBeenCalled()
  })

  it('renders loading overlay while store is loading', () => {
    const store = createTestStore()
    store.isLoading = true

    const wrapper = mount(GraphPage, {
      global: {
        stubs: {
          GraphCanvas: GraphCanvasStub,
          GraphControlBar: GraphControlBarStub,
          GraphMinimap: GraphMinimapStub,
          GraphDetailDrawer: GraphDetailDrawerStub
        }
      }
    })

    expect(wrapper.text()).toContain('graph.loading')
  })

  it('renders error overlay and retries fetch when retry button is clicked', async () => {
    const store = createTestStore()
    store.isLoading = false
    store.error = 'Failed to load graph network.'
    const fetchSpy = vi.spyOn(store, 'fetchGraph')

    const wrapper = mount(GraphPage, {
      global: {
        stubs: {
          GraphCanvas: GraphCanvasStub,
          GraphControlBar: GraphControlBarStub,
          GraphMinimap: GraphMinimapStub,
          GraphDetailDrawer: GraphDetailDrawerStub
        }
      }
    })

    expect(wrapper.text()).toContain('graph.error.title')
    expect(wrapper.text()).toContain('Failed to load graph network.')

    const retryBtn = wrapper.findAll('button').find((b) => b.text().includes('graph.error.retry'))
    expect(retryBtn).toBeDefined()

    await retryBtn!.trigger('click')
    expect(fetchSpy).toHaveBeenCalledWith(true)
  })

  it('renders empty state when there are 0 filtered nodes', async () => {
    const store = createTestStore()
    store.isLoading = false
    store.error = null
    store.rawData = {
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
    const resetSpy = vi.spyOn(store, 'resetFilters')

    const wrapper = mount(GraphPage, {
      global: {
        stubs: {
          GraphCanvas: GraphCanvasStub,
          GraphControlBar: GraphControlBarStub,
          GraphMinimap: GraphMinimapStub,
          GraphDetailDrawer: GraphDetailDrawerStub
        }
      }
    })

    expect(wrapper.text()).toContain('graph.empty.title')
    expect(wrapper.text()).toContain('graph.empty.description')

    const resetBtn = wrapper.findAll('button').find((b) => b.text().includes('graph.resetFilters'))
    expect(resetBtn).toBeDefined()

    await resetBtn!.trigger('click')
    expect(resetSpy).toHaveBeenCalled()
  })

  it('embeds control bar, canvas, minimap, and detail drawer', () => {
    const store = createTestStore()
    store.isLoading = false
    store.rawData = {
      nodes: [{ id: 'n1', label: 'Node 1', type: 'topic', category: 'BackendDotNet' }],
      edges: [],
      stats: {
        totalNodes: 1,
        totalEdges: 0,
        nodeTypeCounts: {},
        pillarCounts: {},
        masteredCardsCount: 0
      }
    }

    const wrapper = mount(GraphPage, {
      global: {
        stubs: {
          GraphCanvas: GraphCanvasStub,
          GraphControlBar: GraphControlBarStub,
          GraphMinimap: GraphMinimapStub,
          GraphDetailDrawer: GraphDetailDrawerStub
        }
      }
    })

    expect(wrapper.find('[data-testid="graph-canvas"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="graph-control-bar"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="graph-minimap"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="graph-detail-drawer"]').exists()).toBe(true)
  })
})
