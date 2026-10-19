<script setup lang="ts">
import { computed } from 'vue'
import { useNow } from '@vueuse/core'
import { LoaderCircle, RefreshCw } from 'lucide-vue-next'
import { useAdminDashboardStore } from '~/stores/useAdminDashboardStore'
import AdminObservationRow from './AdminObservationRow.vue'
const store = useAdminDashboardStore()
const { locale } = useI18n()
const now = useNow()
const statusStale = computed(() => Boolean(store.status && (store.statusError || store.isStatusLoading || now.value.getTime() - Date.parse(store.status.observedAt) > 300_000)))
const aiStale = computed(() => Boolean(store.aiProbe && now.value.getTime() - Date.parse(store.aiProbe.checkedAt) > 300_000))
const cooldown = computed(() => store.lastProbeStartedAt ? Math.max(0, Math.ceil((60_000 - (now.value.getTime() - store.lastProbeStartedAt)) / 1000)) : 0)
const apiObservation = computed(() => store.status ? { status: 'healthy' as const, checkedAt: store.status.observedAt, latencyMs: null } : store.statusError ? { status: 'unavailable' as const, checkedAt: '', latencyMs: null } : null)
function timestamp(value: string) { return new Intl.DateTimeFormat(locale.value === 'vi' ? 'vi-VN' : 'en-US', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) }
</script>

<template>
  <section class="glass-card p-4 sm:p-5" :aria-busy="store.isStatusLoading">
    <h2 class="text-base sm:text-lg font-bold">{{ $t('admin.dashboard.services') }}</h2>
    <AdminObservationRow :label="$t('admin.dashboard.api')" :observation="apiObservation" :stale="statusStale" :loading="store.isStatusLoading" />
    <AdminObservationRow :label="$t('admin.dashboard.database')" :observation="store.status?.database" :stale="statusStale" :loading="store.isStatusLoading" />
    <p class="text-sm sm:text-base leading-relaxed text-slate-500 dark:text-slate-400">{{ $t('admin.dashboard.api_hint') }}</p>
    <p v-if="store.status" class="mt-3 font-mono text-xs text-slate-500 dark:text-slate-400">{{ $t('admin.dashboard.observed_at') }} {{ timestamp(store.status.observedAt) }}</p>
    <button v-if="store.statusError" type="button" :disabled="store.isStatusLoading" class="mt-3 min-h-11 rounded-xl border border-slate-300 dark:border-white/[0.08] px-4 text-sm sm:text-base font-semibold whitespace-nowrap shrink-0" @click="store.fetchStatus">{{ $t('admin.access.retry') }}</button>
  </section>
  <section class="glass-card space-y-3 p-4 sm:p-5" :aria-busy="store.isProbing">
    <h2 class="text-base sm:text-lg font-bold">{{ $t('admin.dashboard.ai_services') }}</h2>
    <AdminObservationRow :label="$t('admin.dashboard.text_generation')" :observation="store.aiProbe?.textGeneration" :stale="aiStale" :loading="store.isProbing" />
    <AdminObservationRow :label="$t('admin.dashboard.embeddings')" :observation="store.aiProbe?.embeddings" :stale="aiStale" :loading="store.isProbing" />
    <p class="text-sm sm:text-base leading-relaxed text-slate-500 dark:text-slate-400">{{ $t('admin.dashboard.ai_hint') }}</p>
    <p v-if="store.probeError" role="status" class="text-sm sm:text-base text-amber-700 dark:text-amber-300">{{ store.probeError === 'RATE_LIMITED' ? $t('admin.errors.rate_limited') : $t('admin.errors.probe_failed') }}</p>
    <button type="button" :disabled="store.isProbing || cooldown > 0" class="flex min-h-11 w-full items-center justify-center gap-2 rounded-xl bg-brand-600 px-3 text-sm sm:text-base font-semibold text-white transition-colors hover:bg-brand-500 disabled:opacity-50 whitespace-nowrap shrink-0" @click="store.probeAi">
      <LoaderCircle v-if="store.isProbing" class="h-4 w-4 animate-spin" aria-hidden="true" /><RefreshCw v-else class="h-4 w-4" aria-hidden="true" />{{ $t('admin.dashboard.check_ai') }}
    </button>
    <p v-if="cooldown > 0 && !store.isProbing" class="text-xs font-mono text-slate-500 dark:text-slate-400">{{ $t('admin.dashboard.cooldown', { seconds: cooldown }) }}</p>
    <p v-if="store.aiProbe" class="font-mono text-xs text-slate-500 dark:text-slate-400">{{ $t('admin.dashboard.observed_at') }} {{ timestamp(store.aiProbe.checkedAt) }}</p>
  </section>
</template>
