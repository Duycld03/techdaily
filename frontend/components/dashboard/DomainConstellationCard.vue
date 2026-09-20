<script setup lang="ts">
import { computed } from 'vue'
import { Network, Compass, Sparkles } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    nodeCount?: number
    edgeCount?: number
    activePillar?: string
  }>(),
  {
    nodeCount: 148,
    edgeCount: 210,
    activePillar: 'Distributed Systems'
  }
)

interface ConstellationNode {
  id: string
  name: string
  x: number
  y: number
  color: string
}

const nodes: ConstellationNode[] = [
  { id: 'dist', name: 'Distributed', x: 120, y: 22, color: '#8b5cf6' },
  { id: 'backend', name: 'Runtime', x: 44, y: 44, color: '#a78bfa' },
  { id: 'db', name: 'Database', x: 196, y: 44, color: '#22d3ee' },
  { id: 'system', name: 'Architecture', x: 72, y: 92, color: '#38bdf8' },
  { id: 'craft', name: 'Craft', x: 168, y: 92, color: '#c084fc' }
]

const edges = [
  { x1: 120, y1: 22, x2: 44, y2: 44 },
  { x1: 120, y1: 22, x2: 196, y2: 44 },
  { x1: 44, y1: 44, x2: 72, y2: 92 },
  { x1: 196, y1: 44, x2: 168, y2: 92 },
  { x1: 72, y1: 92, x2: 168, y2: 92 },
  { x1: 44, y1: 44, x2: 168, y2: 92 },
  { x1: 196, y1: 44, x2: 72, y2: 92 },
  { x1: 120, y1: 22, x2: 72, y2: 92 },
  { x1: 120, y1: 22, x2: 168, y2: 92 }
]
</script>

<template>
  <div class="glass-card p-3 sm:p-3.5 flex flex-col justify-between group hover:border-white/[0.12] transition-all shrink-0">
    <!-- Header -->
    <div class="flex items-center justify-between mb-2">
      <div class="flex items-center gap-2">
        <div class="w-7 h-7 rounded-lg bg-brand-500/10 text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
          <Network class="w-4 h-4" :stroke-width="1.5" />
        </div>
        <span class="text-xs font-bold text-slate-800 dark:text-slate-200">
          {{ $t('dashboard.domain_constellation') }}
        </span>
      </div>

      <NuxtLink
        to="/graph"
        class="text-xs font-semibold text-brand-400 hover:text-brand-300 flex items-center gap-1 transition-colors"
      >
        <span>{{ $t('dashboard.open_cosmos') }}</span>
        <Compass class="w-3.5 h-3.5 shrink-0" :stroke-width="1.5" />
      </NuxtLink>
    </div>

    <!-- Constellation Canvas -->
    <div class="flex flex-col items-center select-none w-full">
      <div class="relative w-full h-32 sm:h-34 my-0.5 flex items-center justify-center">
        <svg viewBox="0 0 240 116" class="w-full h-full max-h-34 overflow-visible">
          <defs>
            <radialGradient id="constellationCenterGlow" cx="50%" cy="50%" r="50%">
              <stop offset="0%" stop-color="#8b5cf6" stop-opacity="0.16" />
              <stop offset="60%" stop-color="#8b5cf6" stop-opacity="0.04" />
              <stop offset="100%" stop-color="#8b5cf6" stop-opacity="0" />
            </radialGradient>
            <linearGradient id="edgeGlowGradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#8b5cf6" stop-opacity="0.4" />
              <stop offset="50%" stop-color="#22d3ee" stop-opacity="0.25" />
              <stop offset="100%" stop-color="#7c3aed" stop-opacity="0.35" />
            </linearGradient>
          </defs>

          <!-- Ambient Glow Backdrop -->
          <circle cx="120" cy="58" r="54" fill="url(#constellationCenterGlow)" />

          <!-- Constellation Edges -->
          <line
            v-for="(e, idx) in edges"
            :key="`edge-${idx}`"
            :x1="e.x1"
            :y1="e.y1"
            :x2="e.x2"
            :y2="e.y2"
            stroke="url(#edgeGlowGradient)"
            stroke-width="1.2"
            stroke-dasharray="2 3"
            class="transition-opacity duration-300 group-hover:stroke-opacity-70"
          />

          <!-- Constellation Nodes & Labels -->
          <g v-for="n in nodes" :key="n.id">
            <circle
              :cx="n.x"
              :cy="n.y"
              r="5"
              :fill="n.color"
              :stroke="n.color"
              stroke-width="1.5"
              fill-opacity="0.25"
            />
            <circle :cx="n.x" :cy="n.y" r="2.5" :fill="n.color" class="shadow-sm" />
            <!-- Micro node name -->
            <text
              :x="n.x"
              :y="n.y > 60 ? n.y + 13 : n.y - 8"
              text-anchor="middle"
              class="text-[8px] sm:text-[9px] font-mono font-medium fill-slate-400 dark:fill-slate-400 select-none tracking-tight"
            >
              {{ n.name }}
            </text>
          </g>
        </svg>
      </div>

      <!-- Telemetry Counters Footer -->
      <div class="grid grid-cols-2 gap-2 w-full mt-1 text-center font-mono">
        <div class="p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
          <div class="text-sm sm:text-base font-black text-slate-800 dark:text-white tabular-nums">
            {{ nodeCount }}
          </div>
          <div class="text-[9px] text-slate-500 dark:text-slate-400 uppercase tracking-wider">
            {{ $t('dashboard.connected_nodes') || 'Nodes' }}
          </div>
        </div>

        <div class="p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
          <div class="text-sm sm:text-base font-black text-slate-800 dark:text-white tabular-nums">
            {{ edgeCount }}
          </div>
          <div class="text-[9px] text-slate-500 dark:text-slate-400 uppercase tracking-wider">
            {{ $t('dashboard.active_relations') || 'Relations' }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
