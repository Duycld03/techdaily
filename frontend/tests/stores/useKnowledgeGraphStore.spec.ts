import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useKnowledgeGraphStore, type GraphNode, type KnowledgeGraphResponse } from '~/stores/useKnowledgeGraphStore'
import { useCategoryStore } from '~/stores/useCategoryStore'
import { graphCategory, otherGraphCategory } from '../fixtures/graphCategories'
import { useAuthStore } from '~/stores/useAuthStore'
import { adminTestJwt, clearAuthStorage, deferred } from '../helpers/admin'

const node = (id: string, type: string, overrides: Partial<GraphNode> = {}): GraphNode => ({
  id, type, label: id, categoryId: graphCategory.id, category: graphCategory, ...overrides
})
const mockGraphData: KnowledgeGraphResponse = {
  nodes: [
    node('pillar-cloud', 'pillar'),
    node('pillar-data', 'pillar', { categoryId: otherGraphCategory.id, category: otherGraphCategory }),
    node('chunk-cloud', 'chunk', { label: 'Cloud execution', subtitle: 'Day 3', summary: 'Ephemeral segments', tags: ['concurrency'] }),
    node('book-cloud', 'book', { label: 'Cloud book', tags: ['replication'] }),
    node('card-cloud', 'card', { label: 'Cloud retention', status: 'Mastered' }),
    node('card-data', 'card', { status: 'Learning', categoryId: otherGraphCategory.id, category: otherGraphCategory }),
    node('highlight-cloud', 'highlight'),
    node('unclassified', 'card', { categoryId: null, category: null, status: 'Learning' })
  ],
  edges: [
    { id: 'book-hub', source: 'book-cloud', target: 'pillar-cloud', relationType: 'BookToPillar' },
    { id: 'chunk-book', source: 'chunk-cloud', target: 'book-cloud', relationType: 'ChunkToBook' },
    { id: 'card-chunk', source: 'card-cloud', target: 'chunk-cloud', relationType: 'CardToChunk' },
    { id: 'data-hub', source: 'card-data', target: 'pillar-data', relationType: 'CardToPillar' },
    { id: 'dangling', source: 'missing', target: 'book-cloud', relationType: 'SharedTag' }
  ],
  stats: { totalNodes: 8, totalEdges: 5, nodeTypeCounts: {}, pillarCounts: { [graphCategory.id]: 5, [otherGraphCategory.id]: 1 }, masteredCardsCount: 1 }
}

const { get } = vi.hoisted(() => ({ get: vi.fn() }))
vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({ get })
}))

