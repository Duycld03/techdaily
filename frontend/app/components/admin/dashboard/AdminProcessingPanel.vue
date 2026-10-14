<script setup lang="ts">
import type { AdminOverview } from '~/types/adminDashboard'
defineProps<{ overview: AdminOverview | null }>()
const states = ['pending', 'processing', 'ready', 'failed'] as const
</script>

<template>
  <section class="glass-card space-y-4 p-4 sm:p-5">
    <div><h2 class="text-base sm:text-lg font-bold">{{ $t('admin.dashboard.processing_title') }}</h2><p class="mt-2 text-sm sm:text-base leading-relaxed text-slate-500 dark:text-slate-400">{{ $t('admin.dashboard.processing_hint') }}</p></div>
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <div v-for="scope in ['personal', 'catalog'] as const" :key="scope" class="rounded-xl border border-slate-200/80 p-4 dark:border-white/[0.06]">
        <h3 class="mb-3 text-sm sm:text-base font-semibold">{{ $t(`admin.dashboard.${scope}`) }}</h3>
        <dl class="space-y-3">
          <div v-for="state in states" :key="state" class="flex items-center justify-between gap-3">
            <dt class="text-sm sm:text-base" :class="state === 'failed' ? 'text-rose-600 dark:text-rose-300' : 'text-slate-500 dark:text-slate-400'">{{ $t(`admin.dashboard.state_${state}`) }}</dt>
            <dd class="font-mono text-base font-semibold tabular-nums whitespace-nowrap shrink-0">{{ overview ? (scope === 'personal' ? overview.personalDocuments[state] : overview.catalogDocuments.processing[state]) : $t('admin.dashboard.unavailable_short') }}</dd>
          </div>
        </dl>
      </div>
    </div>
  </section>
</template>
