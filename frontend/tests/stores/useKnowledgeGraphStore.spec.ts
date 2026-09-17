import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import {
  useKnowledgeGraphStore,
  type KnowledgeGraphResponse
} from '~/stores/useKnowledgeGraphStore'

const mockGraphData: KnowledgeGraphResponse = {
  nodes: [
    {
      id: 'topic_1',
      label: 'PostgreSQL MVCC & Vacuum',
      type: 'topic',
      category: 'DatabaseStorage',
      subtitle: 'Day 3',
      dayOrder: 3,
      summary: 'Multi-version concurrency control mechanics and autovacuum tuning',
      difficulty: 'Advanced',
      tags: ['database', 'postgres', 'concurrency']
    },
    {
      id: 'topic_2',
      label: '.NET CLR Generational GC',
      type: 'topic',
      category: 'BackendDotNet',
      subtitle: 'Day 5',
      dayOrder: 5,
      summary: 'Ephemeral segments and Large Object Heap allocation internals',
      difficulty: 'Expert',
      tags: ['dotnet', 'memory', 'garbage-collection']
    },
    {
      id: 'book_1',
      label: 'Designing Data-Intensive Applications',
      type: 'book',
      category: 'DatabaseStorage',
      summary: 'Reliable, scalable, and maintainable systems',
      tags: ['storage', 'replication']
    },
    {
      id: 'card_1',
      label: 'What causes PostgreSQL table bloat?',
      type: 'card',
      category: 'DatabaseStorage',
      status: 'Mastered',
      intervalDays: 25,
      easeFactor: 2.6,
      repetitionCount: 4,
      tags: ['postgres', 'vacuum']
    },
    {
      id: 'card_2',
      label: 'Explain .NET GC Gen 2 promotion threshold',
      type: 'card',
      category: 'BackendDotNet',
      status: 'Learning',
      intervalDays: 2,
      easeFactor: 2.3,
      repetitionCount: 1,
      tags: ['dotnet', 'gc']
    },
    {
      id: 'highlight_1',
      label: 'MVCC creates a new tuple version on every update statement',
      type: 'highlight',
      category: 'DatabaseStorage',
      summary: 'Highlights from DDIA chapter 7',
      tags: ['mvcc', 'concurrency']
    }
  ],
  edges: [
    {
      id: 'edge_1',
      source: 'topic_1',
      target: 'book_1',
      relationType: 'BookToTopic',
      label: 'References'
    },
    {
      id: 'edge_2',
      source: 'card_1',
      target: 'topic_1',
      relationType: 'CardToTopic',
      label: 'Retention'
    },
    {
      id: 'edge_3',
      source: 'card_2',
      target: 'topic_2',
      relationType: 'CardToTopic',
      label: 'Retention'
    },
    {
      id: 'edge_4',
      source: 'highlight_1',
      target: 'book_1',
      relationType: 'HighlightToBook',
      label: 'Excerpt'
    }
  ],
  stats: {
    totalNodes: 6,
    totalEdges: 4,
    nodeTypeCounts: {
      topic: 2,
      book: 1,
      card: 2,
      highlight: 1
    },
    pillarCounts: {
      DatabaseStorage: 4,
      BackendDotNet: 2
    },
    masteredCardsCount: 1
  }
}

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/api/v1/graph')) return mockGraphData
      throw new Error('Not found')
    })
  })
}))

