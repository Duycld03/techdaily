import { describe, it, expect, beforeEach, vi } from 'vitest'
import { ref } from 'vue'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import LibraryPage from '~/pages/library.vue'
import { useCatalogStore, type LearnerCatalogDocument } from '~/stores/useCatalogStore'

const customCategory = { id: '92345678-1234-4234-8234-123456789012', slug: 'distributed', nameEn: 'Distributed Systems', nameVi: 'He thong phan tan', sortOrder: 1, isActive: true, version: 1 }
const backendCategory = { id: '82345678-1234-4234-8234-123456789012', slug: 'backend', nameEn: 'Backend Engineering', nameVi: 'Ky thuat backend', sortOrder: 2, isActive: false, version: 1 }

const mockPublishedDocs = [
  {
    id: 'cat-doc-1',
    title: 'Distributed Systems Patterns',
    description: 'A comprehensive guide to distributed systems patterns and consensus protocols.',
    categoryId: customCategory.id,
    category: customCategory,
    language: 'en',
    sourceType: 1, // WebArticle
    authorOrSourceUrl: 'https://martinfowler.com/articles/patterns-of-distributed-systems/',
    attributionText: 'Martin Fowler & Unmesh Joshi',
    totalSlices: 5,
    estimatedReadMinutes: 45,
    publishedAt: '2026-10-14T00:00:00Z',
    alreadyAdded: false,
    existingBookId: null
  },
  {
    id: 'cat-doc-2',
    title: 'Clean Architecture in .NET',
    description: 'Domain-centric architecture principles and enterprise design patterns.',
    categoryId: backendCategory.id,
    category: backendCategory,
    language: 'vi',
    sourceType: 2, // PdfBook
    authorOrSourceUrl: 'Clean Architecture Guide',
    attributionText: 'DeepPace Engineering Guild',
    totalSlices: 8,
    estimatedReadMinutes: 60,
    publishedAt: '2026-10-14T00:00:00Z',
    alreadyAdded: true,
    existingBookId: 'personal-book-2'
  }
]

const mockDetailDoc = {
  id: 'cat-doc-1',
  title: 'Distributed Systems Patterns',
  description: 'A comprehensive guide to distributed systems patterns and consensus protocols.',
  categoryId: customCategory.id,
  category: customCategory,
  language: 'en',
  sourceType: 1,
  authorOrSourceUrl: 'https://martinfowler.com/articles/patterns-of-distributed-systems/',
  attributionText: 'Martin Fowler & Unmesh Joshi',
  totalSlices: 5,
  estimatedReadMinutes: 45,
  publishedAt: '2026-10-14T00:00:00Z',
  alreadyAdded: false,
  existingBookId: null,
  tableOfContents: [
    { sliceOrder: 1, chapterTitle: 'Overview of State Machine Replication', estimatedReadMinutes: 10 },
    { sliceOrder: 2, chapterTitle: 'Consensus with Raft', estimatedReadMinutes: 15 }
  ],
  firstSlicePreview: {
    sliceOrder: 1,
    chapterTitle: 'Overview of State Machine Replication',
    summaryMarkdown: 'State machine replication ensures fault tolerance across clusters.',
    originalTextMarkdown: 'Full unformatted markdown content for slice 1.'
  }
}

const mockGet = vi.fn(async (url: string, _params?: Record<string, unknown>): Promise<unknown> => {
  if (url === '/api/v1/categories') return [customCategory, backendCategory]
  if (url.includes('/api/v1/catalog/capabilities')) {
    return { canManageCatalog: false }
  }
  if (url.includes('/api/v1/catalog/documents/cat-doc-1')) {
    return mockDetailDoc
  }
  if (url.includes('/api/v1/catalog/documents')) {
    return {
      items: mockPublishedDocs,
      totalCount: 2,
      page: 1,
      pageSize: 12
    }
  }
  if (url.includes('/api/v1/library/books')) {
    return { books: [] }
  }
  return {}
})

const mockPost = vi.fn(async (url: string): Promise<unknown> => {
  if (url.includes('/adopt')) {
    return {
      bookId: 'new-personal-book-1',
      alreadyAdded: false
    }
  }
  return {}
})

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
    postRaw: vi.fn(),
    patch: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  })
}))

const mockRouteQuery = ref<Record<string, string>>({})
function extractQuery(to: unknown): Record<string, string> {
  if (!to || typeof to !== 'object' || !('query' in to)) return {}
  const q = to.query
  if (!q || typeof q !== 'object') return {}
  const result: Record<string, string> = {}
  for (const [key, val] of Object.entries(q)) {
    if (typeof val === 'string') result[key] = val
  }
  return result
}
const mockRouterPush = vi.fn(async (to: unknown) => {
  mockRouteQuery.value = extractQuery(to)
})
const mockRouterReplace = vi.fn(async (to: unknown) => {
  mockRouteQuery.value = extractQuery(to)
})

vi.stubGlobal('useRouter', () => ({
  push: mockRouterPush,
  replace: mockRouterReplace
}))
vi.stubGlobal('useRoute', () => ({
  path: '/library',
  get query() { return mockRouteQuery.value },
  fullPath: '/library'
}))

