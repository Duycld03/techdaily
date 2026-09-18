import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import TodayPage from '~/pages/today.vue'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
import { useLibraryStore } from '~/stores/useLibraryStore'

describe('pages/today.vue - Studio Control Bar & 3-Column Layout', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  const mockPacerData = {
    topic: {
      id: 't-1',
      title: 'PostgreSQL 17 Deep Dive',
      dayOrder: 1,
      category: 1,
      summary: 'PostgreSQL MVCC & Isolation'
    },
    pacer: {
      bookId: 'book-101',
      bookTitle: 'PostgreSQL 17 Internals',
      currentChunkOrder: 3,
      totalChunks: 10,
      chapterTitle: 'MVCC Architecture & Vacuum',
      hasPrevious: true,
      hasNext: true,
      availableBooks: [
        {
          id: 'book-101',
          title: 'PostgreSQL 17 Internals',
          currentChunkOrder: 3,
          totalChunks: 10,
          progressPercentage: 30,
          isActive: true
        }
      ]
    },
    documentChunk: {
      id: 'chunk-3',
      chunkOrder: 3,
      chapterTitle: 'MVCC Architecture & Vacuum',
      summaryMarkdown: 'Summary',
      originalTextMarkdown: 'Original text'
    },
    question: {
      id: 'q-1',
      questionText: 'Which isolation level prevents Phantom Reads?',
      options: ['Read Committed', 'Repeatable Read', 'Serializable'],
      correctOptionIndex: 1
    },
    estimatedMinutes: 4
  }

  it('renders Studio Control Bar with book title, slice order, and toggles', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = mockPacerData as any
    vi.spyOn(focusStore, 'fetchTodayFocus').mockResolvedValue(mockPacerData as any)

    const wrapper = mount(TodayPage, {
      global: {
        stubs: {
          DocReaderPane: { template: '<div class="doc-reader-stub">Reader</div>' },
          InterviewChallengePane: { template: '<div class="challenge-stub">Challenge</div>' },
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Studio Control Bar elements
    expect(wrapper.text()).toContain('PostgreSQL 17 Internals')
    expect(wrapper.text()).toContain('MVCC Architecture & Vacuum')
    expect(wrapper.text()).toContain('3/10')
    expect(wrapper.text()).toContain('4m')
  })

  it('toggles Left Rail Outline Navigator when Outline button is clicked', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = mockPacerData as any
    vi.spyOn(focusStore, 'fetchTodayFocus').mockResolvedValue(mockPacerData as any)

    const libraryStore = useLibraryStore()
    libraryStore.selectedBook = {
      id: 'book-101',
      title: 'PostgreSQL 17 Internals',
      chunks: [
        { id: 'c-1', chunkOrder: 1, chapterTitle: 'Overview', estimatedReadMinutes: 3 },
        { id: 'c-2', chunkOrder: 2, chapterTitle: 'Storage Engine', estimatedReadMinutes: 4 },
        { id: 'c-3', chunkOrder: 3, chapterTitle: 'MVCC Architecture', estimatedReadMinutes: 4 }
      ]
    } as any

    const wrapper = mount(TodayPage, {
      global: {
        stubs: {
          DocReaderPane: { template: '<div class="doc-reader-stub">Reader</div>' },
          InterviewChallengePane: { template: '<div class="challenge-stub">Challenge</div>' },
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Initially outline is closed
    expect(wrapper.find('aside').exists()).toBe(false)

    // Find Outline button and click
    const outlineBtn = wrapper.find('button[title*="Outline"]')
    expect(outlineBtn.exists()).toBe(true)
    await outlineBtn.trigger('click')

    // Aside outline rail is now open
    expect(wrapper.find('aside').exists()).toBe(true)
    expect(wrapper.text()).toContain('Storage Engine')
  })

  it('toggles Scenario Copilot Dock for full immersion reading', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = mockPacerData as any
    vi.spyOn(focusStore, 'fetchTodayFocus').mockResolvedValue(mockPacerData as any)

    const wrapper = mount(TodayPage, {
      global: {
        stubs: {
          DocReaderPane: { template: '<div class="doc-reader-stub">Reader</div>' },
          InterviewChallengePane: { template: '<div class="challenge-stub">Challenge</div>' },
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Initially Scenario Challenge dock is open
    expect(wrapper.find('.challenge-stub').exists()).toBe(true)

    // Find Dock toggle button and click
    const dockBtn = wrapper.find('button[title*="Dock"], button[title*="Scenario"]')
    expect(dockBtn.exists()).toBe(true)
    await dockBtn.trigger('click')

    // Scenario Challenge dock is collapsed (full immersion mode)
    expect(wrapper.find('.challenge-stub').exists()).toBe(false)
  })
})
