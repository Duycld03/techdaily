import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import AskBookDrawer from '~/components/reader/AskBookDrawer.vue'
import { useLibraryStore } from '~/stores/useLibraryStore'

vi.mock('~/composables/useMarkdownRenderer', () => ({
  useMarkdownRenderer: () => ({
    render: vi.fn((md: string) => `<p>${md}</p>`),
    isHighlighterReady: ref(true)
  })
}))

vi.mock('~/composables/useApiError', () => ({
  useApiError: () => ({
    formatError: vi.fn((err: any) => err.message || 'Error occurred')
  })
}))

describe('AskBookDrawer.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  const globalConfig = {
    stubs: {
      Teleport: true
    },
    mocks: {
      $t: (k: string, params?: any) => {
        if (params?.current) return `Slice ${params.current} of ${params.total}`
        return k
      }
    }
  }

  it('does not render content when isOpen is false', () => {
    const wrapper = mount(AskBookDrawer, {
      props: {
        isOpen: false,
        bookId: 'book-1',
        bookTitle: 'DDIA'
      },
      global: globalConfig
    })

    expect(wrapper.find('h2').exists()).toBe(false)
  })

  it('renders title and suggested questions when isOpen is true and empty', () => {
    const wrapper = mount(AskBookDrawer, {
      props: {
        isOpen: true,
        bookId: 'book-1',
        bookTitle: 'Designing Data-Intensive Applications'
      },
      global: globalConfig
    })

    expect(wrapper.text()).toContain('reader.ask_book_title')
    expect(wrapper.text()).toContain('Designing Data-Intensive Applications')
    expect(wrapper.text()).toContain('What are the core architecture principles explained here?')
  })

  it('emits close when close button is clicked', async () => {
    const wrapper = mount(AskBookDrawer, {
      props: {
        isOpen: true,
        bookId: 'book-1'
      },
      global: globalConfig
    })

    const closeBtn = wrapper.find('button[title="reader.close_drawer"]')
    expect(closeBtn.exists()).toBe(true)
    await closeBtn.trigger('click')

    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('asks question via library store and displays citations', async () => {
    const store = useLibraryStore()
    vi.spyOn(store, 'askBook').mockResolvedValueOnce({
      answerMarkdown: 'LSM-Trees write sequentially to append-only logs.',
      citations: [
        {
          chunkOrder: 3,
          chapterTitle: 'Storage Engines',
          relevanceScore: 0.92,
          excerpt: 'LSM trees optimize write throughput...'
        }
      ]
    })

    const wrapper = mount(AskBookDrawer, {
      props: {
        isOpen: true,
        bookId: 'book-1',
        currentChunkId: 'chk-3',
        currentChunkOrder: 3
      },
      global: globalConfig
    })

    const textarea = wrapper.find('textarea')
    await textarea.setValue('How do LSM Trees work?')

    const sendBtn = wrapper.find('button[title="reader.ask_btn"]')
    await sendBtn.trigger('click')

    expect(store.askBook).toHaveBeenCalledWith('book-1', 'How do LSM Trees work?', 'chk-3', 'en')

    // Wait for promise resolution
    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('How do LSM Trees work?')
      expect(wrapper.text()).toContain('LSM-Trees write sequentially')
      expect(wrapper.text()).toContain('Storage Engines')
      expect(wrapper.text()).toContain('92%')
    })

    // Click citation pill to verify jump-to-slice emit
    const citationBtn = wrapper.find('button[title="LSM trees optimize write throughput..."]')
    expect(citationBtn.exists()).toBe(true)
    await citationBtn.trigger('click')

    expect(wrapper.emitted('jump-to-slice')).toBeTruthy()
    expect(wrapper.emitted('jump-to-slice')![0]).toEqual([3])
  })
})
