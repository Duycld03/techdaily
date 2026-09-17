import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import GraphCanvas from '~/components/graph/GraphCanvas.vue'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

describe('GraphCanvas.vue', () => {
  beforeEach(() => {
    const dummyContext = {
      fillRect: vi.fn(),
      clearRect: vi.fn(),
      getImageData: vi.fn(() => ({ data: new Array(4) })),
      putImageData: vi.fn(),
      createImageData: vi.fn(),
      setTransform: vi.fn(),
      drawImage: vi.fn(),
      save: vi.fn(),
      fillText: vi.fn(),
      restore: vi.fn(),
      beginPath: vi.fn(),
      moveTo: vi.fn(),
      lineTo: vi.fn(),
      closePath: vi.fn(),
      stroke: vi.fn(),
      translate: vi.fn(),
      scale: vi.fn(),
      rotate: vi.fn(),
      arc: vi.fn(),
      fill: vi.fn(),
      measureText: vi.fn(() => ({ width: 0 })),
      transform: vi.fn(),
      rect: vi.fn(),
      clip: vi.fn(),
      canvas: { width: 800, height: 600 }
    }

    HTMLCanvasElement.prototype.getContext = vi.fn().mockImplementation((contextId: string) => {
      if (contextId === '2d') return dummyContext
      return null
    })

    if (typeof OffscreenCanvas !== 'undefined') {
      OffscreenCanvas.prototype.getContext = vi.fn().mockImplementation((contextId: string) => {
        if (contextId === '2d') return dummyContext
        return null
      })
    }
  })

  function createTestStore() {
    setActivePinia(createPinia())
    const store = useKnowledgeGraphStore()
    store.rawData = {
      nodes: [
        { id: 'topic_1', label: 'MVCC', type: 'topic', category: 'DatabaseStorage' },
        { id: 'card_1', label: 'Postgres Bloat', type: 'card', category: 'DatabaseStorage', status: 'Mastered' }
      ],
      edges: [
        { id: 'edge_1', source: 'card_1', target: 'topic_1', relationType: 'CardToTopic' }
      ],
      stats: {
        totalNodes: 2,
        totalEdges: 1,
        nodeTypeCounts: {},
        pillarCounts: {},
        masteredCardsCount: 1
      }
    }
    return store
  }

  it('renders canvas container and emits cy-ready event on mount', () => {
    createTestStore()
    const wrapper = mount(GraphCanvas)

    expect(wrapper.find('div.relative.w-full.h-full').exists()).toBe(true)
    expect(wrapper.emitted('cy-ready')).toBeTruthy()
  })

  it('exposes fitScreen method and cy getter', () => {
    createTestStore()
    const wrapper = mount(GraphCanvas)

    expect(typeof wrapper.vm.fitScreen).toBe('function')
    expect(typeof wrapper.vm.cy).toBe('function')
    expect(wrapper.vm.cy()).toBeDefined()
  })

  it('destroys cytoscape instance cleanly on unmount', () => {
    createTestStore()
    const wrapper = mount(GraphCanvas)
    const cy = wrapper.vm.cy()
    const destroySpy = vi.spyOn(cy, 'destroy')

    wrapper.unmount()
    expect(destroySpy).toHaveBeenCalled()
  })
})
