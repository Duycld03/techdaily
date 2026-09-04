import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import DocReaderPane from '~/components/today/DocReaderPane.vue'

vi.mock('~/composables/useMarkdownRenderer', () => ({
  useMarkdownRenderer: () => ({
    render: vi.fn((md: string) => {
      // Emulate Shiki markdown renderer with code block wrapper
      if (md.includes('```')) {
        return `<div class="code-block-wrapper max-w-full w-full min-w-0"><div class="code-content overflow-x-auto max-w-full w-full"><pre class="shiki overflow-x-auto max-w-full"><code>queryClient.setQueryData(['items'], (old) => updateLocal(old, itemId));</code></pre></div></div>`
      }
      return `<p>${md}</p>`
    }),
    isHighlighterReady: ref(true)
  })
}))

const mockTopic = {
  id: 'topic-day-5',
  dayOrder: 5,
  slug: 'state-management-server-caching',
  title: 'State Management & Server State Caching',
  category: 0,
  difficulty: 'Senior',
  summary: 'Client State vs Server State Caching',
  deepDiveMarkdown: '```ts\nqueryClient.setQueryData([\'items\'], (old) => updateLocal(old, itemId));\n```',
  benchmarkSnippet: 'Stale-While-Revalidate Flow'
}

const mockDocumentChunk = {
  id: 'chunk-day-5',
  documentBookId: 'book-1',
  chunkOrder: 5,
  chapterTitle: 'State Management & Server State Caching',
  originalTextMarkdown: 'Original context',
  summaryMarkdown: 'Summary',
  keyTakeaways: ['Key Takeaway 1', 'Key Takeaway 2'],
  language: 'en',
  estimatedReadMinutes: 3
}

describe('DocReaderPane.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders topic title and key takeaways properly', () => {
    const wrapper = mount(DocReaderPane, {
      props: {
        topic: mockTopic,
        documentChunk: mockDocumentChunk
      },
      global: {
        mocks: {
          $t: (key: string) => key,
          t: (key: string) => key,
          locale: 'en'
        }
      }
    })

    expect(wrapper.text()).toContain('State Management & Server State Caching')
    expect(wrapper.text()).toContain('Client State vs Server State Caching')
    expect(wrapper.text()).toContain('Key Takeaway 1')
    expect(wrapper.text()).toContain('Key Takeaway 2')
  })

  it('renders code snippet with full content and horizontal scrolling classes without clipping', () => {
    const wrapper = mount(DocReaderPane, {
      props: {
        topic: mockTopic,
        documentChunk: mockDocumentChunk
      },
      global: {
        mocks: {
          $t: (key: string) => key,
          t: (key: string) => key,
          locale: 'en'
        }
      }
    })

    const readerContent = wrapper.find('.doc-reader-content')
    expect(readerContent.exists()).toBe(true)
    // Must NOT have overflow-x-hidden which clips long code lines
    expect(readerContent.classes()).not.toContain('overflow-x-hidden')
    expect(readerContent.classes()).toContain('min-w-0')
    expect(readerContent.classes()).toContain('max-w-full')

    // Must preserve full long code line
    expect(wrapper.text()).toContain("queryClient.setQueryData(['items'], (old) => updateLocal(old, itemId));")
    expect(wrapper.html()).toContain('overflow-x-auto')
  })
})
