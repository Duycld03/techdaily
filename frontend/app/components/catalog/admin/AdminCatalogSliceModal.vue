<script setup lang="ts">
import AppModal from '~/components/ui/AppModal.vue'
import type { AdminCatalogSlice } from '~/stores/useCatalogStore'

defineProps<{
  open: boolean
  slice: AdminCatalogSlice | null
}>()

defineEmits<{
  (e: 'close'): void
}>()
</script>

<template>
  <AppModal
    :open="open"
    :title="$t('admin_catalog.modal.inspect_slice_title', { order: slice?.sliceOrder || '' })"
    max-width="max-w-3xl"
    @close="$emit('close')"
  >
    <div v-if="slice" class="space-y-4 text-xs sm:text-sm">
      <div class="flex items-center justify-between border-b border-slate-100 dark:border-white/[0.06] pb-2 gap-2">
        <h4 class="font-bold text-slate-900 dark:text-white truncate">
          {{ slice.chapterTitle }}
        </h4>
        <span class="text-slate-500 dark:text-slate-400 font-mono text-xs whitespace-nowrap shrink-0">
          {{ $t('admin_catalog.modal.reading_time_approx', { minutes: slice.estimatedReadMinutes }) }}
        </span>
      </div>

      <div v-if="slice.summaryMarkdown" class="space-y-1.5">
        <span class="font-bold text-slate-700 dark:text-slate-300">
          {{ $t('admin_catalog.modal.executive_summary') }}
        </span>
        <p class="p-3 rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/60 dark:border-white/[0.06] leading-relaxed">
          {{ slice.summaryMarkdown }}
        </p>
      </div>

      <div class="space-y-1.5">
        <span class="font-bold text-slate-700 dark:text-slate-300">
          {{ $t('admin_catalog.modal.source_markdown') }}
        </span>
        <div class="p-3 rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/60 dark:border-white/[0.06] max-h-80 overflow-y-auto font-mono text-xs whitespace-pre-wrap leading-relaxed">
          {{ slice.originalTextMarkdown }}
        </div>
      </div>
    </div>

    <template #footer>
      <button
        type="button"
        @click="$emit('close')"
        class="px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0 cursor-pointer"
      >
        {{ $t('admin_catalog.modal.close') }}
      </button>
    </template>
  </AppModal>
</template>
