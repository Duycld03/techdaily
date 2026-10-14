<script setup lang="ts">
import { ref, computed } from 'vue'
import { FileText, Globe, UploadCloud, Eye, Loader2, Plus } from 'lucide-vue-next'
import AppSelect, { type SelectOption } from '~/components/common/AppSelect.vue'
import { useCatalogStore } from '~/stores/useCatalogStore'
import { useApiError } from '~/composables/useApiError'

const emit = defineEmits<{
  (e: 'cancel'): void
  (e: 'created', documentId: string): void
}>()

const { t, locale } = useI18n()
const catalogStore = useCatalogStore()
const toast = useToast()
const { formatError } = useApiError()

// Form State
const draftSourceMode = ref<'pdf' | 'url'>('pdf')
const title = ref('')
const category = ref<number>(0)
const language = ref<string>(locale.value || 'en')
const sourceUrl = ref('')
const attribution = ref('')

const pdfFile = ref<File | null>(null)
const isDraggingFile = ref(false)
const pdfInputRef = ref<HTMLInputElement | null>(null)

// URL Preview State
const previewUrlInput = ref('')
const isPreviewingUrl = ref(false)
const urlPreviewResult = ref<{
  title: string
  sourceUrl: string
  extractedTextPreview: string
  estimatedMinutes?: number
  isPdfDetected?: boolean
  detectedPdfUrl?: string | null
} | null>(null)

const isSubmitting = ref(false)

const categoryOptions = computed<SelectOption[]>(() => [
  { value: 0, label: t('library.categories.frontend') },
  { value: 1, label: t('library.categories.backend') },
  { value: 2, label: t('library.categories.database') },
  { value: 3, label: t('library.categories.system_design') },
  { value: 4, label: t('library.categories.craft') },
  { value: 5, label: t('library.categories.mental_models') },
  { value: 6, label: t('library.categories.habits') }
])

const languageOptions = computed<SelectOption[]>(() => [
  { value: 'en', label: t('library.language_english') },
  { value: 'vi', label: t('library.language_vietnamese') }
])

function handleFileSelect(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files && input.files[0]) {
    pdfFile.value = input.files[0]
    if (!title.value.trim()) {
      title.value = input.files[0].name.replace(/\.pdf$/i, '')
    }
  }
}

function handleFileDrop(e: DragEvent) {
  isDraggingFile.value = false
  if (e.dataTransfer?.files && e.dataTransfer.files[0]) {
    const file = e.dataTransfer.files[0]
    if (file.type === 'application/pdf' || file.name.endsWith('.pdf')) {
      pdfFile.value = file
      if (!title.value.trim()) {
        title.value = file.name.replace(/\.pdf$/i, '')
      }
    } else {
      toast.error(t('admin_catalog.toasts.only_pdf_supported'))
    }
  }
}

async function handlePreviewUrl() {
  if (!previewUrlInput.value.trim()) {
    toast.error(t('admin_catalog.toasts.enter_valid_url'))
    return
  }

  isPreviewingUrl.value = true
  try {
    const preview = await catalogStore.previewUrl(previewUrlInput.value.trim())
    urlPreviewResult.value = preview
    if (!title.value.trim() && preview.title) {
      title.value = preview.title
    }
    sourceUrl.value = preview.sourceUrl
  } catch (err: unknown) {
    toast.error(formatError(err))
  } finally {
    isPreviewingUrl.value = false
  }
}

