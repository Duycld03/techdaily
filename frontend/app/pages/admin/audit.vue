<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  ScrollText,
  Search,
  RefreshCw,
  Filter,
  RotateCcw,
  Clock,
  User,
  Activity,
  CheckCircle2,
  Clock3,
  XCircle,
  AlertCircle,
  Loader2
} from 'lucide-vue-next'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useAdminAuditStore, type AdminAuditEventSummary } from '~/stores/useAdminAuditStore'
import BoardLayout from '~/components/layout/BoardLayout.vue'
import AppModal from '~/components/ui/AppModal.vue'
import AppSelect, { type SelectOption } from '~/components/common/AppSelect.vue'

definePageMeta({
  layout: 'admin'
})

const access = useAdminAccessStore()
const store = useAdminAuditStore()
const { t } = useI18n()

const selectedOutcome = ref<string>('')
const actionInput = ref<string>('')
const targetIdInput = ref<string>('')
const fromInput = ref<string>('')
const toInput = ref<string>('')

const outcomeOptions = computed<SelectOption[]>(() => [
  { value: '', label: t('admin_audit.filter_outcome_all') },
  { value: 'Succeeded', label: t('admin_audit.outcome_succeeded') },
  { value: 'Accepted', label: t('admin_audit.outcome_accepted') },
  { value: 'Rejected', label: t('admin_audit.outcome_rejected') },
  { value: 'Failed', label: t('admin_audit.outcome_failed') }
])

function formatOutcome(outcome: string): string {
  switch (outcome) {
    case 'Succeeded':
      return t('admin_audit.outcome_succeeded')
    case 'Accepted':
      return t('admin_audit.outcome_accepted')
    case 'Rejected':
      return t('admin_audit.outcome_rejected')
    case 'Failed':
      return t('admin_audit.outcome_failed')
    default:
      return outcome
  }
}
const detailModalOpen = ref(false)

onMounted(async () => {
  if (access.canReadAudit) {
    await store.fetchEvents()
  }
})

function applyFilters() {
  store.filters = {
    action: actionInput.value || undefined,
    targetId: targetIdInput.value || undefined,
    outcome: (selectedOutcome.value as any) || undefined,
    from: fromInput.value || undefined,
    to: toInput.value || undefined
  }
  store.page = 1
  store.fetchEvents()
}

function handleReset() {
  actionInput.value = ''
  targetIdInput.value = ''
  selectedOutcome.value = ''
  fromInput.value = ''
  toInput.value = ''
  store.resetFilters()
}

function handlePageChange(newPage: number) {
  store.page = newPage
  store.fetchEvents()
}

async function inspectEvent(evt: AdminAuditEventSummary) {
  await store.selectEvent(evt.id)
  detailModalOpen.value = true
}

const totalPages = computed(() => Math.ceil(store.totalCount / store.pageSize) || 1)
</script>

