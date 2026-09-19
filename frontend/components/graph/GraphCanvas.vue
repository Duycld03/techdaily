<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount, computed } from 'vue'
import { useEventListener } from '@vueuse/core'
import cytoscape, { type Core, type EventObject, type Stylesheet, type CoseLayoutOptions } from 'cytoscape'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

const emit = defineEmits<{
  (e: 'cy-ready', cy: Core): void
}>()

const store = useKnowledgeGraphStore()
const colorMode = useColorMode()
const isDark = computed(() => colorMode.value === 'dark')

const containerRef = ref<HTMLDivElement | null>(null)
let cy: Core | null = null

function getStylesheet(dark: boolean): Stylesheet[] {
  const textClr = dark ? '#f8fafc' : '#0f172a'
  const borderClr = dark ? '#27272a' : '#cbd5e1'
  const edgeClr = dark ? '#27272a' : '#cbd5e1'
  const activeClr = '#38bdf8'

  return [
    // Base node style
    {
      selector: 'node',
      style: {
        'label': 'data(label)',
        'color': textClr,
        'font-size': 11,
        'font-family': 'ui-sans-serif, system-ui, -apple-system, sans-serif',
        'font-weight': 500,
        'text-valign': 'bottom',
        'text-margin-y': 6,
        'text-max-width': 100,
        'text-overflow-wrap': 'ellipsis',
        'border-width': 1.5,
        'border-color': borderClr,
        'background-opacity': 1,
        'transition-property': 'background-color, border-color, border-width, opacity',
        'transition-duration': 0.25
      }
    },
    // Pillar nodes (Architectural Hubs)
    {
      selector: 'node[type = "pillar"]',
      style: {
        'shape': 'ellipse',
        'width': 54,
        'height': 54,
        'border-width': 3.5,
        'border-color': activeClr,
        'font-size': 12,
        'font-weight': 'bold',
        'color': textClr,
        'z-index': 10
      }
    },
    // Pillar node colors by category with glowing accent borders
    {
      selector: 'node[type = "pillar"][category = "FrontendWeb"], node[type = "pillar"][category = "Frontend"]',
      style: {
        'background-color': '#f59e0b',
        'border-color': '#fbbf24'
      }
    },
    {
      selector: 'node[type = "pillar"][category = "BackendRuntime"], node[type = "pillar"][category = "BackendDotNet"], node[type = "pillar"][category = "DotNet"]',
      style: {
        'background-color': '#0284c7',
        'border-color': '#38bdf8'
      }
    },
    {
      selector: 'node[type = "pillar"][category = "DatabaseStorage"], node[type = "pillar"][category = "Postgres"]',
      style: {
        'background-color': '#0891b2',
        'border-color': '#22d3ee'
      }
    },
    {
      selector: 'node[type = "pillar"][category = "SystemDesign"], node[type = "pillar"][category = "DistributedSystems"]',
      style: {
        'background-color': '#8b5cf6',
        'border-color': '#a78bfa'
      }
    },
    {
      selector: 'node[type = "pillar"][category = "EngineeringCraft"], node[type = "pillar"][category = "Craft"]',
      style: {
        'background-color': '#ec4899',
        'border-color': '#f472b6'
      }
    },
    // Topic nodes (default fallback)
    {
      selector: 'node[type = "topic"]',
      style: {
        'shape': 'ellipse',
        'width': 36,
        'height': 36,
        'background-color': '#0284c7'
      }
    },
    // Topic colors by pillar: DotNet (#38bdf8), Postgres (#34d399), DistributedSystems (#a78bfa), Frontend (#fbbf24), Craft (#fb7185)
    {
      selector: 'node[type = "topic"][category = "BackendRuntime"], node[type = "topic"][category = "BackendDotNet"], node[type = "topic"][category = "DotNet"]',
      style: {
        'background-color': '#38bdf8'
      }
    },
    {
      selector: 'node[type = "topic"][category = "DatabaseStorage"], node[type = "topic"][category = "Postgres"]',
      style: {
        'background-color': '#34d399'
      }
    },
    {
      selector: 'node[type = "topic"][category = "SystemDesign"], node[type = "topic"][category = "DistributedSystems"]',
      style: {
        'background-color': '#a78bfa'
      }
    },
    {
      selector: 'node[type = "topic"][category = "FrontendWeb"], node[type = "topic"][category = "Frontend"]',
      style: {
        'background-color': '#fbbf24'
      }
    },
    {
      selector: 'node[type = "topic"][category = "EngineeringCraft"], node[type = "topic"][category = "Craft"]',
      style: {
        'background-color': '#fb7185'
      }
    },
    // Book nodes
    {
      selector: 'node[type = "book"]',
      style: {
        'shape': 'round-rectangle',
        'width': 34,
        'height': 26,
        'background-color': dark ? '#94a3b8' : '#475569'
      }
    },
    // Card nodes
    {
      selector: 'node[type = "card"]',
      style: {
        'shape': 'diamond',
        'width': 26,
        'height': 26,
        'background-color': '#3b82f6',
        'label': ''
      }
    },
    // Colors by card mastery: Learning (#f59e0b), Reviewing (#3b82f6), Mastered (#7c3aed)
    {
      selector: 'node[type = "card"][status = "Learning"], node[type = "card"][status = "learning"]',
      style: {
        'background-color': '#f59e0b'
      }
    },
    {
      selector: 'node[type = "card"][status = "Reviewing"], node[type = "card"][status = "reviewing"]',
      style: {
        'background-color': '#3b82f6'
      }
    },
    {
      selector: 'node[type = "card"][status = "Mastered"], node[type = "card"][status = "mastered"]',
      style: {
        'background-color': '#7c3aed',
        'border-color': '#c4b5fd',
        'width': 28,
        'height': 28
      }
    },
    // Highlight nodes
    {
      selector: 'node[type = "highlight"]',
      style: {
        'shape': 'hexagon',
        'width': 22,
        'height': 22,
        'background-color': dark ? '#c4b5fd' : '#8b5cf6',
        'label': ''
      }
    },
    // Level-of-Detail: Revealed labels on hover, selection, or zoom-in
    {
      selector: 'node[type = "card"]:selected, node[type = "highlight"]:selected, node[type = "card"].label-revealed, node[type = "highlight"].label-revealed',
      style: {
        'label': 'data(label)',
        'z-index': 90
      }
    },
    // Selected node state
    {
      selector: 'node:selected',
      style: {
        'border-width': 3.5,
        'border-color': activeClr,
        'border-opacity': 1,
        'z-index': 100
      }
    },
    // Base edge style
    {
      selector: 'edge',
      style: {
        'width': 1.5,
        'line-color': edgeClr,
        'curve-style': 'bezier',
        'opacity': 0.8,
        'transition-property': 'line-color, opacity, width',
        'transition-duration': 0.25
      }
    },
    // Constellation edges linking curriculum topics and library books to parent pillar hubs
    {
      selector: 'edge[relationType = "TopicToPillar"], edge[relationType = "BookToPillar"], edge[relationType = "topictopillar"], edge[relationType = "booktopillar"]',
      style: {
        'curve-style': 'bezier',
        'opacity': 0.85,
        'width': 2.0,
        'line-color': edgeClr
      }
    },
    // Card edges to highlight or pillar
    {
      selector: 'edge[relationType = "CardToHighlight"], edge[relationType = "CardToPillar"]',
      style: {
        'curve-style': 'bezier',
        'line-style': 'dotted',
        'width': 1.2,
        'line-color': edgeClr,
        'opacity': 0.6
      }
    },
    {
      selector: 'edge[relationType = "SharedTag"]',
      style: {
        'line-style': 'dashed',
        'line-dash-pattern': [4, 4],
        'line-color': dark ? '#60a5fa' : '#3b82f6',
        'width': 1.5
      }
    },
    // Live search dimming classes
    {
      selector: 'node.search-dimmed',
      style: {
        'opacity': 0.15
      }
    },
    {
      selector: 'edge.search-dimmed',
      style: {
        'opacity': 0.05
      }
    },
    {
      selector: 'node.search-matched',
      style: {
        'opacity': 1,
        'border-width': 3,
        'border-color': activeClr,
        'z-index': 99
      }
    },
    // Interactive visual legend dimming classes
    {
      selector: 'node.legend-dimmed',
      style: {
        'opacity': 0.15
      }
    },
    {
      selector: 'edge.legend-dimmed',
      style: {
        'opacity': 0.05
      }
    }
  ]
}