describe('useKnowledgeGraphStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('fetches graph data and populates rawData and stats', async () => {
    const store = useKnowledgeGraphStore()
    expect(store.rawData).toBeNull()
    expect(store.isLoading).toBe(false)

    await store.fetchGraph()

    expect(store.rawData).not.toBeNull()
    expect(store.filteredNodes).toHaveLength(6)
    expect(store.filteredEdges).toHaveLength(4)
    expect(store.stats?.totalNodes).toBe(6)
    expect(store.stats?.masteredCardsCount).toBe(1)
    expect(store.activePillars).toEqual(
      expect.arrayContaining(['DatabaseStorage', 'BackendDotNet'])
    )
  })

  it('filters nodes by category pillar', async () => {
    const store = useKnowledgeGraphStore()
    await store.fetchGraph()

    store.setCategory('BackendDotNet')
    expect(store.filteredNodes).toHaveLength(2)
    expect(store.filteredNodes.map((n) => n.id)).toEqual(['topic_2', 'card_2'])

    // Filtered edges should only keep edges between visible nodes
    expect(store.filteredEdges).toHaveLength(1)
    expect(store.filteredEdges[0]?.id).toBe('edge_3')
  })

  it('filters nodes by node type', async () => {
    const store = useKnowledgeGraphStore()
    await store.fetchGraph()

    store.setNodeType('card')
    expect(store.filteredNodes).toHaveLength(2)
    expect(store.filteredNodes.every((n) => n.type === 'card')).toBe(true)

    store.setNodeType('topic')
    expect(store.filteredNodes).toHaveLength(2)
    expect(store.filteredNodes.every((n) => n.type === 'topic')).toBe(true)
  })

  it('filters card nodes by flashcard mastery status', async () => {
    const store = useKnowledgeGraphStore()
    await store.fetchGraph()

    store.setMastery('Mastered')
    // Non-card nodes are preserved, but learning card_2 is excluded
    expect(store.filteredNodes.find((n) => n.id === 'card_1')).toBeDefined()
    expect(store.filteredNodes.find((n) => n.id === 'card_2')).toBeUndefined()
    expect(store.filteredNodes).toHaveLength(5)

    // Combining nodeType='card' with mastery='Mastered'
    store.setNodeType('card')
    expect(store.filteredNodes).toHaveLength(1)
    expect(store.filteredNodes[0]?.id).toBe('card_1')
  })

  it('filters nodes by search query matching label, summary, subtitle, or tags', async () => {
    const store = useKnowledgeGraphStore()
    await store.fetchGraph()

    // Matching label
    store.setSearchQuery('PostgreSQL')
    expect(store.filteredNodes.map((n) => n.id)).toEqual(
      expect.arrayContaining(['topic_1', 'card_1'])
    )

    // Matching summary
    store.setSearchQuery('Large Object Heap')
    expect(store.filteredNodes).toHaveLength(1)
    expect(store.filteredNodes[0]?.id).toBe('topic_2')

    // Matching subtitle
    store.setSearchQuery('Day 3')
    expect(store.filteredNodes).toHaveLength(1)
    expect(store.filteredNodes[0]?.id).toBe('topic_1')

    // Matching tag
    store.setSearchQuery('replication')
    expect(store.filteredNodes).toHaveLength(1)
    expect(store.filteredNodes[0]?.id).toBe('book_1')
  })

  it('selects node and retrieves selectedNode getter', async () => {
    const store = useKnowledgeGraphStore()
    await store.fetchGraph()

    expect(store.selectedNode).toBeNull()

    store.selectNode('topic_1')
    expect(store.selectedNode?.label).toBe('PostgreSQL MVCC & Vacuum')

    store.selectNode(null)
    expect(store.selectedNode).toBeNull()
  })

  it('resets all filters restoring full visibility', async () => {
    const store = useKnowledgeGraphStore()
    await store.fetchGraph()

    store.setCategory('DatabaseStorage')
    store.setNodeType('card')
    store.setMastery('Mastered')
    store.setSearchQuery('bloat')
    store.selectNode('card_1')

    expect(store.filteredNodes).toHaveLength(1)
    expect(store.selectedNodeId).toBe('card_1')

    store.resetFilters()

    expect(store.selectedCategory).toBe('all')
    expect(store.selectedNodeType).toBe('all')
    expect(store.selectedMastery).toBe('all')
    expect(store.searchQuery).toBe('')
    expect(store.selectedNodeId).toBeNull()
    expect(store.filteredNodes).toHaveLength(6)
    expect(store.filteredEdges).toHaveLength(4)
  })
})
