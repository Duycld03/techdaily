<script setup lang="ts">
import { computed } from 'vue'
import { Award, TrendingUp } from 'lucide-vue-next'

const props = defineProps<{
  masteredCount: number
  totalCount: number
}>()

const masteryRate = computed(() => {
  if (!props.totalCount || props.totalCount <= 0) return 0
  const rate = (props.masteredCount / props.totalCount) * 100
  return Math.min(100, Math.max(0, Math.round(rate)))
})

const learningCount = computed(() => {
  return Math.max(0, props.totalCount - props.masteredCount)
})

// Semi-circle arc circumference for radius = 45: π * 45 ≈ 141.37
const arcCircumference = 141.37

const strokeDashoffset = computed(() => {
  const percent = masteryRate.value / 100
  return arcCircumference - percent * arcCircumference
})
const tierInfo = computed(() => {
  const rate = masteryRate.value
  if (rate <= 25) {
    return {
      labelKey: 'review.tier_starting',
      badgeClass: 'bg-slate-100 dark:bg-canvas-subtle text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-white/[0.08]',
      arcColor: 'text-slate-400 dark:text-slate-500'
    }
  }
  if (rate <= 50) {
    return {
      labelKey: 'review.tier_building',
      badgeClass: 'bg-amber-50 dark:bg-amber-950/50 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-500/20',
      arcColor: 'text-amber-500'
    }
  }
  if (rate <= 75) {
    return {
      labelKey: 'review.tier_solid',
      badgeClass: 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border border-sky-500/20',
      arcColor: 'text-sky-500'
    }
  }
  return {
    labelKey: 'review.tier_mastered',
    badgeClass: 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20',
    arcColor: 'text-brand-500'
  }
})
</script>

<template>
  <div
    class="rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col justify-between h-full min-h-[190px]"
  >
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
          <Award class="w-4 h-4" />
        </div>
        <div>
          <h3 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
            {{ $t('review.mastery_rate') }}
          </h3>
          <p class="text-[11px] text-slate-500 dark:text-slate-400">
            {{ $t('review.mastery_desc') }}
          </p>
        </div>
      </div>
      <span :class="['px-2.5 py-1 rounded-full text-[11px] font-bold border whitespace-nowrap shrink-0', tierInfo.badgeClass]">
        {{ $t(tierInfo.labelKey) }}
      </span>
    </div>

    <!-- Semi-circular Gauge and Breakdown -->
    <div class="flex items-center justify-center gap-5 py-1">
      <div class="relative w-28 h-16 flex items-end justify-center">
        <svg class="w-28 h-16 overflow-visible" viewBox="0 0 100 55" aria-label="Mastery rate gauge">
          <path
            d="M 5 50 A 45 45 0 0 1 95 50"
            fill="none"
            stroke="currentColor"
            stroke-width="8"
            stroke-linecap="round"
            class="text-slate-100 dark:text-white/[0.06]"
          />
          <path
            d="M 5 50 A 45 45 0 0 1 95 50"
            fill="none"
            stroke="currentColor"
            stroke-width="8"
            stroke-linecap="round"
            :class="['transition-all duration-700', tierInfo.arcColor]"
            :stroke-dasharray="arcCircumference"
            :stroke-dashoffset="strokeDashoffset"
          />
        </svg>
        <div class="absolute bottom-0 text-center">
          <span class="text-xl font-black text-slate-900 dark:text-white leading-none tabular-nums">{{ masteryRate }}%</span>
          <span class="block text-[10px] text-slate-400 font-medium">{{ $t('review.mastery_label') }}</span>
        </div>
      </div>

      <div class="space-y-1 text-xs text-slate-500 dark:text-slate-400">
        <div class="flex items-center gap-2">
          <span class="w-2 h-2 rounded-full bg-brand-500 shrink-0"></span>
          <span class="tabular-nums">{{ $t('review.mastery_count_mastered', { count: masteredCount }) }}</span>
        </div>
        <div class="flex items-center gap-2">
          <span class="w-2 h-2 rounded-full bg-slate-300 dark:bg-white/[0.2] shrink-0"></span>
          <span class="tabular-nums">{{ $t('review.mastery_count_learning', { count: learningCount }) }}</span>
        </div>
      </div>
    </div>

    <!-- Footer Summary & Weekly Trend -->
    <div class="pt-2 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
      <span class="tabular-nums">{{ $t('review.mastery_total_cards', { count: totalCount }) }}</span>
      <span class="inline-flex items-center gap-1 font-mono text-emerald-500 font-bold whitespace-nowrap shrink-0">
        <TrendingUp class="w-3.5 h-3.5" />
        <span>{{ $t('review.mastery_weekly_trend', { percent: 12 }) }}</span>
      </span>
    </div>
  </div>
</template>
