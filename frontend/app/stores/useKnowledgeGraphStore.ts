import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import { useApiClient } from '~/composables/useApiClient'
import { browserStorage, readStored } from '~/utils/clientStorage'
import { useCategoryStore, type CategoryDescriptor } from '~/stores/useCategoryStore'
import { useAuthStore } from '~/stores/useAuthStore'

export interface GraphNode {
  id: string
  label: string
  type: string
  categoryId: string | null
  category: CategoryDescriptor | null
  subtitle?: string | null
  summary?: string | null
  difficulty?: string | null
  status?: string | null
  intervalDays?: number | null
  easeFactor?: number | null
  repetitionCount?: number | null
  documentChunkId?: string | null
  bookId?: string | null
  tags?: string[] | null
  createdAt?: string | null
  embleUrl?: string | null
  emblemUrl?: string | null
}

export interface GraphEdge {
  id: string
  source: string
  target: string
  relationType: string
  label?: string | null
  weight?: number | null
}

export interface GraphStats {
  totalNodes: number
  totalEdges: number
  nodeTypeCounts: Record<string, number>
  pillarCounts: Record<string, number>
  masteredCardsCount: number
}

export interface KnowledgeGraphResponse {
  nodes: GraphNode[]
  edges: GraphEdge[]
  stats: GraphStats
}

