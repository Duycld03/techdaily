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

vi.mock('~/components/reader/ReaderAudioPlayer.vue', () => ({
  default: {
    name: 'ReaderAudioPlayer',
    props: ['chunk', 'disableAutoAdvance'],
    template: '<div class="reader-audio-player-stub" :data-chunk-id="chunk?.id" :data-disable-auto-advance="String(disableAutoAdvance)">AudioPlayerStub</div>'
  }
}))
const mockDocumentChunk = {
  id: 'chunk-day-5',
  documentBookId: 'book-1',
  chunkOrder: 5,
  chapterTitle: 'State Management & Server State Caching',
  originalTextMarkdown: "```ts\nqueryClient.setQueryData(['items'], (old) => updateLocal(old, itemId));\n```",
  summaryMarkdown: 'Client State vs Server State Caching',
  keyTakeaways: ['Key Takeaway 1', 'Key Takeaway 2'],
  language: 'en',
  estimatedReadMinutes: 3,
  isAiFormatted: true
}

describe('DocReaderPane.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders chunk title, summary, and key takeaways properly', () => {
    const wrapper = mount(DocReaderPane, {
      props: {
        chunk: mockDocumentChunk
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

  it('renders code snippet with full content', () => {
    const wrapper = mount(DocReaderPane, {
      props: {
        chunk: mockDocumentChunk
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
    expect(wrapper.text()).toContain("queryClient.setQueryData(['items'], (old) => updateLocal(old, itemId));")
  })

  it('does not render micro quiz container, providing distraction-free reading', () => {
    const wrapper = mount(DocReaderPane, {
      props: {
        chunk: mockDocumentChunk
      },
      global: {
        mocks: {
          $t: (key: string) => key,
          t: (key: string) => key,
          locale: 'en'
        }
      }
    })

    expect(wrapper.find('.micro-quiz-container').exists()).toBe(false)
  })

  it('renders Aa button, toggles popover, and dynamically binds typography styles', async () => {
    const wrapper = mount(DocReaderPane, {
      props: {
        chunk: mockDocumentChunk
      },
      global: {
        mocks: {
          $t: (key: string) => key,
          t: (key: string) => key,
          locale: 'en'
        }
      }
    })

    // Aa button exists in header
    const aaButton = wrapper.findAll('button').find(b => b.text().includes('Aa'))
    expect(aaButton).toBeDefined()
    expect(aaButton!.exists()).toBe(true)

    // Popover is initially closed
    expect(wrapper.text()).not.toContain('reader.font_size')

    // Click Aa button to open popover
    await aaButton!.trigger('click')
    expect(wrapper.text()).toContain('reader.font_size')
    expect(wrapper.text()).toContain('reader.font_family')
    expect(wrapper.text()).toContain('reader.line_spacing')

    // Reader content has dynamic inline style and font class
    const readerContent = wrapper.find('.doc-reader-content')
    expect(readerContent.exists()).toBe(true)
    expect(readerContent.attributes('style')).toContain('font-size: 16px')
    expect(readerContent.attributes('style')).toContain('line-height: 1.75')

    // Dismiss via Escape key
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    await wrapper.vm.$nextTick()
    expect(wrapper.text()).not.toContain('reader.font_size')
  })

  it('renders ReaderAudioPlayer when chunk is AI-formatted', () => {
    const wrapper = mount(DocReaderPane, {
      props: {
        chunk: mockDocumentChunk
      },
      global: {
        stubs: {
          ReaderAudioPlayer: {
            name: 'ReaderAudioPlayer',
            template: '<div class="reader-audio-player-stub" :data-chunk-id="chunk?.id" :data-disable-auto-advance="String(disableAutoAdvance)">AudioPlayerStub</div>',
            props: ['chunk', 'disableAutoAdvance']
          }
        },
        mocks: {
          $t: (key: string) => key,
          t: (key: string) => key,
          locale: 'en'
        }
      }
    })

    const audioStub = wrapper.find('.reader-audio-player-stub')
    expect(audioStub.exists()).toBe(true)
    expect(audioStub.attributes('data-chunk-id')).toBe('chunk-day-5')
    expect(audioStub.attributes('data-disable-auto-advance')).toBe('true')
    const playerComp = wrapper.findComponent({ name: 'ReaderAudioPlayer' })
    expect(playerComp.exists()).toBe(true)
    expect(playerComp.props('disableAutoAdvance')).toBe(true)
  })

  it('does not render ReaderAudioPlayer container when chunk is not AI-formatted', () => {
    const wrapper = mount(DocReaderPane, {
      props: {
        chunk: {
          ...mockDocumentChunk,
          isAiFormatted: false
        }
      },
      global: {
        stubs: {
          ReaderAudioPlayer: {
            name: 'ReaderAudioPlayer',
            template: '<div class="reader-audio-player-stub">AudioPlayerStub</div>',
            props: ['chunk']
          }
        },
        mocks: {
          $t: (key: string) => key,
          t: (key: string) => key,
          locale: 'en'
        }
      }
    })

    expect(wrapper.find('.reader-audio-player-stub').exists()).toBe(false)
  })
})
