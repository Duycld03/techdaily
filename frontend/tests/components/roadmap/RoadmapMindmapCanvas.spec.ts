import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import RoadmapMindmapCanvas from '~/components/roadmap/RoadmapMindmapCanvas.vue'
import type { BookDetail } from '~/stores/useLibraryStore'
import type { CurriculumRoadmapData } from '~/stores/useRoadmapStore'

const mockPush = vi.fn()

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: mockPush
  }),
  useRoute: () => ({
    path: '/roadmap',
    query: {}
  })
}))

describe('components/roadmap/RoadmapMindmapCanvas.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  const mockBook: BookDetail = {
    id: 'book-123',
    title: 'Designing Data-Intensive Applications',
    slug: 'ddia',
    sourceType: 1,
    category: 2,
    totalChunks: 3,
    isPublished: true,
    progressPercentage: 33,
    createdAt: '2026-01-01',
    chunks: []
  }

  const mockMilestones = [
    {
      chapterTitle: 'Chapter 1: Foundations',
      chapterIndex: 1,
      isCompleted: true,
      isActive: false,
      completedSlicesCount: 1,
      totalSlicesCount: 1,
      slices: [
        {
          id: 'slice-1',
          chunkOrder: 1,
          sliceTitle: 'Reliability',
          summaryMarkdown: 'Faults and reliability.',
          estimatedReadMinutes: 8,
          isCompleted: true,
          isActiveToday: false,
          isUpcoming: false
        }
      ]
    },
    {
      chapterTitle: 'Chapter 2: Data Models',
      chapterIndex: 2,
      isCompleted: false,
      isActive: true,
      completedSlicesCount: 0,
      totalSlicesCount: 2,
      slices: [
        {
          id: 'slice-2',
          chunkOrder: 2,
          sliceTitle: 'Relational vs Document',
          summaryMarkdown: 'Schema on read vs schema on write.',
          estimatedReadMinutes: 12,
          isCompleted: false,
          isActiveToday: true,
          isUpcoming: false
        },
        {
          id: 'slice-3',
          chunkOrder: 3,
          sliceTitle: 'Query Languages',
          summaryMarkdown: 'Declarative vs imperative queries.',
          estimatedReadMinutes: 10,
          isCompleted: false,
          isActiveToday: false,
          isUpcoming: true
        }
      ]
    }
  ]

  it('renders root node and chapter branches with the active chapter expanded by default', () => {
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    // Root node
    const rootNode = wrapper.find('[data-testid="root-node"]')
    expect(rootNode.exists()).toBe(true)
    expect(rootNode.text()).toContain('Designing Data-Intensive Applications')

    // Chapter nodes
    const ch1 = wrapper.find('[data-testid="chapter-node-1"]')
    const ch2 = wrapper.find('[data-testid="chapter-node-2"]')
    expect(ch1.exists()).toBe(true)
    expect(ch2.exists()).toBe(true)

    // Active chapter (ch-2) is expanded by default -> slice-2 and slice-3 should be visible
    expect(wrapper.find('[data-testid="slice-node-slice-2"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="slice-node-slice-3"]').exists()).toBe(true)
    // Collapsed chapter (ch-1) -> slice-1 should not be in slices layout
    expect(wrapper.find('[data-testid="slice-node-slice-1"]').exists()).toBe(false)
  })

  it('toggles chapter branch expand and collapse on click', async () => {
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    // ch-1 is collapsed initially -> click to expand
    const ch1 = wrapper.find('[data-testid="chapter-node-1"]')
    await ch1.trigger('click')

    expect(wrapper.find('[data-testid="slice-node-slice-1"]').exists()).toBe(true)

    // ch-2 is expanded initially -> click to collapse
    const ch2 = wrapper.find('[data-testid="chapter-node-2"]')
    await ch2.trigger('click')

    expect(wrapper.find('[data-testid="slice-node-slice-2"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="slice-node-slice-3"]').exists()).toBe(false)
  })

  it('controls zoom in, zoom out, fit to screen, expand all, and collapse all via toolbar buttons', async () => {
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    // Expand all
    const btnExpandAll = wrapper.find('[data-testid="btn-expand-all"]')
    await btnExpandAll.trigger('click')

    expect(wrapper.find('[data-testid="slice-node-slice-1"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="slice-node-slice-2"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="slice-node-slice-3"]').exists()).toBe(true)

    // Collapse all
    const btnCollapseAll = wrapper.find('[data-testid="btn-collapse-all"]')
    await btnCollapseAll.trigger('click')

    expect(wrapper.find('[data-testid="slice-node-slice-1"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="slice-node-slice-2"]').exists()).toBe(false)

    // Zoom In
    const btnZoomIn = wrapper.find('[data-testid="btn-zoom-in"]')
    await btnZoomIn.trigger('click')

    // Zoom Out
    const btnZoomOut = wrapper.find('[data-testid="btn-zoom-out"]')
    await btnZoomOut.trigger('click')

    // Fit to Screen
    const btnFit = wrapper.find('[data-testid="btn-fit-screen"]')
    await btnFit.trigger('click')
  })

  it('navigates to /today when active today slice is clicked', async () => {
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    const activeSliceNode = wrapper.find('[data-testid="slice-node-slice-2"]')
    expect(activeSliceNode.exists()).toBe(true)
    await activeSliceNode.trigger('click')

    expect(mockPush).toHaveBeenCalledWith('/today?bookId=book-123&chunkOrder=2')
  })

  it('navigates to /read/[bookId]?slice={chunkOrder} when upcoming slice is clicked', async () => {
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    const upcomingSliceNode = wrapper.find('[data-testid="slice-node-slice-3"]')
    expect(upcomingSliceNode.exists()).toBe(true)
    await upcomingSliceNode.trigger('click')

    expect(mockPush).toHaveBeenCalledWith('/read/book-123?slice=3')
  })

  it('renders curriculum track and navigates to /today?day={dayOrder} when curriculum day is clicked', async () => {
    const mockCurriculumData: CurriculumRoadmapData = {
      totalDays: 30,
      completedDaysCount: 1,
      currentActiveDay: 2,
      overallProgressPercentage: 3,
      modules: [
        {
          category: 0,
          moduleTitle: 'Frontend Architecture',
          description: 'Web performance',
          startDay: 1,
          endDay: 2,
          completedCount: 1,
          totalCount: 2,
          days: [
            {
              dayOrder: 1,
              slug: 'vue-internals',
              title: 'Reactivity System',
              summary: 'Proxies and effects',
              difficulty: 1,
              isCompleted: true,
              isActiveToday: false,
              isUnlocked: true,
              drillScore: 85
            },
            {
              dayOrder: 2,
              slug: 'nuxt-ssr',
              title: 'Universal Hydration',
              summary: 'Hydration flow',
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

    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        roadmapData: mockCurriculumData,
        isCurriculumSelected: true
      }
    })

    const rootNode = wrapper.find('[data-testid="root-node"]')
    expect(rootNode.text()).toContain('30-Day Senior Curriculum')

    // Day 2 is active today -> expanded by default
    const day2Node = wrapper.find('[data-testid="slice-node-2"]')
    expect(day2Node.exists()).toBe(true)

    await day2Node.trigger('click')
    expect(mockPush).toHaveBeenCalledWith('/today?day=2')
  })

  it('centers view on active milestone when focus active button is clicked', async () => {
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    const btnFocus = wrapper.find('[data-testid="btn-focus-active"]')
    expect(btnFocus.exists()).toBe(true)
    await btnFocus.trigger('click')

    // Zoom should be at 100%
    expect(wrapper.text()).toContain('100%')
  })

  it('enforces auto-accordion behavior when chapter count exceeds 12', async () => {
    // Generate 15 chapters
    const fifteenMilestones = Array.from({ length: 15 }, (_, i) => ({
      chapterTitle: `Chapter ${i + 1}`,
      chapterIndex: i + 1,
      isCompleted: false,
      isActive: i === 0,
      completedSlicesCount: 0,
      totalSlicesCount: 1,
      slices: [
        {
          id: `slice-${i + 1}`,
          chunkOrder: i + 1,
          sliceTitle: `Topic ${i + 1}`,
          estimatedReadMinutes: 5,
          isCompleted: false,
          isActiveToday: i === 0,
          isUpcoming: i > 0
        }
      ]
    }))

    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: fifteenMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 1
      }
    })

    // Initially chapter 1 is active and expanded
    expect(wrapper.find('[data-testid="slice-node-slice-1"]').exists()).toBe(true)

    // Click chapter 5 to expand -> chapter 1 should auto-collapse!
    const ch5 = wrapper.find('[data-testid="chapter-node-5"]')
    await ch5.trigger('click')

    expect(wrapper.find('[data-testid="slice-node-slice-5"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="slice-node-slice-1"]').exists()).toBe(false)
  })

  it('filters nodes and auto-expands matching chapters on in-canvas search', async () => {
    vi.useFakeTimers()
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    const searchInput = wrapper.find('[data-testid="mindmap-search-input"]')
    expect(searchInput.exists()).toBe(true)
    const searchContainer = searchInput.element.closest('div')
    expect(searchContainer?.classList.contains('z-20')).toBe(true)
    // Search for "Reliability" which belongs to collapsed chapter 1
    await searchInput.setValue('Reliability')
    vi.advanceTimersByTime(200)
    await wrapper.vm.$nextTick()

    // Chapter 1 should be auto-expanded because its slice matches!
    const slice1Node = wrapper.find('[data-testid="slice-node-slice-1"]')
    expect(slice1Node.exists()).toBe(true)
    expect(slice1Node.classes()).toContain('ring-2')

    // Clear search button should appear
    const btnClear = wrapper.find('[data-testid="btn-clear-search"]')
    expect(btnClear.exists()).toBe(true)
    await btnClear.trigger('click')
    vi.advanceTimersByTime(200)
    await wrapper.vm.$nextTick()

    expect((searchInput.element as HTMLInputElement).value).toBe('')
    vi.useRealTimers()
  })

  it('captures mouse pan events on window and releases panning when mouseup is fired on window', async () => {
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    const container = wrapper.find('.relative.w-full')
    expect(container.classes()).toContain('cursor-grab')

    // mousedown initiates pan
    await container.trigger('mousedown', { clientX: 100, clientY: 100 })
    expect(container.classes()).toContain('cursor-grabbing')

    // mousemove on window updates pan position
    window.dispatchEvent(new MouseEvent('mousemove', { clientX: 150, clientY: 160 }))
    await wrapper.vm.$nextTick()

    // mouseup on window outside container terminates pan
    window.dispatchEvent(new MouseEvent('mouseup'))
    await wrapper.vm.$nextTick()

    expect(container.classes()).toContain('cursor-grab')
    expect(container.classes()).not.toContain('cursor-grabbing')
  })

  it('suppresses transitions on SVG bezier edges while panning', async () => {
    const wrapper = mount(RoadmapMindmapCanvas, {
      props: {
        selectedBook: mockBook,
        chapterMilestones: mockMilestones,
        isCurriculumSelected: false,
        activeBookId: 'book-123',
        currentChunkOrder: 2
      }
    })

    const container = wrapper.find('.relative.w-full')
    const edgePath = wrapper.find('.edges-layer path')
    expect(edgePath.classes()).toContain('transition-all')
    expect(edgePath.classes()).not.toContain('transition-none')

    // mousedown initiates pan -> transition-none should be applied
    await container.trigger('mousedown', { clientX: 100, clientY: 100 })
    expect(edgePath.classes()).toContain('transition-none')

    // mouseup terminates pan -> transition-all restored
    window.dispatchEvent(new MouseEvent('mouseup'))
    await wrapper.vm.$nextTick()
    expect(edgePath.classes()).toContain('transition-all')
  })
})
