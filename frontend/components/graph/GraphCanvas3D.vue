<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount, computed } from 'vue'
import {
  RotateCcw,
  Compass,
  ZoomIn,
  ZoomOut,
  Maximize2,
  Type
} from 'lucide-vue-next'
import { useKnowledgeGraphStore, type GraphNode, type GraphEdge } from '~/stores/useKnowledgeGraphStore'

const emit = defineEmits<{
  (e: 'ready', graph: any): void
}>()

const store = useKnowledgeGraphStore()
const colorMode = useColorMode()
const isDark = computed(() => colorMode.value === 'dark')

const containerRef = ref<HTMLDivElement | null>(null)
let graphInstance: any = null
const isAutoRotate = ref(false)
const hoveredNode = ref<GraphNode | null>(null)
const showAllLabels = ref(false)

function toggleAllLabels() {
  showAllLabels.value = !showAllLabels.value
  if (graphInstance) {
    graphInstance.refresh()
  }
}

// Category Colors matching 2D palette
function getCategoryColor(category?: string | null): string {
  const cat = (category || '').toLowerCase()
  if (cat === 'backendruntime' || cat === 'backenddotnet' || cat === 'dotnet') return '#0284c7' // Sky
  if (cat === 'databasestorage' || cat === 'database') return '#059669' // Emerald
  if (cat === 'systemdesign') return '#7c3aed' // Purple
  if (cat === 'frontendweb' || cat === 'frontend') return '#f59e0b' // Amber
  if (cat === 'engineeringcraft') return '#e11d48' // Rose
  return '#64748b' // Slate
}

// Card SM-2 Status Colors
function getCardStatusColor(status?: string | null): string {
  const s = (status || '').toLowerCase()
  if (s === 'mastered') return '#10b981'
  if (s === 'reviewing') return '#3b82f6'
  return '#f59e0b' // Learning
}

// Node sizing by entity hierarchy
function getNodeVal(node: GraphNode): number {
  const type = (node.type || '').toLowerCase()
  if (type === 'pillar') return 24
  if (type === 'topic') return 10
  if (type === 'book') return 8
  if (type === 'card') return 5
  return 4 // highlight
}