describe('useKnowledgeGraphStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    get.mockReset().mockResolvedValue(mockGraphData)
  })

  async function signIn(id: string) {
    const auth = useAuthStore()
    await auth.setSession(adminTestJwt(id), {
      id, email: `${id}@example.test`, name: id, preferredLocale: 'en'
    }, false)
    return auth
  }

  it.each(['logout', 'replacement', 'same-account', 'admin-access'] as const)(
    'synchronously clears private graph state on %s while preserving view preference',
    async change => {
      const auth = await signIn('account-a')
      const store = useKnowledgeGraphStore()
      await store.fetchGraph()
      store.selectNode('card-cloud')
      store.setCategoryId(graphCategory.id)
      store.setNodeType('card')
      store.setMastery('mastered')
      store.setSearchQuery('private query')
      store.setHoveredLegendType('card')
      store.setViewMode('3d')
      store.error = 'private failure'
      store.isLoading = true

      if (change === 'same-account') auth.invalidateStoredSession()
      const transition = change === 'logout' ? auth.clearSession()
        : change === 'replacement' ? signIn('account-b')
          : change === 'same-account' ? signIn('account-a')
            : auth.invalidateAdminAccess()

      expect(store.rawData).toBeNull()
      expect(store.filteredNodes).toEqual([])
      expect(store.filteredEdges).toEqual([])
      expect(store.activePillars).toEqual([])
      expect(store.stats).toBeNull()
      expect(store.selectedNode).toBeNull()
      expect(store.selectedNodeId).toBeNull()
      expect(store.filters).toEqual({ categoryId: 'all', nodeType: 'all', mastery: 'all', searchQuery: '' })
      expect(store.hoveredLegendType).toBeNull()
      expect(store.error).toBeNull()
      expect(store.isLoading).toBe(false)
      expect(store.viewMode).toBe('3d')
      expect(localStorage.getItem('deeppace_graph_view_mode')).toBe('3d')
      await transition
    }
  )

  it('fetches the replacement account graph instead of reusing the previous cache', async () => {
    await signIn('account-a')
    const store = useKnowledgeGraphStore()
    await store.fetchGraph()
    await signIn('account-b')
    const graphB = { ...mockGraphData, nodes: [node('account-b-card', 'card', { categoryId: otherGraphCategory.id, category: otherGraphCategory })], edges: [] }
    get.mockResolvedValueOnce(graphB)
    await store.fetchGraph()
    expect(get).toHaveBeenCalledTimes(2)
    expect(store.filteredNodes.map(value => value.id)).toEqual(['account-b-card'])
    expect(store.activePillars).toEqual([otherGraphCategory])
  })

  it.each(['success', 'error', 'unauthorized'] as const)(
    'ignores a delayed old-account %s while the replacement graph request is loading',
    async outcome => {
      await signIn('account-a')
      const store = useKnowledgeGraphStore()
      const old = deferred<KnowledgeGraphResponse>()
      const current = deferred<KnowledgeGraphResponse>()
      get.mockReturnValueOnce(old.promise).mockReturnValueOnce(current.promise)
      const oldRequest = store.fetchGraph()
      const auth = await signIn('account-b')
      const currentRequest = store.fetchGraph()
      if (outcome === 'success') old.resolve(mockGraphData)
      else old.reject(outcome === 'unauthorized' ? { status: 401, data: { detail: 'A expired' } } : new Error('A failed'))
      await oldRequest
      expect(store.rawData).toBeNull()
      expect(store.activePillars).toEqual([])
      expect(store.error).toBeNull()
      expect(store.isLoading).toBe(true)
      expect(auth.user?.id).toBe('account-b')
      const graphB = { ...mockGraphData, nodes: [node('account-b-card', 'card')], edges: [] }
      current.resolve(graphB)
      await currentRequest
      expect(store.rawData).toEqual(graphB)
      expect(store.error).toBeNull()
      expect(store.isLoading).toBe(false)
    }
  )

  it.each(['success', 'error'] as const)('preserves the completed replacement graph on delayed old-account %s', async outcome => {
    await signIn('account-a')
    const store = useKnowledgeGraphStore()
    const old = deferred<KnowledgeGraphResponse>()
    const graphB = { ...mockGraphData, nodes: [node('account-b-card', 'card', { categoryId: otherGraphCategory.id, category: otherGraphCategory })], edges: [] }
    get.mockReturnValueOnce(old.promise).mockResolvedValueOnce(graphB)
    const oldRequest = store.fetchGraph()
    await signIn('account-b')
    await store.fetchGraph()
    store.selectNode('account-b-card')
    store.setSearchQuery('account-b')
    if (outcome === 'success') old.resolve(mockGraphData)
    else old.reject(new Error('Private A failure'))
    await oldRequest
    expect(store.rawData).toEqual(graphB)
    expect(store.filteredNodes.map(value => value.id)).toEqual(['account-b-card'])
    expect(store.activePillars).toEqual([otherGraphCategory])
    expect(store.selectedNodeId).toBe('account-b-card')
    expect(store.searchQuery).toBe('account-b')
    expect(store.error).toBeNull()
    expect(store.isLoading).toBe(false)
  })

  it.each(['success', 'error'] as const)('does not repopulate graph state after logout on delayed %s', async outcome => {
    const auth = await signIn('account-a')
    const store = useKnowledgeGraphStore()
    const old = deferred<KnowledgeGraphResponse>()
    get.mockReturnValueOnce(old.promise)
    const request = store.fetchGraph()
    await auth.clearSession()
    if (outcome === 'success') old.resolve(mockGraphData)
    else old.reject(new Error('Private A failure'))
    await request
    expect(store.rawData).toBeNull()
    expect(store.activePillars).toEqual([])
    expect(store.error).toBeNull()
    expect(store.isLoading).toBe(false)
  })

  it('fetches graph data and preserves UUID stats', async () => {
    const store = useKnowledgeGraphStore()
    expect(store.rawData).toBeNull()
    expect(store.isLoading).toBe(false)
    await store.fetchGraph()
    expect(store.filteredNodes).toHaveLength(8)
    expect(store.filteredEdges).toHaveLength(4)
    expect(store.stats?.pillarCounts[graphCategory.id]).toBe(5)
    expect(store.stats?.masteredCardsCount).toBe(1)
  })
  it('resolves localized hub labels from shared metadata while retaining content titles', () => {
    const store = useKnowledgeGraphStore()
    const categories = useCategoryStore()
    categories.categories = [{ ...graphCategory, nameEn: 'Renamed Cloud', nameVi: 'Đám mây mới' }]
    expect(store.getNodeLabel(node('hub', 'pillar', { label: 'Stale title' }), 'en')).toBe('Renamed Cloud')
    expect(store.getNodeLabel(node('hub', 'pillar'), 'vi')).toBe('Đám mây mới')
    expect(store.getNodeLabel(node('book', 'book', { label: 'My title' }), 'vi')).toBe('My title')
  })

  it('exposes represented descriptors including inactive categories, not fixed subjects or unused hubs', () => {
    const store = useKnowledgeGraphStore()
    store.rawData = { ...mockGraphData, nodes: [...mockGraphData.nodes, node('unused', 'pillar', { categoryId: 'unused-id', category: { ...graphCategory, id: 'unused-id' } })] }
    expect(store.activePillars).toEqual([graphCategory, otherGraphCategory])
    expect(store.filteredNodes.some(n => n.id === 'unused')).toBe(false)
  })

  it('reports no knowledge for an empty response or unused hubs alone', () => {
    const store = useKnowledgeGraphStore()
    expect(store.hasAnyNodes).toBe(false)
    store.rawData = { ...mockGraphData, nodes: [node('only-hub', 'pillar')], edges: [] }
    expect(store.hasAnyNodes).toBe(false)
    expect(store.filteredNodes).toEqual([])
  })

  it('filters exclusively by categoryId without classifying null provenance', () => {
    const store = useKnowledgeGraphStore()
    store.rawData = mockGraphData
    store.setCategoryId(graphCategory.id)
    expect(store.filteredNodes.map(n => n.id)).toEqual(['pillar-cloud', 'chunk-cloud', 'book-cloud', 'card-cloud', 'highlight-cloud'])
    expect(store.filteredEdges.map(e => e.id)).toEqual(['book-hub', 'chunk-book', 'card-chunk'])
    expect(store.filteredNodes.some(n => n.id === 'unclassified')).toBe(false)
    store.setCategoryId(otherGraphCategory.id)
    expect(store.filteredNodes.map(n => n.id)).toEqual(['pillar-data', 'card-data'])
  })

  it('removes unused hubs when filters hide every content edge to that hub', () => {
    const store = useKnowledgeGraphStore()
    store.rawData = mockGraphData
    store.setNodeType('card')
    store.setMastery('mastered')
    expect(store.filteredNodes.map(n => n.id)).toEqual(['card-cloud'])
    expect(store.filteredEdges).toEqual([])
    store.setSearchQuery('no match')
    expect(store.filteredNodes).toEqual([])
    expect(store.filteredEdges).toEqual([])
    expect(store.hasAnyNodes).toBe(true)
  })

  it('retains unclassified content without creating a fallback hub', () => {
    const store = useKnowledgeGraphStore()
    store.rawData = { ...mockGraphData, nodes: [node('unclassified', 'card', { categoryId: null, category: null })], edges: [] }
    expect(store.filteredNodes.map(n => n.id)).toEqual(['unclassified'])
    expect(store.activePillars).toEqual([])
  })

  it('filters card mastery without hiding non-card content', () => {
    const store = useKnowledgeGraphStore()
    store.rawData = mockGraphData
    store.setMastery('Mastered')
    expect(store.filteredNodes.some(n => n.id === 'book-cloud')).toBe(true)
    expect(store.filteredNodes.some(n => n.id === 'card-cloud')).toBe(true)
    expect(store.filteredNodes.some(n => n.id === 'card-data')).toBe(false)
    expect(store.filteredNodes.some(n => n.id === 'pillar-data')).toBe(false)
  })

  it.each(['Cloud execution', 'Ephemeral segments', 'Day 3', 'concurrency'])('searches labels, summaries, subtitles and tags: %s', (query) => {
    const store = useKnowledgeGraphStore()
    store.rawData = mockGraphData
    store.setSearchQuery(query)
    expect(store.filteredNodes.map(n => n.id)).toEqual(['chunk-cloud'])
  })

  it('selects nodes and resets all filters', () => {
    const store = useKnowledgeGraphStore()
    store.rawData = mockGraphData
    store.selectNode('card-cloud')
    expect(store.selectedNode?.label).toBe('Cloud retention')
    store.setCategoryId(graphCategory.id)
    store.setNodeType('card')
    store.setMastery('mastered')
    store.setSearchQuery('retention')
    expect(store.filters.categoryId).toBe(graphCategory.id)
    store.resetFilters()
    expect(store.selectedCategoryId).toBe('all')
    expect(store.selectedNodeType).toBe('all')
    expect(store.selectedMastery).toBe('all')
    expect(store.searchQuery).toBe('')
    expect(store.selectedNode).toBeNull()
    expect(store.filteredNodes).toHaveLength(8)
  })
})
