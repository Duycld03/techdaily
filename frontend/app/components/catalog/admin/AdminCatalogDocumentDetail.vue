<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import {
  CheckCircle2,
  AlertTriangle,
  RefreshCw,
  Loader2,
  Check,
  X
} from 'lucide-vue-next'
import AppSelect, { type SelectOption } from '~/components/common/AppSelect.vue'
import AdminCatalogSlicesReview from '~/components/catalog/admin/AdminCatalogSlicesReview.vue'
import type { AdminCatalogDocument } from '~/stores/useCatalogStore'
import { useCategoryStore } from '~/stores/useCategoryStore'

const props = defineProps<{
  document: AdminCatalogDocument
  isSavingMetadata: boolean
  isPreparingSlices: boolean
  isPublishing: boolean
  isHiding: boolean
  isRetrying: boolean
}>()

const emit = defineEmits<{
  (e: 'save-metadata', payload: {
    title: string
    categoryId: string
    language: string
    authorOrSourceUrl?: string
    attributionText?: string
  }): void
  (e: 'retry-processing', file?: File): void
  (e: 'prepare-slices'): void
  (e: 'inspect-slice', order: number): void
  (e: 'publish'): void
  (e: 'hide'): void
}>()

const { t, locale } = useI18n()
const categoryStore = useCategoryStore()
onMounted(() => { void categoryStore.ensureCategories() })

// Local Edit Metadata State
const editTitle = ref('')
const editCategoryId = ref('')
const editLanguage = ref('en')
const editAttribution = ref('')
const editSourceUrl = ref('')
const retryPdfFile = ref<File | null>(null)


function formatUnmetRequirement(req: string): string {
  if (!req) return ''
  if (req.includes('Document processing must be complete')) {
    return t('admin_catalog.unmet_processing_ready')
  }
  if (req.includes('at least one extracted slice')) {
    return t('admin_catalog.unmet_slices_min')
  }
  if (req.includes('readable source content')) {
    return t('admin_catalog.unmet_slices_readable')
  }
  const targetMatch = req.match(/First (\d+) initial slices require AI initial formatting/)
  if (targetMatch) {
    return t('admin_catalog.unmet_initial_slices_formatted', { target: targetMatch[1] })
  }
  if (req.includes('Source attribution or author URL is required')) {
    return t('admin_catalog.unmet_attribution_required')
  }
  return req
}

const isPublished = computed(() => props.document?.visibility === 'Published')

watch(() => props.document, (doc) => {
  if (doc) {
    editTitle.value = doc.title
    editCategoryId.value = doc.categoryId
    editLanguage.value = doc.language || 'en'
    editAttribution.value = doc.attributionText || ''
    editSourceUrl.value = doc.authorOrSourceUrl || ''
  }
}, { immediate: true })

const categoryOptions = computed<SelectOption[]>(() => {
  const current = categoryStore.getCategoryById(props.document.categoryId) || props.document.category
  const options = categoryStore.activeCategories.map(c => ({
    value: c.id,
    label: categoryStore.getCategoryName(c, locale.value)
  }))
  if (!options.some(c => c.value === current.id)) {
    return [...options, { value: current.id, label: `${categoryStore.getCategoryName(current, locale.value)} (${t('categories.inactive')})` }]
  }
  return options
})
const hasValidCategory = computed(() => editCategoryId.value === props.document.categoryId ||
  (!categoryStore.error && !categoryStore.isLoading && categoryStore.activeCategories.some(c => c.id === editCategoryId.value)))

