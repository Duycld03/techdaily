import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import GraphCanvas3D from '~/components/graph/GraphCanvas3D.vue'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

const mockControls = {
  enableDamping: false,
  dampingFactor: 0,
  maxDistance: 0,
  minDistance: 0,
  autoRotate: false,
  autoRotateSpeed: 0,
  addEventListener: vi.fn(),
  removeEventListener: vi.fn()
}

const mockCamera = {
  position: { x: 0, y: 100, z: 400 }
}

const mockGraphInstance = {
  backgroundColor: vi.fn().mockReturnThis(),
  showNavInfo: vi.fn().mockReturnThis(),
  nodeId: vi.fn().mockReturnThis(),
  nodeVal: vi.fn().mockReturnThis(),
  nodeColor: vi.fn().mockReturnThis(),
  nodeLabel: vi.fn().mockReturnThis(),
  nodeResolution: vi.fn().mockReturnThis(),
  nodeRelSize: vi.fn().mockReturnThis(),
  nodeThreeObjectExtend: vi.fn().mockReturnThis(),
  nodeThreeObject: vi.fn().mockReturnThis(),
  linkSource: vi.fn().mockReturnThis(),
  linkTarget: vi.fn().mockReturnThis(),
  linkColor: vi.fn().mockReturnThis(),
  linkWidth: vi.fn().mockReturnThis(),
  linkOpacity: vi.fn().mockReturnThis(),
  linkDirectionalParticles: vi.fn().mockReturnThis(),
  linkDirectionalParticleWidth: vi.fn().mockReturnThis(),
  linkDirectionalParticleSpeed: vi.fn().mockReturnThis(),
  warmupTicks: vi.fn().mockReturnThis(),
  cooldownTicks: vi.fn().mockReturnThis(),
  cooldownTime: vi.fn().mockReturnThis(),
  onNodeClick: vi.fn().mockReturnThis(),
  onNodeHover: vi.fn().mockReturnThis(),
  onBackgroundClick: vi.fn().mockReturnThis(),
  controls: vi.fn().mockReturnValue(mockControls),
  camera: vi.fn().mockReturnValue(mockCamera),
  cameraPosition: vi.fn().mockReturnThis(),
  zoomToFit: vi.fn().mockReturnThis(),
  graphData: vi.fn().mockReturnThis(),
  _destructor: vi.fn()
}

vi.mock('3d-force-graph', () => {
  return {
    default: () => () => mockGraphInstance
  }
})

vi.mock('three-spritetext', () => {
  return {
    default: class MockSpriteText {
      text: string
      color = ''
      textHeight = 10
      position = { y: 0 }
      constructor(text: string) {
        this.text = text
      }
    }
  }
})

