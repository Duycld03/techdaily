<script setup lang="ts">
import { computed } from 'vue'
import { Search, Loader2 } from 'lucide-vue-next'
import AppSelect, { type SelectOption } from '~/components/common/AppSelect.vue'
import type { AdminCatalogDocument } from '~/stores/useCatalogStore'

const props = defineProps<{
  documents: AdminCatalogDocument[]
  selectedDocId: string | null
  isCreatingNewDraft: boolean
  isLoading: boolean
  search: string
  visibilityFilter: string
  statusFilter: string
}>()

const emit = defineEmits<{
  (e: 'update:search', value: string): void
  (e: 'update:visibilityFilter', value: string): void
  (e: 'update:statusFilter', value: string): void
  (e: 'filter-change'): void
  (e: 'select-document', id: string): void
}>()

const { t } = useI18n()

const searchValue = computed({
  get: () => props.search,
  set: (val: string) => emit('update:search', val)
})

const localVisibilityFilter = computed({
  get: () => props.visibilityFilter,
  set: (val: string) => emit('update:visibilityFilter', val)
})

const localStatusFilter = computed({
  get: () => props.statusFilter,
  set: (val: string) => emit('update:statusFilter', val)
})

const visibilityFilterOptions = computed<SelectOption[]>(() => [
  { value: '', label: t('admin_catalog.filters.all_visibility') },
  { value: 'Draft', label: t('admin_catalog.filters.visibility_draft') },
  { value: 'Published', label: t('admin_catalog.filters.visibility_published') },
  { value: 'Hidden', label: t('admin_catalog.filters.visibility_hidden') }
])

const statusFilterOptions = computed<SelectOption[]>(() => [
  { value: '', label: t('admin_catalog.filters.all_processing') },
  { value: 'Pending', label: t('admin_catalog.filters.status_pending') },
  { value: 'Processing', label: t('admin_catalog.filters.status_processing') },
  { value: 'Ready', label: t('admin_catalog.filters.status_ready') },
  { value: 'Failed', label: t('admin_catalog.filters.status_failed') }
])

function getVisibilityLabel(visibility: string) {
  switch (visibility) {
    case 'Published': return t('admin_catalog.filters.visibility_published')
    case 'Hidden': return t('admin_catalog.filters.visibility_hidden')
    case 'Draft': return t('admin_catalog.filters.visibility_draft')
    default: return visibility
  }
}

function getStatusLabel(status: string) {
  switch (status) {
    case 'Ready': return t('admin_catalog.filters.status_ready')
    case 'Processing': return t('admin_catalog.filters.status_processing')
    case 'Pending': return t('admin_catalog.filters.status_pending')
    case 'Failed': return t('admin_catalog.filters.status_failed')
    default: return status
  }
}
</script>

<template>
  <div class="space-y-3 flex flex-col h-full">
    <!-- Filter Controls -->
    <div class="space-y-2">
      <div class="relative">
        <Search class="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
        <input
          v-model="searchValue"
          @keyup.enter="$emit('filter-change')"
          type="text"
          :placeholder="$t('admin_catalog.filters.search_placeholder')"
          class="w-full pl-8 pr-2.5 py-1.5 text-xs sm:text-sm rounded-xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-200 placeholder-slate-400 focus:outline-none focus:border-brand-500 transition-colors"
        />
      </div>

      <div class="grid grid-cols-2 gap-1.5">
        <AppSelect
          v-model="localVisibilityFilter"
          :options="visibilityFilterOptions"
          size="sm"
          @change="$emit('filter-change')"
        />
        <AppSelect
          v-model="localStatusFilter"
          :options="statusFilterOptions"
          size="sm"
          @change="$emit('filter-change')"
        />
      </div>
    </div>

    <!-- Document List -->
    <div class="flex-1 overflow-y-auto space-y-1.5 pr-0.5 max-h-52 md:max-h-none min-h-[120px] md:min-h-[300px]">
      <div
        v-if="isLoading && documents.length === 0"
        class="py-12 text-center text-xs text-slate-400 space-y-2"
      >
        <Loader2 class="w-6 h-6 animate-spin mx-auto text-brand-500" />
        <p>{{ $t('admin_catalog.filters.loading_catalog') }}</p>
      </div>

      <div
        v-else-if="documents.length === 0"
        class="py-12 text-center text-xs sm:text-sm text-slate-400 italic"
      >
        {{ $t('admin_catalog.filters.no_documents_match') }}
      </div>

      <button
        v-for="doc in documents"
        :key="doc.id"
        type="button"
        @click="$emit('select-document', doc.id)"
        :class="[
          'w-full text-left p-3 rounded-2xl border transition-all space-y-1.5 block cursor-pointer',
          selectedDocId === doc.id && !isCreatingNewDraft
            ? 'bg-brand-500/10 border-brand-500/40 dark:bg-white/[0.08] shadow-sm'
            : 'bg-white dark:bg-canvas-subtle border-slate-200/70 dark:border-white/[0.06] hover:border-slate-300 dark:hover:border-white/[0.12]'
        ]"
      >
        <div class="flex items-center justify-between gap-1.5">
          <span :class="[
            'px-2 py-0.5 rounded-md text-[10px] font-bold uppercase tracking-wider border whitespace-nowrap shrink-0',
            doc.visibility === 'Published'
              ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20'
              : doc.visibility === 'Hidden'
              ? 'bg-slate-500/10 text-slate-600 dark:text-slate-400 border-slate-500/20'
              : 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20'
          ]">
            {{ getVisibilityLabel(doc.visibility) }}
          </span>

          <span class="text-[10px] font-mono font-semibold text-slate-400 whitespace-nowrap shrink-0">
            {{ $t('admin_catalog.filters.revision_short', { rev: doc.revision }) }}
          </span>
        </div>

        <h4 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white line-clamp-2 leading-snug">
          {{ doc.title }}
        </h4>

        <div class="flex items-center justify-between text-[11px] text-slate-500 dark:text-slate-400 pt-0.5">
          <span v-if="doc.processingStatus !== 'Ready'" class="flex items-center gap-1 min-w-0">
            <span :class="[
              'w-1.5 h-1.5 rounded-full shrink-0',
              doc.processingStatus === 'Failed' ? 'bg-rose-500' : 'bg-brand-500 animate-pulse'
            ]"></span>
            <span class="truncate">{{ getStatusLabel(doc.processingStatus) }}</span>
          </span>
          <span v-else></span>

          <span v-if="doc.totalSlices > 0" class="shrink-0 font-medium">
            {{ $t('admin_catalog.filters.slices_count', { count: doc.totalSlices }) }}
          </span>
        </div>
      </button>
    </div>
  </div>
</template>
