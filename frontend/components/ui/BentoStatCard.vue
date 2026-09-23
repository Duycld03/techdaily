<script setup lang="ts">
import { computed } from 'vue'
import type { Component } from 'vue'

const props = withDefaults(
  defineProps<{
    icon: Component
    label: string
    value: string | number
    unit?: string
    delta?: string
    deltaTrend?: 'up' | 'down' | 'flat'
    progress?: number
    accent?: 'brand' | 'cyber' | 'amber' | 'emerald'
  }>(),
  {
    accent: 'brand',
    progress: 0,
    deltaTrend: 'flat'
  }
)

const accentMap = {
  brand: {
    icon: 'text-brand-500 dark:text-brand-400 bg-brand-500/10',
    bar: 'bg-brand-500'
  },
  cyber: {
    icon: 'text-cyber-500 dark:text-cyber-400 bg-cyber-500/10',
    bar: 'bg-cyber-500'
  },
  amber: {
    icon: 'text-streak-amber bg-streak-amber/10',
    bar: 'bg-streak-amber'
  },
  emerald: {
    icon: 'text-emerald-500 dark:text-emerald-400 bg-emerald-500/10',
    bar: 'bg-emerald-500'
  }
} as const

const accentClasses = computed(() => accentMap[props.accent])
const clampedProgress = computed(() => Math.max(0, Math.min(100, props.progress)))
const deltaClass = computed(() => {
  if (props.deltaTrend === 'up') return 'text-emerald-600 dark:text-emerald-400'
  if (props.deltaTrend === 'down') return 'text-rose-600 dark:text-rose-400'
  return 'text-slate-500 dark:text-slate-400'
})
</script>

<template>
  <div class="glass-card p-3.5 sm:p-4 space-y-3">
    <div class="flex items-start justify-between gap-3">
      <div class="space-y-0.5">
        <p class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {{ label }}
        </p>
        <p class="flex items-baseline gap-1">
          <span class="text-xl sm:text-2xl font-bold tabular-nums text-slate-900 dark:text-white tracking-tight">{{ value }}</span>
          <span v-if="unit" class="text-xs sm:text-sm font-medium text-slate-400 dark:text-slate-500">{{ unit }}</span>
        </p>
      </div>
      <span :class="['flex h-8 w-8 sm:h-9 sm:w-9 shrink-0 items-center justify-center rounded-lg', accentClasses.icon]">
        <component :is="icon" class="h-4 w-4" :stroke-width="1.5" />
      </span>
    </div>

    <div class="space-y-1.5">
      <div class="h-1.5 w-full overflow-hidden rounded-full bg-slate-200/80 dark:bg-white/[0.06]">
        <div
          :class="['h-full rounded-full transition-all duration-500', accentClasses.bar]"
          :style="{ width: `${clampedProgress}%` }"
        />
      </div>
      <div class="flex items-center justify-between text-[11px]">
        <span class="tabular-nums text-slate-400 dark:text-slate-500">{{ clampedProgress }}%</span>
        <span v-if="delta" :class="['font-semibold tabular-nums', deltaClass]">{{ delta }}</span>
      </div>
    </div>
  </div>
</template>