function runLayout() {
  if (!cy || cy.nodes().length === 0) return

  // Headless test environment check (where layout dimensions are 0)
  const container = containerRef.value
  if (container && (container.clientWidth === 0 || container.clientHeight === 0)) {
    const fallback = cy.layout({ name: 'preset' })
    fallback.run()
    return
  }

  // CoSE force-directed layout cooling down quickly (freezes in <= 1.5s, 0% CPU idle)
  const coseOptions: CoseLayoutOptions = {
    name: 'cose',
    animate: true,
    animationDuration: 1000,
    coolingFactor: 0.95,
    numIter: 300,
    randomize: true,
    componentSpacing: 140,
    fit: true,
    padding: 100,
    nodeRepulsion: () => 500000,
    idealEdgeLength: () => 140,
    edgeElasticity: () => 100,
    gravity: 70,
    stop: () => {
      // Simulation freezes completely, drops to 0% CPU idle
    }
  }

  const layout = cy.layout(coseOptions)
  layout.run()
}

function syncElements() {
  if (!cy) return

  cy.batch(() => {
    cy!.elements().remove()

    const nodeElements = store.filteredNodes.map((node) => ({
      group: 'nodes' as const,
      data: {
        id: node.id,
        label: node.label,
        type: (node.type || 'topic').toLowerCase(),
        category: node.category,
        status: node.status,
        summary: node.summary
      }
    }))

    const edgeElements = store.filteredEdges.map((edge) => ({
      group: 'edges' as const,
      data: {
        id: edge.id,
        source: edge.source,
        target: edge.target,
        relationType: edge.relationType,
        label: edge.label
      }
    }))

    cy!.add([...nodeElements, ...edgeElements])
  })

  runLayout()
  applySearchHighlights()

  if (store.selectedNodeId) {
    const target = cy.getElementById(store.selectedNodeId)
    if (target && target.length > 0) {
      target.select()
    }
  }
}

