import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AdminDocumentsPage from '../../app/pages/admin/documents.vue'
import { useAdminAccessStore } from '../../app/stores/useAdminAccessStore'
import { useAdminDocumentsStore } from '../../app/stores/useAdminDocumentsStore'
import type { AdminDocumentDetail, AdminDocumentInventoryItem } from '../../app/stores/useAdminDocumentsStore'
import { ref } from 'vue'
import en from '../../app/i18n/locales/en.json'
import viLocale from '../../app/i18n/locales/vi.json'

const category = { id: 'category-1', slug: 'architecture', nameEn: 'Architecture', nameVi: 'Kiến trúc', sortOrder: 1, isActive: false, version: 1 }

const { mockGet, mockPost } = vi.hoisted(() => ({
  mockGet: vi.fn(),
  mockPost: vi.fn()
}))

vi.mock('../../app/composables/useApiClient', async importOriginal => ({
  ...await importOriginal<typeof import('../../app/composables/useApiClient')>(),
  useApiClient: () => ({ get: mockGet, post: mockPost })
}))

const mockDocs: AdminDocumentInventoryItem[] = [
  {
    id: 'doc-1',
    scope: 'Personal',
    title: 'Kubernetes Architecture',
    ownerUserId: 'user-1',
    ownerName: 'Alice',
    ownerEmail: 'alice@example.com',
    sourceType: 'PdfBook',
    categoryId: category.id,
    category,
    visibility: null,
    language: 'en',
    status: 'Failed',
    sourceAvailability: 'Available',
    createdAt: '2026-10-15T00:00:00Z',
    observedAt: '2026-10-15T00:00:00Z'
  },
  {
    id: 'doc-2',
    scope: 'Catalog',
    title: 'Distributed Transactions',
    ownerUserId: null,
    ownerName: null,
    ownerEmail: null,
    sourceType: 'WebDocUrl',
    categoryId: category.id,
    category,
    visibility: 'Hidden',
    language: 'en',
    status: 'Ready',
    sourceAvailability: 'Available',
    createdAt: '2026-10-15T00:00:00Z',
    observedAt: '2026-10-15T00:00:00Z'
  }
]

const mockDocDetail: AdminDocumentDetail = {
  id: 'doc-1',
  scope: 'Personal',
  title: 'Kubernetes Architecture',
  ownerUserId: 'user-1',
  ownerName: 'Alice',
  ownerEmail: 'alice@example.com',
  sourceType: 'PdfBook',
  categoryId: category.id,
  category,
  visibility: null,
  language: 'en',
  status: 'Failed',
  sourceAvailability: 'Available',
  createdAt: '2026-10-15T00:00:00Z',
  observedAt: '2026-10-15T00:00:00Z',
  controlVersion: 2,
  currentAttemptGeneration: 1,
  currentAttemptId: 'att-1',
  isRetryEligible: true,
  ineligibleReasonCode: null,
  ineligibleReason: null,
  retentionExpiresAtUtc: null,
  sanitizedSourceUrl: null
}

