import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, shallowMount } from '@vue/test-utils'
import { ref } from 'vue'
import ReaderPage from '../../app/pages/read/[bookId].vue'
import { useReaderTypography } from '../../app/composables/useReaderTypography'
import type { BookDetail, ChunkSummary } from '../../app/stores/useLibraryStore'
import type { AudioNavigationRequest } from '../../app/components/reader/ReaderAudioPlayer.vue'

enableAutoUnmount(afterEach)

function fullSlice(order: number): ChunkSummary {
  return {
    id: `chunk-${order}`,
    chunkOrder: order,
    chapterTitle: ['Reliability', 'Data Models', 'Storage'][order - 1]!,
    summaryMarkdown: '',
    originalTextMarkdown: `Complete narration for chapter ${order}. This content is ready to read.`,
    keyTakeaways: [],
    estimatedReadMinutes: 2,
    isAiFormatted: true,
    language: 'en',
  }
}

function bookOverview(): BookDetail {
  return {
    id: 'reader-book', title: 'Data Systems', slug: 'data-systems', sourceType: 0,
    categoryId: 'reader-category',
    category: {
      id: 'reader-category', slug: 'data-systems', nameEn: 'Data Systems',
      nameVi: 'Data Systems', sortOrder: 0, isActive: true, version: 1,
    },
    totalChunks: 3, isPublished: true, createdAt: '2026-10-01T00:00:00Z', language: 'en',
    // The real overview API returns metadata without the full slice content.
    chunks: [1, 2, 3].map(order => ({ ...fullSlice(order), originalTextMarkdown: '' })),
  }
}

function navigation(isCurrent = () => true): AudioNavigationRequest {
  return { isCurrent, accept: () => isCurrent(), cancel: vi.fn() }
}

let destination = Promise.withResolvers<ChunkSummary | null>()
let deferredOrder = 2
const query = ref<Record<string, string>>({})

beforeEach(() => {
  vi.useFakeTimers()
  localStorage.clear()
  destination = Promise.withResolvers<ChunkSummary | null>()
  deferredOrder = 2
  query.value = {}
  vi.stubGlobal('useRoute', () => ({ params: { bookId: 'reader-book' }, get query() { return query.value } }))
  vi.stubGlobal('useRouter', () => ({ push: vi.fn(), replace: vi.fn() }))
  vi.stubGlobal('useAuthStore', () => ({ isAuthenticated: true, user: { id: 'reader-account' }, sessionGeneration: 0 }))
  vi.stubGlobal('useDailyFocusStore', () => ({ fetchTodayFocus: vi.fn().mockResolvedValue(undefined) }))
  vi.stubGlobal('useNotesStore', () => ({}))
  vi.stubGlobal('useReaderTypography', useReaderTypography)
  vi.stubGlobal('useMarkdownRenderer', () => ({ render: () => '', isHighlighterReady: ref(true) }))
  vi.stubGlobal('useLibraryStore', () => ({
    isLoading: false,
    fetchBookById: vi.fn().mockImplementation(async () => bookOverview()),
    fetchSlice: vi.fn().mockImplementation(async (_bookId: string, order: number) => (
      order === deferredOrder ? destination.promise : fullSlice(order)
    )),
  }))
  vi.spyOn(HTMLElement.prototype, 'scrollTo').mockImplementation(() => {})
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  vi.useRealTimers()
})

describe('reader narration navigation', () => {
  it('loads complete next-slice content before accepting natural continuation', async () => {
    const wrapper = shallowMount(ReaderPage)
    await flushPromises()

    wrapper.getComponent({ name: 'ReaderAudioPlayer' }).vm.$emit('auto-advance', navigation())
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Reliability')
    expect(localStorage.getItem('deeppace_bookmark_reader-book')).toBeNull()

    destination.resolve(fullSlice(2))
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Data Models')
    expect(localStorage.getItem('deeppace_bookmark_reader-book')).toBe('2')
  })

  it('keeps the current slice if playback permission is canceled while destination content loads', async () => {
    const wrapper = shallowMount(ReaderPage)
    await flushPromises()
    let current = true

    wrapper.getComponent({ name: 'ReaderAudioPlayer' }).vm.$emit('auto-advance', navigation(() => current))
    current = false
    destination.resolve(fullSlice(2))
    await flushPromises()

    expect(wrapper.get('main h1').text()).toBe('Reliability')
    expect(localStorage.getItem('deeppace_bookmark_reader-book')).toBeNull()
  })

  it('does not replace a manually selected source with an older pending continuation', async () => {
    const wrapper = shallowMount(ReaderPage)
    await flushPromises()

    wrapper.getComponent({ name: 'ReaderAudioPlayer' }).vm.$emit('auto-advance', navigation())
    wrapper.getComponent({ name: 'ReaderTocSidebar' }).vm.$emit('select-chunk', 2)
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Storage')

    destination.resolve(fullSlice(2))
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Storage')
    expect(localStorage.getItem('deeppace_bookmark_reader-book')).toBe('3')
  })

  it('keeps the current slice when destination content cannot be loaded', async () => {
    const wrapper = shallowMount(ReaderPage)
    await flushPromises()

    wrapper.getComponent({ name: 'ReaderAudioPlayer' }).vm.$emit('auto-advance', navigation())
    destination.resolve(null)
    await flushPromises()

    expect(wrapper.get('main h1').text()).toBe('Reliability')
    expect(localStorage.getItem('deeppace_bookmark_reader-book')).toBeNull()
  })

  it('prepares complete content before media-session next-track navigation', async () => {
    const wrapper = shallowMount(ReaderPage)
    await flushPromises()

    wrapper.getComponent({ name: 'ReaderAudioPlayer' }).vm.$emit('seek-slice', 'next', navigation())
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Reliability')

    destination.resolve(fullSlice(2))
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Data Models')
  })

  it('prepares complete content before media-session previous-track navigation', async () => {
    deferredOrder = 1
    query.value = { slice: '2' }
    const wrapper = shallowMount(ReaderPage)
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Data Models')

    wrapper.getComponent({ name: 'ReaderAudioPlayer' }).vm.$emit('seek-slice', 'prev', navigation())
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Data Models')

    destination.resolve(fullSlice(1))
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Reliability')
  })

  it('honors a valid external slice-query change without remounting the reader', async () => {
    const wrapper = shallowMount(ReaderPage)
    await flushPromises()
    query.value = { slice: '3' }
    await flushPromises()
    expect(wrapper.get('main h1').text()).toBe('Storage')
  })
})
