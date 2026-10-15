<script setup lang="ts">
import { computed } from 'vue'
import { BookOpen, Plus, ExternalLink, Layers, CheckCircle2, Loader2, Clock, Globe } from 'lucide-vue-next'
import AppModal from '~/components/ui/AppModal.vue'
import type { LearnerCatalogDocumentDetailDto } from '~/stores/useCatalogStore'

const props = defineProps<{
  open: boolean
  document: LearnerCatalogDocumentDetailDto | null
  isAdopting?: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'adopt', documentId: string): void
}>()

const { t } = useI18n()

function parseCategory(val: unknown): number | undefined {
  if (typeof val === 'number' && !isNaN(val)) return val
  if (typeof val === 'string') {
    const parsed = parseInt(val, 10)
    if (!isNaN(parsed)) return parsed
    const nameMap: Record<string, number> = {
      frontendweb: 0,
      frontend: 0,
      backendruntime: 1,
      backend: 1,
      databasestorage: 2,
      database: 2,
      systemdesign: 3,
      engineeringcraft: 4,
      craft: 4,
      mentalmodels: 5,
      habitsproductivity: 6,
      habits: 6
    }
    const lower = val.toLowerCase().replace(/[^a-z0-9]/g, '')
    if (lower in nameMap) return nameMap[lower]
  }
  return undefined
}

const categoryLabel = computed(() => {
  if (!props.document) return ''
  const cat = parseCategory(props.document.category)
  switch (cat) {
    case 0: return t('library.categories.frontend')
    case 1: return t('library.categories.backend')
    case 2: return t('library.categories.database')
    case 3: return t('library.categories.system_design')
    case 4: return t('library.categories.craft')
    case 5: return t('library.categories.mental_models')
    case 6: return t('library.categories.habits')
    default: return t('library.categories.all')
  }
})

const isExternalUrl = computed(() => {
  if (!props.document?.authorOrSourceUrl) return false
  return props.document.authorOrSourceUrl.startsWith('http://') || props.document.authorOrSourceUrl.startsWith('https://')
})
</script>

