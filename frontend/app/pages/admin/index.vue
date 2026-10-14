<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { RefreshCw, ShieldCheck } from 'lucide-vue-next'
import BentoDashboardLayout from '~/components/layout/BentoDashboardLayout.vue'
import AdminInventoryPanel from '~/components/admin/dashboard/AdminInventoryPanel.vue'
import AdminProcessingPanel from '~/components/admin/dashboard/AdminProcessingPanel.vue'
import AdminServicesPanel from '~/components/admin/dashboard/AdminServicesPanel.vue'
import { useAdminDashboardStore } from '~/stores/useAdminDashboardStore'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
const store = useAdminDashboardStore()
const access = useAdminAccessStore()
const { locale } = useI18n()
const refreshing = computed(() => store.isOverviewLoading || store.isStatusLoading)
const stale = computed(() => Boolean(store.overview && (store.isOverviewLoading || store.overviewError)))
const observedAt = computed(() => store.overview ? new Intl.DateTimeFormat(locale.value === 'vi' ? 'vi-VN' : 'en-US', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(store.overview.observedAt)) : null)
onMounted(() => { if (access.isAuthorized) void store.refresh() })
</script>

<template>
  <BentoDashboardLayout v-if="access.isAuthorized" :action-label="$t('admin.dashboard.inventory')" :dock-label="$t('admin.dashboard.services')" data-admin-dashboard>
    <template #header>
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="flex items-start gap-3"><div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl border border-brand-500/20 bg-brand-500/10 text-brand-500"><ShieldCheck class="h-6 w-6" :stroke-width="1.5" aria-hidden="true" /></div><div><h1 class="text-xl sm:text-2xl font-bold">{{ $t('admin.dashboard.title') }}</h1><p class="mt-2 text-sm sm:text-base text-slate-500 dark:text-slate-400">{{ $t('admin.dashboard.description') }}</p></div></div>
        <button type="button" :disabled="refreshing" class="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-slate-300 dark:border-white/[0.08] px-4 text-sm sm:text-base font-semibold disabled:opacity-50 whitespace-nowrap shrink-0" @click="store.refresh"><RefreshCw class="h-4 w-4" :class="{ 'animate-spin': refreshing }" aria-hidden="true" />{{ $t('admin.dashboard.refresh') }}</button>
      </div>
    </template>
    <template #action-stage>
      <AdminInventoryPanel :overview="store.overview" :loading="store.isOverviewLoading" :error="Boolean(store.overviewError)" :stale="stale" @retry="store.fetchOverview" />
      <AdminProcessingPanel :overview="store.overview" />
    </template>
    <template #telemetry-dock><AdminServicesPanel /></template>
    <template #footer><p v-if="observedAt" class="font-mono text-xs text-slate-500 dark:text-slate-400">{{ $t('admin.dashboard.observed_at') }} {{ observedAt }}</p></template>
  </BentoDashboardLayout>
</template>
