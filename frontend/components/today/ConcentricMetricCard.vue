<script setup lang="ts">
import { computed } from 'vue'
import { BrainCircuit, ArrowUpRight, Flame } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    actualMinutes?: number
    goalMinutes?: number
    masteredCards?: number
    totalCards?: number
    dueCards?: number
  }>(),
  {
    actualMinutes: 0,
    goalMinutes: 10,
    masteredCards: 0,
    totalCards: 0,
    dueCards: 0
  }
)

const outerRadius = 46
const outerCircumference = 2 * Math.PI * outerRadius // ~289.03

const innerRadius = 34
const innerCircumference = 2 * Math.PI * innerRadius // ~213.63
const paceProgress = computed(() => {
  if (props.goalMinutes <= 0) return 0
  return Math.min(1, Math.max(0, props.actualMinutes / props.goalMinutes))
})

const retentionProgress = computed(() => {
  if (props.totalCards <= 0) return 0
  return Math.min(1, Math.max(0, props.masteredCards / props.totalCards))
})

const outerDashoffset = computed(() => {
  return outerCircumference * (1 - paceProgress.value)
})

const innerDashoffset = computed(() => {
  return innerCircumference * (1 - retentionProgress.value)
})

const retentionPercentage = computed(() => {
  if (props.totalCards <= 0) return 100
  return Math.round((props.masteredCards / props.totalCards) * 100)
})
</script>

<template>
  <div class="glass-card p-3.5 sm:p-4 flex flex-col justify-between group hover:border-white/[0.12] transition-all min-h-0">
    <!-- Header -->
    <div class="flex items-center justify-between gap-3 mb-2.5">
      <div class="flex items-center gap-2.5">
        <div class="w-8 h-8 rounded-lg bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] flex items-center justify-center shrink-0">
          <BrainCircuit class="w-4 h-4" />
        </div>
        <div>
          <h3 class="text-sm font-bold text-slate-900 dark:text-white tracking-tight">
            {{ $t('dashboard.metrics_title') }}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {{ $t('dashboard.metrics_subtitle') }}
          </p>
        </div>
      </div>
    </div>

    <!-- Main Concentric Rings and Readout -->
    <div class="flex flex-col sm:flex-row items-center justify-around gap-3 my-1">
      <!-- SVG Rings -->
      <div class="relative w-28 h-28 sm:w-32 sm:h-32 flex items-center justify-center shrink-0">
        <svg class="w-full h-full transform -rotate-90" viewBox="0 0 120 120">
          <!-- Outer Ring Track -->
          <circle
            cx="60"
            cy="60"
            :r="outerRadius"
            class="stroke-slate-200/80 dark:stroke-white/10 fill-none"
            stroke-width="7"
          />
          <!-- Outer Ring Progress (Daily Goal Pace) -->
          <circle
            cx="60"
            cy="60"
            :r="outerRadius"
            class="stroke-brand-500 fill-none transition-all duration-700 ease-out"
            stroke-width="7"
            stroke-linecap="round"
            :stroke-dasharray="outerCircumference"
            :stroke-dashoffset="outerDashoffset"
          />

          <!-- Inner Ring Track -->
          <circle
            cx="60"
            cy="60"
            :r="innerRadius"
            class="stroke-slate-200/80 dark:stroke-white/10 fill-none"
            stroke-width="6"
          />
          <!-- Inner Ring Progress (SM-2 Retention Health) -->
          <circle
            cx="60"
            cy="60"
            :r="innerRadius"
            class="stroke-cyber-500 fill-none transition-all duration-700 ease-out"
            stroke-width="6"
            stroke-linecap="round"
            :stroke-dasharray="innerCircumference"
            :stroke-dashoffset="innerDashoffset"
          />
        </svg>

        <!-- Central Numeric Display -->
        <div class="absolute inset-0 flex flex-col items-center justify-center text-center">
          <span class="text-lg sm:text-xl font-black text-slate-900 dark:text-white tracking-tight leading-none">
            {{ retentionPercentage }}%
          </span>
          <span class="text-[9px] font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider mt-0.5">
            {{ $t('dashboard.retention') }}
          </span>
        </div>
      </div>

      <!-- Legend & Numeric Breakdown -->
      <div class="flex flex-col gap-2 w-full sm:w-auto text-xs">
        <!-- Daily Goal Pace Row -->
        <div class="flex items-center justify-between gap-3 p-2 rounded-lg bg-slate-100/60 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
          <div class="flex items-center gap-2">
            <span class="w-2.5 h-2.5 rounded-full bg-brand-500 shrink-0"></span>
            <span class="text-slate-600 dark:text-slate-300 font-medium">{{ $t('dashboard.daily_goal') }}</span>
          </div>
          <span class="font-bold text-slate-900 dark:text-white">
            {{ actualMinutes }} / {{ goalMinutes }}m
          </span>
        </div>

        <!-- SM-2 Retention Row -->
        <div class="flex items-center justify-between gap-3 p-2 rounded-lg bg-slate-100/60 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
          <div class="flex items-center gap-2">
            <span class="w-2.5 h-2.5 rounded-full bg-cyber-500 shrink-0"></span>
            <span class="text-slate-600 dark:text-slate-300 font-medium">{{ $t('dashboard.sm2_retention') }}</span>
          </div>
          <span class="font-bold text-slate-900 dark:text-white">
            {{ masteredCards }} / {{ totalCards || 0 }}
          </span>
        </div>
      </div>
    </div>

    <!-- Review Due Action Banner -->
    <div class="mt-2.5 pt-2.5 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between">
      <div class="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400">
        <Flame class="w-3.5 h-3.5 text-amber-500 shrink-0" />
        <span>{{ dueCards }} {{ $t('dashboard.cards_due') }}</span>
      </div>

      <NuxtLink
        to="/review"
        :class="[
          'px-3 py-1.5 rounded-lg text-xs font-bold transition-all flex items-center gap-1.5 shrink-0',
          dueCards > 0
            ? 'bg-brand-600 hover:bg-brand-500 text-white shadow-sm shadow-brand-500/20 active:scale-95'
            : 'bg-slate-100 dark:bg-white/[0.04] text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 border border-transparent dark:border-white/[0.06]'
        ]"
      >
        <span>{{ dueCards > 0 ? $t('dashboard.review_now') : $t('dashboard.view_deck') }}</span>
        <ArrowUpRight class="w-3.5 h-3.5" />
      </NuxtLink>
    </div>
  </div>
</template>
