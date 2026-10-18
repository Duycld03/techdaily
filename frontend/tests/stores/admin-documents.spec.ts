import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAdminDocumentsStore } from '../../app/stores/useAdminDocumentsStore'
import type { AdminDocumentDetail, AdminDocumentAttemptItem } from '../../app/stores/useAdminDocumentsStore'
import { clearAuthStorage } from '../helpers/admin'

const { get, post } = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn()
}))

vi.mock('../../app/composables/useApiClient', async importOriginal => ({
  ...await importOriginal<typeof import('../../app/composables/useApiClient')>(),
  useApiClient: () => ({ get, post })
}))

describe('useAdminDocumentsStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    get.mockReset()
    post.mockReset()
  })

  it('initializes with default state', () => {
    const store = useAdminDocumentsStore()
    expect(store.documents).toEqual([])
    expect(store.totalCount).toBe(0)
    expect(store.page).toBe(1)
    expect(store.pageSize).toBe(20)
    expect(store.search).toBe('')
    expect(store.scopeFilter).toBeNull()
    expect(store.selectedDocument).toBeNull()
    expect(store.attempts).toEqual([])
    expect(store.isLoading).toBe(false)
  })

  it('fetchDocuments serializes query parameters into URL and sets documents', async () => {
    const store = useAdminDocumentsStore()
    get.mockResolvedValueOnce({
      items: [
        {
          id: 'doc-1',
          scope: 'Personal',
          title: 'Kubernetes Guide',
          ownerUserId: 'u-1',
          ownerName: 'Alice',
          ownerEmail: 'alice@example.com',
          sourceType: 'PdfBook',
          categoryId: 'category-1',
          category: { id: 'category-1', slug: 'devops', nameEn: 'DevOps', nameVi: 'Vận hành', sortOrder: 1, isActive: false, version: 1 },
          visibility: null,
          language: 'en',
          status: 'Ready',
          sourceAvailability: 'Available',
          createdAt: '2026-10-15T00:00:00Z',
          observedAt: '2026-10-15T00:00:00Z'
        }
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20
    })

    store.search = 'k8s'
    store.scopeFilter = 'Personal'
    store.sourceTypeFilter = 'PdfBook'
    store.statusFilter = 'Ready'

    await store.fetchDocuments()

    expect(get).toHaveBeenCalledTimes(1)
    const callUrl = get.mock.calls[0][0] as string
    expect(callUrl).toContain('/api/v1/admin/documents?')
    expect(callUrl).toContain('search=k8s')
    expect(callUrl).toContain('scope=Personal')
    expect(callUrl).toContain('sourceType=PdfBook')
    expect(callUrl).toContain('status=Ready')
    expect(callUrl).toContain('page=1')
    expect(callUrl).toContain('pageSize=20')

    expect(store.documents).toHaveLength(1)
    expect(store.documents[0].title).toBe('Kubernetes Guide')
    expect(store.totalCount).toBe(1)
  })

  it('reuses in-flight promise for concurrent fetchDocuments calls', async () => {
    const store = useAdminDocumentsStore()
    const { promise, resolve } = Promise.withResolvers<unknown>()
    get.mockReturnValueOnce(promise)

    const p1 = store.fetchDocuments()
    const p2 = store.fetchDocuments()

    expect(get).toHaveBeenCalledTimes(1)
    resolve({ items: [], totalCount: 0, page: 1, pageSize: 20 })
    await Promise.all([p1, p2])
  })

  it('selectDocument fetches detail and attempts', async () => {
    const store = useAdminDocumentsStore()
    const docDetail = {
      id: 'doc-123',
      scope: 'Personal',
      title: 'Doc 123',
      ownerUserId: 'u-1',
      ownerName: 'Alice',
      ownerEmail: 'alice@example.com',
      sourceType: 'PdfBook',
      categoryId: 'category-1',
      category: { id: 'category-1', slug: 'backend', nameEn: 'Backend', nameVi: 'Máy chủ', sortOrder: 1, isActive: true, version: 1 },
      visibility: null,
      language: 'en',
      status: 'Ready',
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

    const attemptsResult = {
      items: [
        {
          attemptId: 'att-1',
          targetDocumentId: 'doc-123',
          scope: 'Personal',
          origin: 'UserUpload',
          stage: 'Slicing',
          state: 'Failed',
          progressPercent: 40,
          queuedAtUtc: '2026-10-15T00:00:00Z',
          startedAtUtc: '2026-10-15T00:00:05Z',
          endedAtUtc: '2026-10-15T00:01:00Z',
          safeErrorCode: 'ERR_TIMEOUT',
          safeErrorGuidance: 'Processing timed out'
        }
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20
    }

    get.mockResolvedValueOnce(docDetail)
    get.mockResolvedValueOnce(attemptsResult)

    await store.selectDocument('doc-123')

    expect(get).toHaveBeenCalledWith('/api/v1/admin/documents/Personal/doc-123', { cache: 'no-store' })
    expect(store.selectedDocument).toEqual(docDetail)
    expect(store.attempts).toHaveLength(1)
    expect(store.attempts[0].attemptId).toBe('att-1')
  })

  it('selectDocument(null) clears detail and attempts', async () => {
    const store = useAdminDocumentsStore()
    store.selectedDocument = { id: 'some' } as unknown as AdminDocumentDetail
    store.attempts = [{ attemptId: 'att' }] as unknown as AdminDocumentAttemptItem[]
    store.attemptsTotalCount = 1

    await store.selectDocument(null)

    expect(store.selectedDocument).toBeNull()
    expect(store.attempts).toEqual([])
    expect(store.attemptsTotalCount).toBe(0)
  })

  it('retryDocument submits retry command and refreshes detail and attempts', async () => {
    const store = useAdminDocumentsStore()
    store.selectedDocument = {
      id: 'doc-123',
      scope: 'Personal',
      controlVersion: 1,
      currentAttemptId: 'att-1'
    } as unknown as AdminDocumentDetail

    post.mockResolvedValueOnce({
      attemptId: 'att-2',
      targetDocumentId: 'doc-123',
      state: 'Queued',
      controlVersion: 2
    })

    // for selectDocument refresh
    get.mockResolvedValueOnce({
      id: 'doc-123',
      scope: 'Personal',
      controlVersion: 2,
      currentAttemptId: 'att-2'
    })
    get.mockResolvedValueOnce({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20
    })

    const res = await store.retryDocument('Administrator manual retry')

    expect(post).toHaveBeenCalledTimes(1)
    const [url, payload] = post.mock.calls[0] as [string, { documentId: string; attemptId: string; expectedVersion: number; reason: string }]
    expect(url).toBe('/api/v1/admin/documents/Personal/doc-123/retry')
    expect(payload.documentId).toBe('doc-123')
    expect(payload.attemptId).toBe('att-1')
    expect(payload.expectedVersion).toBe(1)
    expect(payload.reason).toBe('Administrator manual retry')
    expect(res?.attemptId).toBe('att-2')
  })

  it('reset clears all query filters, selection, and error states', () => {
    const store = useAdminDocumentsStore()
    store.search = 'abc'
    store.scopeFilter = 'Catalog'
    store.page = 3
    store.totalCount = 100
    store.error = 'Something broke'

    store.reset()

    expect(store.search).toBe('')
    expect(store.scopeFilter).toBeNull()
    expect(store.page).toBe(1)
    expect(store.totalCount).toBe(0)
    expect(store.error).toBeNull()
  })

  it('refreshes selected detail from authoritative reads when inventory refresh observes catalog hiding', async () => {
    const store = useAdminDocumentsStore()
    const published = {
      id: 'catalog-1', scope: 'Catalog', title: 'Catalog source', ownerUserId: null, ownerName: 'System Curated', ownerEmail: null,
      sourceType: 'WebDocUrl', categoryId: 'category-1',
      category: { id: 'category-1', slug: 'web', nameEn: 'Web', nameVi: 'Trang web', sortOrder: 1, isActive: false, version: 1 },
      language: 'en', status: 'Ready', visibility: 'Published', sourceAvailability: 'Available',
      createdAt: '2026-10-15T00:00:00Z', observedAt: '2026-10-15T00:00:00Z', controlVersion: 1,
      currentAttemptGeneration: 1, currentAttemptId: null, isRetryEligible: false, ineligibleReasonCode: 'AlreadyReady',
      ineligibleReason: null, retentionExpiresAtUtc: null, sanitizedSourceUrl: null
    }
    let hidden = false
    get.mockImplementation((url: string) => {
      const current = { ...published, visibility: hidden ? 'Hidden' : 'Published' }
      if (url.includes('/attempts')) return Promise.resolve({ items: [], totalCount: 0, page: 1, pageSize: 20 })
      if (url.includes('/documents?')) return Promise.resolve({ items: [current], totalCount: 1, page: 1, pageSize: 20 })
      return Promise.resolve(current)
    })
    await store.fetchDocuments()
    await store.selectDocument('catalog-1', 'Catalog')
    hidden = true
    await store.fetchDocuments(true)
    expect(store.documents[0]).toMatchObject({ id: 'catalog-1', visibility: 'Hidden', status: 'Ready' })
    expect(store.selectedDocument).toMatchObject({ id: 'catalog-1', visibility: 'Hidden', status: 'Ready' })
    expect(post).not.toHaveBeenCalled()
  })

  it('handles detail failure during refresh without losing selected identity and exposes detail error', async () => {
    const store = useAdminDocumentsStore()
    const doc = {
      id: 'catalog-fail-1', scope: 'Catalog', title: 'Catalog fail test', ownerUserId: null, ownerName: 'System Curated', ownerEmail: null,
      sourceType: 'WebDocUrl', categoryId: 'category-1',
      category: { id: 'category-1', slug: 'web', nameEn: 'Web', nameVi: 'Trang web', sortOrder: 1, isActive: false, version: 1 },
      language: 'en', status: 'Ready', visibility: 'Published', sourceAvailability: 'Available',
      createdAt: '2026-10-15T00:00:00Z', observedAt: '2026-10-15T00:00:00Z', controlVersion: 1,
      currentAttemptGeneration: 1, currentAttemptId: null, isRetryEligible: false, ineligibleReasonCode: 'AlreadyReady',
      ineligibleReason: null, retentionExpiresAtUtc: null, sanitizedSourceUrl: null
    }
    get.mockImplementation((url: string) => {
      if (url.includes('/documents?')) return Promise.resolve({ items: [doc], totalCount: 1, page: 1, pageSize: 20 })
      if (url.includes('/attempts')) return Promise.resolve({ items: [], totalCount: 0, page: 1, pageSize: 20 })
      return Promise.reject(new Error('Detail network failure'))
    })

    await store.fetchDocuments()
    await store.selectDocument('catalog-fail-1', 'Catalog')

    expect(store.selectedDocumentId).toBe('catalog-fail-1')
    expect(store.selectedDocument).toBeNull()
    expect(store.detailError).toBe('Detail network failure')
  })

  it('retains successfully fetched detail when processing attempts request fails', async () => {
    const store = useAdminDocumentsStore()
    const doc = {
      id: 'catalog-attempt-fail', scope: 'Catalog', title: 'Catalog attempt fail', ownerUserId: null, ownerName: 'System Curated', ownerEmail: null,
      sourceType: 'WebDocUrl', categoryId: 'category-1',
      category: { id: 'category-1', slug: 'web', nameEn: 'Web', nameVi: 'Trang web', sortOrder: 1, isActive: false, version: 1 },
      language: 'en', status: 'Ready', visibility: 'Published', sourceAvailability: 'Available',
      createdAt: '2026-10-15T00:00:00Z', observedAt: '2026-10-15T00:00:00Z', controlVersion: 1,
      currentAttemptGeneration: 1, currentAttemptId: null, isRetryEligible: false, ineligibleReasonCode: 'AlreadyReady',
      ineligibleReason: null, retentionExpiresAtUtc: null, sanitizedSourceUrl: null
    }
    get.mockImplementation((url: string) => {
      if (url.includes('/documents?')) return Promise.resolve({ items: [doc], totalCount: 1, page: 1, pageSize: 20 })
      if (url.includes('/attempts')) return Promise.reject(new Error('Attempts history unavailable'))
      return Promise.resolve(doc)
    })

    await store.fetchDocuments()
    await store.selectDocument('catalog-attempt-fail', 'Catalog')

    expect(store.selectedDocument).toMatchObject({ id: 'catalog-attempt-fail', title: 'Catalog attempt fail' })
    expect(store.attemptsError).toBe('Attempts history unavailable')
    expect(store.attempts).toEqual([])
  })

  it('does not drop subsequent inventory requests with different criteria when an earlier fetch is in flight', async () => {
    const store = useAdminDocumentsStore()
    const { promise: firstPromise, resolve: resolveFirst } = Promise.withResolvers<unknown>()

    get.mockImplementation((url: string) => {
      if (url.includes('search=first')) return firstPromise
      return Promise.resolve({ items: [], totalCount: 0, page: 1, pageSize: 20 })
    })

    store.search = 'first'
    const p1 = store.fetchDocuments()

    store.search = 'second'
    const p2 = store.fetchDocuments()

    resolveFirst!({ items: [], totalCount: 0, page: 1, pageSize: 20 })
    await Promise.all([p1, p2])

    const searchParams = get.mock.calls.map(([url]) => new URL(String(url), 'https://test.local').searchParams.get('search'))
    expect(searchParams).toContain('second')
  })
})
