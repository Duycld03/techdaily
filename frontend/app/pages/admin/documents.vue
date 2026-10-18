<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  FileCog,
  Search,
  RefreshCw,
  RotateCcw,
  Clock,
  Loader2,
  AlertCircle
} from 'lucide-vue-next'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useAdminDocumentsStore } from '~/stores/useAdminDocumentsStore'
import type { AdminDocumentInventoryItem } from '~/stores/useAdminDocumentsStore'
import { useCategoryStore } from '~/stores/useCategoryStore'
import type { components } from '~/types/api.generated'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import AppModal from '~/components/ui/AppModal.vue'
import { useToast } from '~/composables/useToast'

definePageMeta({
  layout: 'admin'
})

const access = useAdminAccessStore()
const store = useAdminDocumentsStore()
const categoryStore = useCategoryStore()
const toast = useToast()
const { t, locale } = useI18n()
const searchInput = ref('')
const selectedScope = ref<'Personal' | 'Catalog' | ''>('')
const sourceTypeKeys: Record<components['schemas']['SourceType'], string> = {
  PdfBook: 'admin_documents.source_PdfBook',
  MarkdownSeries: 'admin_documents.source_MarkdownSeries',
  WebDocUrl: 'admin_documents.source_WebDocUrl'
}
const selectedStatus = ref('')
const selectedId = ref<string | null>(null)

// Retry modal state
const isRetryModalOpen = ref(false)
const retryReason = ref('')
const retryError = ref<string | null>(null)

onMounted(async () => {
  if (access.canManageDocumentOperations) {
    await store.fetchDocuments()
    const first = store.documents[0]
    if (first && !selectedId.value) {
      handleSelect(first)
    }
  }
})

function handleSearch() {
  store.search = searchInput.value
  store.page = 1
  store.fetchDocuments()
}

function handleScopeFilter(scope: 'Personal' | 'Catalog' | '') {
  selectedScope.value = scope
  store.scopeFilter = scope || null
  store.page = 1
  store.fetchDocuments()
}

function handleStatusFilter(status: string) {
  selectedStatus.value = status
  store.statusFilter = status || null
  store.page = 1
  store.fetchDocuments()
}

function handlePageChange(newPage: number) {
  store.page = newPage
  store.fetchDocuments()
}

async function handleSelect(doc: AdminDocumentInventoryItem) {
  selectedId.value = doc.id
  await store.selectDocument(doc.id, doc.scope)
}

function openRetryModal() {
  isRetryModalOpen.value = true
  retryReason.value = ''
  retryError.value = null
}

function closeRetryModal() {
  isRetryModalOpen.value = false
  retryReason.value = ''
  retryError.value = null
}

async function submitRetry() {
  if (!retryReason.value.trim()) {
    retryError.value = t('admin_documents.retry_reason_required')
    return
  }

  retryError.value = null
  try {
    const res = await store.retryDocument(retryReason.value.trim())
    if (res) {
      toast.success(t('admin_documents.retry_success'))
      closeRetryModal()
    } else {
      retryError.value = t('admin_documents.retry_error')
    }
  } catch (err: unknown) {
    retryError.value = err instanceof Error ? err.message : t('admin_documents.retry_error')
  }
}

const totalPages = computed(() => Math.ceil(store.totalCount / store.pageSize) || 1)

function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}
function formatDocumentStatus(status: string): string {
  switch (status.toLowerCase()) {
    case 'ready':
      return t('admin_documents.status_ready')
    case 'failed':
      return t('admin_documents.status_failed')
    case 'processing':
    case 'running':
      return t('admin_documents.status_processing')
    case 'queued':
    case 'pending':
      return t('admin_documents.status_queued')
    default:
      return status
  }
}

function formatScope(scope: string): string {
  if (scope.toLowerCase() === 'personal') return t('admin_documents.scope_personal')
  if (scope.toLowerCase() === 'catalog') return t('admin_documents.scope_catalog')
  return scope
}

function formatOwner(doc: AdminDocumentInventoryItem): string {
  return doc.scope === 'Catalog'
    ? t('admin_documents.system_catalog')
    : doc.ownerName || t('admin_documents.owner_unknown')
}

