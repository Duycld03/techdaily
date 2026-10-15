<script setup lang="ts">
import { computed } from 'vue'
import { BookOpen, Plus, Eye, ExternalLink, Clock, Layers, CheckCircle2, Loader2, FileText, Globe } from 'lucide-vue-next'
import type { LearnerCatalogDocumentDto } from '~/stores/useCatalogStore'

const props = defineProps<{
  document: LearnerCatalogDocumentDto
  isAdopting?: boolean
}>()

const emit = defineEmits<{
  (e: 'preview', document: LearnerCatalogDocumentDto): void
  (e: 'adopt', document: LearnerCatalogDocumentDto): void
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

const sourceTypeBadge = computed(() => {
  const st = props.document.sourceType
  const isWeb = st === 1 || String(st).toLowerCase() === 'webdocurl' || String(st).toLowerCase() === 'web'
  const isPdf = st === 2 || String(st).toLowerCase() === 'pdfbook' || String(st).toLowerCase() === 'pdf'
  const isMarkdown = st === 3 || String(st).toLowerCase() === 'markdownseries' || String(st).toLowerCase() === 'markdown'

  if (isWeb) {
    return { label: t('library.discover.source_web'), icon: Globe, class: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20' }
  }
  if (isPdf) {
    return { label: t('library.discover.source_pdf'), icon: FileText, class: 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20' }
  }
  if (isMarkdown) {
    return { label: t('library.discover.source_markdown'), icon: Layers, class: 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border-sky-500/20' }
  }
  return { label: t('library.discover.source_all'), icon: FileText, class: 'bg-slate-500/10 text-slate-600 dark:text-slate-400 border-slate-500/20' }
})

const languageBadge = computed(() => {
  const lang = (props.document.language || 'en').toLowerCase()
  return lang.startsWith('vi') ? 'VI' : 'EN'
})
</script>

<template>
  <div class="glass-card flex flex-col justify-between p-5 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] hover:border-brand-500/30 transition-all duration-200 shadow-sm hover:shadow-md bg-white dark:bg-canvas-subtle/80">
    <div class="space-y-3.5">
      <!-- Badge Row -->
      <div class="flex flex-wrap items-center justify-between gap-2">
        <div class="flex flex-wrap items-center gap-1.5">
          <!-- Category Badge -->
          <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 whitespace-nowrap shrink-0">
            {{ categoryLabel }}
          </span>

          <!-- Source Type Badge -->
          <span :class="['inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium border whitespace-nowrap shrink-0', sourceTypeBadge.class]">
            <component :is="sourceTypeBadge.icon" class="w-3 h-3 shrink-0" />
            <span>{{ sourceTypeBadge.label }}</span>
          </span>
        </div>

        <!-- Language Badge -->
        <span class="px-2 py-0.5 rounded-md text-[11px] font-bold tracking-wider bg-slate-100 dark:bg-white/[0.06] text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-white/[0.08] shrink-0">
          {{ languageBadge }}
        </span>
      </div>

      <!-- Title & Description -->
      <div class="space-y-1.5">
        <h3 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white leading-snug line-clamp-2">
          {{ document.title }}
        </h3>
        <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-400 line-clamp-3 leading-relaxed">
          {{ document.description }}
        </p>
      </div>

      <!-- Attribution & Source -->
      <div v-if="document.attributionText || document.authorOrSourceUrl" class="pt-1 text-xs text-slate-500 dark:text-slate-400 flex items-center gap-1.5 flex-wrap">
        <span class="font-medium text-slate-700 dark:text-slate-300 shrink-0">{{ $t('library.discover.attribution') }}:</span>
        <span class="line-clamp-1 italic">{{ document.attributionText || document.authorOrSourceUrl }}</span>
      </div>

      <!-- Slices & Reading Time -->
      <div class="flex items-center gap-4 text-xs font-medium text-slate-500 dark:text-slate-400 pt-1">
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

    <!-- Action Buttons Footer -->
    <div class="flex items-center justify-between gap-2.5 pt-4 mt-4 border-t border-slate-100 dark:border-white/[0.06]">
      <!-- Preview Button -->
      <button
        type="button"
        data-testid="catalog-card-preview-btn"
        @click="$emit('preview', document)"
        class="inline-flex items-center gap-1.5 px-3 py-2 text-xs sm:text-sm font-semibold rounded-xl text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08] transition-colors whitespace-nowrap shrink-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 cursor-pointer"
      >
        <Eye class="w-4 h-4 shrink-0 text-slate-500" />
        <span class="whitespace-nowrap shrink-0">{{ $t('library.discover.preview_doc') }}</span>
      </button>

      <!-- Adopt or Open Book -->
      <NuxtLink
        v-if="document.alreadyAdded || document.existingBookId"
        data-testid="catalog-card-open-book-btn"
        :to="document.existingBookId ? { path: `/read/${document.existingBookId}`, query: { from: '/library' } } : '/library'"
        class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs sm:text-sm font-semibold rounded-xl bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 transition-all active:scale-95 whitespace-nowrap shrink-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-500"
      >
        <CheckCircle2 class="w-4 h-4 shrink-0" />
        <span class="whitespace-nowrap shrink-0">{{ $t('library.discover.open_book') }}</span>
      </NuxtLink>

      <button
        v-else
        type="button"
        :disabled="isAdopting"
        @click="$emit('adopt', document)"
        class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs sm:text-sm font-semibold rounded-xl bg-brand-500 hover:bg-brand-600 disabled:opacity-50 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
      >
        <Loader2 v-if="isAdopting" class="w-4 h-4 animate-spin shrink-0" />
        <Plus v-else class="w-4 h-4 shrink-0" />
        <span class="whitespace-nowrap shrink-0">{{ isAdopting ? $t('library.discover.adding') : $t('library.discover.add_to_library') }}</span>
      </button>
    </div>
  </div>
</template>