describe('GraphCanvas3D.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setActivePinia(createPinia())
  })

  function createTestStore() {
    const store = useKnowledgeGraphStore()
    store.rawData = {
      nodes: [
        { id: 'pillar-BackendRuntime', label: 'Backend Runtime', type: 'pillar', category: 'BackendRuntime' },
        { id: 'topic-csharp-clr', label: 'C# CLR Execution', type: 'topic', category: 'BackendRuntime' },
        { id: 'card-1', label: 'Memory Allocation Drill', type: 'card', category: 'BackendRuntime', status: 'Mastered' }
      ],
      edges: [
        { id: 'e1', source: 'topic-csharp-clr', target: 'pillar-BackendRuntime', relationType: 'TopicToPillar' },
        { id: 'e2', source: 'card-1', target: 'topic-csharp-clr', relationType: 'CardToTopic' }
      ],
      stats: {
        totalNodes: 3,
        totalEdges: 2,
        nodeTypeCounts: { pillar: 1, topic: 1, card: 1 },
        pillarCounts: { BackendRuntime: 3 },
        masteredCardsCount: 1
      }
    }
    return store
  }

  it('mounts and initializes 3d-force-graph instance', async () => {
    createTestStore()
    const wrapper = mount(GraphCanvas3D)
    await flushPromises()

    expect(wrapper.find('div.w-full.h-full.relative').exists()).toBe(true)
    expect(wrapper.emitted('ready')).toBeTruthy()
    expect(mockGraphInstance.graphData).toHaveBeenCalled()
  })

  it('exposes fitScreen and toggleAutoRotate methods and controls camera position', async () => {
    vi.useFakeTimers()
    createTestStore()
    const wrapper = mount(GraphCanvas3D)
    await flushPromises()

    expect(typeof wrapper.vm.fitScreen).toBe('function')
    expect(typeof wrapper.vm.toggleAutoRotate).toBe('function')

    wrapper.vm.fitScreen()
    expect(mockGraphInstance.zoomToFit).toHaveBeenCalledWith(1200, 60)

    // Toggle auto-rotate on
    wrapper.vm.toggleAutoRotate()
    const initialCalls = mockGraphInstance.cameraPosition.mock.calls.length

    // Advance timers by 100ms (4 steps of 25ms interval)
    vi.advanceTimersByTime(100)
    expect(mockGraphInstance.cameraPosition.mock.calls.length).toBeGreaterThan(initialCalls)
    const lastCall = mockGraphInstance.cameraPosition.mock.calls.at(-1)?.[0]
    expect(lastCall).toHaveProperty('x')
    expect(lastCall).toHaveProperty('y')
    expect(lastCall).toHaveProperty('z')

    // Toggle auto-rotate off
    wrapper.vm.toggleAutoRotate()
    const callsAfterOff = mockGraphInstance.cameraPosition.mock.calls.length
    vi.advanceTimersByTime(100)
    expect(mockGraphInstance.cameraPosition.mock.calls.length).toBe(callsAfterOff)

    vi.useRealTimers()
  })

  it('halts auto-rotation timer on unmount and when switching viewMode to 2d', async () => {
    vi.useFakeTimers()
    const store = createTestStore()
    const wrapper = mount(GraphCanvas3D)
    await flushPromises()

    // Start auto-rotate
    wrapper.vm.toggleAutoRotate()
    vi.advanceTimersByTime(50)
    const callsBeforeUnmount = mockGraphInstance.cameraPosition.mock.calls.length
    expect(callsBeforeUnmount).toBeGreaterThan(0)

    // Unmount
    wrapper.unmount()
    vi.advanceTimersByTime(100)
    expect(mockGraphInstance.cameraPosition.mock.calls.length).toBe(callsBeforeUnmount)

    vi.useRealTimers()
  })

  it('halts auto-rotation timer when switching viewMode to 2d', async () => {
    vi.useFakeTimers()
    const store = createTestStore()
    store.viewMode = '3d'
    const wrapper = mount(GraphCanvas3D)
    await flushPromises()

    wrapper.vm.toggleAutoRotate()
    vi.advanceTimersByTime(50)
    const callsBefore = mockGraphInstance.cameraPosition.mock.calls.length
    expect(callsBefore).toBeGreaterThan(0)

    // Switch viewMode to 2d
    store.viewMode = '2d'
    await flushPromises()
    vi.advanceTimersByTime(100)
    expect(mockGraphInstance.cameraPosition.mock.calls.length).toBe(callsBefore)

    vi.useRealTimers()
  })

  it('renders floating 3D HUD controls with buttons', async () => {
    createTestStore()
    const wrapper = mount(GraphCanvas3D)
    await flushPromises()

    const hudButtons = wrapper.findAll('div.absolute.bottom-5.right-5 button')
    expect(hudButtons.length).toBe(4) // AutoRotate, FitScreen, ZoomIn, ZoomOut
  })

  it('cleans up graph instance on unmount', async () => {
    createTestStore()
    const wrapper = mount(GraphCanvas3D)
    await flushPromises()

    wrapper.unmount()
    expect(mockGraphInstance._destructor).toHaveBeenCalled()
  })
})
