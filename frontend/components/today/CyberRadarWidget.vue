<script setup lang="ts">
import { computed } from 'vue'

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

// Static blip positions mapped relative to 160x160 center (80,80)
const blips = [
  { cx: 80, cy: 38, r: 2.5, color: '#22d3ee', pulse: true },   // Top (Frontend)
  { cx: 118, cy: 62, r: 3, color: '#8b5cf6', pulse: false },   // Top-Right (Backend)
  { cx: 104, cy: 112, r: 2.5, color: '#06b6d4', pulse: true }, // Bottom-Right (Database)
  { cx: 52, cy: 110, r: 3, color: '#a78bfa', pulse: false },   // Bottom-Left (System Design)
  { cx: 42, cy: 65, r: 2, color: '#22d3ee', pulse: false },    // Top-Left (Craft)
  { cx: 96, cy: 75, r: 2, color: '#38bdf8', pulse: false },    // Inner cluster
  { cx: 68, cy: 92, r: 2, color: '#818cf8', pulse: false }     // Inner cluster
]
</script>

<template>
  <div class="flex flex-col items-center select-none w-full">
    <!-- Telemetry Status Header -->
    <div class="w-full flex items-center justify-between text-[11px] font-mono mb-2">
      <div class="flex items-center gap-1.5">
        <span class="w-2 h-2 rounded-full bg-cyber-400 animate-ping" />
        <span class="font-bold text-cyber-400 uppercase tracking-wider">Telemetry Live</span>
      </div>
      <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
        ONLINE
      </span>
    </div>

    <!-- Polar Coordinate Radar Display -->
    <div class="relative w-32 h-32 sm:w-36 sm:h-36 my-1">
      <svg
        viewBox="0 0 160 160"
        class="w-full h-full"
      >
        <defs>
          <!-- Radial glow for radar center -->
          <radialGradient id="radarCenterGlow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stop-color="#22d3ee" stop-opacity="0.35" />
            <stop offset="100%" stop-color="#22d3ee" stop-opacity="0" />
          </radialGradient>

          <!-- Sweep hand gradient -->
          <linearGradient id="sweepGradient" x1="0%" y1="100%" x2="100%" y2="0%">
            <stop offset="0%" stop-color="#22d3ee" stop-opacity="0.8" />
            <stop offset="100%" stop-color="#8b5cf6" stop-opacity="0.1" />
          </linearGradient>
        </defs>

        <!-- Ambient circular grid backdrop -->
        <circle cx="80" cy="80" r="68" fill="url(#radarCenterGlow)" />

        <!-- Concentric Distance Range Rings -->
        <circle cx="80" cy="80" r="18" fill="none" stroke="rgba(34, 211, 238, 0.25)" stroke-width="1" stroke-dasharray="2 2" />
        <circle cx="80" cy="80" r="35" fill="none" stroke="rgba(34, 211, 238, 0.2)" stroke-width="1" />
        <circle cx="80" cy="80" r="52" fill="none" stroke="rgba(139, 92, 246, 0.25)" stroke-width="1" stroke-dasharray="3 3" />
        <circle cx="80" cy="80" r="68" fill="none" stroke="rgba(34, 211, 238, 0.35)" stroke-width="1.5" />

        <!-- Crosshair Coordinate Axis -->
        <line x1="80" y1="8" x2="80" y2="152" stroke="rgba(34, 211, 238, 0.2)" stroke-width="1" stroke-dasharray="2 3" />
        <line x1="8" y1="80" x2="152" y2="80" stroke="rgba(34, 211, 238, 0.2)" stroke-width="1" stroke-dasharray="2 3" />

        <!-- Node Blips -->
        <g v-for="(b, i) in blips" :key="i">
          <circle
            :cx="b.cx"
            :cy="b.cy"
            :r="b.r"
            :fill="b.color"
            :class="b.pulse ? 'animate-pulse' : ''"
          />
          <circle
            v-if="b.pulse"
            :cx="b.cx"
            :cy="b.cy"
            :r="b.r + 3"
            fill="none"
            :stroke="b.color"
            stroke-width="0.75"
            opacity="0.5"
          />
        </g>

        <!-- Rotating Radar Sweep Line -->
        <g class="origin-[80px_80px] animate-[spin_3.5s_linear_infinite]">
          <line
            x1="80"
            y1="80"
            x2="80"
            y2="12"
            stroke="url(#sweepGradient)"
            stroke-width="2"
            stroke-linecap="round"
          />
        </g>

        <!-- Center Hub Node -->
        <circle cx="80" cy="80" r="3.5" fill="#22d3ee" class="shadow-sm" />
        <circle cx="80" cy="80" r="1.5" fill="#ffffff" />
      </svg>
    </div>

    <!-- Telemetry Statistics Footer -->
    <div class="grid grid-cols-2 gap-2 w-full mt-1.5 text-center font-mono">
      <div class="p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
        <div class="text-base font-black text-cyber-400">
          {{ nodeCount }}
        </div>
        <div class="text-[9px] text-slate-500 dark:text-slate-400 uppercase tracking-wider">
          Nodes
        </div>
      </div>

      <div class="p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
        <div class="text-base font-black text-brand-400">
          {{ edgeCount }}
        </div>
        <div class="text-[9px] text-slate-500 dark:text-slate-400 uppercase tracking-wider">
          Relations
        </div>
      </div>
    </div>
  </div>
</template>
