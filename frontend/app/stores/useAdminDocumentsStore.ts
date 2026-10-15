import { defineStore } from 'pinia'
import { shallowRef, watch } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiClient } from '~/composables/useApiClient'

export interface AdminDocumentInventoryItem {
  id: string
  scope: 'Personal' | 'Catalog'
  title: string
  ownerUserId: string | null
  ownerName: string | null
  ownerEmail: string | null
  sourceType: string
  category: string
  language: string
  status: string
  sourceAvailability: string
  createdAt: string
  observedAt: string
}

export interface AdminDocumentDetail extends AdminDocumentInventoryItem {
  controlVersion: number
  currentAttemptGeneration: number
  currentAttemptId: string | null
  isRetryEligible: boolean
  ineligibleReasonCode: string | null
  ineligibleReason: string | null
  retentionExpiresAtUtc: string | null
  sanitizedSourceUrl: string | null
}

export interface AdminDocumentAttemptItem {
  attemptId: string
  targetDocumentId: string
  scope: string
  origin: string
  stage: string
  state: string
  progressPercent: number
  queuedAtUtc: string
  startedAtUtc: string | null
  endedAtUtc: string | null
  safeErrorCode: string | null
  safeErrorGuidance: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface RetryResponse {
  attemptId: string
  targetDocumentId: string
  state: string
  controlVersion: number
}

export const useAdminDocumentsStore = defineStore('admin-documents', () => {
  const auth = useAuthStore()
  const api = useApiClient()

  const documents = shallowRef<AdminDocumentInventoryItem[]>([])
  const totalCount = shallowRef(0)
  const page = shallowRef(1)
  const pageSize = shallowRef(20)
  const search = shallowRef('')
  const scopeFilter = shallowRef<string | null>(null)
  const sourceTypeFilter = shallowRef<string | null>(null)
  const statusFilter = shallowRef<string | null>(null)

  const selectedDocumentId = shallowRef<string | null>(null)
  const selectedDocument = shallowRef<AdminDocumentDetail | null>(null)
  const attempts = shallowRef<AdminDocumentAttemptItem[]>([])
  const attemptsTotalCount = shallowRef(0)
  const attemptsPage = shallowRef(1)

  const isLoading = shallowRef(false)
  const isLoadingDetail = shallowRef(false)
  const isLoadingAttempts = shallowRef(false)
  const isRetrying = shallowRef(false)
  const error = shallowRef<string | null>(null)

  let fetchGeneration = 0
  let detailGeneration = 0
  let activeFetchPromise: Promise<void> | null = null

  function reset() {
    fetchGeneration++
    detailGeneration++
    activeFetchPromise = null
    documents.value = []
    totalCount.value = 0
    page.value = 1
    search.value = ''
    scopeFilter.value = null
    sourceTypeFilter.value = null
    statusFilter.value = null
    selectedDocumentId.value = null
    selectedDocument.value = null
    attempts.value = []
    attemptsTotalCount.value = 0
    attemptsPage.value = 1
    isLoading.value = false
    isLoadingDetail.value = false
    isLoadingAttempts.value = false
    isRetrying.value = false
    error.value = null
  }

  watch([() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration], reset, { flush: 'sync' })

  async function fetchDocuments(force = false): Promise<void> {
    if (activeFetchPromise && !force) return activeFetchPromise

    const generation = ++fetchGeneration
    isLoading.value = true
    error.value = null

    const promise = (async () => {
      try {
        const params = new URLSearchParams()
        params.set('page', page.value.toString())
        params.set('pageSize', pageSize.value.toString())
        if (search.value.trim()) params.set('search', search.value.trim())
        if (scopeFilter.value) params.set('scope', scopeFilter.value)
        if (sourceTypeFilter.value) params.set('sourceType', sourceTypeFilter.value)
        if (statusFilter.value) params.set('status', statusFilter.value)

        const res = await api.get<PagedResult<AdminDocumentInventoryItem>>(`/api/v1/admin/documents?${params.toString()}`, {
          cache: 'no-store'
        })

        if (generation !== fetchGeneration) return

        documents.value = res.items
        totalCount.value = res.totalCount
      } catch (err: unknown) {
        if (generation !== fetchGeneration) return
        error.value = err instanceof Error ? err.message : 'Failed to fetch document inventory'
      } finally {
        if (generation === fetchGeneration) {
          isLoading.value = false
          activeFetchPromise = null
        }
      }
    })()

    activeFetchPromise = promise
    return promise
  }

  async function selectDocument(id: string | null, scope: string = 'Personal'): Promise<void> {
    selectedDocumentId.value = id
    const gen = ++detailGeneration

    if (!id) {
      selectedDocument.value = null
      attempts.value = []
      attemptsTotalCount.value = 0
      return
    }

    isLoadingDetail.value = true
    isLoadingAttempts.value = true

    try {
      const detail = await api.get<AdminDocumentDetail>(`/api/v1/admin/documents/${scope}/${id}`, { cache: 'no-store' })
      if (gen !== detailGeneration) return
      selectedDocument.value = detail

      await fetchAttempts(id, scope, 1)
    } catch {
      if (gen !== detailGeneration) return
      selectedDocument.value = null
      attempts.value = []
      attemptsTotalCount.value = 0
    } finally {
      if (gen === detailGeneration) {
        isLoadingDetail.value = false
        isLoadingAttempts.value = false
      }
    }
  }

  async function fetchAttempts(documentId: string, scope: string = 'Personal', pageNum = 1): Promise<void> {
    const gen = detailGeneration
    isLoadingAttempts.value = true
    try {
      const params = new URLSearchParams()
      params.set('page', pageNum.toString())
      params.set('pageSize', '20')
      const res = await api.get<PagedResult<AdminDocumentAttemptItem>>(`/api/v1/admin/documents/${scope}/${documentId}/attempts?${params.toString()}`, {
        cache: 'no-store'
      })
      if (gen !== detailGeneration) return
      attempts.value = res.items
      attemptsTotalCount.value = res.totalCount
      attemptsPage.value = res.page
    } finally {
      if (gen === detailGeneration) {
        isLoadingAttempts.value = false
      }
    }
  }

  async function retryDocument(reason: string): Promise<RetryResponse | null> {
    if (!selectedDocument.value) return null
    const doc = selectedDocument.value
    if (!doc.currentAttemptId) return null

    isRetrying.value = true
    try {
      const payload = {
        scope: doc.scope,
        documentId: doc.id,
        attemptId: doc.currentAttemptId,
        expectedVersion: doc.controlVersion,
        requestId: crypto.randomUUID(),
        reason
      }
      const res = await api.post<RetryResponse>(`/api/v1/admin/documents/${doc.scope}/${doc.id}/retry`, payload)
      // refresh detail & attempts
      await selectDocument(doc.id, doc.scope)
      return res
    } finally {
      isRetrying.value = false
    }
  }

  return {
    documents,
    totalCount,
    page,
    pageSize,
    search,
    scopeFilter,
    sourceTypeFilter,
    statusFilter,
    selectedDocumentId,
    selectedDocument,
    attempts,
    attemptsTotalCount,
    attemptsPage,
    isLoading,
    isLoadingDetail,
    isLoadingAttempts,
    isRetrying,
    error,
    reset,
    fetchDocuments,
    selectDocument,
    fetchAttempts,
    retryDocument
  }
})
