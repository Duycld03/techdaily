import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import { BookOpen, CheckCircle2 } from 'lucide-vue-next'
import LibraryPage from '~/pages/library.vue'
import AppSelect from '~/components/common/AppSelect.vue'
import { useLibraryStore, type Book } from '~/stores/useLibraryStore'

const category = { id: '92345678-1234-4234-8234-123456789012', slug: 'custom-topic', nameEn: 'Custom Topic', nameVi: 'Chu de', sortOrder: 1, isActive: true, version: 1 }
const inactiveCategory = { ...category, id: '82345678-1234-4234-8234-123456789012', slug: 'retired', nameEn: 'Retired Topic', isActive: false }
let booksResponse: unknown[] | undefined

const mockBooks = [
  {
    id: 'book-1',
    title: 'Designing Data-Intensive Applications',
    slug: 'ddia',
    sourceType: 0,
    categoryId: category.id,
    category,
    totalChunks: 12,
    isPublished: true,
    createdAt: '2026-08-31T00:00:00Z'
  },
  {
    id: 'book-2',
    title: 'Atomic Habits',
    slug: 'atomic-habits',
    sourceType: 0,
    categoryId: inactiveCategory.id,
    category: inactiveCategory,
    totalChunks: 8,
    isPublished: true,
    createdAt: '2026-09-01T00:00:00Z'
  }
]

const mockGet = vi.fn(async (url: string): Promise<any> => {
  if (url === '/api/v1/categories') return [category, inactiveCategory]
  if (url.includes('/api/v1/library/books')) {
    return { books: booksResponse ?? [...mockBooks] }
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
  if (url.includes('/api/v1/library/import')) {
    return {
      book: {
        id: 'book-imported-1',
        title: body.title,
        categoryId: body.categoryId,
        category,
        totalChunks: 1,
        isPublished: true,
        createdAt: '2026-09-01T00:00:00Z'
      }
    }
  }
  return {}
})
const mockPostRaw = vi.fn(async (_url: string, _body: FormData) => {
  return {
    book: {
      id: 'uploaded-pdf-1',
      title: 'Sample PDF',
      categoryId: category.id,
      category,
      totalChunks: 0,
      status: 'Processing'
    }
  }
})
const mockPatch = vi.fn(async (_url: string, body: Record<string, unknown>) => {
  return {
    id: 'book-1',
    title: body.title,
    categoryId: body.categoryId,
    category,
    authorOrSourceUrl: body.authorOrSourceUrl,
    language: (body.language as string) ?? 'en'
  }
})


vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
    postRaw: mockPostRaw,
    patch: mockPatch,
    delete: vi.fn(),
    download: vi.fn()
  })
}))

