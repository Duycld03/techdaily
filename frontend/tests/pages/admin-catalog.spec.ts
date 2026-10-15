import { describe, it, expect, beforeEach, vi } from 'vitest'
import { ref } from 'vue'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import AdminCatalogPage from '~/pages/admin/catalog.vue'
import { useCatalogStore, type AdminCatalogDocumentDetailDto } from '~/stores/useCatalogStore'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'

const mockAdminDocs = [
  {
    id: 'admin-doc-1',
    title: 'Distributed Systems Patterns',
    category: 3,
    sourceType: 1,
    visibility: 'Draft',
    processingStatus: 'Ready',
    totalSlices: 5,
    revision: 1,
    concurrencyVersion: 1,
    reviewedByUserId: null,
    createdAt: '2026-10-14T00:00:00Z',
    updatedAt: '2026-10-14T00:00:00Z'
  }
]

const mockAdminDocDetail: AdminCatalogDocumentDetailDto = {
  id: 'admin-doc-1',
  title: 'Distributed Systems Patterns',
  category: 3,
  language: 'en',
  sourceType: 1,
  visibility: 'Draft',
  processingStatus: 'Ready',
  progressPercentage: 100,
  statusMessage: 'Ready for reading',
  processingErrorMessage: null,
  totalSlices: 5,
  revision: 1,
  concurrencyVersion: 1,
  reviewedByUserId: null,
  reviewedAt: null,
  publishedByUserId: null,
  publishedAt: null,
  hiddenByUserId: null,
  hiddenAt: null,
  authorOrSourceUrl: 'https://martinfowler.com/articles/patterns-of-distributed-systems/',
  attributionText: 'Martin Fowler',
  permissionBasis: 'CC-BY-4.0',
  permissionEvidence: 'Article header citation',
  confirmedDistributionRights: true,
  confirmedAiProcessingRights: true,
  isEligibleForPublication: false,
  unmetPublicationRequirements: ['Administrator review is required before publication.'],
  createdAt: '2026-10-14T00:00:00Z',
  updatedAt: '2026-10-14T00:00:00Z',
  slices: [
    {
      id: 'slice-1',
      catalogDocumentId: 'admin-doc-1',
      sliceOrder: 1,
      chapterTitle: 'Overview of State Machine Replication',
      summaryMarkdown: 'Summary',
      estimatedReadMinutes: 10,
      isAiFormatted: true
    }
  ]
}

const mockGet = vi.fn(async (url: string): Promise<unknown> => {
  if (url.includes('/api/v1/catalog/capabilities')) {
    return { canManageCatalog: true }
  }
  if (url.includes('/api/v1/admin/catalog/documents/admin-doc-1')) {
    return mockAdminDocDetail
  }
  if (url.includes('/api/v1/admin/catalog/documents')) {
    return {
      items: mockAdminDocs,
      totalCount: 1,
      page: 1,
      pageSize: 20
    }
  }
  return {}
})

const mockPost = vi.fn(async (url: string): Promise<unknown> => {
  if (url.includes('/publish')) {
    return {
      ...mockAdminDocDetail,
      visibility: 'Published',
      isEligibleForPublication: true,
      unmetPublicationRequirements: []
    }
  }
  if (url.includes('/review')) {
    return {
      ...mockAdminDocDetail,
      reviewedByUserId: 'admin-user-1',
      isEligibleForPublication: true,
      unmetPublicationRequirements: []
    }
  }
  return {}
})

const mockPut = vi.fn(async (): Promise<unknown> => {
  return {
    ...mockAdminDocDetail,
    revision: 2
  }
})

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
    postRaw: vi.fn(),
    patch: vi.fn(),
    put: mockPut,
    delete: vi.fn()
  })
}))

const mockToast = {
  success: vi.fn(),
  error: vi.fn(),
  info: vi.fn()
}

vi.mock('#imports', async () => {
  const actual = await vi.importActual('#imports')
  return {
    ...actual,
    useToast: () => mockToast
  }
})

;(globalThis as any).useRouter = () => ({
  push: vi.fn(),
  replace: vi.fn()
})
;(globalThis as any).useRoute = () => ({
  path: '/admin/catalog',
  query: {},
  fullPath: '/admin/catalog'
})

