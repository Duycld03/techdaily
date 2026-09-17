<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount, type PropType } from 'vue'
import type { Core } from 'cytoscape'
import { Compass } from 'lucide-vue-next'

const props = defineProps({
  cy: {
    type: Object as PropType<Core | null>,
    default: null
  }
})

const canvasRef = ref<HTMLCanvasElement | null>(null)

function getNodeColor(category?: string, type?: string, status?: string): string {
  const t = type?.toLowerCase()
  if (t === 'book') return '#94a3b8'
  if (t === 'card') {
    const s = status?.toLowerCase()
    if (s === 'mastered') return '#10b981'
    if (s === 'learning') return '#f59e0b'
    return '#3b82f6'
  }
  if (t === 'highlight') return '#c4b5fd'

  const cat = category?.toLowerCase() || ''
  if (cat.includes('dotnet') || cat.includes('backend')) return '#38bdf8'
  if (cat.includes('postgres') || cat.includes('database') || cat.includes('storage')) return '#34d399'
  if (cat.includes('system') || cat.includes('distributed')) return '#a78bfa'
  if (cat.includes('frontend') || cat.includes('web')) return '#fbbf24'
  if (cat.includes('craft')) return '#fb7185'

  return '#0284c7'
}

function drawMinimap() {
  const canvas = canvasRef.value
  if (!canvas) return
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  ctx.clearRect(0, 0, canvas.width, canvas.height)

  const cyInstance = props.cy
  if (!cyInstance || cyInstance.nodes().length === 0) {
    // Render placeholder subtle mesh
    ctx.fillStyle = 'rgba(148, 163, 184, 0.2)'
    ctx.font = '10px ui-sans-serif, system-ui, sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.fillText('No nodes visible', canvas.width / 2, canvas.height / 2)
    return
  }

  const nodes = cyInstance.nodes()
  const bb = nodes.boundingBox()

  if (bb.w <= 0 || bb.h <= 0) return

  const padding = 12
  const scaleX = (canvas.width - padding * 2) / bb.w
  const scaleY = (canvas.height - padding * 2) / bb.h
  const scale = Math.min(scaleX, scaleY)

  const cx = (bb.x1 + bb.x2) / 2
  const cy = (bb.y1 + bb.y2) / 2

  function toX(modelX: number): number {
    return canvas!.width / 2 + (modelX - cx) * scale
  }
  function toY(modelY: number): number {
    return canvas!.height / 2 + (modelY - cy) * scale
  }

  // 1. Draw nodes as colored dots
  nodes.forEach((node) => {
    const pos = node.position()
    const x = toX(pos.x)
    const y = toY(pos.y)
    const data = node.data()
    const color = getNodeColor(data.category, data.type, data.status)

    ctx.beginPath()
    ctx.arc(x, y, 2.5, 0, Math.PI * 2)
    ctx.fillStyle = color
    ctx.fill()
  })

  // 2. Draw current viewport boundary
  const extent = cyInstance.extent()
  const vx1 = toX(extent.x1)
  const vy1 = toY(extent.y1)
  const vx2 = toX(extent.x2)
  const vy2 = toY(extent.y2)

  const rectX = Math.min(vx1, vx2)
  const rectY = Math.min(vy1, vy2)
  const rectW = Math.abs(vx2 - vx1)
  const rectH = Math.abs(vy2 - vy1)

  // Viewport highlight
  ctx.fillStyle = 'rgba(56, 189, 248, 0.15)'
  ctx.fillRect(rectX, rectY, rectW, rectH)

  ctx.strokeStyle = 'rgba(56, 189, 248, 0.85)'
  ctx.lineWidth = 1.5
  ctx.strokeRect(rectX, rectY, rectW, rectH)
}

function onCanvasClick(evt: MouseEvent) {
  const canvas = canvasRef.value
  const cyInstance = props.cy
  if (!canvas || !cyInstance || cyInstance.nodes().length === 0) return

  const rect = canvas.getBoundingClientRect()
  const clickX = evt.clientX - rect.left
  const clickY = evt.clientY - rect.top

  const bb = cyInstance.nodes().boundingBox()
  if (bb.w <= 0 || bb.h <= 0) return

  const padding = 12
  const scale = Math.min((canvas.width - padding * 2) / bb.w, (canvas.height - padding * 2) / bb.h)
  const cx = (bb.x1 + bb.x2) / 2
  const cy = (bb.y1 + bb.y2) / 2

  const modelX = cx + (clickX - canvas.width / 2) / scale
  const modelY = cy + (clickY - canvas.height / 2) / scale

  if (Number.isFinite(modelX) && Number.isFinite(modelY)) {
    cyInstance.animate({
      center: { x: modelX, y: modelY },
      duration: 300
    })
  }
}

function attachListeners(cyInstance: Core | null) {
  if (!cyInstance) return
  cyInstance.on('render pan zoom position add remove', drawMinimap)
}

function detachListeners(cyInstance: Core | null) {
  if (!cyInstance) return
  cyInstance.off('render pan zoom position add remove', drawMinimap)
}

watch(
  () => props.cy,
  (newCy, oldCy) => {
    if (oldCy) detachListeners(oldCy)
    if (newCy) {
      attachListeners(newCy)
      drawMinimap()
    }
  }
)

onMounted(() => {
  if (props.cy) {
    attachListeners(props.cy)
  }
  drawMinimap()
})

onBeforeUnmount(() => {
  if (props.cy) {
    detachListeners(props.cy)
  }
})
</script>

<template>
  <div
    class="bg-white/85 dark:bg-slate-900/85 backdrop-blur-md border border-slate-200/80 dark:border-slate-800/80 rounded-2xl shadow-xl p-2.5 space-y-1.5 select-none"
  >
    <div class="flex items-center justify-between px-1">
      <span class="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
        <Compass class="w-3.5 h-3.5 text-brand-500" />
        Minimap
      </span>
      <span class="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
    </div>
    <canvas
      ref="canvasRef"
      width="160"
      height="100"
      class="rounded-xl bg-slate-100/90 dark:bg-slate-950/90 cursor-pointer block border border-slate-200/50 dark:border-slate-800/50"
      title="Click to pan viewport"
      @click="onCanvasClick"
    />
  </div>
</template>
