import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useNotesStore } from '~/stores/useNotesStore'

const mockHighlights = [
  {
    id: 'h-1',
    documentChunkId: 'c-1',
    chapterTitle: 'Reliability',
    bookTitle: 'Designing Data-Intensive Applications',
    selectedText: 'Replication lag can cause stale reads under async replication.',
    note: 'Important for read-after-write consistency',
    tags: ['distributed', 'replication'],
    createdAt: '2026-08-31T10:00:00Z'
  },
  {
    id: 'h-2',
    documentChunkId: 'c-2',
    chapterTitle: 'Reactivity',
    bookTitle: 'Vue 3 Core Architecture',
    selectedText: 'shallowRef avoids recursive proxy wrapping.',
    tags: ['vue', 'performance'],
    createdAt: '2026-08-31T11:00:00Z'
  }
]

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/highlights')) {
        const urlObj = new URL('http://localhost' + url)
        const page = parseInt(urlObj.searchParams.get('page') || '1', 10)
        const pageSize = parseInt(urlObj.searchParams.get('pageSize') || '15', 10)
        return {
          highlights: [...mockHighlights],
          totalCount: 30,
          page,
          pageSize,
          totalPages: 2,
          tagCounts: [
            { tag: 'distributed', count: 12 },
            { tag: 'vue', count: 8 }
          ]
        }
      }
      throw new Error('Not found')
    }),
    post: vi.fn(async (url: string, body: { documentChunkId: string; selectedText: string; note?: string; tags?: string[] }) => {
      if (url.includes('/highlights')) {
        const id = body.selectedText.includes('Replication lag') ? 'h-1' : 'h-3'
        return {
          highlight: {
            id,
            documentChunkId: body.documentChunkId,
            bookTitle: 'Clean Code',
            chapterTitle: 'Functions',
            selectedText: body.selectedText,
            note: body.note,
            tags: body.tags || [],
            createdAt: '2026-08-31T12:00:00Z'
          }
        }
      }
      throw new Error('Not found')
    }),
    put: vi.fn(async (url: string, body: { note?: string; tags?: string[] }) => {
      if (url.includes('/highlights/')) {
        const id = url.split('/highlights/')[1]
        const existing = mockHighlights.find((h) => h.id === id) || mockHighlights[0]
        return {
          highlight: {
            ...existing,
            note: body.note,
            tags: body.tags || existing.tags
          }
        }
      }
      throw new Error('Not found')
    }),
    delete: vi.fn(async (url: string) => {
      return { success: true }
    })
  })
}))

describe('useNotesStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('fetches saved highlights correctly', async () => {
    const notes = useNotesStore()
    expect(notes.highlights).toHaveLength(0)

    await notes.fetchHighlights()
    expect(notes.highlights).toHaveLength(2)
    expect(notes.highlights[0].selectedText).toContain('Replication lag')
  })

  it('manages pagination and global tagCounts correctly', async () => {
    const notes = useNotesStore()
    expect(notes.currentPage).toBe(1)
    expect(notes.pageSize).toBe(15)

    await notes.fetchHighlights({ page: 2, pageSize: 15, tag: 'vue' })
    expect(notes.currentPage).toBe(2)
    expect(notes.pageSize).toBe(15)
    expect(notes.totalCount).toBe(30)
    expect(notes.totalPages).toBe(2)
    expect(notes.tagCounts).toHaveLength(2)
    expect(notes.tagCounts[0].tag).toBe('distributed')
  })

  it('appends and deduplicates highlights when append is true', async () => {
    const notes = useNotesStore()
    await notes.fetchHighlights({ page: 1 })
    expect(notes.highlights).toHaveLength(2)

    await notes.fetchHighlights({ page: 2, append: true })
    expect(notes.highlights).toHaveLength(2)
  })

  it('creates a new highlight note', async () => {
    const notes = useNotesStore()
    await notes.fetchHighlights()

    await notes.createHighlight({
      documentChunkId: 'c-3',
      selectedText: 'Functions should do one thing and do it well.',
      tags: ['clean-code']
    })

    expect(notes.highlights).toHaveLength(3)
    expect(notes.highlights[0].selectedText).toContain('Functions should do one thing')
  })

  it('creates a new highlight with an attached personal reflection note and tags', async () => {
    const notes = useNotesStore()
    await notes.fetchHighlights()

    const result = await notes.createHighlight({
      documentChunkId: 'c-4',
      selectedText: 'LSM-Trees append writes sequentially to WAL.',
      note: 'Sequential writes turn random I/O into deterministic streaming.',
      tags: ['storage', 'lsm-tree']
    })

    expect(result.note).toBe('Sequential writes turn random I/O into deterministic streaming.')
    expect(result.tags).toEqual(['storage', 'lsm-tree'])
    expect(notes.highlights[0].note).toBe('Sequential writes turn random I/O into deterministic streaming.')
  })

  it('deletes a highlight note', async () => {
    const notes = useNotesStore()
    await notes.fetchHighlights()
    expect(notes.highlights).toHaveLength(2)

    await notes.deleteHighlight('h-1')
    expect(notes.highlights).toHaveLength(1)
    expect(notes.highlights[0].id).toBe('h-2')
  })

  it('updates existing highlight in place when backend returns existing id', async () => {
    const notes = useNotesStore()
    await notes.fetchHighlights()
    expect(notes.highlights).toHaveLength(2)

    const updated = await notes.createHighlight({
      documentChunkId: 'c-1',
      selectedText: 'Replication lag can cause stale reads under async replication.',
      note: 'Updated note for h-1'
    })

    expect(updated.id).toBe('h-1')
    expect(notes.highlights).toHaveLength(2)
    expect(notes.highlights[0].id).toBe('h-1')
    expect(notes.highlights[0].note).toBe('Updated note for h-1')
  })

  it('updates a highlight via updateHighlight action', async () => {
    const notes = useNotesStore()
    await notes.fetchHighlights()
    expect(notes.highlights).toHaveLength(2)

    const result = await notes.updateHighlight('h-1', {
      note: 'Updated reflection note',
      tags: ['consistency', 'raft']
    })

    expect(result.id).toBe('h-1')
    expect(result.note).toBe('Updated reflection note')
    expect(result.tags).toEqual(['consistency', 'raft'])
    expect(notes.highlights[0].note).toBe('Updated reflection note')
    expect(notes.highlights[0].tags).toEqual(['consistency', 'raft'])
  })
})
