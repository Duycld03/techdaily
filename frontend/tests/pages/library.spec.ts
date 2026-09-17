import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import LibraryPage from '~/pages/library.vue'
import { useLibraryStore } from '~/stores/useLibraryStore'

const mockBooks = [
  {
    id: 'book-1',
    title: 'Designing Data-Intensive Applications',
    slug: 'ddia',
    sourceType: 0,
    category: 2,
    totalChunks: 12,
    isPublished: true,
    createdAt: '2026-08-31T00:00:00Z'
  },
  {
    id: 'book-2',
    title: 'Atomic Habits',
    slug: 'atomic-habits',
    sourceType: 0,
    category: 4,
    totalChunks: 8,
    isPublished: true,
    createdAt: '2026-09-01T00:00:00Z'
  }
]

const mockGet = vi.fn(async (url: string) => {
  if (url.includes('/api/v1/library/books')) {
    return { books: [...mockBooks] }
  }
  return {}
})

const mockPost = vi.fn(async (url: string, body: Record<string, unknown>) => {
  if (url.includes('/api/v1/library/crawl-url')) {
    if (typeof body.url === 'string' && body.url.includes('pdf-viewer')) {
      return {
        title: 'Embedded Architecture Book',
        sourceUrl: body.url,
        markdownContent: '',
        estimatedWordCount: 0,
        isPdfDetected: true,
        detectedPdfUrl: 'https://example.com/files/book.pdf'
      }
    }
    return {
      title: 'Crawled Web Article',
      sourceUrl: body.url,
      markdownContent: '# Hello Article',
      estimatedWordCount: 120,
      isPdfDetected: false,
      detectedPdfUrl: null
    }
  }
  if (url.includes('/api/v1/library/import-remote-pdf')) {
    return {
      bookId: 'book-remote-1',
      title: body.title,
      status: 'Processing',
      message: 'Queued'
    }
  }
  return {}
})

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
    postRaw: vi.fn(),
    delete: vi.fn(),
    download: vi.fn()
  })
}))

describe('library.vue (Universal Pillars & Remote PDF Crawler)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('renders category filter chips including Category 4 (Engineering Craft & Mindset)', async () => {
    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('library.categories.craft')
    expect(wrapper.text()).toContain('library.categories.system_design')
    expect(wrapper.text()).toContain('library.categories.database')
    expect(wrapper.text()).toContain('library.categories.backend')
    expect(wrapper.text()).toContain('library.categories.frontend')
  })

  it('detects embedded PDF during URL crawl and renders preview card with direct import action', async () => {
    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })
    // Open import modal
    const importBtn = wrapper.find('button.bg-brand-600')
    await importBtn.trigger('click')
    await flushPromises()

    // Switch to URL Crawler tab (Tab 3)
    const tabButtons = wrapper.findAll('button')
    const urlTabBtn = tabButtons.find(b => b.text().includes('library.tab_url'))
    expect(urlTabBtn).toBeDefined()
    await urlTabBtn!.trigger('click')
    await flushPromises()

    // Input URL pointing to an embedded PDF viewer
    const urlInput = wrapper.find('input[type="url"]')
    expect(urlInput.exists()).toBe(true)
    await urlInput.setValue('https://thuviensach.vn/pdf-viewer?id=123')

    // Click fetch content button
    const fetchBtn = wrapper.findAll('button').find(b => b.text().includes('library.fetch_url_btn'))
    expect(fetchBtn).toBeDefined()
    await fetchBtn!.trigger('click')
    await flushPromises()

    // Verify embedded PDF preview card is displayed
    expect(wrapper.text()).toContain('library.embedded_pdf_detected')
    expect(wrapper.text()).toContain('https://example.com/files/book.pdf')
    expect(wrapper.text()).toContain('library.import_detected_pdf')

    // Click "Import & Slice PDF Directly" button
    const importPdfBtn = wrapper.findAll('button').find(b => b.text().includes('library.import_detected_pdf'))
    expect(importPdfBtn).toBeDefined()
    await importPdfBtn!.trigger('click')
    await flushPromises()

    // Verify remote PDF import was sent with correct payload
    expect(mockPost).toHaveBeenCalledWith(
      '/api/v1/library/import-remote-pdf',
      expect.objectContaining({
        pdfUrl: 'https://example.com/files/book.pdf',
        title: 'Embedded Architecture Book'
      })
    )
  })
})
