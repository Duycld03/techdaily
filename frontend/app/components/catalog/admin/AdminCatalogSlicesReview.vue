<script setup lang="ts">
import { Layers, Clock, Eye, Loader2 } from 'lucide-vue-next'
import type { AdminCatalogDocument } from '~/stores/useCatalogStore'

defineProps<{
  document: AdminCatalogDocument
  isPreparingSlices: boolean
}>()

defineEmits<{
  (e: 'prepare-slices'): void
  (e: 'inspect-slice', order: number): void
}>()
</script>

<template>
  <div class="p-6 rounded-3xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] shadow-sm space-y-4">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 dark:border-white/[0.06] pb-3">
      <div>
        <h3 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white flex items-center gap-2">
          <Layers class="w-4 h-4 text-brand-500 shrink-0" />
          <span>{{ $t('admin_catalog.slices_review') }}</span>
        </h3>
        <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-0.5">
          <span v-if="document.totalSlices > 0 && document.processingStatus === 'Ready'">
            {{ $t('admin_catalog.slices_ready_summary', { total: document.totalSlices }) }}
          </span>
          <span v-else>
            {{ $t('admin_catalog.document.slices_extracted_summary', { total: document.totalSlices }) }}
          </span>
        </p>
      </div>

      <div class="flex flex-wrap items-center gap-2.5 shrink-0">
        <!-- Prepare Initial Slices Action: only rendered when slices are unextracted / document not ready -->
        <button
          v-if="document.processingStatus !== 'Ready'"
          type="button"
          :disabled="isPreparingSlices || document.processingStatus === 'Processing'"
          @click="$emit('prepare-slices')"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs sm:text-sm font-semibold rounded-xl bg-brand-500/10 hover:bg-brand-500/20 text-brand-600 dark:text-brand-400 border border-brand-500/20 transition-all active:scale-95 whitespace-nowrap shrink-0 disabled:opacity-50 cursor-pointer"
        >
          <Loader2 v-if="isPreparingSlices" class="w-4 h-4 animate-spin shrink-0" />
          <Clock v-else class="w-4 h-4 shrink-0" />
          <span class="whitespace-nowrap">{{ isPreparingSlices ? $t('admin_catalog.preparing') : $t('admin_catalog.prepare_action') }}</span>
        </button>
      </div>
    </div>

    <!-- Slices Table / List -->
    <div class="space-y-1.5 max-h-72 overflow-y-auto pr-1">
      <div
        v-for="order in document.totalSlices"
        :key="order"
        class="flex items-center justify-between p-3 rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/60 dark:border-white/[0.04] text-xs sm:text-sm"
      >
        <div class="flex items-center gap-2.5 min-w-0 pr-3">
          <span class="w-6 h-6 rounded-full bg-slate-200 dark:bg-white/[0.08] text-slate-700 dark:text-slate-300 flex items-center justify-center font-mono font-bold text-xs shrink-0">
            {{ order }}
          </span>
          <span class="font-semibold text-slate-800 dark:text-slate-200 truncate">
            {{ $t('admin_catalog.document.slice_default_title', { order }) }}
          </span>
        </div>

        <div class="flex items-center gap-3 shrink-0">

          <button
            type="button"
            @click="$emit('inspect-slice', order)"
            class="p-1 text-slate-500 hover:text-brand-500 transition-colors cursor-pointer shrink-0"
            :title="$t('admin_catalog.document.inspect_slice_tooltip')"
          >
            <Eye class="w-4 h-4 shrink-0" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