const languageOptions = computed<SelectOption[]>(() => [
  { value: 'en', label: t('library.language_english') },
  { value: 'vi', label: t('library.language_vietnamese') }
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

function handleSave() {
  if (!hasValidCategory.value) return
  emit('save-metadata', {
    title: editTitle.value.trim(),
    categoryId: editCategoryId.value,
    language: editLanguage.value,
    authorOrSourceUrl: editSourceUrl.value.trim() || undefined,
    attributionText: editAttribution.value.trim() || undefined,
  })
}

function handleRetry() {
  emit('retry-processing', retryPdfFile.value || undefined)
  retryPdfFile.value = null
}
</script>

<template>
  <div class="max-w-4xl space-y-6">
    <!-- Overview Header Banner -->
    <div class="p-6 rounded-3xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] shadow-sm space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div class="flex flex-wrap items-center gap-2">
          <!-- Visibility Badge -->
          <span :class="[
            'px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider border whitespace-nowrap shrink-0',
            document.visibility === 'Published'
              ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20'
              : document.visibility === 'Hidden'
              ? 'bg-slate-500/10 text-slate-600 dark:text-slate-400 border-slate-500/20'
              : 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20'
          ]">
            {{ getVisibilityLabel(document.visibility) }}
          </span>

          <!-- Processing Status Badge -->
          <span :class="[
            'px-3 py-1 rounded-full text-xs font-bold border whitespace-nowrap shrink-0',
            document.processingStatus === 'Ready'
              ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20'
              : document.processingStatus === 'Failed'
              ? 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20'
              : 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border-brand-500/20'
          ]">
            <span>{{ getStatusLabel(document.processingStatus) }}</span>
          </span>

          <!-- Revision Badge -->
          <span class="px-2.5 py-1 rounded-lg text-xs font-mono font-semibold bg-slate-100 dark:bg-white/[0.06] text-slate-600 dark:text-slate-300 border border-slate-200/80 dark:border-white/[0.08] whitespace-nowrap shrink-0">
            {{ $t('admin_catalog.document.revision', { rev: document.revision }) }}
          </span>
        </div>

        <!-- Reviewed Badge -->
        <div v-if="document.reviewedByUserId" class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 text-xs font-semibold border border-emerald-500/20 whitespace-nowrap shrink-0">
          <CheckCircle2 class="w-3.5 h-3.5 shrink-0" />
          <span>{{ $t('admin_catalog.reviewed') }}</span>
        </div>
      </div>

      <div class="space-y-1">
        <h2 class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white leading-tight">
          {{ document.title }}
        </h2>
        <p v-if="document.authorOrSourceUrl" class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-mono break-all">
          {{ document.authorOrSourceUrl }}
        </p>
      </div>

      <!-- In-Progress Status Bar -->
      <div
        v-if="document.processingStatus === 'Processing' || document.processingStatus === 'Pending'"
        class="p-4 rounded-2xl bg-brand-50 dark:bg-brand-500/10 border border-brand-200 dark:border-brand-500/20 space-y-2 text-xs sm:text-sm"
      >
        <div class="flex items-center justify-between text-brand-700 dark:text-brand-300 font-semibold gap-2">
          <span class="truncate">{{ document.processingStatusMessage || $t('admin_catalog.document.status_processing_default') }}</span>
        </div>
      </div>

      <!-- Failure & Retry Box -->
      <div
        v-else-if="document.processingStatus === 'Failed'"
        class="p-5 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 space-y-3"
      >
        <div class="flex items-start gap-2.5 text-rose-800 dark:text-rose-300">
          <AlertTriangle class="w-5 h-5 shrink-0 mt-0.5" />
          <div class="space-y-1">
            <h4 class="text-sm sm:text-base font-bold">{{ $t('admin_catalog.document.preparation_failed') }}</h4>
            <p class="text-xs sm:text-sm">{{ document.processingErrorMessage || $t('admin_catalog.document.preparation_failed_default') }}</p>
          </div>
        </div>

        <!-- Optional Replacement PDF for PDF Source -->
        <div v-if="document.sourceType === 'PdfBook'" class="pt-2 flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
          <input
            type="file"
            accept="application/pdf"
            @change="(e) => retryPdfFile = (e.target as HTMLInputElement).files?.[0] || null"
            class="text-xs sm:text-sm text-slate-500 file:mr-2 file:py-1.5 file:px-3 file:rounded-lg file:border-0 file:text-xs file:font-semibold file:bg-rose-100 dark:file:bg-rose-900/40 file:text-rose-700 dark:file:text-rose-300"
          />
        </div>

        <div class="pt-1">
          <button
            type="button"
            :disabled="isRetrying"
            @click="handleRetry"
            class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs sm:text-sm font-semibold transition-all active:scale-95 whitespace-nowrap shrink-0 disabled:opacity-50 cursor-pointer"
          >
            <Loader2 v-if="isRetrying" class="w-4 h-4 animate-spin shrink-0" />
            <RefreshCw v-else class="w-4 h-4 shrink-0" />
            <span>{{ $t('admin_catalog.retry_preparation') }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Section 1: Metadata & Distribution Rights Editor -->
    <div class="p-6 rounded-3xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] shadow-sm space-y-4">
      <div class="flex items-center justify-between border-b border-slate-100 dark:border-white/[0.06] pb-3 gap-2">
        <h3 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white">
          {{ $t('admin_catalog.metadata') }}
        </h3>
        <button
          type="button"
          :disabled="isSavingMetadata || isPublished || !hasValidCategory"
          @click="handleSave"
          class="inline-flex items-center gap-1.5 px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl bg-slate-900 hover:bg-slate-800 dark:bg-white dark:hover:bg-slate-100 text-white dark:text-slate-900 transition-all active:scale-95 whitespace-nowrap shrink-0 disabled:opacity-50 cursor-pointer"
        >
          <Loader2 v-if="isSavingMetadata" class="w-4 h-4 animate-spin shrink-0" />
          <Check v-else class="w-4 h-4 shrink-0" />
          <span>{{ isSavingMetadata ? $t('admin_catalog.saving') : $t('admin_catalog.save_changes') }}</span>
        </button>
      </div>

      <!-- Published Read-Only Notice Banner -->
      <div
        v-if="isPublished"
        data-testid="admin-catalog-published-notice"
        class="p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/60 flex items-start gap-2.5 text-xs sm:text-sm text-amber-800 dark:text-amber-300"
      >
        <AlertTriangle class="w-4 h-4 shrink-0 mt-0.5 text-amber-600 dark:text-amber-400" />
        <span>{{ $t('admin_catalog.metadata_published_readonly') }}</span>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div class="sm:col-span-2 space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.form.document_title') }}
          </label>
          <input
            v-model="editTitle"
            :disabled="isPublished"
            type="text"
            class="w-full px-3.5 py-2 text-xs sm:text-sm rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-200 focus:outline-none focus:border-brand-500 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          />
        </div>

        <div class="space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.form.category') }}
          </label>
          <AppSelect
            v-model="editCategoryId"
            :options="categoryOptions"
            :disabled="isPublished"
            size="md"
          />
          <div v-if="categoryStore.error" role="alert">
            {{ $t('categories.metadataUnavailable') }}
            <button type="button" @click="categoryStore.ensureCategories()">{{ $t('categories.retry') }}</button>
          </div>
          <p v-else-if="categoryStore.hasLoaded && !categoryStore.activeCategories.length">{{ $t('categories.noActiveCategories') }}</p>
        </div>

        <div class="space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.form.language') }}
          </label>
          <AppSelect
            v-model="editLanguage"
            :options="languageOptions"
            :disabled="isPublished"
            size="md"
          />
        </div>

        <div class="sm:col-span-2 space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.form.author_or_source_url') }}
          </label>
          <input
            v-model="editSourceUrl"
            :disabled="isPublished"
            type="text"
            class="w-full px-3.5 py-2 text-xs sm:text-sm rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-200 focus:outline-none focus:border-brand-500 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          />
        </div>

        <div class="sm:col-span-2 space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.attribution_text') }}
          </label>
          <input
            v-model="editAttribution"
            :disabled="isPublished"
            type="text"
            class="w-full px-3.5 py-2 text-xs sm:text-sm rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-200 focus:outline-none focus:border-brand-500 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          />
        </div>

      </div>
    </div>

    <!-- Section 2: Slices & Preparation Review -->
    <AdminCatalogSlicesReview
      :document="document"
      :is-preparing-slices="isPreparingSlices"
      @prepare-slices="$emit('prepare-slices')"
      @inspect-slice="(order) => $emit('inspect-slice', order)"
    />

    <!-- Section 3: Publication Gating & Visibility Controls -->
    <div class="p-6 rounded-3xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] shadow-sm space-y-4">
      <h3 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white">
        {{ $t('admin_catalog.document.publication_title') }}
      </h3>

      <!-- Unmet Requirements Notice if Ineligible -->
      <div
        v-if="!document.isEligibleForPublication && document.unmetPublicationRequirements?.length"
        class="p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/60 space-y-2 text-xs sm:text-sm"
      >
        <div class="flex items-center gap-2 text-amber-800 dark:text-amber-300 font-bold">
          <AlertTriangle class="w-4 h-4 shrink-0" />
          <span>{{ $t('admin_catalog.unmet_requirements') }}</span>
        </div>
        <ul class="list-disc list-inside space-y-1 text-amber-700 dark:text-amber-400 pl-1">
          <li v-for="req in document.unmetPublicationRequirements" :key="req">
            {{ formatUnmetRequirement(req) }}
          </li>
        </ul>
      </div>

      <!-- Publish / Hide Action Buttons -->
      <div class="flex flex-wrap items-center justify-between gap-3 pt-2">
        <div class="text-xs sm:text-sm text-slate-500 dark:text-slate-400">
          {{ $t('admin_catalog.document.current_visibility') }}
          <strong class="text-slate-800 dark:text-slate-200 ml-1">{{ getVisibilityLabel(document.visibility) }}</strong>
        </div>

        <div class="flex items-center gap-3">
          <!-- Hide Button -->
          <button
            v-if="document.visibility === 'Published'"
            type="button"
            :disabled="isHiding"
            @click="$emit('hide')"
            class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-slate-200 hover:bg-slate-300 dark:bg-white/[0.08] dark:hover:bg-white/[0.12] text-slate-800 dark:text-slate-200 font-semibold text-xs sm:text-sm transition-all active:scale-95 whitespace-nowrap shrink-0 disabled:opacity-50 cursor-pointer"
          >
            <Loader2 v-if="isHiding" class="w-4 h-4 animate-spin shrink-0" />
            <X v-else class="w-4 h-4 shrink-0" />
            <span>{{ isHiding ? $t('admin_catalog.hiding') : $t('admin_catalog.hide_action') }}</span>
          </button>

          <!-- Publish Button -->
          <!-- Publish Button (hidden when already published) -->
          <button
            v-if="document.visibility !== 'Published'"
            type="button"
            :disabled="!document.isEligibleForPublication || isPublishing"
            @click="$emit('publish')"
            class="inline-flex items-center gap-2 px-6 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 disabled:opacity-40 disabled:cursor-not-allowed text-white font-semibold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all active:scale-95 whitespace-nowrap shrink-0 cursor-pointer"
          >
            <Loader2 v-if="isPublishing" class="w-4 h-4 animate-spin shrink-0" />
            <CheckCircle2 v-else class="w-4 h-4 shrink-0" />
            <span>{{ isPublishing ? $t('admin_catalog.publishing') : $t('admin_catalog.publish_action') }}</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
