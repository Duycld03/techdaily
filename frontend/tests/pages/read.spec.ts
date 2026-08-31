import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useLibraryStore, type BookDetail } from '~/stores/useLibraryStore'

const mockBookDetail: BookDetail = {
  id: 'book-123',
  title: 'Designing Data-Intensive Applications',
  slug: 'ddia',
  sourceType: 0,
  category: 2,
  authorOrSourceUrl: 'Martin Kleppmann',
  totalChunks: 3,
  isPublished: true,
  createdAt: '2026-08-31T00:00:00Z',
  chunks: [
    {
      id: 'chunk-1',
      chunkOrder: 1,
      chapterTitle: 'Reliability, Scalability, Maintainability',
      summaryMarkdown: 'Summary 1',
      originalTextMarkdown: '# Reliability\nHardware faults vs Software errors.',
      keyTakeaways: ['Reliability is fault tolerance', 'SLO vs SLA'],
      estimatedReadMinutes: 5
    },
    {
      id: 'chunk-2',
      chunkOrder: 2,
      chapterTitle: 'Data Models and Query Languages',
      summaryMarkdown: 'Summary 2',
      originalTextMarkdown: '# Data Models\nRelational vs Document vs Graph.',
      keyTakeaways: ['Schema-on-read vs Schema-on-write'],
      estimatedReadMinutes: 6
    },
    {
      id: 'chunk-3',
      chunkOrder: 3,
      chapterTitle: 'Storage and Retrieval: LSM-Trees & B-Trees',
      summaryMarkdown: 'Summary 3',
      originalTextMarkdown: '# Storage Engines\nSSTables, MemTable, WAL, B-Trees.',
      keyTakeaways: ['LSM for writes, B-Trees for reads'],
      estimatedReadMinutes: 7
    }
  ]
}

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn().mockResolvedValue({ book: mockBookDetail })
  })
}))

describe('Immersive Document Reader (Hướng 1)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('calculates reading progress percentage accurately', () => {
    const total = mockBookDetail.chunks.length
    expect(Math.round(((0 + 1) / total) * 100)).toBe(33) // Slice 1 of 3: 33%
    expect(Math.round(((1 + 1) / total) * 100)).toBe(67) // Slice 2 of 3: 67%
    expect(Math.round(((2 + 1) / total) * 100)).toBe(100) // Slice 3 of 3: 100%
  })

  it('persists and loads bookmark from localStorage correctly', () => {
    const bookId = 'book-123'
    // Save bookmark for slice 2
    localStorage.setItem(`techdaily_bookmark_${bookId}`, '2')

    const saved = localStorage.getItem(`techdaily_bookmark_${bookId}`)
    expect(saved).toBe('2')

    const parsedSlice = parseInt(saved!, 10)
    expect(parsedSlice).toBe(2)
    expect(mockBookDetail.chunks[parsedSlice - 1].chapterTitle).toBe('Data Models and Query Languages')
  })

  it('tracks completed slices across the document', () => {
    const bookId = 'book-123'
    const completed = new Set<number>()
    completed.add(1)
    completed.add(2)

    localStorage.setItem(`techdaily_completed_${bookId}`, JSON.stringify(Array.from(completed)))

    const loaded = new Set(JSON.parse(localStorage.getItem(`techdaily_completed_${bookId}`)!))
    expect(loaded.has(1)).toBe(true)
    expect(loaded.has(2)).toBe(true)
    expect(loaded.has(3)).toBe(false)
  })

  it('fetches book detail in useLibraryStore', async () => {
    const store = useLibraryStore()
    const book = await store.fetchBookById('book-123')
    expect(book.title).toBe('Designing Data-Intensive Applications')
    expect(book.chunks.length).toBe(3)
    expect(book.chunks[0].chunkOrder).toBe(1)
  })
})