function formatCategory(doc: AdminDocumentInventoryItem): string {
  return categoryStore.getCategoryName(doc.category, locale.value) || t('admin_documents.category_unavailable')
}

function formatSourceType(sourceType: string): string {
  return Object.hasOwn(sourceTypeKeys, sourceType)
    ? t(sourceTypeKeys[sourceType as components['schemas']['SourceType']])
    : t('admin_documents.source_unknown')
}

function formatSourceAvailability(availability: string): string {
  switch (availability.toLowerCase()) {
    case 'missing':
      return t('admin_documents.source_missing')
    case 'available':
      return t('admin_documents.source_available')
    case 'permanent':
      return t('admin_documents.source_permanent')
    case 'temporary':
      return t('admin_documents.source_temporary')
    default:
      return availability
  }
}

function formatIneligibleReason(code?: string | null, fallback?: string | null): string {
  if (code) {
    const key = `admin_documents.ineligible_${code}`
    const translated = t(key)
    if (translated && translated !== key) return translated
  }
  return fallback || t('admin_documents.retry_ineligible')
}


function stateBadgeClass(state: string): string {
  const s = state.toLowerCase()
  if (s === 'ready' || s === 'succeeded') return 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-400 border-emerald-500/20'
  if (s === 'failed') return 'bg-rose-500/10 text-rose-700 dark:text-rose-400 border-rose-500/20'
  if (s === 'processing' || s === 'running') return 'bg-amber-500/10 text-amber-700 dark:text-amber-400 border-amber-500/20'
  if (s === 'queued') return 'bg-sky-500/10 text-sky-700 dark:text-sky-400 border-sky-500/20'
  return 'bg-slate-500/10 text-slate-700 dark:text-slate-400 border-slate-500/20'
}
</script>

