import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import RoadmapPage from '~/pages/roadmap.vue'
import { useRoadmapStore } from '~/stores/useRoadmapStore'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
import { useLibraryStore } from '~/stores/useLibraryStore'
import { useAuthStore } from '~/stores/useAuthStore'

const mockPush = vi.fn()
let mockRouteQuery: Record<string, string | undefined> = {}

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: mockPush,
    replace: vi.fn()
  }),
  useRoute: () => ({
    path: '/roadmap',
    query: mockRouteQuery
  })
}))

const RoadmapMindmapCanvasStub = {
  name: 'RoadmapMindmapCanvas',
  template: '<div data-testid="mindmap-canvas-stub">Mindmap Canvas</div>',
  props: ['selectedBook', 'chapterMilestones', 'roadmapData', 'isCurriculumSelected', 'activeBookId']
}

describe('pages/roadmap.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockRouteQuery = {}
    setActivePinia(createPinia())
  })

  function setupMockStores() {
    const focusStore = useDailyFocusStore()
    const libraryStore = useLibraryStore()
    const roadmapStore = useRoadmapStore()
    const authStore = useAuthStore()

    // Mock focusStore
    focusStore.data = {
      pacer: {
        bookId: 'book-1',
        bookTitle: 'Designing Data-Intensive Applications',
        chapterTitle: 'Chapter 2: Data Models',
        currentChunkOrder: 2,
        totalChunks: 6,
        progressPercentage: 33,
        hasPrevious: true,
        hasNext: true,
        availableBooks: [
          {
            id: 'book-1',
            title: 'Designing Data-Intensive Applications',
            progressPercentage: 33,
            totalChunks: 6,
            currentChunkOrder: 2,
            isActive: true
          },
          {
            id: 'book-2',
            title: 'PostgreSQL 17 Internals',
            progressPercentage: 20,
            totalChunks: 10,
            currentChunkOrder: 2,
            isActive: false
          }
        ]
      }
    } as unknown as typeof focusStore.data

    vi.spyOn(focusStore, 'fetchTodayFocus').mockResolvedValue(focusStore.data ?? undefined)
    vi.spyOn(focusStore, 'switchBook').mockResolvedValue(focusStore.data!.pacer!)

    // Mock libraryStore
    libraryStore.selectedBook = {
      id: 'book-1',
      title: 'Designing Data-Intensive Applications',
      slug: 'ddia',
      sourceType: 1,
      category: 2,
      totalChunks: 6,
      isPublished: true,
      progressPercentage: 33,
      createdAt: '2026-01-01',
      chunks: [
        {
          id: 'chunk-1',
          chunkOrder: 1,
          chapterTitle: 'Foundations: Reliability',
          summaryMarkdown: 'Building reliable systems.',
          originalTextMarkdown: '',
          keyTakeaways: [],
          estimatedReadMinutes: 10
        },
        {
          id: 'chunk-2',
          chunkOrder: 2,
          chapterTitle: 'Foundations: Scalability',
          summaryMarkdown: 'Measuring and planning for load.',
          originalTextMarkdown: '',
          keyTakeaways: [],
          estimatedReadMinutes: 12
        }
      ]
    } as unknown as typeof libraryStore.selectedBook

    vi.spyOn(libraryStore, 'fetchBookById').mockResolvedValue(libraryStore.selectedBook!)

    // Mock roadmapStore
    roadmapStore.roadmapData = {
      totalDays: 30,
      completedDaysCount: 8,
      currentActiveDay: 9,
      overallProgressPercentage: 27,
      modules: [
        {
          category: 0,
          moduleTitle: 'Frontend & Web Core',
          description: 'Browser architecture and reactive patterns',
          startDay: 1,
          endDay: 7,
          completedCount: 7,
          totalCount: 7,
          days: []
        },
        {
          category: 1,
          moduleTitle: 'Backend & Systems',
          description: 'High throughput distributed systems',
          startDay: 8,
          endDay: 14,
          completedCount: 1,
          totalCount: 7,
          days: [
            {
              dayOrder: 9,
              slug: 'event-loop',
              title: 'Concurrency & Event Loops',
              summary: 'Threading vs async execution.',
              difficulty: 1,
              isCompleted: false,
              isActiveToday: true,
              isUnlocked: true,
              drillScore: null
            }
          ]
        }
      ]
    }

    vi.spyOn(roadmapStore, 'fetchRoadmap').mockResolvedValue()

    return { focusStore, libraryStore, roadmapStore, authStore }
  }

  it('renders a single unified header without competing view tabs', async () => {
    setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // Competing dual tabs must NOT exist
    const tabs = wrapper.findAll('button')
    const hasOldTab = tabs.some(t => t.text() === 'Switch to Curriculum' || t.text() === 'Active Book')
    expect(hasOldTab).toBe(false)

    // Single unified header with track switcher exists
    const trackSwitcherBtn = wrapper.find('[data-testid="track-switcher-btn"]')
    expect(trackSwitcherBtn.exists()).toBe(true)
    expect(trackSwitcherBtn.text()).toContain('Designing Data-Intensive Applications')
  })

  it('toggles the track switcher dropdown and renders available books and library link without legacy demo track', async () => {
    setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // Popover is closed initially
    expect(wrapper.find('[data-testid="track-menu-popover"]').exists()).toBe(false)

    // Click to open
    const trackSwitcherBtn = wrapper.find('[data-testid="track-switcher-btn"]')
    await trackSwitcherBtn.trigger('click')

    const popover = wrapper.find('[data-testid="track-menu-popover"]')
    expect(popover.exists()).toBe(true)

    // Book options
    expect(wrapper.find('[data-testid="track-book-option-book-1"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="track-book-option-book-2"]').exists()).toBe(true)

    // Legacy demo curriculum option should NOT be in the dropdown
    expect(wrapper.find('[data-testid="track-curriculum-option"]').exists()).toBe(false)

    // Library link
    expect(wrapper.find('[data-testid="track-browse-library-link"]').exists()).toBe(true)
  })

  it('switches book track when an in-progress book is selected', async () => {
    const { focusStore, libraryStore } = setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // Open dropdown
    await wrapper.find('[data-testid="track-switcher-btn"]').trigger('click')

    // Click second book
    await wrapper.find('[data-testid="track-book-option-book-2"]').trigger('click')
    await flushPromises()

    // Must call switchBook on focusStore and fetchBookById on libraryStore
    expect(focusStore.switchBook).toHaveBeenCalledWith('book-2', 'en')
    expect(libraryStore.fetchBookById).toHaveBeenCalledWith('book-2')

    // Dropdown popover closes
    expect(wrapper.find('[data-testid="track-menu-popover"]').exists()).toBe(false)
  })

  it('switches to curriculum track when query track=curriculum is present', async () => {
    mockRouteQuery = { track: 'curriculum' }
    setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // Modules list should be displayed
    expect(wrapper.text()).toContain('Frontend & Web Core')
    expect(wrapper.text()).toContain('Backend & Systems')
  })

  it('renders empty state with library CTA when no active book and no available books exist', async () => {
    const { focusStore, libraryStore } = setupMockStores()
    focusStore.data = null as unknown as typeof focusStore.data
    libraryStore.selectedBook = null

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    expect(wrapper.find('[data-testid="roadmap-empty-state"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('roadmap.no_active_books')
  })

  it('renders track switcher popover with book options when triggered', async () => {
    setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // Open track switcher dropdown
    await wrapper.find('[data-testid="track-switcher-btn"]').trigger('click')

    const popover = wrapper.find('[data-testid="track-menu-popover"]')
    expect(popover.exists()).toBe(true)

    // All elements must be rendered inside the popover
    expect(wrapper.find('[data-testid="track-book-option-book-1"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="track-book-option-book-2"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="track-curriculum-option"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="track-browse-library-link"]').exists()).toBe(true)
  })

  it('renders RoadmapViewSwitcher and toggles between timeline and mindmap views', async () => {
    setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // View switcher tablist exists
    const switcher = wrapper.find('[role="tablist"]')
    expect(switcher.exists()).toBe(true)

    // Initially in timeline view -> timeline chapters visible, mindmap canvas not rendered
    expect(wrapper.find('[data-testid="mindmap-canvas-stub"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('Foundations')
    expect(wrapper.text()).toContain('Reliability')

    // Switch to mindmap view
    const mindmapTab = wrapper.find('#tab-mindmap')
    await mindmapTab.trigger('click')
    await flushPromises()

    // Mindmap canvas should now be rendered
    expect(wrapper.find('[data-testid="mindmap-canvas-stub"]').exists()).toBe(true)
  })

  it('navigates to /today when active slice in timeline is clicked', async () => {
    setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // Expand chapters
    const expandBtn = wrapper.findAll('button').find(b => b.text().includes('roadmap.expand_all'))
    if (expandBtn) {
      await expandBtn.trigger('click')
    }

    // Click "Start Today's Drill" button on active chapter or active slice
    const startTodayBtn = wrapper.findAll('button').find(b => b.text().includes('roadmap.start_today'))
    expect(startTodayBtn).toBeDefined()
    await startTodayBtn?.trigger('click')

    expect(mockPush).toHaveBeenCalledWith('/today?bookId=book-1&chunkOrder=2')
  })

  it('consolidates standalone delimiter-less chunks into balanced modules for large books', async () => {
    const focusStore = useDailyFocusStore()
    const libraryStore = useLibraryStore()
    setupMockStores()

    // Generate 16 standalone chunks without colons
    const chunks = Array.from({ length: 16 }, (_, i) => ({
      id: `standalone-chunk-${i + 1}`,
      chunkOrder: i + 1,
      chapterTitle: `Standalone Topic ${i + 1}`,
      summaryMarkdown: `Summary for topic ${i + 1}`,
      originalTextMarkdown: '',
      keyTakeaways: [],
      estimatedReadMinutes: 5
    }))

    libraryStore.selectedBook = {
      id: 'book-large',
      title: 'Large Architecture Monograph',
      slug: 'large-arch',
      sourceType: 1,
      category: 1,
      totalChunks: 16,
      isPublished: true,
      progressPercentage: 10,
      createdAt: '2026-01-01',
      chunks
    } as unknown as typeof libraryStore.selectedBook

    focusStore.data = {
      pacer: {
        bookId: 'book-large',
        bookTitle: 'Large Architecture Monograph',
        currentChunkOrder: 1,
        totalChunks: 16,
        progressPercentage: 10,
        hasPrevious: false,
        hasNext: true,
        availableBooks: []
      }
    } as unknown as typeof focusStore.data

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // 16 standalone chunks clustered by max 4 should yield exactly 4 chapters instead of 16
    const chapterHeaders = wrapper.findAll('.group\\/chapter, [data-chapter-index]')
    // Pass to mindmap stub
    const mindmapStub = wrapper.findComponent({ name: 'RoadmapMindmapCanvas' })
    expect(mindmapStub.exists()).toBe(false) // In timeline mode

    // Switch to mindmap to inspect passed prop
    const mindmapTab = wrapper.find('#tab-mindmap')
    await mindmapTab.trigger('click')
    await flushPromises()

    const mindmapCanvas = wrapper.findComponent({ name: 'RoadmapMindmapCanvas' })
    expect(mindmapCanvas.exists()).toBe(true)
    const milestones = mindmapCanvas.props('chapterMilestones')
    expect(milestones.length).toBe(4)
    expect(milestones[0].slices.length).toBe(4)
    expect(milestones[3].slices.length).toBe(4)
  })

  it('renders continuous vertical timeline spine and chapter node beads in book track', async () => {
    setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // Spine container and vertical gradient connector line
    const spineContainer = wrapper.find('.relative.pl-6')
    expect(spineContainer.exists()).toBe(true)

    const spineLine = spineContainer.find('.absolute.w-0\\.5')
    expect(spineLine.exists()).toBe(true)
    // Milestone beads for chapters exist
    const milestoneBeads = wrapper.findAll('.rounded-full.z-10')
    expect(milestoneBeads.length).toBeGreaterThan(0)
  })

  it('renders illuminated active telemetry highlights on active slice', async () => {
    setupMockStores()

    const wrapper = mount(RoadmapPage, {
      global: {
        stubs: {
          RoadmapMindmapCanvas: RoadmapMindmapCanvasStub,
          NuxtLink: { template: '<a><slot /></a>' }
        }
      }
    })
    await flushPromises()

    // Active today slice has active telemetry ring & brand styling
    const activeSlice = wrapper.find('.ring-2.ring-brand-500\\/30')
    expect(activeSlice.exists()).toBe(true)
    expect(activeSlice.text()).toContain('roadmap.today')
  })
})
