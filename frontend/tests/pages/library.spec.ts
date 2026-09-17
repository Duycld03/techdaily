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
const mockPostRaw = vi.fn(async (_url: string, _body: FormData) => {
  return {
    book: {
      id: 'uploaded-pdf-1',
      title: 'Sample PDF',
      category: 4,
      totalChunks: 0,
      status: 'Processing'
    }
  }
})

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
    postRaw: mockPostRaw,
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

  it('renders verbatim category hint in import modal tabs', async () => {
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

    // Tab 0 (Markdown) contains hint
    expect(wrapper.text()).toContain('library.verbatim_category_hint')

    // Switch to PDF tab
    const tabButtons = wrapper.findAll('button')
    const pdfTabBtn = tabButtons.find(b => b.text().includes('library.tab_pdf'))
    expect(pdfTabBtn).toBeDefined()
    await pdfTabBtn!.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('library.verbatim_category_hint')

    // Switch to URL tab
    const urlTabBtn = tabButtons.find(b => b.text().includes('library.tab_url'))
    expect(urlTabBtn).toBeDefined()
    await urlTabBtn!.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('library.verbatim_category_hint')
  })

  it('renders localized category badges for string enums, string aliases, and numeric IDs without fallback to Engineering', async () => {
    const customBooks = [
      { id: 'b-frontend', title: 'Web Perf', category: 'FrontendWeb', totalChunks: 3, status: 'Ready' },
      { id: 'b-backend', title: 'DotNet Core', category: 'BackendDotNet', totalChunks: 4, status: 'Ready' },
      { id: 'b-db', title: 'PostgreSQL Internals', category: 'DatabaseStorage', totalChunks: 5, status: 'Ready' },
      { id: 'b-sys', title: 'Distributed Systems', category: 'SystemDesign', totalChunks: 6, status: 'Ready' },
      { id: 'b-craft', title: 'The Pragmatic Programmer', category: 'EngineeringCraft', totalChunks: 7, status: 'Ready' },
      { id: 'b-num0', title: 'Num 0', category: 0, totalChunks: 1, status: 'Ready' },
      { id: 'b-num1', title: 'Num 1', category: 1, totalChunks: 1, status: 'Ready' },
      { id: 'b-num2', title: 'Num 2', category: 2, totalChunks: 1, status: 'Ready' },
      { id: 'b-num3', title: 'Num 3', category: 3, totalChunks: 1, status: 'Ready' },
      { id: 'b-num4', title: 'Num 4', category: 4, totalChunks: 1, status: 'Ready' },
      { id: 'b-alias', title: 'Alias Craft', category: 'craft', totalChunks: 2, status: 'Ready' }
    ]

    mockGet.mockImplementationOnce(async (url: string) => {
      if (url.includes('/api/v1/library/books')) {
        return { books: customBooks }
      }
      return {}
    })

    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    await flushPromises()

    // None of the cards should fallback to the raw unlocalized string 'Engineering'
    expect(wrapper.text()).not.toContain('Engineering')
    expect(wrapper.text()).toContain('library.categories.frontend')
    expect(wrapper.text()).toContain('library.categories.backend')
    expect(wrapper.text()).toContain('library.categories.database')
    expect(wrapper.text()).toContain('library.categories.system_design')
    expect(wrapper.text()).toContain('library.categories.craft')
  })

  it('renders dynamic localized status messages with parameter interpolation for in-progress books', async () => {
    const processingBooks = [
      {
        id: 'p-1',
        title: 'Book Curating',
        category: 'EngineeringCraft',
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'AI is curating initial slice 3/12...'
      },
      {
        id: 'p-2',
        title: 'Book Queued',
        category: 'BackendDotNet',
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'File uploaded, queued for processing...'
      },
      {
        id: 'p-3',
        title: 'Book Remote',
        category: 'DatabaseStorage',
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Remote PDF download initiated. Processing chapters...'
      },
      {
        id: 'p-4',
        title: 'Book Structure',
        category: 'SystemDesign',
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Analyzing document structure and bookmarks...'
      },
      {
        id: 'p-5',
        title: 'Book Persisting',
        category: 'FrontendWeb',
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Persisting chapters and slices...'
      },
      {
        id: 'p-6',
        title: 'Book Pages',
        category: 'FrontendWeb',
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Analyzing content: page 15/120'
      },
      {
        id: 'p-7',
        title: 'Book Topic',
        category: 'FrontendWeb',
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Extracting topic: Chapter 2 - Concurrency'
      },
      {
        id: 'p-8',
        title: 'Book Complete',
        category: 'FrontendWeb',
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Extracted 15 slices. Complete!'
      }
    ]

    mockGet.mockImplementationOnce(async (url: string) => {
      if (url.includes('/api/v1/library/books')) {
        return { books: processingBooks }
      }
      return {}
    })

    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    await flushPromises()
    // Verify localized status translation keys are matched for all processing statuses
    expect(wrapper.text()).toContain('library.status_curating_slice')
    expect(wrapper.text()).toContain('library.status_uploaded_queued')
    expect(wrapper.text()).toContain('library.status_remote_download')
    expect(wrapper.text()).toContain('library.status_analyzing_structure')
    expect(wrapper.text()).toContain('library.status_persisting_slices')
    expect(wrapper.text()).toContain('library.status_analyzing_pages')
    expect(wrapper.text()).toContain('library.status_extracting_topic')
    expect(wrapper.text()).toContain('library.status_extracted_complete')
  })

  it('does NOT render GitBook Reader in book card footer', async () => {
    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    await flushPromises()

    expect(wrapper.text()).not.toContain('GitBook Reader')
  })
  it('binds import modal inputs, placeholders, and service notes to localized keys', async () => {
    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    // Open modal
    const importBtn = wrapper.find('button.bg-brand-600')
    await importBtn.trigger('click')
    await flushPromises()

    // Tab 1 (Markdown): textarea placeholder
    const textarea = wrapper.find('textarea')
    expect(textarea.exists()).toBe(true)
    expect(textarea.attributes('placeholder')).toBe('library.content_placeholder')

    // Tab 2 (PDF): switch to PDF tab
    const tabButtons = wrapper.findAll('button')
    const pdfTabBtn = tabButtons.find(b => b.text().includes('library.tab_pdf'))
    expect(pdfTabBtn).toBeDefined()
    await pdfTabBtn!.trigger('click')
    await flushPromises()

    // Verify title placeholder
    const textInputs = wrapper.findAll('input[type="text"]')
    const titleInput = textInputs.find(i => i.attributes('placeholder') === 'library.title_placeholder')
    expect(titleInput).toBeDefined()
    expect(titleInput?.attributes('placeholder')).toBe('library.title_placeholder')
    // Trigger PDF processing to display service notes card
    const file = new File(['dummy-pdf-content'], 'sample.pdf', { type: 'application/pdf' })
    const fileInput = wrapper.find('input[type="file"]')
    Object.defineProperty(fileInput.element, 'files', { value: [file] })
    await fileInput.trigger('change')
    await flushPromises()

    // Submit PDF form to trigger isProcessingPdf = true
    const pdfForm = wrapper.findAll('form').find(f => f.text().includes('library.upload_pdf_action'))
    if (pdfForm) {
      await pdfForm.trigger('submit.prevent')
      await flushPromises()
    }

    // Verify service notes
    expect(wrapper.text()).toContain('library.pdf_service_note_1')
    expect(wrapper.text()).toContain('library.pdf_service_note_2')
    expect(wrapper.text()).not.toContain('300 MB Streaming • Background Service')
    expect(wrapper.text()).not.toContain('Look-Ahead Buffer Synthesis')
  })
})
