<script setup lang="ts">
import { computed } from 'vue'
import { Gauge } from 'lucide-vue-next'

const props = defineProps<{
  struggling: number
  developing: number
  comfortable: number
}>()

const total = computed(() => props.struggling + props.developing + props.comfortable)

const buckets = computed(() => {
  const sum = total.value
  const pct = (n: number) => (sum > 0 ? Math.round((n / sum) * 100) : 0)
  return [
    {
      key: 'struggling',
      labelKey: 'review.ease_struggling',
      range: '1.30-1.70',
      count: props.struggling,
      rate: pct(props.struggling),
      bar: 'bg-rose-500',
    },
    {
      key: 'developing',
      labelKey: 'review.ease_developing',
      range: '1.70-2.10',
      count: props.developing,
      rate: pct(props.developing),
      bar: 'bg-amber-500',
    },
    {
      key: 'comfortable',
      labelKey: 'review.ease_comfortable',
      range: '2.10-2.50',
      count: props.comfortable,
      rate: pct(props.comfortable),
      bar: 'bg-emerald-500',
    },
  ]
})
</script>

<template>
  <div
    class="rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col h-full min-h-[190px]"
  >
    <!-- Header -->
    <div class="flex items-center gap-2 mb-3">
      <div
        class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center"
      >
        <Gauge class="w-4 h-4" />
      </div>
      <div>
        <h3 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
          {{ $t('review.ease_dist_title') }}
        </h3>
        <p class="text-[11px] text-slate-500 dark:text-slate-400">
          {{ $t('review.ease_dist_desc') }}
        </p>
      </div>
    </div>

    <!-- Buckets -->
    <div v-if="total > 0" class="flex flex-col gap-3 flex-1 justify-center">
      <div v-for="bucket in buckets" :key="bucket.key" class="space-y-1">
        <div class="flex items-center justify-between gap-2 text-xs">
          <span class="font-semibold text-slate-700 dark:text-slate-200 truncate">
            {{ $t(bucket.labelKey) }}
          </span>
          <span class="flex items-center gap-2 whitespace-nowrap shrink-0 text-slate-500 dark:text-slate-400">
            <span
              class="px-1.5 py-0.5 rounded-md text-[10px] font-mono font-bold bg-slate-500/10 text-slate-500 dark:text-slate-400 border border-slate-500/20 tabular-nums"
            >
              {{ bucket.range }}
            </span>
            <span class="tabular-nums">{{ bucket.count }}</span>
          </span>
        </div>
        <div class="h-1.5 rounded-full bg-slate-100 dark:bg-white/[0.06] overflow-hidden">
          <div
            class="h-full rounded-full transition-all duration-700"
            :class="bucket.bar"
            :style="{ width: `${bucket.rate}%` }"
          ></div>
        </div>
      </div>
    </div>

    <!-- Empty -->
    <div v-else class="flex-1 flex items-center justify-center">
      <p class="text-center text-xs text-slate-500 dark:text-slate-400">
        {{ $t('review.ease_dist_empty') }}
      </p>
    </div>
  </div>
</template>
