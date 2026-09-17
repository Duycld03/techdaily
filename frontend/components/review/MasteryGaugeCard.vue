<script setup lang="ts">
import { computed } from 'vue'
import { Award } from 'lucide-vue-next'

const props = defineProps<{
  masteredCount: number
  totalCount: number
}>()

const masteryRate = computed(() => {
  if (!props.totalCount || props.totalCount <= 0) return 0
  const rate = (props.masteredCount / props.totalCount) * 100
  return Math.min(100, Math.max(0, Math.round(rate)))
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
      badgeClass: 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700',
      arcColor: 'text-slate-400 dark:text-slate-500'
    }
  }
  if (rate <= 50) {
    return {
      labelKey: 'review.tier_building',
      badgeClass: 'bg-amber-50 dark:bg-amber-950/50 text-amber-700 dark:text-amber-300 border-amber-200 dark:border-amber-800',
      arcColor: 'text-amber-500'
    }
  }
  if (rate <= 75) {
    return {
      labelKey: 'review.tier_solid',
      badgeClass: 'bg-sky-50 dark:bg-sky-950/50 text-sky-700 dark:text-sky-300 border-sky-200 dark:border-sky-800',
      arcColor: 'text-brand-500'
    }
  }
  return {
    labelKey: 'review.tier_mastered',
    badgeClass: 'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800',
    arcColor: 'text-emerald-500'
  }
})
</script>

<template>
  <div
    class="rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white p-5 sm:p-6 shadow-sm flex flex-col justify-between h-full min-h-[200px]"
  >
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-xl bg-brand-50 dark:bg-brand-950/60 text-brand-600 dark:text-brand-400 border border-brand-200/60 dark:border-brand-800/60 flex items-center justify-center">
          <Award class="w-4 h-4" />
        </div>
        <div>
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('review.mastery_rate') }}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400 font-medium">
            {{ masteredCount }} / {{ totalCount }} {{ $t('review.mastered_cards').toLowerCase() }}
          </p>
        </div>
      </div>

      <!-- Dynamic Proficiency Tier Badge -->
      <span
        :class="[
          'px-2.5 py-1 rounded-full text-[11px] font-bold border transition-colors whitespace-nowrap',
          tierInfo.badgeClass
        ]"
      >
        {{ $t(tierInfo.labelKey) }}
      </span>
    </div>

    <!-- Semi-Circular Radial Arc Gauge -->
    <div class="relative flex flex-col items-center justify-center my-auto pt-3">
      <div class="relative w-40 h-24 flex items-end justify-center">
        <svg
          class="w-full h-full overflow-visible"
          viewBox="0 0 120 70"
          aria-label="Mastery rate gauge"
        >
          <!-- Background Arc (180deg) -->
          <path
            d="M 15 60 A 45 45 0 0 1 105 60"
            fill="none"
            stroke="currentColor"
            stroke-width="10"
            stroke-linecap="round"
            class="text-slate-100 dark:text-slate-800"
          />

          <!-- Foreground Value Arc -->
          <path
            d="M 15 60 A 45 45 0 0 1 105 60"
            fill="none"
            stroke="currentColor"
            stroke-width="10"
            stroke-linecap="round"
            :stroke-dasharray="arcCircumference"
            :stroke-dashoffset="strokeDashoffset"
            :class="['transition-all duration-700 ease-out', tierInfo.arcColor]"
          />
        </svg>

        <!-- Center Numerical Label -->
        <div class="absolute inset-x-0 bottom-0 text-center flex flex-col items-center pointer-events-none">
          <span class="text-2xl sm:text-3xl font-black tracking-tight text-slate-900 dark:text-white leading-none">
            {{ masteryRate }}%
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
