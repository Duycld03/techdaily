import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import { Check } from 'lucide-vue-next'
import NotesPage from '~/pages/notes.vue'
import { useNotesStore } from '~/stores/useNotesStore'

const defaultHighlights = [
  {
    id: 'h-1',
    documentChunkId: 'c-1',
    chapterTitle: 'Chapter 1: Reliability',
    bookTitle: 'DDIA',
    selectedText: 'Replication lag can cause stale reads under async replication.',
    note: 'Important for read-after-write consistency',
    tags: ['distributed', 'replication'],
    createdAt: '2026-08-31T10:00:00Z',
    hasFlashcard: false
  }
]

let currentHighlights = [...defaultHighlights]

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async () => ({ highlights: [...currentHighlights] })),
    post: vi.fn(async () => ({ cardId: 'card-1' })),
    put: vi.fn(async (_url: string, body: { note?: string; tags?: string[] }) => ({
      highlight: {
        ...currentHighlights[0],
        note: body.note,
        tags: body.tags || []
      }
    })),
    delete: vi.fn(async () => ({ success: true }))
  })
}))

interface NotesComponentInstance {
  tagCounts: Array<{ tag: string; count: number }>
  selectedTag: string | null
  selectTag: (tag: string | null) => void
  filteredHighlights: Array<{ id: string }>
}

