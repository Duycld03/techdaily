import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

export interface Highlight {
  id: string
  documentChunkId: string
  chapterTitle: string
  bookTitle: string
  selectedText: string
  note?: string
  tags: string[]
  createdAt: string
  hasFlashcard?: boolean
}

export interface TagCount {
  tag: string
  count: number
}

export const useNotesStore = defineStore('notes', () => {
  const highlights = ref<Highlight[]>([])
  const isLoading = ref(false)
  const isCreating = ref(false)
  const error = ref<string | null>(null)

  const currentPage = ref(1)
  const pageSize = ref(15)
  const totalCount = ref(0)
  const totalPages = ref(0)
  const tagCounts = ref<TagCount[]>([])

  async function fetchHighlights(
    paramsOrTag?:
      | {
          tag?: string
          search?: string
          page?: number
          pageSize?: number
          append?: boolean
        }
      | string,
    maybePage?: number
  ) {
    let tag: string | undefined
    let search: string | undefined
    let page: number | undefined
    let size: number | undefined
    let append = false

    if (typeof paramsOrTag === 'object' && paramsOrTag !== null) {
      tag = paramsOrTag.tag
      search = paramsOrTag.search
      page = paramsOrTag.page
      size = paramsOrTag.pageSize
      append = Boolean(paramsOrTag.append)
    } else {
      tag = paramsOrTag
      page = maybePage
    }

    const targetPage = page ?? (append ? currentPage.value + 1 : 1)
    const targetSize = size ?? pageSize.value

    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const queryParams = new URLSearchParams()
      if (tag) queryParams.append('tag', tag)
      if (search) queryParams.append('search', search)
      queryParams.append('page', targetPage.toString())
      queryParams.append('pageSize', targetSize.toString())

      const res = await api.get<{
        highlights: Highlight[]
        totalCount?: number
        page?: number
        pageSize?: number
        totalPages?: number
        tagCounts?: TagCount[]
      }>(`/api/v1/notes/highlights?${queryParams.toString()}`)

      const items = res.highlights || []
      if (append) {
        const existingIds = new Set<string>(highlights.value.map((h) => h.id))
        for (const item of items) {
          if (!existingIds.has(item.id)) {
            highlights.value.push(item)
            existingIds.add(item.id)
          }
        }
      } else {
        highlights.value = items
      }

      totalCount.value = res.totalCount ?? highlights.value.length
      totalPages.value =
        res.totalPages ??
        (totalCount.value > 0 ? Math.ceil(totalCount.value / targetSize) : 0)
      currentPage.value = res.page ?? targetPage
      pageSize.value = res.pageSize ?? targetSize
      if (res.tagCounts) {
        tagCounts.value = res.tagCounts
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to load highlights.'
    } finally {
      isLoading.value = false
    }
  }

  async function createHighlight(params: {
    documentChunkId: string
    selectedText: string
    note?: string
    tags?: string[]
  }) {
    isCreating.value = true
    try {
      const api = useApiClient()
      const res = await api.post<{ highlight: Highlight }>('/api/v1/notes/highlights', params)
      const highlightItem = res.highlight || (res as unknown as Highlight)
      const existingIndex = highlights.value.findIndex((h) => h.id === highlightItem.id)
      if (existingIndex !== -1) {
        highlights.value[existingIndex] = highlightItem
      } else {
        highlights.value.unshift(highlightItem)
      }
      return highlightItem
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to save highlight.'
      throw err
    } finally {
      isCreating.value = false
    }
  }

  async function updateHighlight(id: string, payload: { note?: string; tags?: string[] }) {
    const api = useApiClient()
    const res = await api.put<{ highlight: Highlight }>(`/api/v1/notes/highlights/${id}`, payload)
    const updated = res.highlight || (res as unknown as Highlight)
    const idx = highlights.value.findIndex((h) => h.id === id)
    if (idx !== -1) {
      highlights.value[idx] = updated
    }
    return updated
  }

  async function deleteHighlight(id: string) {
    try {
      const api = useApiClient()
      await api.delete(`/api/v1/notes/highlights/${id}`)
      highlights.value = highlights.value.filter((h) => h.id !== id)
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to delete highlight.'
      throw err
    }
  }

  return {
    highlights,
    isLoading,
    isCreating,
    error,
    currentPage,
    pageSize,
    totalCount,
    totalPages,
    tagCounts,
    fetchHighlights,
    createHighlight,
    updateHighlight,
    deleteHighlight
  }
})