function matchesLegendType(node: GraphNode, legendType: string): boolean {
  const nodeType = (node.type || '').toLowerCase()
  const status = (node.status || 'learning').toLowerCase()

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

function hexToRgba(hex: string, alpha: number): string {
  if (hex.startsWith('rgba')) {
    return hex.replace(/[\d.]+\)$/, `${alpha})`)
  }
  if (!hex.startsWith('#') || hex.length < 7) {
    return hex
  }
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

function getBaseNodeColor(node: GraphNode): string {
  const type = (node.type || '').toLowerCase()
  if (type === 'card') return getCardStatusColor(node.status)
  if (type === 'highlight') return '#06b6d4'
  if (type === 'book') return '#6366f1'
  return getCategoryColor(node.category)
}

// Node color resolver with interactive legend hover dimming
function getNodeColor(node: GraphNode): string {
  const baseColor = getBaseNodeColor(node)
  const hoveredLegend = store.hoveredLegendType
  if (hoveredLegend) {
    const matches = matchesLegendType(node, hoveredLegend)
    if (!matches) {
      return hexToRgba(baseColor, 0.15)
    }
  }
  return baseColor
}

function isEdgeConnectedToNode(edge: any, targetNodeId: string | null): boolean {
  if (!targetNodeId) return false
  const sourceId = typeof edge.source === 'object' ? edge.source?.id : edge.source
  const targetId = typeof edge.target === 'object' ? edge.target?.id : edge.target
  return sourceId === targetNodeId || targetId === targetNodeId
}

// Format 3D tooltip text
function getTooltipHtml(node: GraphNode): string {
  const type = (node.type || 'Node').toUpperCase()
  const category = node.category || 'General'
  const dark = isDark.value
  const bg = dark ? 'rgba(15, 23, 42, 0.92)' : 'rgba(255, 255, 255, 0.95)'
  const clr = dark ? '#f8fafc' : '#0f172a'
  const subClr = dark ? '#94a3b8' : '#64748b'
  const border = dark ? 'rgba(51, 65, 85, 0.8)' : 'rgba(203, 213, 225, 0.8)'

  let extra = ''
  if (node.type === 'card') {
    extra = `<div style="font-size: 11px; margin-top: 4px; color: #10b981;">Interval: ${node.intervalDays ?? 1}d • EF: ${(node.easeFactor ?? 2.5).toFixed(2)}</div>`
  } else if (node.type === 'topic' && node.difficulty) {
    extra = `<div style="font-size: 11px; margin-top: 4px; color: #38bdf8;">Difficulty: ${node.difficulty} • Day ${node.dayOrder ?? 1}</div>`
  } else if (node.type === 'highlight' && node.tags?.length) {
    extra = `<div style="font-size: 11px; margin-top: 4px; color: #38bdf8;">Tags: ${node.tags.slice(0, 3).join(', ')}</div>`
  }

  return `
    <div style="background: ${bg}; color: ${clr}; border: 1px solid ${border}; border-radius: 12px; padding: 10px 14px; font-family: ui-sans-serif, system-ui, sans-serif; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.3); max-width: 260px; pointer-events: none;">
      <div style="display: flex; align-items: center; gap: 6px; margin-bottom: 4px;">
        <span style="font-size: 10px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.5px; background: rgba(56, 189, 248, 0.2); color: #38bdf8; padding: 2px 6px; border-radius: 6px;">${type}</span>
        <span style="font-size: 11px; color: ${subClr};">${category}</span>
      </div>
      <div style="font-size: 13px; font-weight: 700; line-height: 1.3;">${node.label}</div>
      ${extra}
    </div>
  `
}

function updateGraphData() {
  if (!graphInstance) return

  const nodes = store.filteredNodes.map((n) => ({
    ...n,
    x: undefined,
    y: undefined,
    z: undefined
  }))

  const nodeIds = new Set(nodes.map((n) => n.id))
  const edges = store.filteredEdges
    .filter((e) => nodeIds.has(e.source) && nodeIds.has(e.target))
    .map((e) => ({
      ...e,
      source: e.source,
      target: e.target
    }))

  graphInstance.graphData({ nodes, links: edges })
}

function fitScreen() {
  if (!graphInstance) return
  if (isAutoRotate.value) {
    isAutoRotate.value = false
    stopAutoRotate()
  }
  graphInstance.zoomToFit(1200, 60)
}

let autoRotateTimer: ReturnType<typeof setInterval> | null = null
let currentOrbitAngle = 0

function startAutoRotate() {
  if (autoRotateTimer || !graphInstance) return

  const camera = graphInstance.camera()
  if (camera) {
    const pos = camera.position
    currentOrbitAngle = Math.atan2(pos.x, pos.z)
  }

  // Smooth orbital rotation: 40 FPS (25ms interval), ~60s for a full 360-degree revolution
  const angleDelta = (2 * Math.PI) / 2400

  autoRotateTimer = setInterval(() => {
    if (!graphInstance) {
      stopAutoRotate()
      return
    }

    const camera = graphInstance.camera()
    if (!camera) return

    const pos = camera.position
    const radius = Math.hypot(pos.x, pos.z) || 450
    currentOrbitAngle += angleDelta

    graphInstance.cameraPosition({
      x: radius * Math.sin(currentOrbitAngle),
      y: pos.y,
      z: radius * Math.cos(currentOrbitAngle)
    })
  }, 25)
}

function stopAutoRotate() {
  if (autoRotateTimer) {
    clearInterval(autoRotateTimer)
    autoRotateTimer = null
  }
}

function toggleAutoRotate() {
  isAutoRotate.value = !isAutoRotate.value
  if (isAutoRotate.value) {
    startAutoRotate()
  } else {
    stopAutoRotate()
  }
}

function zoomIn() {
  if (!graphInstance) return
  const camera = graphInstance.camera()
  if (camera) {
    const pos = camera.position
    graphInstance.cameraPosition(
      { x: pos.x * 0.75, y: pos.y * 0.75, z: pos.z * 0.75 },
      undefined,
      400
    )
  }
}

function zoomOut() {
  if (!graphInstance) return
  const camera = graphInstance.camera()
  if (camera) {
    const pos = camera.position
    graphInstance.cameraPosition(
      { x: pos.x * 1.3, y: pos.y * 1.3, z: pos.z * 1.3 },
      undefined,
      400
    )
  }
}

onMounted(async () => {
  if (typeof window === 'undefined' || !containerRef.value) return

  try {
    // Dynamic import to eliminate SSR ReferenceError: window is not defined
    const ForceGraph3DModule = await import('3d-force-graph')
    const ForceGraph3D = ForceGraph3DModule.default
    const SpriteTextModule = await import('three-spritetext')
    const SpriteText = SpriteTextModule.default

    const bgClr = isDark.value ? '#09090b' : '#f8fafc'

    graphInstance = ForceGraph3D()(containerRef.value)
      .backgroundColor(bgClr)
      .showNavInfo(false)
      .nodeId('id')
      .nodeVal((node: any) => getNodeVal(node))
      .nodeColor((node: any) => getNodeColor(node))
      .nodeLabel((node: any) => getTooltipHtml(node))
      .nodeResolution(24)
      .nodeRelSize(3)
      .nodeThreeObjectExtend(true)
      .nodeThreeObject((node: any) => {
        const isPillar = node.type === 'pillar'
        const isBook = node.type === 'book'
        const isTopic = node.type === 'topic'
        const isSelected = store.selectedNodeId === node.id
        const isHovered = hoveredNode.value?.id === node.id

        // Level of Detail (LOD) Decluttering:
        // Restrict billboard text labels strictly to Pillar Hubs at overview distance.
        // Reveal non-pillar labels only when showAllLabels is toggled on, or upon hover/selection.
        if (!isPillar && !showAllLabels.value && !isSelected && !isHovered) {
          return null
        }

        const sprite = new SpriteText(node.label || node.id)
        sprite.color = isDark.value ? '#ffffff' : '#0f172a'
        sprite.textHeight = isPillar ? 7 : (isBook ? 5.5 : 4)
        sprite.backgroundColor = isDark.value ? 'rgba(18, 18, 21, 0.85)' : 'rgba(255, 255, 255, 0.85)'
        sprite.padding = [1.5, 3]
        sprite.borderRadius = 4
        sprite.position.y = isPillar ? 22 : (isBook ? 15 : 11)
        return sprite
      })
      .linkSource('source')
      .linkTarget('target')
      .linkColor((edge: any) => {
        const activeNodeId = hoveredNode.value?.id || store.selectedNodeId
        if (activeNodeId && isEdgeConnectedToNode(edge, activeNodeId)) {
          return '#38bdf8'
        }
        return isDark.value ? 'rgba(148, 163, 184, 0.22)' : 'rgba(100, 116, 139, 0.28)'
      })
      .linkWidth((edge: any) => {
        const activeNodeId = hoveredNode.value?.id || store.selectedNodeId
        return activeNodeId && isEdgeConnectedToNode(edge, activeNodeId) ? 2.5 : 0.8
      })
      .linkOpacity(0.35)
      .linkDirectionalParticles((edge: any) => {
        const activeNodeId = hoveredNode.value?.id || store.selectedNodeId
        return activeNodeId && isEdgeConnectedToNode(edge, activeNodeId) ? 4 : 0
      })
      .linkDirectionalParticleWidth(1.8)
      .linkDirectionalParticleSpeed(0.007)
      // Bounded physics warmup to avoid UI freeze and save mobile battery
      .warmupTicks(70)
      .cooldownTicks(120)
      .cooldownTime(2500)
      .onNodeClick((node: any) => {
        if (!node) return
        store.selectNode(node.id)

        // Disarm auto-rotation on node selection to focus camera on selected entity
        if (isAutoRotate.value) {
          isAutoRotate.value = false
          stopAutoRotate()
        }
        // Smooth camera fly-to on selection
        if (graphInstance && node.x !== undefined) {
          const distance = 90
          const distRatio = 1 + distance / Math.hypot(node.x || 1, node.y || 1, node.z || 1)
          graphInstance.cameraPosition(
            { x: (node.x || 0) * distRatio, y: (node.y || 0) * distRatio, z: (node.z || 0) * distRatio },
            node,
            1200
          )
        }
      })
      .onNodeHover((node: any) => {
        hoveredNode.value = node || null
        if (containerRef.value) {
          containerRef.value.style.cursor = node ? 'pointer' : 'default'
        }
      })
      .onBackgroundClick(() => {
        store.selectNode(null)
      })

    // Configure OrbitControls
    const controls = graphInstance.controls()
    if (controls) {
      controls.enableDamping = true
      controls.dampingFactor = 0.1
      controls.maxDistance = 1400
      controls.minDistance = 30
    }

    // Pause auto-rotation during manual drag interaction and resume if enabled
    if (controls && typeof controls.addEventListener === 'function') {
      controls.addEventListener('start', () => {
        stopAutoRotate()
      })
      controls.addEventListener('end', () => {
        if (isAutoRotate.value) {
          startAutoRotate()
        }
      })
    }

    updateGraphData()
    emit('ready', graphInstance)

    // Initial camera position overview
    setTimeout(() => {
      if (graphInstance) {
        graphInstance.cameraPosition({ x: 0, y: 100, z: 420 }, { x: 0, y: 0, z: 0 }, 1500)
      }
    }, 400)
  } catch (err) {
    console.error('Failed to initialize 3D Knowledge Graph WebGL canvas:', err)
  }
})

// Watch for data changes
watch(
  () => [store.filteredNodes, store.filteredEdges],
  () => {
    updateGraphData()
  },
  { deep: true }
)

// Watch for dark mode changes
watch(
  isDark,
  (dark) => {
    if (!graphInstance) return
    graphInstance.backgroundColor(dark ? '#09090b' : '#f8fafc')
  }
)

// Watch for view mode changes (e.g. switching back to 2D view)
watch(
  () => store.viewMode,
  (mode) => {
    if (mode !== '3d') {
      isAutoRotate.value = false
      stopAutoRotate()
    }
  }
)

// Watch for interactive visual legend hover dimming
watch(
  () => store.hoveredLegendType,
  () => {
    if (graphInstance) {
      graphInstance.refresh()
    }
  }
)

onBeforeUnmount(() => {
  stopAutoRotate()
  if (graphInstance) {
    try {
      graphInstance._destructor?.()
    } catch {
      // ignore destructor error on unmount
    }
    graphInstance = null
  }
})

defineExpose({
  fitScreen,
  toggleAutoRotate,
  toggleAllLabels,
  showAllLabels
})
</script>

<template>
  <div class="w-full h-full relative overflow-hidden">
    <!-- WebGL Canvas Container -->
    <div ref="containerRef" class="w-full h-full" />

    <!-- 3D Navigation & Orbit HUD Floating Toolbar (Bottom Right) -->
    <div class="absolute bottom-5 right-5 z-20 flex flex-col gap-2 pointer-events-auto">
      <!-- Auto Rotate Toggle -->
      <button
        type="button"
        :class="[
          'p-2.5 rounded-xl shadow-lg border backdrop-blur-md transition-all active:scale-95 flex items-center justify-center',
          isAutoRotate
            ? 'bg-brand-500 text-white border-brand-400 shadow-brand-500/20'
            : 'bg-white/80 dark:bg-canvas-elevated/80 text-slate-700 dark:text-slate-200 border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-white/[0.1]'
        ]"
        :title="$t('graph.autoRotate')"
        :aria-label="$t('graph.autoRotate')"
        @click="toggleAutoRotate"
      >
        <Compass class="w-4 h-4" :class="{ 'animate-spin': isAutoRotate }" />
      </button>

      <!-- Show All Labels / LOD Toggle (Clean Cosmos vs Full Inspection) -->
      <button
        type="button"
        :class="[
          'p-2.5 rounded-xl shadow-lg border backdrop-blur-md transition-all active:scale-95 flex items-center justify-center',
          showAllLabels
            ? 'bg-brand-500 text-white border-brand-400 shadow-brand-500/20'
            : 'bg-white/80 dark:bg-canvas-elevated/80 text-slate-700 dark:text-slate-200 border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-white/[0.1]'
        ]"
        :title="showAllLabels ? $t('graph.hud.hideLabels') : $t('graph.hud.showLabels')"
        :aria-label="showAllLabels ? $t('graph.hud.hideLabels') : $t('graph.hud.showLabels')"
        @click="toggleAllLabels"
      >
        <Type class="w-4 h-4" />
      </button>

      <!-- Fit Screen / Reset Camera -->
      <button
        type="button"
        class="p-2.5 rounded-xl bg-white/80 dark:bg-canvas-elevated/80 text-slate-700 dark:text-slate-200 border border-slate-200/80 dark:border-white/[0.08] shadow-lg backdrop-blur-md hover:bg-slate-100 dark:hover:bg-white/[0.1] transition-all active:scale-95 flex items-center justify-center"
        :title="$t('graph.resetCamera')"
        :aria-label="$t('graph.resetCamera')"
        @click="fitScreen"
      >
        <Maximize2 class="w-4 h-4" />
      </button>

      <!-- Zoom In -->
      <button
        type="button"
        class="p-2.5 rounded-xl bg-white/80 dark:bg-canvas-elevated/80 text-slate-700 dark:text-slate-200 border border-slate-200/80 dark:border-white/[0.08] shadow-lg backdrop-blur-md hover:bg-slate-100 dark:hover:bg-white/[0.1] transition-all active:scale-95 flex items-center justify-center"
        title="Zoom in"
        aria-label="Zoom in"
        @click="zoomIn"
      >
        <ZoomIn class="w-4 h-4" />
      </button>

      <!-- Zoom Out -->
      <button
        type="button"
        class="p-2.5 rounded-xl bg-white/80 dark:bg-canvas-elevated/80 text-slate-700 dark:text-slate-200 border border-slate-200/80 dark:border-white/[0.08] shadow-lg backdrop-blur-md hover:bg-slate-100 dark:hover:bg-white/[0.1] transition-all active:scale-95 flex items-center justify-center"
        title="Zoom out"
        aria-label="Zoom out"
        @click="zoomOut"
      >
        <ZoomOut class="w-4 h-4" />
      </button>
    </div>
  </div>
</template>
