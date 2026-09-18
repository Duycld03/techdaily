import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

export interface GraphNode {
  id: string
  label: string
  type: string
  category: string
  subtitle?: string | null
  dayOrder?: number | null
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
  const rawData = ref<KnowledgeGraphResponse | null>(null)
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)
  const selectedNodeId = ref<string | null>(null)

  const searchQuery = ref<string>('')
  const selectedCategory = ref<string>('all')
  const selectedNodeType = ref<string>('all')
  const selectedMastery = ref<string>('all')

  const filteredNodes = computed<GraphNode[]>(() => {
    if (!rawData.value?.nodes) return []

    return rawData.value.nodes.filter((node) => {
      const nodeType = node.type?.toLowerCase()

      // Category pillar filter
      if (selectedCategory.value && selectedCategory.value.toLowerCase() !== 'all') {
        const selCat = selectedCategory.value.toLowerCase()
        const isBackend = selCat === 'backendruntime' || selCat === 'backenddotnet' || selCat === 'dotnet'
        const nodeCat = node.category?.toLowerCase() || ''
        const isNodeBackend = nodeCat === 'backendruntime' || nodeCat === 'backenddotnet' || nodeCat === 'dotnet'

        const isMatchingPillar =
          nodeType === 'pillar' &&
          (nodeCat === selCat ||
           node.id?.toLowerCase() === `pillar-${selCat}` ||
           (isBackend && (node.id?.toLowerCase() === 'pillar-backendruntime' || node.id?.toLowerCase() === 'pillar-backenddotnet')))

        if (isBackend) {
          if (!isMatchingPillar && !isNodeBackend) {
            return false
          }
        } else {
          if (!isMatchingPillar && nodeCat !== selCat) {
            return false
          }
        }
      }

      // Node type filter: if 'topic' is selected, include 'pillar' nodes as the hubs
      if (selectedNodeType.value && selectedNodeType.value.toLowerCase() !== 'all') {
        const selType = selectedNodeType.value.toLowerCase()
        if (selType === 'topic') {
          if (nodeType !== 'topic' && nodeType !== 'pillar') {
            return false
          }
        } else if (nodeType !== selType) {
          return false
        }
      }

      // Mastery filter (applies to card nodes)
      if (selectedMastery.value && selectedMastery.value.toLowerCase() !== 'all') {
        if (node.type?.toLowerCase() === 'card') {
          if (node.status?.toLowerCase() !== selectedMastery.value.toLowerCase()) {
            return false
          }
        }
      }

      // Search query filter (matches label, summary, tags, or subtitle)
      if (searchQuery.value && searchQuery.value.trim() !== '') {
        const query = searchQuery.value.trim().toLowerCase()
        const matchesLabel = node.label?.toLowerCase().includes(query) ?? false
        const matchesSummary = node.summary?.toLowerCase().includes(query) ?? false
        const matchesSubtitle = node.subtitle?.toLowerCase().includes(query) ?? false
        const matchesTags = node.tags?.some((tag) => tag.toLowerCase().includes(query)) ?? false

        if (!matchesLabel && !matchesSummary && !matchesSubtitle && !matchesTags) {
          return false
        }
      }

      return true
    })
  })

  const filteredEdges = computed<GraphEdge[]>(() => {
    if (!rawData.value?.edges) return []

    const visibleNodeIds = new Set(filteredNodes.value.map((n) => n.id))
    return rawData.value.edges.filter(
      (edge) => visibleNodeIds.has(edge.source) && visibleNodeIds.has(edge.target)
    )
  })

  const selectedNode = computed<GraphNode | null>(() => {
    if (!rawData.value?.nodes || !selectedNodeId.value) return null
    return rawData.value.nodes.find((n) => n.id === selectedNodeId.value) ?? null
  })

  const stats = computed<GraphStats | null>(() => {
    return rawData.value?.stats ?? null
  })

  const nodeStats = computed(() => stats.value)

  const activePillars = computed<string[]>(() => {
    if (!rawData.value?.nodes) return []
    const pillars = new Set<string>()
    for (const node of rawData.value.nodes) {
      if (node.category) {
        pillars.add(node.category)
      }
    }
    return Array.from(pillars)
  })

  const filters = computed(() => ({
    category: selectedCategory.value,
    nodeType: selectedNodeType.value,
    mastery: selectedMastery.value,
    searchQuery: searchQuery.value
  }))

  async function fetchGraph(force = false) {
    if (rawData.value && !force) return
    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      rawData.value = await api.get<KnowledgeGraphResponse>('/api/v1/graph')
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'data' in err) {
        const apiErr = err as { data?: { error?: string } }
        error.value = apiErr.data?.error || 'Failed to load knowledge graph.'
      } else if (err instanceof Error) {
        error.value = err.message
      } else {
        error.value = 'Failed to load knowledge graph.'
      }
    } finally {
      isLoading.value = false
    }
  }

  function selectNode(nodeId: string | null) {
    selectedNodeId.value = nodeId
  }

  function setCategory(category: string) {
    selectedCategory.value = category
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

  function resetFilters() {
    selectedCategory.value = 'all'
    selectedNodeType.value = 'all'
    selectedMastery.value = 'all'
    searchQuery.value = ''
    selectedNodeId.value = null
  }

  return {
    rawData,
    isLoading,
    error,
    selectedNodeId,
    searchQuery,
    selectedCategory,
    selectedNodeType,
    selectedMastery,
    filteredNodes,
    filteredEdges,
    selectedNode,
    stats,
    nodeStats,
    activePillars,
    filters,
    fetchGraph,
    selectNode,
    setCategory,
    setNodeType,
    setMastery,
    setSearchQuery,
    resetFilters
  }
})