async function handleSubmit() {
  if (!title.value.trim()) {
    toast.error(t('admin_catalog.toasts.title_required'))
    return
  }



  isSubmitting.value = true
  try {
    if (draftSourceMode.value === 'pdf') {
      if (!pdfFile.value) {
        toast.error(t('admin_catalog.toasts.select_pdf_required'))
        isSubmitting.value = false
        return
      }

      const res = await catalogStore.uploadPdf({
        file: pdfFile.value,
        title: title.value.trim(),
        category: category.value,
        language: language.value,
        authorOrSourceUrl: sourceUrl.value.trim() || undefined,
        attributionText: attribution.value.trim() || undefined
      })

      toast.success(t('admin_catalog.toasts.pdf_draft_created'))
      emit('created', res.id)
    } else {
      if (!sourceUrl.value.trim()) {
        toast.error(t('admin_catalog.toasts.source_url_required'))
        isSubmitting.value = false
        return
      }

      const res = await catalogStore.importUrl({
        sourceUrl: sourceUrl.value.trim(),
        title: title.value.trim(),
        description: title.value.trim(),
        category: category.value,
        language: language.value,
        attributionText: attribution.value.trim() || undefined
      })

      toast.success(t('admin_catalog.toasts.web_draft_created'))
      emit('created', res.id)
    }
  } catch (err: unknown) {
    toast.error(formatError(err))
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="max-w-3xl space-y-6">
    <!-- Header with Source Mode Tabs -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-200/80 dark:border-white/[0.08] pb-4 gap-3">
      <div>
        <h2 class="text-lg sm:text-xl font-bold text-slate-900 dark:text-white">
          {{ $t('admin_catalog.new_draft') }}
        </h2>
        <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-0.5">
          {{ $t('admin_catalog.form.new_draft_subtitle') }}
        </p>
      </div>

      <!-- Ingestion Source Tab Switcher -->
      <div class="flex items-center p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/80 dark:border-white/[0.08] shrink-0 self-start sm:self-auto">
        <button
          type="button"
          @click="draftSourceMode = 'pdf'"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs sm:text-sm font-semibold flex items-center gap-1.5 transition-all whitespace-nowrap shrink-0 cursor-pointer',
            draftSourceMode === 'pdf'
              ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
          ]"
        >
          <FileText class="w-3.5 h-3.5 shrink-0" />
          <span>{{ $t('admin_catalog.upload_pdf') }}</span>
        </button>
        <button
          type="button"
          @click="draftSourceMode = 'url'"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs sm:text-sm font-semibold flex items-center gap-1.5 transition-all whitespace-nowrap shrink-0 cursor-pointer',
            draftSourceMode === 'url'
              ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
          ]"
        >
          <Globe class="w-3.5 h-3.5 shrink-0" />
          <span>{{ $t('admin_catalog.import_url') }}</span>
        </button>
      </div>
    </div>

    <!-- URL Preview Section (Web Mode) -->
    <div v-if="draftSourceMode === 'url'" class="space-y-4 p-5 sm:p-6 rounded-2xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08]">
      <label class="block text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
        {{ $t('admin_catalog.import_url') }}
      </label>
      <div class="flex flex-col sm:flex-row items-stretch sm:items-center gap-2.5">
        <input
          v-model="previewUrlInput"
          type="url"
          :placeholder="$t('admin_catalog.form.url_input_placeholder')"
          class="flex-1 px-3.5 py-2.5 text-xs sm:text-sm rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-200 placeholder-slate-400 focus:outline-none focus:border-brand-500 transition-colors"
        />
        <button
          type="button"
          :disabled="isPreviewingUrl"
          @click="handlePreviewUrl"
          class="inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 dark:bg-white dark:hover:bg-slate-100 text-white dark:text-slate-900 text-xs sm:text-sm font-semibold transition-all whitespace-nowrap shrink-0 disabled:opacity-50 cursor-pointer"
        >
          <Loader2 v-if="isPreviewingUrl" class="w-4 h-4 animate-spin shrink-0" />
          <Eye v-else class="w-4 h-4 shrink-0" />
          <span>{{ $t('admin_catalog.preview_url') }}</span>
        </button>
      </div>

      <!-- Preview Result Box -->
      <div v-if="urlPreviewResult" class="p-4 rounded-xl bg-slate-50 dark:bg-white/[0.02] border border-slate-200/60 dark:border-white/[0.04] space-y-2 text-xs sm:text-sm">
        <div class="flex items-center justify-between gap-2">
          <span class="font-bold text-slate-900 dark:text-white truncate">
            {{ $t('admin_catalog.form.detected_title', { title: urlPreviewResult.title }) }}
          </span>
          <span v-if="urlPreviewResult.isPdfDetected" class="text-amber-500 font-semibold whitespace-nowrap shrink-0">
            {{ $t('admin_catalog.form.pdf_detected') }}
          </span>
        </div>
        <p class="text-slate-600 dark:text-slate-400 line-clamp-3 italic">
          {{ urlPreviewResult.extractedTextPreview }}
        </p>
      </div>
    </div>

    <!-- PDF Upload Box (PDF Mode) -->
    <div v-else class="space-y-3">
      <label class="block text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
        {{ $t('admin_catalog.form.select_or_drop_pdf') }}
      </label>
      <div
        @dragover.prevent="isDraggingFile = true"
        @dragleave.prevent="isDraggingFile = false"
        @drop.prevent="handleFileDrop"
        :class="[
          'border-2 border-dashed rounded-3xl p-8 text-center transition-all cursor-pointer flex flex-col items-center justify-center space-y-3',
          isDraggingFile
            ? 'border-brand-500 bg-brand-500/10'
            : 'border-slate-200 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40'
        ]"
        @click="pdfInputRef?.click()"
      >
        <input
          ref="pdfInputRef"
          type="file"
          accept="application/pdf"
          class="hidden"
          @change="handleFileSelect"
        />
        <div class="w-12 h-12 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 flex items-center justify-center">
          <UploadCloud class="w-6 h-6 shrink-0" />
        </div>
        <div>
          <p class="text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200">
            {{ pdfFile ? pdfFile.name : $t('admin_catalog.form.drop_pdf_prompt') }}
          </p>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
            {{ $t('admin_catalog.form.pdf_size_limit') }}
          </p>
        </div>
      </div>
    </div>

    <!-- Metadata Fields -->
    <div class="space-y-4 p-5 sm:p-6 rounded-2xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08]">
      <h3 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white border-b border-slate-100 dark:border-white/[0.06] pb-2">
        {{ $t('admin_catalog.metadata') }}
      </h3>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div class="sm:col-span-2 space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.form.document_title_required') }}
          </label>
          <input
            v-model="title"
            type="text"
            :placeholder="$t('admin_catalog.form.title_placeholder')"
            class="w-full px-3.5 py-2 text-xs sm:text-sm rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-200 focus:outline-none focus:border-brand-500 transition-colors"
          />
        </div>

        <div class="space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.form.category_required') }}
          </label>
          <AppSelect
            v-model="category"
            :options="categoryOptions"
            size="md"
          />
        </div>

        <div class="space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.form.language_required') }}
          </label>
          <AppSelect
            v-model="language"
            :options="languageOptions"
            size="md"
          />
        </div>

        <div class="sm:col-span-2 space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.form.author_or_source_url') }}
          </label>
          <input
            v-model="sourceUrl"
            type="text"
            :placeholder="$t('admin_catalog.form.url_placeholder')"
            class="w-full px-3.5 py-2 text-xs sm:text-sm rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-200 focus:outline-none focus:border-brand-500 transition-colors"
          />
        </div>

        <div class="sm:col-span-2 space-y-1.5">
          <label class="block text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300">
            {{ $t('admin_catalog.attribution_text') }}
          </label>
          <input
            v-model="attribution"
            type="text"
            :placeholder="$t('admin_catalog.form.attribution_placeholder')"
            class="w-full px-3.5 py-2 text-xs sm:text-sm rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-200 focus:outline-none focus:border-brand-500 transition-colors"
          />
        </div>

      </div>
    </div>

    <!-- Actions -->
    <div class="flex items-center justify-end gap-3 pt-2">
      <button
        type="button"
        @click="$emit('cancel')"
        class="px-4 py-2.5 text-xs sm:text-sm font-semibold rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-200/60 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0 cursor-pointer"
      >
        {{ $t('admin_catalog.cancel') }}
      </button>

      <button
        type="button"
        :disabled="isSubmitting"
        @click="handleSubmit"
        class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 disabled:opacity-50 text-white font-semibold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all active:scale-95 whitespace-nowrap shrink-0 cursor-pointer"
      >
        <Loader2 v-if="isSubmitting" class="w-4 h-4 animate-spin shrink-0" />
        <Plus v-else class="w-4 h-4 shrink-0" :stroke-width="2" />
        <span>{{ isSubmitting ? $t('admin_catalog.creating_draft_btn') : $t('admin_catalog.create_draft_btn') }}</span>
      </button>
    </div>
  </div>
</template>
