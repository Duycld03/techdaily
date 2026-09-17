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

export const useNotesStore = defineStore('notes', () => {
  const highlights = ref<Highlight[]>([])
  const isLoading = ref(false)
  const isCreating = ref(false)
  const error = ref<string | null>(null)

  async function fetchHighlights(tag?: string) {
    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const query = tag ? `?tag=${encodeURIComponent(tag)}` : ''
      const res = await api.get<{ highlights: Highlight[] }>(`/api/v1/notes/highlights${query}`)
      highlights.value = res.highlights
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
    fetchHighlights,
    createHighlight,
    updateHighlight,
    deleteHighlight
  }
})