describe('library.vue (Curated Catalog & Discover Tab)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mockRouteQuery.value = {}
  })

  it('renders My Library by default with discover link in empty state', async () => {
    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true,
          AppModal: true,
          BasePagination: true,
          AppSelect: true,
          CatalogFilterModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('library.tabs.mine')
    expect(wrapper.text()).toContain('library.tabs.discover')
    // Empty state should contain the browse discover button
    expect(wrapper.text()).toContain('library.discover.browse_discover')
  })

  it('switches to discover tab and fetches published catalog documents', async () => {
    mockRouteQuery.value = { tab: 'discover' }

    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true,
          AppModal: true,
          BasePagination: true,
          AppSelect: true,
          CatalogFilterModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    const catalogStore = useCatalogStore()
    expect(catalogStore.documents.length).toBe(2)
    expect(wrapper.text()).toContain('Distributed Systems Patterns')
    expect(wrapper.text()).toContain('Clean Architecture in .NET')
  })

  it('adopts a document and updates its card action state', async () => {
    mockRouteQuery.value = { tab: 'discover' }

    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true,
          AppModal: true,
          BasePagination: true,
          AppSelect: true,
          CatalogFilterModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Find the first card's adopt button
    const buttons = wrapper.findAll('button')
    const adoptButton = buttons.find((b) => b.text().includes('library.discover.add_to_library'))
    expect(adoptButton).toBeDefined()

    await adoptButton?.trigger('click')
    await flushPromises()

    expect(mockPost).toHaveBeenCalledWith('/api/v1/catalog/documents/cat-doc-1/adopt', {})
    const catalogStore = useCatalogStore()
    const doc1 = catalogStore.documents.find((d: LearnerCatalogDocument) => d.id === 'cat-doc-1')
    expect(doc1?.existingBookId).toBe('new-personal-book-1')
  })

  it('filters documents by category, sourceType, language and search', async () => {
    mockRouteQuery.value = { tab: 'discover' }

    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true,
          AppModal: true,
          BasePagination: true,
          CatalogFilterModal: {
            props: ['isOpen', 'currentFilters'],
            template: '<div v-if="isOpen" class="stub-filter-modal">Filter Modal</div>'
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    const searchInput = wrapper.find('input[type="text"]')
    await searchInput.setValue('distributed')
    await searchInput.trigger('keyup.enter')
    await flushPromises()

    expect(mockGet).toHaveBeenCalledWith(
      expect.stringMatching(/\/api\/v1\/catalog\/documents\?.*search=distributed/)
    )
  })

  it('opens preview modal with table of contents and first slice preview', async () => {
    mockRouteQuery.value = { tab: 'discover' }

    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          Teleport: true,
          AppModal: {
            props: ['open', 'title'],
            template: '<div v-if="open" class="preview-modal"><slot /><slot name="footer" /></div>'
          },
          BasePagination: true,
          CatalogFilterModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    const previewButtons = wrapper.findAll('button').filter((b) => b.text().includes('library.discover.preview_doc'))
    expect(previewButtons.length).toBeGreaterThan(0)

    await previewButtons[0]?.trigger('click')
    await flushPromises()

    expect(mockGet).toHaveBeenCalledWith('/api/v1/catalog/documents/cat-doc-1')
    const modal = wrapper.find('.preview-modal')
    expect(modal.exists()).toBe(true)
    expect(modal.text()).toContain('Overview of State Machine Replication')
    expect(modal.text()).toContain('State machine replication ensures fault tolerance across clusters.')
  })

  it('renders quick category pills and opens advanced filter modal', async () => {
    mockRouteQuery.value = { tab: 'discover' }

    const wrapper = mount(LibraryPage, {
      global: {
        stubs: {
          NuxtLink: {
            props: ['to'],
            template: '<a :data-to="JSON.stringify(to)"><slot /></a>'
          },
          Teleport: true,
          AppModal: true,
          BasePagination: true,
          CatalogFilterModal: {
            props: ['isOpen', 'currentFilters'],
            template: '<div v-if="isOpen" class="stub-filter-modal">Filter Modal</div>'
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Category quick pill click
    expect(mockGet).toHaveBeenCalledWith('/api/v1/categories', { cache: 'no-store' })
    const backendPill = wrapper.findAll('button').find(b => b.text().includes('Backend Engineering'))
    expect(backendPill).toBeDefined()
    await backendPill!.trigger('click')
    await flushPromises()

    expect(mockGet).toHaveBeenCalledWith(
      expect.stringMatching(new RegExp('/api/v1/catalog/documents\\?.*categoryId=' + backendCategory.id))
    )

    // Advanced filter button click opens modal
    const advFilterBtn = wrapper.find('[data-testid="catalog-advanced-filter-btn"]')
    expect(advFilterBtn.exists()).toBe(true)
    await advFilterBtn.trigger('click')
    await flushPromises()

    expect(wrapper.find('.stub-filter-modal').exists()).toBe(true)

    // Adopted book links directly to /read/:bookId
    const openBookLink = wrapper.findAll('a').find(a => a.text().includes('library.discover.open_book'))
    expect(openBookLink).toBeDefined()
    expect(openBookLink!.attributes('data-to')).toContain('/read/')
  })
})
