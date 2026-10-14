<script setup lang="ts">
import { Users, Files, LibraryBig } from 'lucide-vue-next'
import type { AdminOverview } from '~/types/adminDashboard'
defineProps<{ overview: AdminOverview | null; loading: boolean; error: boolean; stale: boolean }>()
defineEmits<{ retry: [] }>()
const cards = [
  { key: 'accounts', icon: Users }, { key: 'personal', icon: Files }, { key: 'catalog', icon: LibraryBig }
] as const
</script>

<template>
  <section class="space-y-4" :aria-label="$t('admin.dashboard.inventory')" :aria-busy="loading">
    <div v-if="error" role="status" class="glass-card flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
      <p class="text-sm sm:text-base text-amber-700 dark:text-amber-300">{{ overview ? $t('admin.dashboard.stale_error') : $t('admin.dashboard.overview_error') }}</p>
      <button type="button" :disabled="loading" class="min-h-11 rounded-xl border border-slate-300 dark:border-white/[0.08] px-4 text-sm sm:text-base font-semibold whitespace-nowrap shrink-0" @click="$emit('retry')">{{ $t('admin.access.retry') }}</button>
    </div>
    <div class="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
      <article v-for="card in cards" :key="card.key" class="glass-card flex flex-col gap-3 p-4 sm:p-5">
        <div class="flex items-center justify-between gap-2"><component :is="card.icon" class="h-5 w-5 text-brand-500" :stroke-width="1.5" aria-hidden="true" /><span v-if="stale" class="text-xs font-medium text-amber-600 dark:text-amber-300 whitespace-nowrap shrink-0">{{ $t('admin.dashboard.stale') }}</span></div>
        <h2 class="text-sm sm:text-base font-semibold text-slate-600 dark:text-slate-300">{{ $t(`admin.dashboard.${card.key}`) }}</h2>
        <p v-if="overview" class="font-mono text-3xl sm:text-4xl font-bold tabular-nums">{{ card.key === 'accounts' ? overview.accountCount : card.key === 'personal' ? overview.personalDocuments.total : overview.catalogDocuments.total }}</p>
        <p v-else class="text-sm sm:text-base text-slate-500">{{ loading ? $t('admin.dashboard.loading') : $t('admin.dashboard.unavailable') }}</p>
        <p class="text-sm sm:text-base leading-relaxed text-slate-500 dark:text-slate-400">{{ $t(`admin.dashboard.${card.key}_hint`) }}</p>
      </article>
    </div>
    <article class="glass-card space-y-4 p-4 sm:p-5">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <h2 class="text-base sm:text-lg font-bold">{{ $t('admin.dashboard.publication') }}</h2>
        <NuxtLink to="/admin/catalog" class="inline-flex min-h-11 items-center rounded-xl border border-brand-500/20 bg-brand-500/5 px-3 text-sm sm:text-base font-semibold text-brand-700 dark:text-brand-300 whitespace-nowrap shrink-0">{{ $t('admin.dashboard.manage_documents') }}</NuxtLink>
      </div>
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <div v-for="key in ['draft', 'published', 'hidden'] as const" :key="key" class="flex items-center justify-between gap-3 rounded-xl bg-slate-100/80 p-3 dark:bg-white/[0.03]">
          <span class="text-sm sm:text-base text-slate-600 dark:text-slate-300">{{ $t(`admin.dashboard.${key}`) }}</span>
          <span class="font-mono text-lg font-semibold tabular-nums whitespace-nowrap shrink-0">{{ overview ? overview.catalogDocuments[key] : $t('admin.dashboard.unavailable_short') }}</span>
        </div>
      </div>
    </article>
  </section>
</template>