function applySearchHighlights() {
  if (!cy) return

  const query = store.searchQuery?.trim().toLowerCase()
  if (!query) {
    cy.elements().removeClass('search-dimmed search-matched')
    return
  }

  cy.batch(() => {
    const matchedNodeIds = new Set<string>()

    cy!.nodes().forEach((node) => {
      const data = node.data()
      const matchesLabel = data.label?.toLowerCase().includes(query) ?? false
      const matchesSummary = data.summary?.toLowerCase().includes(query) ?? false

      if (matchesLabel || matchesSummary) {
        matchedNodeIds.add(node.id())
        node.removeClass('search-dimmed')
        node.addClass('search-matched')
      } else {
        node.removeClass('search-matched')
        node.addClass('search-dimmed')
      }
    })

    cy!.edges().forEach((edge) => {
      const sourceId = edge.data('source')
      const targetId = edge.data('target')
      if (matchedNodeIds.has(sourceId) && matchedNodeIds.has(targetId)) {
        edge.removeClass('search-dimmed')
      } else {
        edge.addClass('search-dimmed')
      }
    })
  })
}

function fitScreen() {
  if (!cy || cy.nodes().length === 0) return
  cy.animate({
    fit: { padding: 90 },
    duration: 400
  })
}

function handleResize() {
  if (cy) {
    cy.resize()
  }
}

