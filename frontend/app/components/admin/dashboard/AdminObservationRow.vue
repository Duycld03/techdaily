<script setup lang="ts">
import { computed } from 'vue'
import type { AiObservation, ServiceObservation } from '~/types/adminDashboard'
const props = defineProps<{ label: string; observation?: AiObservation | ServiceObservation | null; stale?: boolean; loading?: boolean }>()
const { t } = useI18n()
const state = computed(() => props.stale && props.observation ? 'stale' : props.loading && !props.observation ? 'loading' : props.observation?.status || 'not_checked')
const badgeClass = computed(() => state.value === 'healthy' ? 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300'
  : state.value === 'unhealthy' ? 'bg-rose-500/10 text-rose-700 dark:text-rose-300'
    : state.value === 'stale' ? 'bg-amber-500/10 text-amber-700 dark:text-amber-300' : 'bg-slate-100 text-slate-600 dark:bg-white/[0.05] dark:text-slate-400')
const model = computed(() => props.observation && 'model' in props.observation ? props.observation.model : null)
const errors: Record<string, string> = {
  AI_API_KEY_MISSING: 'configuration', AI_MODEL_MISSING: 'configuration', AI_TIMEOUT: 'timeout',
  AI_RATE_LIMITED: 'rate_limited', AI_PROVIDER_ERROR: 'provider', AI_UNAVAILABLE: 'provider',
  EMBEDDING_DIMENSION_MISMATCH: 'dimension', DATABASE_UNAVAILABLE: 'database', DATABASE_TIMEOUT: 'timeout'
}
</script>

<template>
  <div class="space-y-2 py-3">
    <div class="flex flex-wrap items-center justify-between gap-2 sm:gap-3"><span class="text-sm sm:text-base font-semibold">{{ label }}</span><span class="rounded-lg px-2.5 py-1 text-xs font-semibold whitespace-nowrap shrink-0" :class="badgeClass">{{ $t(`admin.dashboard.service_${state}`) }}</span></div>
    <p v-if="model" class="break-all font-mono text-sm sm:text-base text-slate-500 dark:text-slate-400">{{ model }}</p>
    <p v-if="observation?.latencyMs != null" class="font-mono text-xs tabular-nums text-slate-500 dark:text-slate-400">{{ observation.latencyMs }} ms</p>
    <p v-if="observation?.errorCode" class="text-sm sm:text-base leading-relaxed text-rose-600 dark:text-rose-300">{{ t(`admin.errors.${errors[observation.errorCode] || 'provider'}`) }}</p>
  </div>
</template>