describe('notes.vue (Dedicated Reading Highlights Hub)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    currentHighlights = [...defaultHighlights]
  })

  it('renders dedicated highlights hub without saved insights tab', async () => {
    const wrapper = mount(NotesPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    await flushPromises()

    // Title should be rendered
    expect(wrapper.text()).toContain('notes.title')
    // No activeTab toggle for saved insights
    expect(wrapper.text()).not.toContain('notes.tab_saved_insights')
    // Highlight content rendered
    expect(wrapper.text()).toContain('Replication lag can cause stale reads')
    expect(wrapper.text()).toContain('DDIA')
  })

  it('toggles inline editing mode on highlight card and saves updates', async () => {
    const wrapper = mount(NotesPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    await flushPromises()

    // Find the Edit Note button
    const editBtn = wrapper.find('button[title="notes.edit_note"]')
    expect(editBtn.exists()).toBe(true)
    await editBtn.trigger('click')

    // Textarea should appear
    const textarea = wrapper.find('textarea')
    expect(textarea.exists()).toBe(true)
    expect((textarea.element as HTMLTextAreaElement).value).toBe('Important for read-after-write consistency')

    // Change note
    await textarea.setValue('Updated reflection note for async replication')

    // Save
    const saveBtn = wrapper.findAll('button').find(b => b.text().includes('notes.save_note'))!
    expect(saveBtn).toBeDefined()
    await saveBtn.trigger('click')
    await flushPromises()

    const notesStore = useNotesStore()
    expect(notesStore.highlights[0]?.note).toBe('Updated reflection note for async replication')
  })

  it('renders flashcard button in disabled In SM-2 state with Check icon when highlight has hasFlashcard true', async () => {
    currentHighlights = [
      {
        id: 'h-flashcard',
        documentChunkId: 'c-2',
        chapterTitle: 'Chapter 2: Data Models',
        bookTitle: 'DDIA',
        selectedText: 'Document models have advantages if data has document-like structure.',
        note: 'Important',
        tags: ['nosql'],
        createdAt: '2026-08-31T11:00:00Z',
        hasFlashcard: true
      }
    ]

    const wrapper = mount(NotesPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    await flushPromises()

    const sm2Btn = wrapper.find('button[title="notes.in_sm2"]')
    expect(sm2Btn.exists()).toBe(true)
    expect(sm2Btn.text()).toContain('notes.in_sm2')
    expect(sm2Btn.attributes('disabled')).toBeDefined()
    expect(sm2Btn.findComponent(Check).exists() || sm2Btn.find('svg.text-emerald-500').exists()).toBe(true)
  })

  it('extracts unique tags, counts them, and renders horizontal tag chip bar', async () => {
    currentHighlights = [
      {
        id: 'h-1',
        documentChunkId: 'c-1',
        chapterTitle: 'Chapter 1: Reliability',
        bookTitle: 'DDIA',
        selectedText: 'Replication lag can cause stale reads under async replication.',
        note: 'Important for consistency',
        tags: ['#Distributed', 'replication'],
        createdAt: '2026-08-31T10:00:00Z',
        hasFlashcard: false
      },
      {
        id: 'h-2',
        documentChunkId: 'c-2',
        chapterTitle: 'Chapter 2: Partitioning',
        bookTitle: 'DDIA',
        selectedText: 'Consistent hashing helps minimize reshuffling.',
        note: 'Partitioning rule',
        tags: ['distributed', 'hashing'],
        createdAt: '2026-08-31T11:00:00Z',
        hasFlashcard: false
      }
    ]

    const wrapper = mount(NotesPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })
    await flushPromises()

    const vm = wrapper.vm as unknown as NotesComponentInstance
    const tagCounts = vm.tagCounts
    expect(tagCounts).toEqual([
      { tag: 'distributed', count: 2 },
      { tag: 'hashing', count: 1 },
      { tag: 'replication', count: 1 }
    ])

    const buttons = wrapper.findAll('button')
    const allChip = buttons.find((b) => b.text().includes('notes.tag_all'))
    expect(allChip).toBeDefined()
    expect(allChip?.text()).toContain('(2)')

    const distChip = buttons.find((b) => b.text().includes('#distributed'))
    expect(distChip).toBeDefined()
    expect(distChip?.text()).toContain('(2)')
  })

  it('filters highlights conjunctively by active tag and search query', async () => {
    currentHighlights = [
      {
        id: 'h-1',
        documentChunkId: 'c-1',
        chapterTitle: 'Chapter 1: Reliability',
        bookTitle: 'DDIA',
        selectedText: 'Replication lag can cause stale reads under async replication.',
        note: 'Consistency notes',
        tags: ['distributed', 'replication'],
        createdAt: '2026-08-31T10:00:00Z',
        hasFlashcard: false
      },
      {
        id: 'h-2',
        documentChunkId: 'c-2',
        chapterTitle: 'Chapter 2: Partitioning',
        bookTitle: 'DDIA',
        selectedText: 'Consistent hashing helps minimize reshuffling.',
        note: 'Partitioning notes',
        tags: ['distributed', 'hashing'],
        createdAt: '2026-08-31T11:00:00Z',
        hasFlashcard: false
      }
    ]

    const wrapper = mount(NotesPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })
    await flushPromises()

    // 1. Select tag 'hashing'
    const vm = wrapper.vm as unknown as NotesComponentInstance
    vm.selectTag('hashing')
    await wrapper.vm.$nextTick()
    expect(vm.filteredHighlights.length).toBe(1)
    expect(vm.filteredHighlights[0]?.id).toBe('h-2')

    // 2. Add search query that does not match h-2
    const searchInput = wrapper.find('input[type="text"]')
    await searchInput.setValue('replication')
    await wrapper.vm.$nextTick()
    expect(vm.filteredHighlights.length).toBe(0)

    // 3. Search query that matches h-2
    await searchInput.setValue('reshuffling')
    await wrapper.vm.$nextTick()
    expect(vm.filteredHighlights.length).toBe(1)
    expect(vm.filteredHighlights[0]?.id).toBe('h-2')

    // 4. Toggle/clear tag by clicking null
    vm.selectTag(null)
    await wrapper.vm.$nextTick()
    expect(vm.selectedTag).toBeNull()
  })

  it('wires tag badge on highlight card to selectTag', async () => {
    currentHighlights = [
      {
        id: 'h-1',
        documentChunkId: 'c-1',
        chapterTitle: 'Chapter 1: Reliability',
        bookTitle: 'DDIA',
        selectedText: 'Replication lag can cause stale reads.',
        note: 'Consistency notes',
        tags: ['distributed'],
        createdAt: '2026-08-31T10:00:00Z',
        hasFlashcard: false
      }
    ]

    const wrapper = mount(NotesPage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })
    await flushPromises()

    const tagBadge = wrapper.findAll('button').find((b) => b.text().trim() === '#distributed')
    expect(tagBadge).toBeDefined()
    await tagBadge?.trigger('click')
    const vm = wrapper.vm as unknown as NotesComponentInstance
    expect(vm.selectedTag).toBe('distributed')

    // Re-clicking toggles it off
    await tagBadge?.trigger('click')
    expect(vm.selectedTag).toBeNull()
  })
})
