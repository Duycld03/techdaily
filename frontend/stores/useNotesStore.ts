import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface Highlight {
  id: string
  documentChunkId: string
  chapterTitle: string
  bookTitle: string
  selectedText: string
  note?: string
  tags: string[]
  createdAt: string
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
    } catch (err: any) {
      error.value = err.message || 'Failed to load highlights.'
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
      highlights.value.unshift(res.highlight)
      return res.highlight
    } catch (err: any) {
      error.value = err.message || 'Failed to save highlight.'
      throw err
    } finally {
      isCreating.value = false
    }
  }

  async function deleteHighlight(id: string) {
    try {
      const api = useApiClient()
      await api.delete(`/api/v1/notes/highlights/${id}`)
      highlights.value = highlights.value.filter((h) => h.id !== id)
    } catch (err: any) {
      error.value = err.message || 'Failed to delete highlight.'
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
    deleteHighlight
  }
})