describe('admin/documents.vue (MasterDetail Operations Page)', () => {
  afterEach(() => vi.unstubAllGlobals())
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()

    const access = useAdminAccessStore()
    access.capabilities = {
      canManageCatalog: false,
      canManageAccounts: false,
      canReadAudit: false,
      canManageDocumentOperations: true
    }
    access.state = 'authorized'

    mockGet.mockImplementation((url: string) => {
      if (url.includes('/api/v1/admin/documents?')) {
        return Promise.resolve({
          items: mockDocs,
          totalCount: 2,
          page: 1,
          pageSize: 20
        })
      }
      if (url.includes('/attempts')) {
        return Promise.resolve({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 20
        })
      }
      if (url.includes('/api/v1/admin/documents/') && url.includes('doc-1') && !url.includes('/attempts')) {
        return Promise.resolve(mockDocDetail)
      }
      return Promise.resolve({})
    })
  })

  it('withholds operations page when canManageDocumentOperations is false', async () => {
    const access = useAdminAccessStore()
    access.capabilities.canManageDocumentOperations = false

    const wrapper = mount(AdminDocumentsPage, {
      global: {
        stubs: {
          MasterDetailLayout: true,
          AppModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()
    expect(wrapper.text()).not.toContain('admin_documents.title')
    expect(mockGet).not.toHaveBeenCalled()
  })

  it('renders MasterDetail operations workspace and loads inventory for authorized admin', async () => {
    const wrapper = mount(AdminDocumentsPage, {
      global: {
        stubs: {
          MasterDetailLayout: {
            template: '<div><slot name="header" /><slot name="nav" /><slot name="content" /></div>'
          },
          AppModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('admin_documents.title')
    expect(wrapper.text()).toContain('Kubernetes Architecture')
    expect(wrapper.text()).toContain('Distributed Transactions')
    expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('/api/v1/admin/documents?'), { cache: 'no-store' })
  })

  it('filters inventory by scope on button click', async () => {
    const wrapper = mount(AdminDocumentsPage, {
      global: {
        stubs: {
          MasterDetailLayout: {
            template: '<div><slot name="header" /><slot name="nav" /><slot name="content" /></div>'
          },
          AppModal: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    const personalFilterBtn = wrapper.findAll('button').find(b => b.text().includes('admin_documents.filter_personal'))
    expect(personalFilterBtn).toBeDefined()

    await personalFilterBtn!.trigger('click')
    await flushPromises()

    const store = useAdminDocumentsStore()
    expect(store.scopeFilter).toBe('Personal')
    expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('scope=Personal'), { cache: 'no-store' })
  })

  it('shows retry button when document is eligible and opens retry modal', async () => {
    const wrapper = mount(AdminDocumentsPage, {
      global: {
        stubs: {
          MasterDetailLayout: {
            template: '<div><slot name="header" /><slot name="nav" /><slot name="content" /></div>'
          },
          AppModal: {
            template: '<div v-if="$attrs[\'open\']" data-modal><slot /></div>'
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    const retryBtn = wrapper.findAll('button').find(b => b.text().includes('admin_documents.retry_button'))
    expect(retryBtn).toBeDefined()

    await retryBtn!.trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-modal]').exists()).toBe(true)
  })

  it('validates retry reason and submits retry command', async () => {
    mockPost.mockResolvedValueOnce({
      attemptId: 'att-new',
      targetDocumentId: 'doc-1',
      state: 'Queued',
      controlVersion: 3
    })

    const wrapper = mount(AdminDocumentsPage, {
      global: {
        stubs: {
          MasterDetailLayout: {
            template: '<div><slot name="header" /><slot name="nav" /><slot name="content" /></div>'
          },
          AppModal: {
            template: '<div data-modal><slot /><slot name="footer" /></div>'
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    await flushPromises()

    // Open modal
    const retryBtn = wrapper.findAll('button').find(b => b.text().includes('admin_documents.retry_button'))
    await retryBtn!.trigger('click')
    await flushPromises()

    // Confirm button should be disabled when reason is empty
    const confirmBtn = wrapper.findAll('button').find(b => b.text().includes('admin_documents.retry_confirm'))
    expect(confirmBtn?.attributes('disabled')).toBeDefined()

    // Fill in reason
    const input = wrapper.find('#retry-reason-input')
    await input.setValue('Re-running failed PDF ingestion after timeout')

    // Confirm button now enabled
    expect(confirmBtn?.attributes('disabled')).toBeUndefined()

    await confirmBtn!.trigger('click')
    await flushPromises()

    expect(mockPost).toHaveBeenCalledTimes(1)
    const [url, payload] = mockPost.mock.calls[0] as [string, { reason: string }]
    expect(url).toBe('/api/v1/admin/documents/Personal/doc-1/retry')
    expect(payload.reason).toBe('Re-running failed PDF ingestion after timeout')
  })

  it('distinguishes hidden publication from ready processing in rail and detail with localized metadata', async () => {
    const locale = ref('en')
    const translate = (key: string) => {
      const dictionary = locale.value === 'vi' ? viLocale : en
      return key.split('.').reduce<unknown>((value, part) => value && typeof value === 'object' ? Reflect.get(value, part) : undefined, dictionary) as string || key
    }
    vi.stubGlobal('useI18n', () => ({ locale, t: translate }))
    mockGet.mockImplementation((url: string) => {
      if (url.includes('/attempts')) return Promise.resolve({ items: [], totalCount: 0, page: 1, pageSize: 20 })
      if (url.includes('/documents?')) return Promise.resolve({ items: mockDocs, totalCount: 2, page: 1, pageSize: 20 })
      const document = url.includes('/Catalog/') ? mockDocs[1]! : { ...mockDocs[0]!, ownerName: null, ownerUserId: null, ownerEmail: null }
      return Promise.resolve({ ...mockDocDetail, ...document, ownerName: document.scope === 'Catalog' ? 'System Curated' : document.ownerName, isRetryEligible: false, ineligibleReasonCode: 'AlreadyReady' })
    })
    const wrapper = mount(AdminDocumentsPage, {
      global: {
        stubs: { MasterDetailLayout: { template: '<div><header><slot name="header" /></header><nav><slot name="nav" /></nav><main><slot name="content" /></main></div>' }, AppModal: true },
        mocks: { $t: translate }
      }
    })
    await flushPromises()
    const catalogRow = wrapper.findAll('nav button').find(button => button.text().includes('Distributed Transactions'))!
    await catalogRow.trigger('click')
    await flushPromises()
    expect(wrapper.find('main').text()).toContain(category.nameEn)
    for (const region of [catalogRow, wrapper.find('main')]) {
      expect(region.text()).toContain(en.admin_documents.publication)
      expect(region.text()).toContain(en.admin_documents.visibility_Hidden)
      expect(region.text()).toContain(en.admin_documents.processing_status)
      expect(region.text()).toContain(en.admin_documents.status_ready)
    }
    expect(wrapper.find('main').text()).toContain(category.nameEn)
    expect(wrapper.text()).not.toContain('System Curated')
    expect(wrapper.text()).not.toContain('WebDocUrl')
    expect(wrapper.text()).not.toContain('"nameEn"')
    const personalRow = wrapper.findAll('nav button').find(button => button.text().includes('Kubernetes Architecture'))!
    expect(personalRow.text()).toContain('Alice')
    expect(personalRow.text()).not.toContain(en.admin_documents.publication)
    locale.value = 'vi'
    await flushPromises()
    expect(wrapper.find('main').text()).toContain(category.nameVi)
    expect(wrapper.find('main').text()).toContain(viLocale.admin_documents.system_catalog)
    expect(wrapper.find('main').text()).toContain(viLocale.admin_documents.source_WebDocUrl)
    expect(wrapper.find('main').text()).toContain(viLocale.admin_documents.ineligible_AlreadyReady)
    expect(catalogRow.text()).toContain(viLocale.admin_documents.scope_catalog)
    expect(wrapper.find('main').text()).toContain(viLocale.admin_documents.visibility_Hidden)
    expect(wrapper.find('main').text()).toContain(viLocale.admin_documents.status_ready)
    expect(wrapper.find('main').text()).toContain('Distributed Transactions')
    await personalRow.trigger('click')
    await flushPromises()
    expect(wrapper.find('main').text()).toContain(viLocale.admin_documents.owner_unknown)
    expect(wrapper.find('main').text()).not.toContain(viLocale.admin_documents.publication)
    expect(wrapper.find('main').text()).not.toContain(viLocale.admin_documents.system_catalog)
    wrapper.unmount()
    vi.unstubAllGlobals()
  })
})
