import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

export interface Book {
  id: string
  title: string
  slug: string
  sourceType: number
  category: number
  authorOrSourceUrl?: string
  totalChunks: number
  isPublished: boolean
  createdAt: string
}

export interface ChunkSummary {
  id: string
  chunkOrder: number
  chapterTitle: string
  summaryMarkdown: string
  originalTextMarkdown: string
  keyTakeaways: string[]
  estimatedReadMinutes: number
}

export interface BookDetail extends Book {
  chunks: ChunkSummary[]
}

export const useLibraryStore = defineStore('library', () => {
  const books = ref<Book[]>([])
  const selectedBook = ref<BookDetail | null>(null)
  const isLoading = ref(false)
  const isImporting = ref(false)
  const error = ref<string | null>(null)

  async function fetchBooks(category?: number, search?: string) {
    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const query = new URLSearchParams()
      if (category !== undefined && category !== null) query.append('category', category.toString())
      if (search) query.append('search', search)

      const res = await api.get<{ books: Book[] }>(`/api/v1/library/books?${query.toString()}`)
      books.value = res.books
    } catch (err: any) {
      error.value = err.message || 'Failed to load books.'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchBookById(id: string) {
    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const res = await api.get<{ book: BookDetail }>(`/api/v1/library/books/${id}`)
      selectedBook.value = res.book
      return res.book
    } catch (err: any) {
      error.value = err.message || 'Failed to load book details.'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function importDocument(params: {
    title: string
    markdownContent: string
    category: number
    sourceUrl?: string
    language?: string
  }) {
    isImporting.value = true
    error.value = null
    try {
      const api = useApiClient()
      const res = await api.post<{ book: Book }>('/api/v1/library/import', {
        title: params.title,
        markdownContent: params.markdownContent,
        category: params.category,
        sourceUrl: params.sourceUrl,
        language: params.language || 'en'
      })

      books.value.unshift(res.book)
      return res.book
    } catch (err: any) {
      error.value = err.message || 'Failed to import document.'
      throw err
    } finally {
      isImporting.value = false
    }
  }

  return {
    books,
    selectedBook,
    isLoading,
    isImporting,
    error,
    fetchBooks,
    fetchBookById,
    importDocument
  }
})
