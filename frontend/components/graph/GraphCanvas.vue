<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount, computed } from 'vue'
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
  const borderClr = dark ? '#334155' : '#cbd5e1'
  const edgeClr = dark ? '#334155' : '#cbd5e1'
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
      selector: 'node[type = "topic"][category = "BackendDotNet"], node[type = "topic"][category = "DotNet"]',
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
        'background-color': '#3b82f6'
      }
    },
    // Colors by card mastery: Learning (#f59e0b), Reviewing (#3b82f6), Mastered (#10b981)
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
        'background-color': '#10b981',
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
        'background-color': dark ? '#c4b5fd' : '#8b5cf6'
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
    // Associative tag edges (dashed)
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
    randomize: false,
    fit: true,
    padding: 60,
    nodeRepulsion: () => 450000,
    idealEdgeLength: () => 90,
    edgeElasticity: () => 100,
    gravity: 80,
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
    fit: { padding: 60 },
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

  emit('cy-ready', cy)
  syncElements()
}

onMounted(() => {
  initCytoscape()
  window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleResize)
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
  <div class="relative w-full h-full overflow-hidden select-none bg-slate-50 dark:bg-slate-950">
    <!-- Subtle architectural dot grid background -->
    <div
      class="absolute inset-0 pointer-events-none opacity-40 dark:opacity-25"
      style="background-image: radial-gradient(circle, rgba(148, 163, 184, 0.4) 1px, transparent 1px); background-size: 24px 24px;"
    />
    <div ref="containerRef" class="relative z-10 w-full h-full" />
  </div>
</template>