<template>
  <div v-if="access.canManageDocumentOperations" class="h-[calc(100vh-3.5rem)] sm:h-[calc(100vh-3.75rem)] flex flex-col overflow-hidden bg-slate-50 dark:bg-canvas">
    <MasterDetailLayout flush nav-class="md:w-84 lg:w-96" class="flex-1 min-h-0" :nav-label="$t('admin_documents.nav_label')" :content-label="$t('admin_documents.title')">
      <!-- Header -->
      <template #header>
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-2.5">
            <FileCog class="h-5 w-5 text-brand-600 dark:text-brand-400 shrink-0" aria-hidden="true" />
            <h1 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white whitespace-nowrap">
              {{ $t('admin_documents.title') }}
            </h1>
            <span class="rounded-full bg-slate-200 dark:bg-white/10 px-2.5 py-0.5 text-xs font-semibold text-slate-600 dark:text-slate-300 shrink-0">
              {{ store.totalCount }}
            </span>
          </div>
          <button
            type="button"
            class="flex items-center gap-1.5 rounded-xl border border-slate-300 dark:border-white/10 px-3 py-1.5 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0"
            :aria-label="$t('admin_documents.refresh')"
            :disabled="store.isLoading"
            @click="store.fetchDocuments(true)"
          >
            <RefreshCw class="h-4 w-4 shrink-0" :class="{ 'animate-spin': store.isLoading }" aria-hidden="true" />
            <span class="hidden sm:inline">{{ $t('admin_documents.refresh') }}</span>
          </button>
        </div>
      </template>

      <!-- Nav Rail (Document Inventory) -->
      <template #nav>
        <div class="flex flex-col gap-3 h-full">
          <!-- Search & Filter bar -->
          <div class="space-y-2">
            <div class="relative">
              <input
                v-model="searchInput"
                type="text"
                :placeholder="$t('admin_documents.search_placeholder')"
                :aria-label="$t('admin_documents.search_placeholder')"
                class="w-full rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle/50 pl-9 pr-3 py-2 text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                @keydown.enter="handleSearch"
              />
              <Search class="absolute left-3 top-2.5 h-4 w-4 text-slate-400" aria-hidden="true" />
            </div>

            <!-- Scope Filter Pills -->
            <div class="flex gap-1.5 overflow-x-auto pb-1">
              <button
                type="button"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedScope === '' ? 'bg-brand-500 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="handleScopeFilter('')"
              >
                {{ $t('admin_documents.filter_all') }}
              </button>
              <button
                type="button"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedScope === 'Personal' ? 'bg-brand-600 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="handleScopeFilter('Personal')"
              >
                {{ $t('admin_documents.filter_personal') }}
              </button>
              <button
                type="button"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedScope === 'Catalog' ? 'bg-brand-600 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="handleScopeFilter('Catalog')"
              >
                {{ $t('admin_documents.filter_catalog') }}
              </button>
            </div>

            <!-- Status Filter Pills -->
            <p class="text-xs font-semibold text-slate-500">{{ $t('admin_documents.processing_status') }}</p>
            <div role="group" :aria-label="$t('admin_documents.processing_status')" class="flex gap-1.5 overflow-x-auto pb-1">
              <button
                type="button"
                class="px-2 py-0.5 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedStatus === '' ? 'bg-slate-800 text-white dark:bg-white/20' : 'bg-slate-100 dark:bg-white/5 text-slate-500'"
                @click="handleStatusFilter('')"
              >
                {{ $t('admin_documents.status_all') }}
              </button>
              <button
                type="button"
                class="px-2 py-0.5 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedStatus === 'Ready' ? 'bg-emerald-600 text-white' : 'bg-slate-100 dark:bg-white/5 text-slate-500'"
                @click="handleStatusFilter('Ready')"
              >
                {{ $t('admin_documents.status_ready') }}
              </button>
              <button
                type="button"
                class="px-2 py-0.5 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedStatus === 'Failed' ? 'bg-rose-600 text-white' : 'bg-slate-100 dark:bg-white/5 text-slate-500'"
                @click="handleStatusFilter('Failed')"
              >
                {{ $t('admin_documents.status_failed') }}
              </button>
              <button
                type="button"
                class="px-2 py-0.5 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedStatus === 'Processing' ? 'bg-amber-600 text-white' : 'bg-slate-100 dark:bg-white/5 text-slate-500'"
                @click="handleStatusFilter('Processing')"
              >
                {{ $t('admin_documents.status_processing') }}
              </button>
            </div>
          </div>

          <!-- Document List -->
          <div class="flex-1 overflow-y-auto space-y-2 min-h-0 pr-1">
            <div v-if="store.isLoading && store.documents.length === 0" class="flex justify-center py-8">
              <Loader2 class="h-6 w-6 animate-spin text-brand-500" />
            </div>

            <div v-else-if="store.documents.length === 0" class="text-center py-8 text-sm text-slate-500 dark:text-slate-400">
              {{ $t('admin_documents.empty_list') }}
            </div>

            <button
              v-for="doc in store.documents"
              :key="doc.id"
              type="button"
              class="w-full text-left p-3 rounded-xl border transition-all flex flex-col gap-1.5"
              :class="selectedId === doc.id
                ? 'bg-brand-50/50 dark:bg-brand-500/10 border-brand-500/30 ring-1 ring-brand-500/20'
                : 'bg-white dark:bg-canvas-subtle/30 border-slate-200 dark:border-white/5 hover:border-slate-300 dark:hover:border-white/10'"
              @click="handleSelect(doc)"
            >
              <div class="flex flex-col items-start gap-1.5 min-w-0">
                <span class="text-sm sm:text-base font-semibold text-slate-900 dark:text-white line-clamp-1">
                  {{ doc.title }}
                </span>
                <span
                  class="rounded-full px-2 py-0.5 text-xs font-semibold border whitespace-nowrap shrink-0"
                  :class="stateBadgeClass(doc.status)"
                >
                  {{ $t('admin_documents.processing_status') }}: {{ formatDocumentStatus(doc.status) }}
                </span>
              </div>

              <span v-if="doc.scope === 'Catalog' && doc.visibility" class="text-xs font-semibold text-slate-600 dark:text-slate-300">
                {{ $t('admin_documents.publication') }}: {{ $t(`admin_documents.visibility_${doc.visibility}`) }}
              </span>
              <div class="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400">
                <span class="rounded bg-slate-100 dark:bg-white/10 px-1.5 py-0.5 font-medium whitespace-nowrap shrink-0">
                  {{ formatScope(doc.scope) }}
                </span>
                <span class="truncate">
                  {{ formatOwner(doc) }}
                </span>
              </div>
            </button>
          </div>

          <!-- Pagination -->
          <div v-if="totalPages > 1" class="flex items-center justify-between border-t border-slate-200 dark:border-white/10 pt-2 text-xs text-slate-500 dark:text-slate-400">
            <span>{{ store.page }} / {{ totalPages }}</span>
            <div class="flex gap-1">
              <button
                type="button"
                class="px-2 py-1 rounded border border-slate-300 dark:border-white/10 disabled:opacity-40 whitespace-nowrap shrink-0"
                :disabled="store.page <= 1"
                @click="handlePageChange(store.page - 1)"
              >
                {{ $t('admin_documents.prev') }}
              </button>
              <button
                type="button"
                class="px-2 py-1 rounded border border-slate-300 dark:border-white/10 disabled:opacity-40 whitespace-nowrap shrink-0"
                :disabled="store.page >= totalPages"
                @click="handlePageChange(store.page + 1)"
              >
                {{ $t('admin_documents.next') }}
              </button>
            </div>
          </div>
        </div>
      </template>

      <!-- Content (Document Detail & Attempts) -->
      <template #content>
        <div v-if="store.isLoadingDetail" class="flex items-center justify-center h-full py-16">
          <Loader2 class="h-8 w-8 animate-spin text-brand-500" />
        </div>

        <div v-else-if="store.detailError" class="flex flex-col items-center justify-center h-full py-16 text-rose-500">
          <AlertCircle class="h-12 w-12 mb-3 text-rose-500" aria-hidden="true" />
          <p class="text-sm sm:text-base font-semibold">{{ store.detailError }}</p>
        </div>

        <div v-else-if="!store.selectedDocument" class="flex flex-col items-center justify-center h-full py-16 text-slate-400">
          <FileCog class="h-12 w-12 mb-3 opacity-20" aria-hidden="true" />
          <p class="text-sm sm:text-base">{{ $t('admin_documents.select_hint') }}</p>
        </div>

        <div v-else class="p-4 sm:p-6 space-y-6 max-w-5xl">
          <!-- Header and Title -->
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 dark:border-white/10 pb-5">
            <div>
              <div class="flex items-center gap-2 mb-1.5 flex-wrap">
                <span class="rounded bg-brand-500/10 text-brand-700 dark:text-brand-300 px-2 py-0.5 text-xs font-semibold border border-brand-500/20 whitespace-nowrap shrink-0">
                  {{ formatScope(store.selectedDocument.scope) }}
                </span>
                <span class="rounded-full px-2.5 py-0.5 text-xs font-semibold border whitespace-nowrap shrink-0" :class="stateBadgeClass(store.selectedDocument.status)">
                  {{ $t('admin_documents.processing_status') }}: {{ formatDocumentStatus(store.selectedDocument.status) }}
                </span>
                <span v-if="store.selectedDocument.scope === 'Catalog' && store.selectedDocument.visibility" class="rounded-full px-2.5 py-0.5 text-xs font-semibold border border-slate-300 dark:border-white/20 text-slate-700 dark:text-slate-200">
                  {{ $t('admin_documents.publication') }}: {{ $t(`admin_documents.visibility_${store.selectedDocument.visibility}`) }}
                </span>
                <span class="text-xs text-slate-400 font-mono">
                  v{{ store.selectedDocument.controlVersion }} (gen {{ store.selectedDocument.currentAttemptGeneration }})
                </span>
              </div>
              <h2 class="text-lg sm:text-xl font-bold text-slate-900 dark:text-white">
                {{ store.selectedDocument.title }}
              </h2>
            </div>

            <!-- Retry Action Button -->
            <div class="flex items-center gap-3">
              <button
                v-if="store.selectedDocument.isRetryEligible"
                type="button"
                class="flex items-center gap-2 rounded-xl bg-brand-600 hover:bg-brand-700 text-white px-4 py-2 text-sm sm:text-base font-semibold transition-colors shadow-sm whitespace-nowrap shrink-0"
                @click="openRetryModal"
              >
                <RotateCcw class="h-4 w-4 shrink-0" aria-hidden="true" />
                {{ $t('admin_documents.retry_button') }}
              </button>
              <div
                v-else
                class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-100 dark:bg-white/5 text-xs text-slate-500 dark:text-slate-400 border border-slate-200 dark:border-white/10"
              >
                <AlertCircle class="h-4 w-4 text-amber-500 shrink-0" />
                <span>{{ formatIneligibleReason(store.selectedDocument.ineligibleReasonCode, store.selectedDocument.ineligibleReason) }}</span>
              </div>
            </div>
          </div>

          <!-- Metadata Grid -->
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <div class="p-3.5 rounded-xl border border-slate-200 dark:border-white/10 bg-white dark:bg-canvas-subtle/50 space-y-1">
              <span class="text-xs text-slate-400 block">{{ $t('admin_documents.owner') }}</span>
              <p class="text-sm font-semibold text-slate-900 dark:text-white truncate">
                {{ formatOwner(store.selectedDocument) }}
              </p>
              <p v-if="store.selectedDocument.ownerEmail" class="text-xs text-slate-500 truncate">
                {{ store.selectedDocument.ownerEmail }}
              </p>
            </div>

            <div class="p-3.5 rounded-xl border border-slate-200 dark:border-white/10 bg-white dark:bg-canvas-subtle/50 space-y-1">
              <span class="text-xs text-slate-400 block">{{ $t('admin_documents.category') }} / {{ $t('admin_documents.language') }}</span>
              <p class="text-sm font-semibold text-slate-900 dark:text-white">
                {{ formatCategory(store.selectedDocument) }} ({{ store.selectedDocument.language }})
              </p>
              <p class="text-xs text-slate-500">
                {{ formatSourceType(store.selectedDocument.sourceType) }} &bull; {{ formatSourceAvailability(store.selectedDocument.sourceAvailability) }}
              </p>
            </div>

            <div class="p-3.5 rounded-xl border border-slate-200 dark:border-white/10 bg-white dark:bg-canvas-subtle/50 space-y-1">
              <span class="text-xs text-slate-400 block">{{ $t('admin_documents.timestamps_heading') }}</span>
              <p class="text-xs text-slate-700 dark:text-slate-300">
                {{ $t('admin_documents.created_label') }}: {{ formatDate(store.selectedDocument.createdAt) }}
              </p>
              <p class="text-xs text-slate-500">
                {{ $t('admin_documents.observed_label') }}: {{ formatDate(store.selectedDocument.observedAt) }}
              </p>
            </div>
          </div>

          <!-- Ingestion Attempts History -->
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <h3 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white flex items-center gap-2">
                <Clock class="h-5 w-5 text-brand-600 dark:text-brand-400 shrink-0" />
                {{ $t('admin_documents.attempts_history') }}
                <span class="text-xs font-normal text-slate-500">({{ store.attemptsTotalCount }})</span>
              </h3>
            </div>

            <div v-if="store.isLoadingAttempts" class="flex justify-center py-6">
              <Loader2 class="h-6 w-6 animate-spin text-brand-500" />
            </div>
            <div v-else-if="store.attemptsError" class="p-4 rounded-xl border border-rose-200 dark:border-rose-500/20 bg-rose-50 dark:bg-rose-500/10 text-xs text-rose-700 dark:text-rose-300">
              {{ store.attemptsError }}
            </div>


            <div v-else-if="store.attempts.length === 0" class="text-center py-8 text-sm text-slate-500 dark:text-slate-400 border border-dashed border-slate-200 dark:border-white/10 rounded-xl">
              {{ $t('admin_documents.no_attempts') }}
            </div>

            <div v-else class="space-y-2.5">
              <div
                v-for="att in store.attempts"
                :key="att.attemptId"
                class="p-4 rounded-xl border border-slate-200 dark:border-white/10 bg-white dark:bg-canvas-subtle/40 space-y-2"
              >
                <div class="flex items-center justify-between gap-2 flex-wrap">
                  <div class="flex items-center gap-2">
                    <span class="text-xs font-mono font-semibold text-slate-700 dark:text-slate-300">
                      {{ att.attemptId.slice(0, 8) }}...
                    </span>
                    <span class="rounded bg-slate-100 dark:bg-white/10 px-2 py-0.5 text-xs font-medium whitespace-nowrap shrink-0">
                      {{ att.origin }} &bull; {{ att.stage }}
                    </span>
                  </div>
                  <span class="rounded-full px-2.5 py-0.5 text-xs font-semibold border whitespace-nowrap shrink-0" :class="stateBadgeClass(att.state)">
                    {{ formatDocumentStatus(att.state) }} ({{ att.progressPercent }}%)
                  </span>
                </div>

                <div class="text-xs text-slate-500 flex gap-4 flex-wrap">
                  <span>{{ $t('admin_documents.queued_label') }}: {{ formatDate(att.queuedAtUtc) }}</span>
                  <span v-if="att.startedAtUtc">{{ $t('admin_documents.started_label') }}: {{ formatDate(att.startedAtUtc) }}</span>
                  <span v-if="att.endedAtUtc">{{ $t('admin_documents.ended_label') }}: {{ formatDate(att.endedAtUtc) }}</span>
                </div>

                <div v-if="att.safeErrorCode || att.safeErrorGuidance" class="mt-2 p-2.5 rounded-lg bg-rose-50 dark:bg-rose-500/10 border border-rose-200 dark:border-rose-500/20 text-xs text-rose-700 dark:text-rose-300">
                  <span class="font-semibold">{{ att.safeErrorCode }}:</span> {{ att.safeErrorGuidance }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </MasterDetailLayout>

    <!-- Retry Confirmation Modal -->
    <AppModal
      :open="isRetryModalOpen"
      :title="$t('admin_documents.retry_modal_title')"
      @close="closeRetryModal"
    >
      <div class="space-y-4">
        <p class="text-sm text-slate-600 dark:text-slate-300">
          {{ $t('admin_documents.retry_modal_desc') }}
        </p>

        <div v-if="store.selectedDocument" class="p-3 rounded-xl bg-slate-100 dark:bg-white/5 border border-slate-200 dark:border-white/10 text-xs space-y-1">
          <p><span class="text-slate-400">{{ $t('admin_documents.detail_title') }}:</span> <strong class="text-slate-800 dark:text-slate-200">{{ store.selectedDocument.title }}</strong></p>
          <p><span class="text-slate-400">{{ $t('admin_documents.scope') }}:</span> {{ formatScope(store.selectedDocument.scope) }} &bull; <span class="text-slate-400">{{ $t('admin_documents.control_version') }}:</span> {{ store.selectedDocument.controlVersion }}</p>
        </div>

        <div class="space-y-1.5">
          <label for="retry-reason-input" class="block text-xs font-semibold text-slate-700 dark:text-slate-200">
            {{ $t('admin_documents.retry_reason_label') }} *
          </label>
          <input
            id="retry-reason-input"
            v-model="retryReason"
            type="text"
            :placeholder="$t('admin_documents.retry_reason_placeholder')"
            class="w-full rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle p-3 text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            @keydown.enter="submitRetry"
          />
        </div>

        <div v-if="retryError" class="p-3 rounded-xl bg-rose-50 dark:bg-rose-500/10 border border-rose-200 dark:border-rose-500/20 text-xs text-rose-700 dark:text-rose-300">
          {{ retryError }}
        </div>

      </div>

      <template #footer>
        <button
          type="button"
          class="px-4 py-2 text-sm font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/5 rounded-xl transition-colors whitespace-nowrap shrink-0 cursor-pointer"
          @click="closeRetryModal"
        >
          {{ $t('admin_documents.retry_cancel') }}
        </button>
        <button
          type="button"
          class="flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-xl transition-colors shadow-sm disabled:opacity-50 whitespace-nowrap shrink-0 cursor-pointer"
          :disabled="store.isRetrying || !retryReason.trim()"
          @click="submitRetry"
        >
          <Loader2 v-if="store.isRetrying" class="h-4 w-4 animate-spin shrink-0" />
          <span>{{ $t('admin_documents.retry_confirm') }}</span>
        </button>
      </template>
    </AppModal>
  </div>
</template>