function initCytoscape() {
  if (!containerRef.value) return

  cy = cytoscape({
    container: containerRef.value,
    style: getStylesheet(isDark.value),
    elements: [],
    layout: { name: 'null' },
    minZoom: 0.2,
    maxZoom: 3.0,
    userZoomingEnabled: true,
    userPanningEnabled: true,
    boxSelectionEnabled: false,
    wheelSensitivity: 0.3
  })

  // Interactivity: tap node to select, tap background to deselect
  cy.on('tap', 'node', (evt: EventObject) => {
    const node = evt.target
    store.selectNode(node.id())
  })

  cy.on('tap', (evt: EventObject) => {
    if (evt.target === cy) {
      store.selectNode(null)
    }
  })

  // LOD: Hover to reveal label on cards and highlights
  cy.on('mouseover', 'node[type = "card"], node[type = "highlight"]', (evt: EventObject) => {
    evt.target.addClass('label-revealed')
  })

  cy.on('mouseout', 'node[type = "card"], node[type = "highlight"]', (evt: EventObject) => {
    if (!evt.target.selected() && cy && cy.zoom() < 1.1) {
      evt.target.removeClass('label-revealed')
    }
  })

  // Dynamic LOD on zoom: zoom >= 1.1 reveals all leaf labels
  cy.on('zoom', () => {
    if (!cy) return
    const zoom = cy.zoom()
    if (zoom >= 1.1) {
      cy.elements('node[type = "card"], node[type = "highlight"]').addClass('label-revealed')
    } else {
      cy.elements('node[type = "card"]:unselected, node[type = "highlight"]:unselected').removeClass('label-revealed')
    }
  })

  emit('cy-ready', cy)
  syncElements()
}

onMounted(() => {
  initCytoscape()
})

useEventListener(typeof window !== 'undefined' ? window : null, 'resize', handleResize)

onBeforeUnmount(() => {
  if (cy) {
    cy.destroy()
    cy = null
  }
})

// Watch color mode for reactive dark/light palette synchronization
watch(isDark, (newVal) => {
  if (cy) {
    cy.style(getStylesheet(newVal))
  }
})

// Watch store data changes
watch(
  [() => store.filteredNodes, () => store.filteredEdges],
  () => {
    syncElements()
  },
  { deep: false }
)

// Watch search query changes
watch(
  () => store.searchQuery,
  () => {
    applySearchHighlights()
  }
)

function matchesLegendType(nodeData: any, legendType: string): boolean {
  const nodeType = (nodeData.type || '').toLowerCase()
  const status = (nodeData.status || 'learning').toLowerCase()

  if (legendType === 'pillar') return nodeType === 'pillar'
  if (legendType === 'topic') return nodeType === 'topic'
  if (legendType === 'book') return nodeType === 'book'
  if (legendType === 'highlight') return nodeType === 'highlight'
  if (legendType === 'card') return nodeType === 'card'
  if (legendType === 'learning') return nodeType === 'card' && status === 'learning'
  if (legendType === 'reviewing') return nodeType === 'card' && status === 'reviewing'
  if (legendType === 'mastered') return nodeType === 'card' && status === 'mastered'
  return true
}

// Watch legend hover changes for interactive visual dimming
watch(
  () => store.hoveredLegendType,
  (type) => {
    if (!cy) return
    if (!type) {
      cy.batch(() => {
        cy?.nodes().removeClass('legend-dimmed')
        cy?.edges().removeClass('legend-dimmed')
      })
      return
    }
    cy.batch(() => {
      cy?.nodes().forEach((node) => {
        const d = node.data()
        if (matchesLegendType(d, type)) {
          node.removeClass('legend-dimmed')
        } else {
          node.addClass('legend-dimmed')
        }
      })
      cy?.edges().addClass('legend-dimmed')
    })
  }
)

// Watch selection changes
watch(
  () => store.selectedNodeId,
  (newId) => {
    if (!cy) return
    if (newId) {
      cy.$(':selected').unselect()
      const target = cy.getElementById(newId)
      if (target && target.length > 0) {
        target.select()
        cy.animate({
          center: { eles: target },
          zoom: Math.max(cy.zoom(), 1.1),
          duration: 400
        })
      }
    } else {
      cy.$(':selected').unselect()
    }
  }
)

defineExpose({
  fitScreen,
  cy: () => cy
})
</script>

<template>
  <div class="relative w-full h-full overflow-hidden select-none bg-slate-50 dark:bg-canvas">
    <!-- Subtle architectural dot grid background -->
    <div
      class="absolute inset-0 pointer-events-none opacity-40 dark:opacity-25"
      style="background-image: radial-gradient(circle, rgba(148, 163, 184, 0.4) 1px, transparent 1px); background-size: 24px 24px;"
    />
    <div ref="containerRef" class="relative z-10 w-full h-full" />
  </div>
</template>