export const useKnowledgeGraphStore = defineStore('knowledgeGraph', () => {
  const categoryStore = useCategoryStore()
  const auth = useAuthStore()
  let generation = 0
  const rawData = ref<KnowledgeGraphResponse | null>(null)
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)
  const selectedNodeId = ref<string | null>(null)
  const VIEW_MODE_KEY = 'deeppace_graph_view_mode'
  const initialViewMode = typeof window !== 'undefined'
    ? ((readStored(browserStorage('localStorage'), VIEW_MODE_KEY) as '2d' | '3d') || '2d')
    : '2d'
  const viewMode = ref<'2d' | '3d'>(initialViewMode === '3d' ? '3d' : '2d')
  const searchQuery = ref<string>('')
  const selectedCategoryId = ref<string>('all')
  const selectedNodeType = ref<string>('all')
  const selectedMastery = ref<string>('all')
  const hoveredLegendType = ref<string | null>(null)

  watch(
    [() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration],
    () => {
      generation++
      rawData.value = null
      isLoading.value = false
      error.value = null
      resetFilters()
      hoveredLegendType.value = null
    },
    { flush: 'sync' }
  )

  const filteredNodes = computed<GraphNode[]>(() => {
    const nodes = rawData.value?.nodes ?? []
    const query = searchQuery.value.trim().toLowerCase()
    const content = nodes.filter((node) => {
      const type = node.type.toLowerCase()
      if (type === 'pillar') return false
      if (selectedCategoryId.value !== 'all' && node.categoryId !== selectedCategoryId.value) return false
      if (selectedNodeType.value !== 'all' && type !== selectedNodeType.value.toLowerCase()) return false
      if (selectedMastery.value !== 'all' && type === 'card' && node.status?.toLowerCase() !== selectedMastery.value.toLowerCase()) return false
      if (!query) return true
      return [node.label, node.summary, node.subtitle, ...(node.tags ?? [])]
        .some(value => value?.toLowerCase().includes(query))
    })
    const contentIds = new Set(content.map(node => node.id))
    const representedIds = new Set(content.map(node => node.categoryId).filter(Boolean))
    const connectedHubIds = new Set<string>()
    for (const edge of rawData.value?.edges ?? []) {
      if (contentIds.has(edge.source)) connectedHubIds.add(edge.target)
      if (contentIds.has(edge.target)) connectedHubIds.add(edge.source)
    }
    return nodes.filter(node => contentIds.has(node.id) ||
      (node.type.toLowerCase() === 'pillar' && connectedHubIds.has(node.id) && !!node.categoryId && representedIds.has(node.categoryId) &&
        (selectedCategoryId.value === 'all' || node.categoryId === selectedCategoryId.value)))
  })

  const filteredEdges = computed<GraphEdge[]>(() => {
    if (!rawData.value?.edges) return []

    const visibleNodeIds = new Set(filteredNodes.value.map((n) => n.id))
    return rawData.value.edges.filter(
      (edge) => visibleNodeIds.has(edge.source) && visibleNodeIds.has(edge.target)
    )
  })

  // True when the API returned at least one raw node (before client-side filters).
  // Distinguishes "user has learned nothing yet" from "filters hid every node".
  const hasAnyNodes = computed<boolean>(() => rawData.value?.nodes.some(node => node.type.toLowerCase() !== 'pillar') ?? false)

  const selectedNode = computed<GraphNode | null>(() => {
    if (!rawData.value?.nodes || !selectedNodeId.value) return null
    return rawData.value.nodes.find((n) => n.id === selectedNodeId.value) ?? null
  })

  const stats = computed<GraphStats | null>(() => {
    return rawData.value?.stats ?? null
  })

  const nodeStats = computed(() => stats.value)

  const activePillars = computed<CategoryDescriptor[]>(() => {
    const categories = new Map<string, CategoryDescriptor>()
    for (const node of rawData.value?.nodes ?? []) {
      if (node.type.toLowerCase() !== 'pillar' && node.categoryId && node.category) {
        categories.set(node.categoryId, categoryStore.getCategoryById(node.categoryId) ?? node.category)
      }
    }
    return [...categories.values()].sort((a, b) => a.sortOrder - b.sortOrder || a.id.localeCompare(b.id))
  })

  const filters = computed(() => ({
    categoryId: selectedCategoryId.value,
    nodeType: selectedNodeType.value,
    mastery: selectedMastery.value,
    searchQuery: searchQuery.value
  }))

  function getNodeLabel(node: GraphNode, locale?: string): string {
    if (node.type.toLowerCase() !== 'pillar') return node.label
    const category = categoryStore.getCategoryById(node.categoryId) ?? node.category
    return categoryStore.getCategoryName(category, locale)
  }

  async function fetchGraph(force = false) {
    auth.init()
    if (rawData.value && !force) return
    const currentGeneration = generation
    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const response = await api.get<KnowledgeGraphResponse>('/api/v1/graph')
      if (currentGeneration !== generation) return
      rawData.value = response
    } catch (err: unknown) {
      if (currentGeneration !== generation) return
      if (err && typeof err === 'object' && 'data' in err) {
        const apiErr = err as { data?: { detail?: string } }
        error.value = apiErr.data?.detail || 'Failed to load knowledge graph.'
      } else if (err instanceof Error) {
        error.value = err.message
      } else {
        error.value = 'Failed to load knowledge graph.'
      }
    } finally {
      if (currentGeneration === generation) isLoading.value = false
    }
  }

  function selectNode(nodeId: string | null) {
    selectedNodeId.value = nodeId
  }

  function setCategoryId(categoryId: string) {
    selectedCategoryId.value = categoryId
  }

  function setNodeType(type: string) {
    selectedNodeType.value = type
  }

  function setMastery(status: string) {
    selectedMastery.value = status
  }

  function setSearchQuery(query: string) {
    searchQuery.value = query
  }
  function setViewMode(mode: '2d' | '3d') {
    viewMode.value = mode
    if (typeof window !== 'undefined') {
      try {
        localStorage.setItem(VIEW_MODE_KEY, mode)
      } catch {
        // ignore localStorage errors
      }
    }
  }

  function resetFilters() {
    selectedCategoryId.value = 'all'
    selectedNodeType.value = 'all'
    selectedMastery.value = 'all'
    searchQuery.value = ''
    selectedNodeId.value = null
  }

  function setHoveredLegendType(type: string | null) {
    hoveredLegendType.value = type
  }

  return {
    rawData,
    isLoading,
    error,
    selectedNodeId,
    searchQuery,
    selectedCategoryId,
    selectedNodeType,
    selectedMastery,
    viewMode,
    filteredNodes,
    filteredEdges,
    hasAnyNodes,
    selectedNode,
    stats,
    nodeStats,
    activePillars,
    filters,
    fetchGraph,
    getNodeLabel,
    selectNode,
    setCategoryId,
    setNodeType,
    setMastery,
    setSearchQuery,
    setViewMode,
    resetFilters,
    hoveredLegendType,
    setHoveredLegendType,
  }
})