describe('library.vue (Universal Pillars & Remote PDF Crawler)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    booksResponse = undefined
    vi.stubGlobal('useRoute', () => ({ path: '/library', query: {}, fullPath: '/library' }))
    vi.stubGlobal('useRouter', () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }))
  })

  it('refreshes rejected inactive metadata while preserving the import draft and selected UUID', async () => {
    const wrapper = mount(LibraryPage, { global: { stubs: { NuxtLink: true, Teleport: true } } })
    await flushPromises()
    await wrapper.find('button.bg-brand-600').trigger('click')
    await flushPromises()
    const title = wrapper.get('input[placeholder="library.markdown_title_placeholder"]')
    const content = wrapper.get('textarea')
    const source = wrapper.get('input[type="url"]')
    await title.setValue('My draft')
    await content.setValue('# Keep this markdown')
    await source.setValue('https://example.com/source')
    const select = wrapper.findAllComponents(AppSelect).find(select => select.props('ariaLabel') === 'library.category_label')!
    select.vm.$emit('update:modelValue', category.id)
    await flushPromises()
    mockPost.mockRejectedValueOnce({ code: 'Category.Inactive', message: 'Inactive category' })
    mockGet.mockImplementationOnce(async () => [{ ...category, isActive: false }, inactiveCategory])
    await wrapper.get('form').trigger('submit')
    await flushPromises()
    expect((title.element as HTMLInputElement).value).toBe('My draft')
    expect((content.element as HTMLTextAreaElement).value).toBe('# Keep this markdown')
    expect((source.element as HTMLInputElement).value).toBe('https://example.com/source')
    expect(select.props('modelValue')).toBe(category.id)
    expect(select.props('options').some((option: { value: string }) => option.value === category.id)).toBe(false)
    const submit = wrapper.findAll('button').find(button => button.text() === 'library.import_action')!
    expect(submit.attributes('disabled')).toBeDefined()
    expect(mockGet).toHaveBeenLastCalledWith('/api/v1/categories', { cache: 'no-store' })
  })

  it('renders dynamically fetched category filter chips and advanced filtering', async () => {
    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })
    await flushPromises()

    expect(mockGet).toHaveBeenCalledWith('/api/v1/categories', { cache: 'no-store' })
    expect(wrapper.text()).toContain('Custom Topic')
    expect(wrapper.text()).toContain('Retired Topic')

    const advancedBtn = wrapper.find('[data-testid="library-advanced-filter-btn"]')
    expect(advancedBtn.exists()).toBe(true)
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

    wrapper.findAllComponents(AppSelect).find(select => select.props('options')?.some((option: { value: string }) => option.value === category.id))!.vm.$emit('update:modelValue', category.id)
    await flushPromises()
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
        title: 'Embedded Architecture Book',
        categoryId: category.id
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

  it('renders stored descriptors for active and inactive books without enum fallback', async () => {
    const wrapper = mount(LibraryPage, { global: { stubs: { NuxtLink: true, Teleport: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Custom Topic')
    expect(wrapper.text()).toContain('Retired Topic')
    expect(wrapper.text()).not.toContain('library.categories.frontend')
  })

  it('renders dynamic localized status messages with parameter interpolation for in-progress books', async () => {
    const processingBooks = [
      {
        id: 'p-1',
        title: 'Book Curating',
        categoryId: category.id, category,
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'AI is curating initial slice 3/12...'
      },
      {
        id: 'p-2',
        title: 'Book Queued',
        categoryId: category.id, category,
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'File uploaded, queued for processing...'
      },
      {
        id: 'p-3',
        title: 'Book Remote',
        categoryId: category.id, category,
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Remote PDF download initiated. Processing chapters...'
      },
      {
        id: 'p-4',
        title: 'Book Structure',
        categoryId: category.id, category,
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Analyzing document structure and bookmarks...'
      },
      {
        id: 'p-5',
        title: 'Book Persisting',
        categoryId: category.id, category,
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Persisting chapters and slices...'
      },
      {
        id: 'p-6',
        title: 'Book Pages',
        categoryId: category.id, category,
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Analyzing content: page 15/120'
      },
      {
        id: 'p-7',
        title: 'Book Topic',
        categoryId: category.id, category,
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Extracting topic: Chapter 2 - Concurrency'
      },
      {
        id: 'p-8',
        title: 'Book Complete',
        categoryId: category.id, category,
        totalChunks: 0,
        status: 'Processing',
        statusMessage: 'Extracted 15 slices. Complete!'
      }
    ]

    booksResponse = processingBooks
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

    wrapper.findAllComponents(AppSelect).find(select => select.props('options')?.some((option: { value: string }) => option.value === category.id))!.vm.$emit('update:modelValue', category.id)
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

  it('sends the selected document language (overriding the interface locale) in the PDF upload payload', async () => {
    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })
    await flushPromises()

    // Open the import modal and switch to the PDF Upload tab.
    await wrapper.find('button.bg-brand-600').trigger('click')
    await flushPromises()
    const pdfTabBtn = wrapper.findAll('button').find(b => b.text().includes('library.tab_pdf'))
    expect(pdfTabBtn).toBeDefined()
    await pdfTabBtn!.trigger('click')
    await flushPromises()

    // Attach a PDF file.
    const file = new File(['dummy-pdf-content'], 'sample.pdf', { type: 'application/pdf' })
    const fileInput = wrapper.find('input[type="file"]')
    Object.defineProperty(fileInput.element, 'files', { value: [file] })
    await fileInput.trigger('change')
    await flushPromises()

    // Choose Vietnamese explicitly, overriding the 'en' interface locale.
    const languageSelect = wrapper.findAllComponents(AppSelect)
      .find(s => s.props('ariaLabel') === 'library.document_language')
    expect(languageSelect).toBeDefined()
    languageSelect!.vm.$emit('update:modelValue', 'vi')
    await flushPromises()
    wrapper.findAllComponents(AppSelect).find(select => select.props('options')?.some((option: { value: string }) => option.value === category.id))!.vm.$emit('update:modelValue', category.id)
    await flushPromises()

    // Submit the PDF upload form.
    const pdfForm = wrapper.findAll('form').find(f => f.text().includes('library.upload_pdf_action'))
    expect(pdfForm).toBeDefined()
    await pdfForm!.trigger('submit.prevent')
    await flushPromises()

    // The upload payload carries the chosen language, not the interface locale.
    expect(mockPostRaw).toHaveBeenCalled()
    const sentForm = mockPostRaw.mock.calls[0]![1]
    expect(sentForm.get('language')).toBe('vi')
  })

  it('renders book cards with title and slice progress', async () => {
    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true
        },
        mocks: {
          $t: (key: string, params?: Record<string, unknown>) => {
            if (params && 'slice' in params) {
              return `${key}:${params.slice}`
            }
            return key
          }
        }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('Designing Data-Intensive Applications')
    expect(wrapper.text()).toContain('Atomic Habits')
  })

  it('does not infer a category from an aspnet URL and requires an explicit UUID selection', async () => {
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
    const urlTabBtn = tabButtons.find((b) => b.text().includes('library.tab_url'))
    expect(urlTabBtn).toBeDefined()
    await urlTabBtn!.trigger('click')
    await flushPromises()

    // Input URL with aspnetcore
    const urlInput = wrapper.find('input[type="url"]')
    await urlInput.setValue('https://learn.microsoft.com/aspnet/core/overview')

    // Click fetch content button
    const fetchBtn = wrapper.findAll('button').find((b) => b.text().includes('library.fetch_url_btn'))
    expect(fetchBtn).toBeDefined()
    await fetchBtn!.trigger('click')
    await flushPromises()

    // Switch back to Markdown tab to inspect and submit import
    const mdTabBtn = tabButtons.find((b) => b.text().includes('library.tab_markdown'))
    expect(mdTabBtn).toBeDefined()
    await mdTabBtn!.trigger('click')
    await flushPromises()

    const select = wrapper.findAllComponents(AppSelect).find(select => select.props('options')?.some((option: { value: string }) => option.value === category.id))!
    expect(select.props('modelValue')).toBe('')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(mockPost.mock.calls.some(([url]) => url === '/api/v1/library/import')).toBe(false)
    select.vm.$emit('update:modelValue', category.id)
    await flushPromises()

    // Submit the markdown form
    const form = wrapper.find('form')
    expect(form.exists()).toBe(true)
    await form.trigger('submit')
    await flushPromises()
    // Only the explicitly selected UUID is submitted.
    expect(mockPost).toHaveBeenCalledWith(
      '/api/v1/library/import',
      expect.objectContaining({
        categoryId: category.id
      })
    )
  })

  it('renders unread ready book cards with neutral Obsidian BookOpen badge and no emerald styling classes', async () => {
    booksResponse = [{
      ...mockBooks[0],
      id: 'book-ready-1',
      status: 'Ready'
    }]

    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Find the ready badge
    const badge = wrapper.find('.grid > div div.mt-auto.pt-3 > div')
    expect(badge.exists()).toBe(true)
    expect(badge.text()).toContain('library.ready_to_read')

    // Must render BookOpen icon
    // Must render BookOpen icon, not CheckCircle2
    expect(badge.findComponent(BookOpen).exists()).toBe(true)
    expect(badge.findComponent(CheckCircle2).exists()).toBe(false)
  })

  describe('Edit Book Modal & Metadata Update', () => {
    it('opens edit modal with pre-populated values when edit button is clicked', async () => {
      const wrapper = mount(LibraryPage, {
        global: {
          stubs: {
            NuxtLink: true,
            Teleport: true
          }
        }
      })
      await flushPromises()

      const editButtons = wrapper.findAll('button[title="library.edit_book"]')
      expect(editButtons.length).toBeGreaterThan(0)

      await editButtons[0].trigger('click')
      await flushPromises()

      const titleInput = wrapper.find('#edit-book-title')
      expect(titleInput.exists()).toBe(true)
      expect((titleInput.element as HTMLInputElement).value).toBe('Designing Data-Intensive Applications')
    })

    it('submits updated book metadata via PATCH API and updates library store', async () => {
      const wrapper = mount(LibraryPage, {
        global: {
          stubs: {
            NuxtLink: true,
            Teleport: true
          }
        }
      })
      await flushPromises()

      const editButtons = wrapper.findAll('button[title="library.edit_book"]')
      await editButtons[0].trigger('click')
      await flushPromises()

      const titleInput = wrapper.find('#edit-book-title')
      await titleInput.setValue('Designing Data-Intensive Systems (Updated)')

      const form = wrapper.find('#edit-book-form')
      await form.trigger('submit.prevent')
      await flushPromises()

      expect(mockPatch).toHaveBeenCalledWith(
        expect.stringContaining('/api/v1/library/books/book-1'),
        expect.objectContaining({
          title: 'Designing Data-Intensive Systems (Updated)',
          categoryId: category.id,
          language: 'en'
        })
      )
    })
  })

  describe('Single-Row Header Filter Bar Parity', () => {
    it('renders search input and category chips with Discover parity and no inline reset button', async () => {
      const wrapper = mount(LibraryPage, {
        global: {
          stubs: {
            NuxtLink: true,
            Teleport: true
          }
        }
      })
      await flushPromises()

      const searchInput = wrapper.find('[data-testid="library-search-input"]')
      expect(searchInput.exists()).toBe(true)

      const categoryContainer = wrapper.find('[data-testid="library-category-chips"]')
      expect(categoryContainer.exists()).toBe(true)

      // Inline reset button must be removed for toolbar parity with Discover
      const resetBtn = wrapper.find('[data-testid="library-reset-filters-btn"]')
      expect(resetBtn.exists()).toBe(false)
    })

    it('resets category filter when "All" chip is clicked', async () => {
      const mockPush = vi.fn()
      vi.stubGlobal('useRouter', () => ({
        push: mockPush,
        replace: vi.fn(),
        back: vi.fn()
      }))

      const wrapper = mount(LibraryPage, {
        global: {
          stubs: {
            NuxtLink: true,
            Teleport: true
          }
        }
      })
      await flushPromises()

      const categoryChips = wrapper.findAll('[data-testid="library-category-chips"] button')
      expect(categoryChips.length).toBeGreaterThan(1)

      // Select a category chip
      await categoryChips[1].trigger('click')
      await flushPromises()

      expect(mockPush).toHaveBeenCalledWith(
        expect.objectContaining({
          query: expect.objectContaining({
            categoryId: category.id
          })
        })
      )

      // Click "All" category chip (first button) to reset category
      await categoryChips[0].trigger('click')
      await flushPromises()

      expect(mockPush).toHaveBeenCalledWith(
        expect.objectContaining({
          query: expect.objectContaining({
            categoryId: undefined
          })
        })
      )
    })

    it('shows clear button with X icon when search input has value and clears input on click', async () => {
      const wrapper = mount(LibraryPage, {
        global: {
          stubs: {
            NuxtLink: true,
            Teleport: true
          },
          mocks: {
            $t: (key: string) => key === 'common.clear_search' ? 'Clear search' : key
          }
        }
      })
      await flushPromises()

      const searchInput = wrapper.find('[data-testid="library-search-input"]')
      await searchInput.setValue('distributed systems')
      await flushPromises()

      const clearBtn = wrapper.find('button[aria-label="Clear search"]')
      expect(clearBtn.exists()).toBe(true)
      // Inline reset button should remain absent
      expect(wrapper.find('[data-testid="library-reset-filters-btn"]').exists()).toBe(false)

      await clearBtn.trigger('click')
      await flushPromises()

      expect((searchInput.element as HTMLInputElement).value).toBe('')
    })
  })
})
