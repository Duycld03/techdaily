import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useLibraryStore } from '~/stores/useLibraryStore'

const mockBooks = [
  {
    id: 'b-1',
    title: 'Designing Data-Intensive Applications',
    slug: 'ddia',
    sourceType: 0,
    category: 1,
    authorOrSourceUrl: 'Martin Kleppmann',
    totalChunks: 12,
    isPublished: true,
    createdAt: '2026-08-31T00:00:00Z'
  },
  {
    id: 'b-2',
    title: 'Vue 3 Core Architecture',
    slug: 'vue3-core',
    sourceType: 0,
    category: 0,
    authorOrSourceUrl: 'Evan You',
    totalChunks: 8,
    isPublished: true,
    createdAt: '2026-08-31T00:00:00Z'
  }
]

const mockPost = vi.fn(async (url: string, body: any) => {
  if (url.includes('/import')) {
    return {
      book: {
        id: 'b-3',
        title: body.title,
        slug: 'sre-book',
        sourceType: 1,
        category: body.category,
        totalChunks: 5,
        isPublished: true,
        createdAt: '2026-08-31T00:00:00Z'
      }
    }
  }
  if (url.includes('/curate')) {
    return {
      chunk: {
        id: 'chk-1',
        chunkOrder: 1,
        chapterTitle: 'Reliability, Scalability, and Maintainability',
        summaryMarkdown: 'Curated summary',
        originalTextMarkdown: '# Curated markdown',
        keyTakeaways: ['Takeaway 1', 'Takeaway 2'],
        estimatedReadMinutes: 5,
        isAiFormatted: true
      }
    }
  }
  throw new Error('Not found')
})

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/books/b-1')) {
        return {
          book: {
            ...mockBooks[0],
            chunks: [
              {
                id: 'chk-1',
                chunkOrder: 1,
                chapterTitle: 'Reliability, Scalability, and Maintainability',
                summaryMarkdown: 'Core qualities of data systems.',
                originalTextMarkdown: 'Systems must maintain performance...',
                keyTakeaways: ['High availability', 'Fault tolerance'],
                estimatedReadMinutes: 4
              }
            ]
          }
        }
      }
      if (url.includes('/books')) {
        return { books: [...mockBooks] }
      }
      throw new Error('Not found')
    }),
    post: mockPost
  })
}))

describe('useLibraryStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('fetches books list with categories and search filtering', async () => {
    const library = useLibraryStore()
    expect(library.books).toHaveLength(0)

    await library.fetchBooks()
    expect(library.books).toHaveLength(2)
    expect(library.books[0].title).toBe('Designing Data-Intensive Applications')
  })

  it('fetches specific book details with chunks', async () => {
    const library = useLibraryStore()
    const book = await library.fetchBookById('b-1')

    expect(book.id).toBe('b-1')
    expect(book.chunks).toHaveLength(1)
    expect(book.chunks[0].chapterTitle).toBe('Reliability, Scalability, and Maintainability')
  })

  it('imports new document and triggers chunking', async () => {
    const library = useLibraryStore()
    const result = await library.importDocument({
      title: 'Site Reliability Engineering',
      markdownContent: '# Chapter 1\nSRE principles...',
      category: 3
    })

    expect(result.id).toBe('b-3')
    expect(result.title).toBe('Site Reliability Engineering')
    expect(result.totalChunks).toBe(5)
    expect(library.books).toHaveLength(1)
  })

  it('deduplicates concurrent curateSlice calls for the same slice', async () => {
    const library = useLibraryStore()

    const p1 = library.curateSlice('b-1', 1)
    const p2 = library.curateSlice('b-1', 1)

    const [res1, res2] = await Promise.all([p1, p2])
    expect(res1).toEqual(res2)
    expect(res1?.isAiFormatted).toBe(true)

    // Verify mockPost was called only once despite two concurrent invocations
    expect(mockPost).toHaveBeenCalledTimes(1)
  })
})