describe('admin/catalog.vue (MasterDetail Catalog Management)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    useAdminAccessStore().state = 'authorized'
  })

  it('withholds catalog data until the shared administrator gate admits the account', async () => {
    useAdminAccessStore().state = 'denied'

    const wrapper = mount(AdminCatalogPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          MasterDetailLayout: true,
          AppSelect: true,
          AppModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    expect(wrapper.text()).not.toContain('admin_catalog.title')
    // Must NOT have called admin documents endpoint
    expect(mockGet).not.toHaveBeenCalledWith(expect.stringMatching(/\/api\/v1\/admin\/catalog\/documents/))
  })

  it('renders MasterDetail workspace for authorized administrators and displays document details', async () => {
    const wrapper = mount(AdminCatalogPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          MasterDetailLayout: {
            template: '<div><slot name="header" /><slot name="nav" /><slot name="content" /></div>'
          },
          AppSelect: true,
          AppModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('admin_catalog.title')
    expect(wrapper.text()).toContain('Distributed Systems Patterns')
    expect(wrapper.text()).toContain('admin_catalog.document.revision')
  })

  it('enforces publication gating when requirements are unmet', async () => {
    const wrapper = mount(AdminCatalogPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          MasterDetailLayout: {
            template: '<div><slot name="header" /><slot name="nav" /><slot name="content" /></div>'
          },
          AppSelect: true,
          AppModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Unmet requirements box must be rendered
    expect(wrapper.text()).toContain('admin_catalog.unmet_requirements')
    expect(wrapper.text()).toContain('Administrator review is required before publication.')

    // The publish button must be disabled
    const publishButton = wrapper.findAll('button').find((b) => b.text().includes('admin_catalog.publish_action'))
    expect(publishButton).toBeDefined()
    expect(publishButton?.attributes('disabled')).toBeDefined()
  })

  it('publishes document when eligible and calls publish endpoint with expected revision', async () => {
    mockGet.mockImplementation(async (url: string) => {
      if (url.includes('/api/v1/catalog/capabilities')) {
        return { canManageCatalog: true }
      }
      if (url.includes('/api/v1/admin/catalog/documents/admin-doc-1')) {
        return {
          ...mockAdminDocDetail,
          isEligibleForPublication: true,
          unmetPublicationRequirements: []
        }
      }
      if (url.includes('/api/v1/admin/catalog/documents')) {
        return { items: mockAdminDocs, totalCount: 1, page: 1, pageSize: 20 }
      }
      return {}
    })

    const wrapper = mount(AdminCatalogPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          MasterDetailLayout: {
            template: '<div><slot name="header" /><slot name="nav" /><slot name="content" /></div>'
          },
          AppSelect: true,
          AppModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    const publishButton = wrapper.findAll('button').find((b) => b.text().includes('admin_catalog.publish_action'))
    expect(publishButton?.attributes('disabled')).toBeUndefined()

    await publishButton?.trigger('click')
    await flushPromises()

    expect(mockPost).toHaveBeenCalledWith(
      '/api/v1/admin/catalog/documents/admin-doc-1/publish',
      expect.objectContaining({ expectedRevision: 1 })
    )
  })

  it('cleans up administrative state and withholds the page after revoked permission', async () => {
    const wrapper = mount(AdminCatalogPage, {
      global: {
        stubs: {
          NuxtLink: { template: '<a><slot /></a>' },
          MasterDetailLayout: {
            template: '<div><slot name="header" /><slot name="nav" /><slot name="content" /></div>'
          },
          AppSelect: true,
          AppModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Trigger an action that returns 403 (revoked permissions)
    // First update selectedAdminDocument to have 0 slices and status Pending so prepare action is available
    const store = useCatalogStore()
    if (store.selectedAdminDocument) {
      store.selectedAdminDocument = {
        ...store.selectedAdminDocument,
        processingStatus: 'Pending',
        slices: []
      }
    }
    await wrapper.vm.$nextTick()

    mockPost.mockRejectedValueOnce({ status: 403, message: 'Forbidden' })

    const prepareBtn = wrapper.findAll('button').find((b) => b.text().includes('admin_catalog.prepare_action'))
    await prepareBtn?.trigger('click')
    await flushPromises()
    const catalogStore = useCatalogStore()
    expect(catalogStore.canManageCatalog).toBe(false)
    expect(catalogStore.adminDocuments.length).toBe(0)
    expect(useAdminAccessStore().isAuthorized).toBe(false)
    expect(wrapper.text()).not.toContain('admin_catalog.title')
  })
})