<template>
  <AppModal
    :open="open"
    :title="$t('library.discover.preview_title')"
    maxWidth="max-w-4xl"
    @close="$emit('close')"
  >
    <div v-if="document" class="space-y-3.5">
      <!-- Document Summary Header -->
      <div class="space-y-1.5 p-3 rounded-2xl bg-slate-50 dark:bg-canvas-subtle/80 border border-slate-200/80 dark:border-white/[0.08]">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div class="flex flex-wrap items-center gap-2">
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 whitespace-nowrap shrink-0">
              {{ categoryLabel }}
            </span>
            <span class="px-2 py-0.5 rounded-md text-[11px] font-bold tracking-wider bg-slate-100 dark:bg-white/[0.06] text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-white/[0.08] shrink-0">
              {{ (document.language || 'en').toUpperCase() }}
            </span>
          </div>

          <div class="flex items-center gap-3 text-xs text-slate-500 dark:text-slate-400">
            <span class="flex items-center gap-1 shrink-0">
              <Layers class="w-3.5 h-3.5 text-slate-400 shrink-0" />
              <span>{{ $t('library.slices_total', { count: document.totalSlices }) }}</span>
            </span>
            <span v-if="document.estimatedReadMinutes" class="flex items-center gap-1 shrink-0">
              <Clock class="w-3.5 h-3.5 text-slate-400 shrink-0" />
              <span>~{{ document.estimatedReadMinutes }} min</span>
            </span>
          </div>
        </div>

        <h2 class="text-lg sm:text-xl font-bold text-slate-900 dark:text-white leading-tight">
          {{ document.title }}
        </h2>

        <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed">
          {{ document.description }}
        </p>

        <!-- Attribution and Canonical Link -->
        <div v-if="document.attributionText || document.authorOrSourceUrl" class="pt-2 text-xs text-slate-500 dark:text-slate-400 flex flex-wrap items-center gap-2 border-t border-slate-200/60 dark:border-white/[0.04]">
          <span class="font-medium text-slate-700 dark:text-slate-300 shrink-0">{{ $t('library.discover.attribution') }}:</span>
          <span class="italic">{{ document.attributionText || document.authorOrSourceUrl }}</span>
          <a
            v-if="isExternalUrl"
            :href="document.authorOrSourceUrl!"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-1 text-brand-600 dark:text-brand-400 hover:underline shrink-0"
          >
            <span>{{ $t('library.discover.source_link') }}</span>
            <ExternalLink class="w-3 h-3 shrink-0" />
          </a>
        </div>
      </div>

      <!-- Preview Split Columns: TOC + First Slice -->
      <div class="grid grid-cols-1 md:grid-cols-12 gap-4">
        <!-- Table of Contents -->
        <div class="md:col-span-5 space-y-2 border border-slate-200/80 dark:border-white/[0.08] rounded-2xl p-3 bg-white dark:bg-canvas-subtle">
          <div class="flex items-center justify-between border-b border-slate-100 dark:border-white/[0.06] pb-2">
            <h3 class="text-sm font-bold text-slate-900 dark:text-white flex items-center gap-1.5">
              <Layers class="w-4 h-4 text-brand-500 shrink-0" />
              <span class="whitespace-nowrap shrink-0">{{ $t('library.discover.toc') }}</span>
            </h3>
            <span class="text-xs text-slate-500 font-medium shrink-0">{{ document.tableOfContents.length }} slices</span>
          </div>

          <div class="max-h-52 overflow-y-auto space-y-1.5 pr-1 text-xs">
            <div
              v-for="item in document.tableOfContents"
              :key="item.sliceOrder"
              class="flex items-center justify-between p-2 rounded-xl bg-slate-50 dark:bg-white/[0.03] border border-slate-100 dark:border-white/[0.04] text-slate-700 dark:text-slate-300"
            >
              <div class="flex items-center gap-2 min-w-0 pr-2">
                <span class="w-5 h-5 rounded-full bg-slate-200 dark:bg-white/[0.1] text-slate-700 dark:text-slate-300 flex items-center justify-center text-[10px] font-bold shrink-0">
                  {{ item.sliceOrder }}
                </span>
                <span class="truncate font-medium">{{ item.chapterTitle || `Slice ${item.sliceOrder}` }}</span>
              </div>
              <span v-if="item.estimatedReadMinutes" class="text-[11px] text-slate-400 shrink-0">
                ~{{ item.estimatedReadMinutes }}m
              </span>
            </div>
          </div>
        </div>

        <!-- First Slice Preview -->
        <div class="md:col-span-7 space-y-2 border border-slate-200/80 dark:border-white/[0.08] rounded-2xl p-3 bg-white dark:bg-canvas-subtle flex flex-col justify-between">
          <div class="space-y-2">
            <div class="flex items-center justify-between border-b border-slate-100 dark:border-white/[0.06] pb-2">
              <h3 class="text-sm font-bold text-slate-900 dark:text-white flex items-center gap-1.5">
                <BookOpen class="w-4 h-4 text-brand-500 shrink-0" />
                <span class="whitespace-nowrap shrink-0">{{ $t('library.discover.first_slice') }}</span>
              </h3>
              <span v-if="document.firstSlicePreview" class="text-xs text-brand-600 dark:text-brand-400 font-semibold shrink-0">
                Slice {{ document.firstSlicePreview.sliceOrder }}
              </span>
            </div>

            <div v-if="document.firstSlicePreview" class="space-y-2">
              <h4 class="text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200">
                {{ document.firstSlicePreview.chapterTitle }}
              </h4>
              <div class="max-h-52 overflow-y-auto rounded-xl bg-slate-50 dark:bg-white/[0.02] p-3 text-xs sm:text-sm text-slate-700 dark:text-slate-300 leading-relaxed font-mono whitespace-pre-wrap border border-slate-100 dark:border-white/[0.04]">
                {{ document.firstSlicePreview.summaryMarkdown || document.firstSlicePreview.originalTextMarkdown }}
              </div>
            </div>
            <div v-else class="py-8 text-center text-xs text-slate-400 italic">
              No slice preview available for this document.
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal Footer -->
    <template #footer>
      <div class="flex items-center justify-end gap-3 w-full">
        <button
          type="button"
          @click="$emit('close')"
          class="px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0"
        >
          {{ $t('common.close') || 'Close' }}
        </button>

        <!-- Open Book or Add to Library -->
        <NuxtLink
          v-if="document?.alreadyAdded || document?.existingBookId"
          :to="document?.existingBookId ? { path: `/read/${document.existingBookId}`, query: { from: '/library' } } : '/library'"
          class="inline-flex items-center gap-2 px-5 py-2.5 text-xs sm:text-sm font-semibold rounded-xl bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 transition-all active:scale-95 whitespace-nowrap shrink-0"
          @click="$emit('close')"
        >
          <CheckCircle2 class="w-4 h-4 shrink-0" />
          <span class="whitespace-nowrap shrink-0">{{ $t('library.discover.open_book') }}</span>
        </NuxtLink>

        <button
          v-else-if="document"
          type="button"
          :disabled="isAdopting"
          @click="$emit('adopt', document.id)"
          class="inline-flex items-center gap-2 px-5 py-2.5 text-xs sm:text-sm font-semibold rounded-xl bg-brand-500 hover:bg-brand-600 disabled:opacity-50 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
        >
          <Loader2 v-if="isAdopting" class="w-4 h-4 animate-spin shrink-0" />
          <Plus v-else class="w-4 h-4 shrink-0" />
          <span class="whitespace-nowrap shrink-0">{{ isAdopting ? $t('library.discover.adding') : $t('library.discover.add_to_library') }}</span>
        </button>
      </div>
    </template>
  </AppModal>
</template>
