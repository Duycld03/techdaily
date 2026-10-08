<script setup lang="ts">
import { AlertTriangle, Clock, Flame } from 'lucide-vue-next'

defineProps<{
  overdueCount: number
  leechCount: number
  atRiskCount: number
}>()

const emit = defineEmits<{ (e: 'review'): void }>()
</script>

<template>
  <div
    class="rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col justify-between h-full min-h-[190px]"
  >
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <div
          class="w-8 h-8 rounded-xl bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20 flex items-center justify-center"
        >
          <AlertTriangle class="w-4 h-4" />
        </div>
        <div>
          <h3 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
            {{ $t('review.atrisk_title') }}
          </h3>
          <p class="text-[11px] text-slate-500 dark:text-slate-400">
            {{ $t('review.atrisk_desc') }}
          </p>
        </div>
      </div>
      <span
        class="px-2.5 py-1 rounded-full text-[11px] font-bold border whitespace-nowrap shrink-0 bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20 tabular-nums"
      >
        {{ atRiskCount }} {{ $t('review.atrisk_total') }}
      </span>
    </div>

    <!-- Metrics -->
    <div class="grid grid-cols-2 gap-3 py-1">
      <div class="flex items-center gap-2">
        <span
          class="w-7 h-7 rounded-lg bg-amber-500/10 text-amber-600 dark:text-amber-400 flex items-center justify-center shrink-0"
        >
          <Clock class="w-3.5 h-3.5" />
        </span>
        <div class="min-w-0">
          <div class="text-lg font-black leading-none tabular-nums">{{ overdueCount }}</div>
          <div class="text-[11px] text-slate-500 dark:text-slate-400 truncate">
            {{ $t('review.atrisk_overdue') }}
          </div>
        </div>
      </div>
      <div class="flex items-center gap-2">
        <span
          class="w-7 h-7 rounded-lg bg-rose-500/10 text-rose-600 dark:text-rose-400 flex items-center justify-center shrink-0"
        >
          <Flame class="w-3.5 h-3.5" />
        </span>
        <div class="min-w-0">
          <div class="text-lg font-black leading-none tabular-nums">{{ leechCount }}</div>
          <div class="text-[11px] text-slate-500 dark:text-slate-400 truncate">
            {{ $t('review.atrisk_leech') }}
          </div>
        </div>
      </div>
    </div>

    <!-- Action / Empty -->
    <div class="pt-2 border-t border-slate-100 dark:border-white/[0.06]">
      <button
        v-if="atRiskCount > 0"
        type="button"
        class="w-full inline-flex items-center justify-center gap-2 px-3 py-2 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-semibold text-xs sm:text-sm shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0 cursor-pointer"
        @click="emit('review')"
      >
        <AlertTriangle class="w-4 h-4" />
        <span>{{ $t('review.atrisk_review_btn') }}</span>
      </button>
      <p
        v-else
        class="text-center text-xs text-emerald-600 dark:text-emerald-400 font-medium py-1"
      >
        {{ $t('review.atrisk_empty') }}
      </p>
    </div>
  </div>
</template>