<template>
  <div v-if="access.canReadAudit" class="p-3 sm:p-6 h-[calc(100vh-3.5rem)] sm:h-[calc(100vh-3.75rem)] flex flex-col bg-slate-50 dark:bg-canvas">
    <BoardLayout class="flex-1 min-h-0">
      <!-- Top Bar Header -->
      <template #header>
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-2.5">
            <ScrollText class="h-5 w-5 text-brand-600 dark:text-brand-400" aria-hidden="true" />
            <h1 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white">
              {{ $t('admin_audit.title') }}
            </h1>
            <span class="rounded-full bg-slate-200 dark:bg-white/10 px-2.5 py-0.5 text-xs font-semibold text-slate-600 dark:text-slate-300">
              {{ store.totalCount }}
            </span>
          </div>

          <div class="flex items-center gap-2">
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-xl border border-slate-300 dark:border-white/10 px-3 py-1.5 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors"
              :disabled="store.isLoading"
              @click="store.fetchEvents()"
            >
              <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': store.isLoading }" aria-hidden="true" />
              <span class="hidden sm:inline">{{ $t('admin_audit.refresh') }}</span>
            </button>
          </div>
        </div>
      </template>

      <!-- Filters Row -->
      <template #filters>
        <div class="flex flex-wrap items-center gap-2 w-full text-xs">
          <!-- Action filter input -->
          <input
            v-model="actionInput"
            type="text"
            :placeholder="$t('admin_audit.filter_action')"
            class="rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle px-3 py-1.5 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            @keydown.enter="applyFilters"
          />

          <!-- Outcome filter select -->
          <div class="w-40 shrink-0">
            <AppSelect
              v-model="selectedOutcome"
              :options="outcomeOptions"
              size="sm"
              :aria-label="$t('admin_audit.outcome')"
              @change="applyFilters"
            />
          </div>

          <!-- Target ID input -->
          <input
            v-model="targetIdInput"
            type="text"
            :placeholder="$t('admin_audit.filter_target_id')"
            class="rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle px-3 py-1.5 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            @keydown.enter="applyFilters"
          />

          <!-- From Date -->
          <input
            v-model="fromInput"
            type="text"
            :placeholder="$t('admin_audit.filter_from')"
            class="rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle px-3 py-1.5 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            @keydown.enter="applyFilters"
          />

          <!-- To Date -->
          <input
            v-model="toInput"
            type="text"
            :placeholder="$t('admin_audit.filter_to')"
            class="rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle px-3 py-1.5 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            @keydown.enter="applyFilters"
          />

          <button
            type="button"
            class="inline-flex items-center gap-1 rounded-xl bg-brand-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-brand-700 transition-colors"
            @click="applyFilters"
          >
            <Filter class="h-3.5 w-3.5" />
            {{ $t('admin_audit.apply') }}
          </button>

          <button
            type="button"
            class="inline-flex items-center gap-1 rounded-xl border border-slate-300 dark:border-white/10 px-3 py-1.5 text-xs font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors"
            @click="handleReset"
          >
            <RotateCcw class="h-3.5 w-3.5" />
            {{ $t('admin_audit.reset') }}
          </button>
        </div>
      </template>

      <!-- Event List Content -->
      <template #content>
        <div v-if="store.isLoading && store.events.length === 0" class="flex justify-center py-12">
          <Loader2 class="h-8 w-8 animate-spin text-brand-500" />
        </div>

        <div v-else-if="store.events.length === 0" class="flex flex-col items-center justify-center py-16 text-slate-500">
          <ScrollText class="h-10 w-10 text-slate-300 dark:text-slate-600 mb-2" />
          <p class="text-sm font-semibold">{{ $t('admin_audit.empty_events') }}</p>
        </div>

        <div v-else class="space-y-2">
          <div
            v-for="evt in store.events"
            :key="evt.id"
            class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-xl border border-slate-200/80 dark:border-white/[0.06] bg-white dark:bg-canvas-subtle/40 p-3.5 hover:bg-slate-50 dark:hover:bg-white/[0.02] cursor-pointer transition-colors"
            @click="inspectEvent(evt)"
          >
            <div class="flex items-start gap-3 min-w-0">
              <!-- Outcome Icon Badge -->
              <div class="mt-0.5 shrink-0">
                <CheckCircle2 v-if="evt.outcome === 'Succeeded'" class="h-5 w-5 text-emerald-500" />
                <Clock3 v-else-if="evt.outcome === 'Accepted'" class="h-5 w-5 text-blue-500" />
                <AlertCircle v-else-if="evt.outcome === 'Rejected'" class="h-5 w-5 text-amber-500" />
                <XCircle v-else class="h-5 w-5 text-rose-500" />
              </div>

              <div class="min-w-0">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="font-bold text-sm text-slate-900 dark:text-white">{{ evt.action }}</span>
                  <span class="rounded bg-slate-100 dark:bg-white/10 px-2 py-0.5 text-xs text-slate-600 dark:text-slate-300 font-mono">
                    {{ evt.targetType }}: {{ evt.targetId }}
                  </span>
                  <span
                    class="rounded-full px-2 py-0.5 text-[11px] font-semibold"
                    :class="{
                      'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400': evt.outcome === 'Succeeded',
                      'bg-blue-500/10 text-blue-600 dark:text-blue-400': evt.outcome === 'Accepted',
                      'bg-amber-500/10 text-amber-600 dark:text-amber-400': evt.outcome === 'Rejected',
                      'bg-rose-500/10 text-rose-600 dark:text-rose-400': evt.outcome === 'Failed'
                    }"
                  >
                    {{ formatOutcome(evt.outcome) }}
                  </span>
                </div>

                <div v-if="evt.reason" class="text-xs text-slate-600 dark:text-slate-300 mt-1 truncate">
                  <strong>{{ $t('admin_audit.reason_label') }}:</strong> {{ evt.reason }}
                </div>
              </div>
            </div>

            <!-- Timestamp with explicit UTC timezone label -->
            <div class="shrink-0 text-xs text-slate-500 dark:text-slate-400 flex items-center gap-1 font-mono">
              <Clock class="h-3.5 w-3.5" />
              <span>{{ new Date(evt.occurredAt).toISOString().replace('T', ' ').substring(0, 19) }} UTC</span>
            </div>
          </div>
        </div>
      </template>

      <!-- Pagination -->
      <template #pagination>
        <div class="flex items-center justify-between w-full text-xs text-slate-500">
          <span>{{ $t('admin_audit.page') }} {{ store.page }} / {{ totalPages }}</span>
          <div class="flex gap-2">
            <button
              type="button"
              class="px-3 py-1.5 rounded-lg border border-slate-300 dark:border-white/10 disabled:opacity-40"
              :disabled="store.page <= 1 || store.isLoading"
              @click="handlePageChange(store.page - 1)"
            >
              {{ $t('admin_audit.prev') }}
            </button>
            <button
              type="button"
              class="px-3 py-1.5 rounded-lg border border-slate-300 dark:border-white/10 disabled:opacity-40"
              :disabled="store.page >= totalPages || store.isLoading"
              @click="handlePageChange(store.page + 1)"
            >
              {{ $t('admin_audit.next') }}
            </button>
          </div>
        </div>
      </template>
    </BoardLayout>

    <!-- Event Detail Modal -->
    <AppModal
      :open="detailModalOpen"
      :title="$t('admin_audit.detail_title')"
      @close="detailModalOpen = false"
    >
      <div v-if="store.selectedEvent" class="space-y-4 text-sm">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs bg-slate-50 dark:bg-canvas-subtle p-3 rounded-xl border border-slate-200 dark:border-white/10">
          <div>
            <span class="text-slate-400 block">{{ $t('admin_audit.action') }}</span>
            <span class="font-bold text-slate-900 dark:text-white">{{ store.selectedEvent.action }}</span>
          </div>
          <div>
            <span class="text-slate-400 block">{{ $t('admin_audit.outcome') }}</span>
            <span class="font-semibold">{{ formatOutcome(store.selectedEvent.outcome) }}</span>
          </div>
          <div>
            <span class="text-slate-400 block">{{ $t('admin_audit.actor') }}</span>
            <span class="font-mono text-slate-900 dark:text-white">{{ store.selectedEvent.actorEmail || store.selectedEvent.actorUserId }}</span>
          </div>
          <div>
            <span class="text-slate-400 block">{{ $t('admin_audit.timestamp') }}</span>
            <span class="font-mono">{{ new Date(store.selectedEvent.occurredAt).toUTCString() }}</span>
          </div>
          <div>
            <span class="text-slate-400 block">{{ $t('admin_audit.target') }}</span>
            <span class="font-mono">{{ store.selectedEvent.targetType }}: {{ store.selectedEvent.targetId }}</span>
          </div>
          <div v-if="store.selectedEvent.correlationId">
            <span class="text-slate-400 block">{{ $t('admin_audit.correlation_id') }}</span>
            <span class="font-mono">{{ store.selectedEvent.correlationId }}</span>
          </div>
        </div>

        <div v-if="store.selectedEvent.reason" class="p-3 bg-slate-50 dark:bg-canvas-subtle rounded-xl border border-slate-200 dark:border-white/10">
          <span class="text-xs text-slate-400 block mb-1 font-semibold">{{ $t('admin_audit.reason_label') }}</span>
          <p class="text-sm text-slate-900 dark:text-white">{{ store.selectedEvent.reason }}</p>
        </div>

        <div v-if="store.selectedEvent.errorCode" class="p-3 bg-amber-500/10 rounded-xl border border-amber-500/20 text-amber-800 dark:text-amber-200 text-xs">
          <strong>{{ $t('admin_audit.error_code') }}:</strong> {{ store.selectedEvent.errorCode }}
        </div>

        <div v-if="store.selectedEvent.changedFields?.length" class="space-y-1">
          <span class="text-xs text-slate-400 block font-semibold">{{ $t('admin_audit.changed_fields') }}</span>
          <div class="flex flex-wrap gap-1.5">
            <span
              v-for="field in store.selectedEvent.changedFields"
              :key="field"
              class="px-2 py-0.5 rounded bg-slate-200 dark:bg-white/10 text-xs font-mono"
            >
              {{ field }}
            </span>
          </div>
        </div>

        <div v-if="store.selectedEvent.statePayload && Object.keys(store.selectedEvent.statePayload).length > 0" class="space-y-1">
          <span class="text-xs text-slate-400 block font-semibold">{{ $t('admin_audit.state_payload') }}</span>
          <pre class="p-3 rounded-xl bg-slate-900 text-slate-100 text-xs font-mono overflow-x-auto">{{ JSON.stringify(store.selectedEvent.statePayload, null, 2) }}</pre>
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end">
          <button
            type="button"
            class="px-4 py-2 rounded-xl border border-slate-300 dark:border-white/10 text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]"
            @click="detailModalOpen = false"
          >
            {{ $t('admin_audit.close') }}
          </button>
        </div>
      </template>
    </AppModal>
  </div>
</template>
