<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useIntervalFn } from '@vueuse/core'
import { BookOpen, Search, Plus, ExternalLink, Layers, X, FileText, Bookmark, Trash2, AlertTriangle, FileUp, Globe, CheckCircle2, UploadCloud, Loader2, Sparkles, Download, Lightbulb } from 'lucide-vue-next'
import BasePagination from '~/components/common/BasePagination.vue'
import AppSelect from '~/components/common/AppSelect.vue'
import { useApiError } from '~/composables/useApiError'
import { useLibraryStore } from '~/stores/useLibraryStore'
const { t, locale } = useI18n()
const route = useRoute()
const router = useRouter()
const { formatError } = useApiError()
const libraryStore = useLibraryStore()
const toast = useToast()
const searchQuery = ref('')
const selectedCategory = ref<number | undefined>(undefined)
const bookmarks = ref<Record<string, number>>({})
const exportingBookId = ref<string | null>(null)

async function handleExportBook(book: any) {
  exportingBookId.value = book.id
  try {
    await libraryStore.exportBookMarkdown(book.id, book.slug)
    toast.success(t('reader.toast_export_success') || 'Notes exported successfully!')
  } catch (err: any) {
    toast.error(err.message || 'Failed to export notes.')
  } finally {
    exportingBookId.value = null
  }
}

// Import modal state
const isImportModalOpen = ref(false)
const activeTab = ref<'markdown' | 'pdf' | 'url'>('markdown')

// Tab 1: Markdown form state
const importTitle = ref('')
const importCategory = ref(0)
const importSourceUrl = ref('')
const importContent = ref('')

// Tab 2: PDF Upload state
const pdfFile = ref<File | null>(null)
const pdfTitle = ref('')
const pdfCategory = ref(0)
const isDraggingPdf = ref(false)
const isUploadingPdf = ref(false)
const isProcessingPdf = ref(false)
const pdfProgress = ref(0)
const pdfStatusMessage = ref('')
const pollingBookId = ref<string | null>(null)

// Tab 3: URL Crawler state
const crawlUrlInput = ref('')
const isCrawling = ref(false)
const crawlSuccess = ref(false)
const isPdfDetected = ref(false)
const detectedPdfUrl = ref<string | null>(null)

// Delete modal state
const bookToDelete = ref<{ id: string; title: string } | null>(null)
const isDeleteModalOpen = ref(false)
const isDeleting = ref(false)

const categories = computed(() => [
  { id: undefined, label: t('library.categories.all') },
  { id: 0, label: t('library.categories.frontend') },
  { id: 1, label: t('library.categories.backend') },
  { id: 2, label: t('library.categories.database') },
  { id: 3, label: t('library.categories.system_design') },
  { id: 4, label: t('library.categories.craft') }
])

const formCategoryOptions = computed(() => [
  { value: 0, label: t('library.categories.frontend') },
  { value: 1, label: t('library.categories.backend') },
  { value: 2, label: t('library.categories.database') },
  { value: 3, label: t('library.categories.system_design') },
  { value: 4, label: t('library.categories.craft') }
])
function getCategoryLabel(category: number | string | undefined | null): string {
  if (category === undefined || category === null) {
    return t('library.categories.craft')
  }

  const normalized = String(category).toLowerCase()
  switch (normalized) {
    case '0':
    case 'frontendweb':
    case 'frontend':
      return t('library.categories.frontend')
    case '1':
    case 'backenddotnet':
    case 'backend':
      return t('library.categories.backend')
    case '2':
    case 'databasestorage':
    case 'database':
      return t('library.categories.database')
    case '3':
    case 'systemdesign':
    case 'system_design':
      return t('library.categories.system_design')
    case '4':
    case 'engineeringcraft':
    case 'craft':
      return t('library.categories.craft')
    default:
      return categories.value.find((c) => String(c.id) === normalized)?.label || t('library.categories.craft')
  }
}

function inferCategoryFromContext(title = '', url = '', content = ''): number {
  const combined = `${title} ${url} ${content}`.toLowerCase()

  if (
    combined.includes('aspnet') ||
    combined.includes('aspnetcore') ||
    combined.includes('dotnet') ||
    combined.includes('.net') ||
    combined.includes('csharp') ||
    combined.includes('c#') ||
    combined.includes('entityframework') ||
    combined.includes('efcore') ||
    combined.includes('golang') ||
    combined.includes('rust') ||
    combined.includes('java') ||
    combined.includes('spring') ||
    combined.includes('python') ||
    combined.includes('concurrency') ||
    combined.includes('runtime') ||
    combined.includes('backend')
  ) {
    return 1 // BackendRuntime
  }

  if (
    combined.includes('postgres') ||
    combined.includes('postgresql') ||
    combined.includes('redis') ||
    combined.includes('mysql') ||
    combined.includes('mongodb') ||
    combined.includes('database') ||
    combined.includes('storage engine') ||
    combined.includes('sql')
  ) {
    return 2 // DatabaseStorage
  }

  if (
    combined.includes('system design') ||
    combined.includes('distributed') ||
    combined.includes('microservice') ||
    combined.includes('kafka') ||
    combined.includes('kubernetes') ||
    combined.includes('docker') ||
    combined.includes('outbox') ||
    combined.includes('event sourcing')
  ) {
    return 3 // SystemDesign
  }

  if (
    combined.includes('atomic habits') ||
    combined.includes('deep work') ||
    combined.includes('pragmatic') ||
    combined.includes('mindset') ||
    combined.includes('productivity') ||
    combined.includes('leadership') ||
    combined.includes('soft skills')
  ) {
    return 4 // EngineeringCraft
  }

  if (
    combined.includes('vue') ||
    combined.includes('react') ||
    combined.includes('angular') ||
    combined.includes('frontend') ||
    combined.includes('browser') ||
    combined.includes('css') ||
    combined.includes('html') ||
    combined.includes('dom') ||
    combined.includes('javascript') ||
    combined.includes('typescript')
  ) {
    return 0 // FrontendWeb
  }

  return 0
}

function getStatusMessage(book: any): string {
  const msg = book?.statusMessage
  if (!msg) {
    return t('library.processing_pdf')
  }

  const curatingMatch = msg.match(/curating (?:initial )?slice (\d+)\/(\d+)/i)
  if (curatingMatch) {
    return t('library.status_curating_slice', { current: curatingMatch[1], total: curatingMatch[2] })
  }

  const analyzingPagesMatch = msg.match(/analyzing (?:content:\s*page\s*)?(\d+)(?:\/(\d+)|\s+pdf\s+pages)?/i)
  if (analyzingPagesMatch) {
    const pages = analyzingPagesMatch[2] ? `${analyzingPagesMatch[1]}/${analyzingPagesMatch[2]}` : analyzingPagesMatch[1]
    return t('library.status_analyzing_pages', { pages })
  }

  const extractingTopicMatch = msg.match(/extracting (?:chapter content|topic):\s*(.+)/i)
  if (extractingTopicMatch) {
    return t('library.status_extracting_topic', { topic: extractingTopicMatch[1].trim() })
  }

  const extractedCompleteMatch = msg.match(/extracted (\d+) slices(?:\.\s*complete!?|\))/i)
  if (extractedCompleteMatch) {
    return t('library.status_extracted_complete', { slices: extractedCompleteMatch[1] })
  }

  if (/uploaded,?\s*queued/i.test(msg)) {
    return t('library.status_uploaded_queued')
  }

  if (/remote pdf download/i.test(msg)) {
    return t('library.status_remote_download')
  }

  if (/analyzing (?:pdf|document) structure/i.test(msg)) {
    return t('library.status_analyzing_structure')
  }

  if (/persisting (?:chapters|slices)/i.test(msg) || /saving slices/i.test(msg)) {
    return t('library.status_persisting_slices')
  }

  if (/parsing pages/i.test(msg)) {
    return t('library.status_parsing_pages')
  }

  if (/generating (?:reading )?chunks/i.test(msg)) {
    return t('library.status_generating_chunks')
  }

  if (/ready (?:for reading|to read)/i.test(msg)) {
    return t('library.status_ready')
  }

  return msg
}

const { pause: stopBackgroundPolling, resume: startBackgroundPolling } = useIntervalFn(
  async () => {
    await libraryStore.fetchBooks({
      category: selectedCategory.value,
      search: searchQuery.value,
      page: libraryStore.currentPage,
      pageSize: libraryStore.pageSize
    })
    const stillActive = libraryStore.books.some(
      b => b.status === 'Processing' || (b.status as any) === 1
    )
    if (!stillActive) {
      stopBackgroundPolling()
    }
  },
  2500,
  { immediate: false }
)

function checkBackgroundPolling() {
  stopBackgroundPolling()
  const hasInProgressBook = libraryStore.books.some(
    b => b.status === 'Processing' || (b.status as any) === 1
  )
  if (hasInProgressBook) {
    startBackgroundPolling()
  }
}

const { pause: stopPdfPolling, resume: startPdfPolling } = useIntervalFn(
  async () => {
    if (!pollingBookId.value) {
      stopPdfPolling()
      return
    }
    try {
      const status = await libraryStore.getBookStatus(pollingBookId.value)
      pdfProgress.value = Math.max(5, status.progressPercentage)
      if (status.statusMessage) {
        pdfStatusMessage.value = status.statusMessage
      }

      if (status.status === 'Ready' || (status.status as any) === 2 || status.statusMessage === 'Ready' || status.progressPercentage === 100) {
        stopPdfPolling()
        pollingBookId.value = null
        isProcessingPdf.value = false
        pdfProgress.value = 100
        toast.success(t('library.toast_upload_success'))
        isImportModalOpen.value = false
        pdfFile.value = null
        pdfTitle.value = ''
        await libraryStore.fetchBooks(selectedCategory.value ?? undefined, searchQuery.value)
      } else if (status.status === 'Failed' || (status.status as any) === 3) {
        stopPdfPolling()
        pollingBookId.value = null
        isProcessingPdf.value = false
        toast.error(status.errorMessage || t('library.processing_failed'))
      }
    } catch {
      // keep polling
    }
  },
  1500,
  { immediate: false }
)

onMounted(async () => {
  const queryPage = route.query.page ? parseInt(route.query.page as string, 10) : 1
  const initialPage = isNaN(queryPage) || queryPage < 1 ? 1 : queryPage

  if (route.query.category !== undefined) {
    const cat = parseInt(route.query.category as string, 10)
    if (!isNaN(cat)) selectedCategory.value = cat
  }
  if (typeof route.query.search === 'string') {
    searchQuery.value = route.query.search
  }

  await loadBooksWithPagination(initialPage)
  checkBackgroundPolling()
  loadBookmarks()
})


function loadBookmarks() {
  if (typeof window === 'undefined') return
  try {
    const loaded: Record<string, number> = {}
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i)
      if (key?.startsWith('techdaily_bookmark_')) {
        const bookId = key.replace('techdaily_bookmark_', '')
        const slice = parseInt(localStorage.getItem(key) || '1', 10)
        loaded[bookId] = slice
      }
    }
    bookmarks.value = loaded
  } catch (e) {
    // ignore
  }
}

async function loadBooksWithPagination(page = 1) {
  await libraryStore.fetchBooks({
    category: selectedCategory.value,
    search: searchQuery.value,
    page,
    pageSize: libraryStore.pageSize
  })
}

function onPageChange(newPage: number) {
  router.replace({
    query: {
      ...route.query,
      page: newPage > 1 ? newPage.toString() : undefined
    }
  })
  loadBooksWithPagination(newPage)
}

function handleCategorySelect(catId?: number) {
  selectedCategory.value = catId
  router.replace({
    query: {
      ...route.query,
      category: catId !== undefined ? catId.toString() : undefined,
      page: undefined
    }
  })
  loadBooksWithPagination(1)
}

function handleSearch() {
  router.replace({
    query: {
      ...route.query,
      search: searchQuery.value ? searchQuery.value : undefined,
      page: undefined
    }
  })
  loadBooksWithPagination(1)
}

async function handleImportSubmit() {
  if (!importTitle.value || !importContent.value) return

  try {
    await libraryStore.importDocument({
      title: importTitle.value,
      markdownContent: importContent.value,
      category: importCategory.value,
      sourceUrl: importSourceUrl.value || undefined
    })

    toast.success(t('library.toast_import_success'))
    importTitle.value = ''
    importContent.value = ''
    importSourceUrl.value = ''
    isImportModalOpen.value = false
  } catch (err: any) {
    toast.error(formatError(err, 'library.toast_import_failed'))
  }
}

function onPdfFileChange(event: Event) {
  const target = event.target as HTMLInputElement
  if (target.files && target.files[0]) {
    selectPdf(target.files[0])
  }
}

function onPdfDrop(event: DragEvent) {
  isDraggingPdf.value = false
  if (event.dataTransfer?.files && event.dataTransfer.files[0]) {
    selectPdf(event.dataTransfer.files[0])
  }
}

function selectPdf(file: File) {
  if (!file.name.toLowerCase().endsWith('.pdf')) {
    toast.error(t('library.toast_pdf_only'))
    return
  }
  if (file.size > 367_001_600) {
    toast.error(t('library.toast_pdf_size_limit'))
    return
  }
  pdfFile.value = file
  if (!pdfTitle.value) {
    pdfTitle.value = file.name.replace(/\.pdf$/i, '')
  }
  const inferredPdfCat = inferCategoryFromContext(pdfTitle.value, file.name)
  if (inferredPdfCat !== 0) {
    pdfCategory.value = inferredPdfCat
  }
  if (importCategory.value === 0 && inferredPdfCat !== 0) {
    importCategory.value = inferredPdfCat
  }
}

async function handlePdfUpload() {
  if (!pdfFile.value) return
  isUploadingPdf.value = true
  isProcessingPdf.value = true
  pdfProgress.value = 5
  pdfStatusMessage.value = t('library.parsing_pdf')

  try {
    const formData = new FormData()
    formData.append('file', pdfFile.value, pdfFile.value.name)
    if (pdfTitle.value) formData.append('title', pdfTitle.value)
    formData.append('category', pdfCategory.value.toString())
    formData.append('language', locale.value || 'vi')

    const book = await libraryStore.uploadPdf(formData)
    isUploadingPdf.value = false

    if (book?.id) {
      pollingBookId.value = book.id
      startPdfPolling()
    } else {
      toast.success(t('library.toast_upload_success'))
      pdfFile.value = null
      pdfTitle.value = ''
      isImportModalOpen.value = false
      isProcessingPdf.value = false
    }
  } catch (err: any) {
    stopPdfPolling()
    pollingBookId.value = null
    isProcessingPdf.value = false
    toast.error(formatError(err, 'library.toast_upload_failed'))
  } finally {
    isUploadingPdf.value = false
  }
}

async function handleCrawlUrl() {
  if (!crawlUrlInput.value) return
  isCrawling.value = true
  crawlSuccess.value = false
  isPdfDetected.value = false
  detectedPdfUrl.value = null

  // Pre-infer category from crawl URL input to avoid defaulting blindly to 0
  const preInferred = inferCategoryFromContext('', crawlUrlInput.value)
  if (preInferred !== 0) {
    importCategory.value = preInferred
  }

  try {
    const result = await libraryStore.crawlUrl(crawlUrlInput.value)
    importTitle.value = result.title
    importSourceUrl.value = result.sourceUrl
    importContent.value = result.markdownContent
    crawlSuccess.value = true

    // Auto-infer category from crawled metadata (title, URL, markdown body)
    const inferred = inferCategoryFromContext(
      result.title,
      result.sourceUrl || crawlUrlInput.value,
      result.markdownContent
    )
    importCategory.value = inferred
    if (result.isPdfDetected) {
      isPdfDetected.value = true
      detectedPdfUrl.value = result.detectedPdfUrl || null
    } else {
      toast.success(t('library.toast_crawl_success'))
      // Switch to markdown tab for preview & confirmation
      activeTab.value = 'markdown'
    }
  } catch (err: any) {
    toast.error(formatError(err, 'library.toast_crawl_failed'))
  } finally {
    isCrawling.value = false
  }
}

async function handleImportRemotePdf() {
  if (!detectedPdfUrl.value) return
  isProcessingPdf.value = true
  try {
    await libraryStore.importRemotePdf({
      pdfUrl: detectedPdfUrl.value,
      title: importTitle.value || 'PDF Document',
      category: importCategory.value,
      language: locale.value || 'vi'
    })
    isImportModalOpen.value = false
    toast.success(t('library.upload_success_async'))
    await loadBooksWithPagination(1)
    checkBackgroundPolling()
  } catch (err: any) {
    toast.error(formatError(err, 'library.toast_import_failed'))
  } finally {
    isProcessingPdf.value = false
  }
}

function openDeleteModal(book: { id: string; title: string }) {
  bookToDelete.value = book
  isDeleteModalOpen.value = true
}

async function confirmDeleteBook() {
  if (!bookToDelete.value) return
  isDeleting.value = true
  try {
    await libraryStore.deleteBook(bookToDelete.value.id)
    toast.success(t('library.toast_delete_success'))
    isDeleteModalOpen.value = false
    bookToDelete.value = null
    await loadBooksWithPagination(libraryStore.currentPage)
  } catch (err: any) {
    toast.error(formatError(err, 'library.toast_delete_failed'))
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div class="max-w-6xl mx-auto p-4 sm:p-6 md:p-10 space-y-6 sm:space-y-8 bg-slate-50 dark:bg-canvas transition-colors duration-200">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-3">
          <BookOpen class="w-7 h-7 text-brand-600 dark:text-brand-400" />
          <span>{{ $t('library.title') }}</span>
        </h1>
        <p class="text-sm md:text-lg text-slate-500 dark:text-slate-400 mt-1.5 font-medium">{{ $t('library.subtitle') }}</p>
      </div>

      <button
        @click="isImportModalOpen = true"
        class="flex items-center gap-2 px-5 py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm transition-all shadow-md shadow-brand-500/20 active:scale-[0.98] shrink-0"
      >
        <Plus class="w-4 h-4" />
        <span>{{ $t('library.import_btn') }}</span>
      </button>
    </div>

    <!-- Filters & Search -->
    <div class="flex flex-col md:flex-row gap-4 justify-between items-stretch md:items-start">
      <!-- Category Pills -->
      <div class="flex flex-wrap gap-2 flex-1">
        <button
          v-for="cat in categories"
          :key="cat.label"
          @click="handleCategorySelect(cat.id)"
          :class="[
            'px-4 py-2 rounded-xl text-xs sm:text-sm border transition-all outline-none focus:outline-none',
            selectedCategory === cat.id
              ? 'bg-slate-100 dark:bg-canvas-elevated border-slate-300 dark:border-white/[0.12] text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:border-slate-300 dark:hover:border-white/[0.16] font-medium'
          ]"
        >
          {{ cat.label }}
        </button>
      </div>

      <!-- Search Input -->
      <div class="relative w-full md:w-80 shrink-0">
        <Search class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
        <input
          v-model="searchQuery"
          @keyup.enter="handleSearch"
          type="text"
          :placeholder="$t('library.search_placeholder')"
          class="w-full pl-10 pr-4 py-2 bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-200 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:border-brand-500 transition-colors shadow-sm"
        />
      </div>
    </div>

    <!-- Books Grid -->
    <div v-if="libraryStore.isLoading" class="flex flex-col items-center justify-center py-20 text-slate-500 dark:text-slate-400 text-sm">
      <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin mb-3"></div>
      <span>{{ $t('library.loading') }}</span>
    </div>

    <div v-else-if="libraryStore.books.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="book in libraryStore.books"
        :key="book.id"
        class="glass-card p-6 sm:p-7 rounded-3xl border-slate-200/80 dark:border-white/[0.08] hover:border-brand-400 dark:hover:border-brand-500/30 transition-all flex flex-col justify-between space-y-4 group shadow-md dark:shadow-none"
      >
        <div class="flex flex-col flex-1">
          <div class="flex items-center justify-between gap-2 mb-3.5">
            <span class="px-3 py-1 rounded-lg bg-brand-50 dark:bg-brand-500/10 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-300 text-xs font-bold">
              {{ getCategoryLabel(book.category) }}
            </span>
            <span class="text-xs text-slate-500 font-mono flex items-center gap-1">
              <Layers class="w-3.5 h-3.5" />
              {{ book.totalChunks }} {{ $t('library.chunks') }}
            </span>
          </div>

          <h3 class="text-lg font-bold text-slate-900 dark:text-white group-hover:text-brand-600 dark:group-hover:text-brand-300 transition-colors line-clamp-2 leading-snug">
            {{ book.title }}
          </h3>

          <p v-if="book.authorOrSourceUrl" class="text-xs sm:text-sm text-slate-500 mt-2 truncate font-mono">
            {{ book.authorOrSourceUrl }}
          </p>

          <div class="mt-auto pt-3">
            <!-- Bookmark Badge if exists -->
            <div v-if="bookmarks[book.id]" class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-brand-50 dark:bg-brand-500/10 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-300 text-xs sm:text-sm font-semibold">
              <Bookmark class="w-3.5 h-3.5 text-brand-500 fill-brand-500" />
              <span>{{ $t('library.resumes_at', { slice: bookmarks[book.id] }) }}</span>
            </div>
            <!-- Ready Badge if no bookmark -->
            <div v-else-if="book.status === 'Ready' || (book.status as unknown as number) === 2" class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 text-xs sm:text-sm font-semibold">
              <BookOpen class="w-3.5 h-3.5 text-slate-500 dark:text-slate-400" />
              <span>{{ $t('library.ready_to_read') }}</span>
            </div>

            <!-- In-Progress Ingestion Indicator (Tier 1 Uploading) -->
            <div v-if="book.status === 'Processing' || (book.status as any) === 1" class="p-3 rounded-2xl bg-brand-500/10 dark:bg-brand-500/15 border border-brand-500/20">
              <div class="flex items-center gap-2 text-xs font-semibold text-brand-700 dark:text-brand-300">
                <Loader2 class="w-3.5 h-3.5 text-brand-500 animate-spin shrink-0" />
                <span class="truncate">{{ getStatusMessage(book) }}</span>
              </div>
            </div>
          </div>
        </div>
        <div class="pt-4 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-between gap-3">
          <div class="flex items-center gap-1.5 min-w-0">
            <button
              @click.stop="openDeleteModal(book)"
              class="p-2 rounded-xl text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 border border-transparent hover:border-rose-200 dark:hover:border-rose-900/50 transition-colors shrink-0"
              :title="$t('library.delete_doc')"
              :aria-label="$t('library.delete_doc')"
            >
              <Trash2 class="w-4 h-4" />
            </button>
            <button
              @click.stop="handleExportBook(book)"
              :disabled="exportingBookId === book.id"
              class="p-2 rounded-xl text-slate-400 hover:text-brand-600 hover:bg-brand-50 dark:hover:bg-brand-500/10 border border-transparent hover:border-brand-200 dark:hover:border-brand-500/20 transition-colors shrink-0 disabled:opacity-50"
              :title="$t('reader.export_obsidian')"
              :aria-label="$t('reader.export_obsidian')"
            >
              <Download class="w-4 h-4" />
            </button>
          </div>

          <NuxtLink
            :to="`/read/${book.id}`"
            class="inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs sm:text-sm transition-transform active:scale-95 shadow-sm whitespace-nowrap shrink-0"
          >
            <span>{{ bookmarks[book.id] ? $t('library.continue_reading') : $t('library.read_book') }}</span>
            <ExternalLink class="w-3.5 h-3.5" />
          </NuxtLink>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <BasePagination
      v-if="libraryStore.books.length > 0"
      :current-page="libraryStore.currentPage"
      :total-pages="libraryStore.totalPages"
      :total-count="libraryStore.totalCount"
      :page-size="libraryStore.pageSize"
      show-summary
      @change="onPageChange"
    />

    <!-- Empty state -->
    <div v-else class="text-center py-16 glass-card rounded-3xl border border-slate-200/80 dark:border-white/[0.08] p-8 shadow-sm">
      <FileText class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto mb-3" />
      <h3 class="text-base font-bold text-slate-800 dark:text-slate-200">{{ $t('library.no_books') }}</h3>
      <p class="text-sm text-slate-500 mt-1">{{ $t('library.empty_desc') }}</p>
    </div>

    <!-- Import Document Modal (Teleported to Body) -->
    <Teleport to="body">
      <div v-if="isImportModalOpen" class="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4 bg-black/75 backdrop-blur-sm animate-in fade-in" @click.self="isImportModalOpen = false">
        <div class="w-full max-w-2xl glass-panel border border-slate-200/80 dark:border-white/[0.08] rounded-3xl shadow-2xl p-5 sm:p-8 md:p-9 space-y-5 sm:space-y-6 animate-in zoom-in-95 max-h-[85dvh] overflow-y-auto [scrollbar-gutter:stable]">
          <div class="flex items-center justify-between pb-3 border-b border-slate-200/80 dark:border-white/[0.08] gap-2">
            <div class="min-w-0">
              <h3 class="text-base sm:text-xl font-bold text-slate-900 dark:text-white truncate">{{ $t('library.import_modal_title') }}</h3>
              <p class="text-xs text-slate-500 dark:text-slate-400 mt-1 line-clamp-1">{{ $t('library.import_modal_desc') }}</p>
            </div>
            <button @click="isImportModalOpen = false" class="p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-canvas-elevated shrink-0" aria-label="Close modal">
              <X class="w-5 h-5" :stroke-width="1.5" />
            </button>
          </div>

          <!-- 3-Tab Selector -->
          <div class="flex items-center gap-1 sm:gap-2 p-1 sm:p-1.5 bg-slate-100 dark:bg-canvas-subtle rounded-2xl border border-slate-200/80 dark:border-white/[0.08]">
            <button
              type="button"
              @click="activeTab = 'markdown'"
              :class="[
                'flex-1 py-2 sm:py-2.5 px-2 sm:px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-1.5 sm:gap-2 transition-colors border',
                activeTab === 'markdown'
                  ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border-slate-200/80 dark:border-white/[0.12]'
                  : 'border-transparent text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              ]"
            >
              <FileText class="w-4 h-4 shrink-0" />
              <span class="truncate">{{ $t('library.tab_markdown') }}</span>
            </button>

            <button
              type="button"
              @click="activeTab = 'pdf'"
              :class="[
                'flex-1 py-2 sm:py-2.5 px-2 sm:px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-1.5 sm:gap-2 transition-colors border',
                activeTab === 'pdf'
                  ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border-slate-200/80 dark:border-white/[0.12]'
                  : 'border-transparent text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              ]"
            >
              <FileUp class="w-4 h-4 shrink-0" />
              <span class="truncate">{{ $t('library.tab_pdf') }}</span>
            </button>

            <button
              type="button"
              @click="activeTab = 'url'"
              :class="[
                'flex-1 py-2 sm:py-2.5 px-2 sm:px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-1.5 sm:gap-2 transition-colors border',
                activeTab === 'url'
                  ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border-slate-200/80 dark:border-white/[0.12]'
                  : 'border-transparent text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              ]"
            >
              <Globe class="w-4 h-4 shrink-0" />
              <span class="truncate">{{ $t('library.tab_url') }}</span>
            </button>
          </div>

          <!-- TAB 1: Markdown Direct Form -->
          <form v-if="activeTab === 'markdown'" @submit.prevent="handleImportSubmit" class="space-y-4">
            <div>
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.title_label') }}</label>
              <input
                v-model="importTitle"
                required
                type="text"
                placeholder="e.g. Designing Data-Intensive Applications — Chapter 5"
                class="w-full px-4 py-3 bg-slate-50 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none"
              />
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.category_label') }}</label>
                <AppSelect
                  v-model="importCategory"
                  :options="formCategoryOptions"
                  :aria-label="$t('library.category_label')"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.url_label') }}</label>
                <input
                  v-model="importSourceUrl"
                  type="url"
                  placeholder="https://..."
                  class="w-full px-4 py-3 bg-slate-50 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none"
                />
              </div>
            </div>

            <div class="p-3 rounded-xl bg-amber-50/80 dark:bg-amber-950/30 border border-amber-200/80 dark:border-amber-900/50 flex items-center gap-2.5 text-xs text-amber-800 dark:text-amber-300">
              <Lightbulb class="w-4 h-4 shrink-0 text-amber-500" />
              <span>{{ $t('library.verbatim_category_hint') }}</span>
            </div>

            <div>
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.content_label') }}</label>
              <textarea
                v-model="importContent"
                required
                rows="6"
                :placeholder="$t('library.content_placeholder')"
                class="w-full p-4 bg-slate-50 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm font-mono text-slate-800 dark:text-slate-200 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none resize-none"
              ></textarea>
            </div>

            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-2.5 sm:gap-3 pt-3 sm:pt-4">
              <button
                type="button"
                @click="isImportModalOpen = false"
                class="w-full sm:w-auto px-5 py-2.5 sm:py-3 rounded-xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-slate-200 dark:border-white/[0.08] sm:border-transparent transition-colors text-center"
              >
                {{ $t('library.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="libraryStore.isImporting"
                class="w-full sm:w-auto flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md transition-all active:scale-95 disabled:opacity-50"
              >
                <span v-if="libraryStore.isImporting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <span>{{ libraryStore.isImporting ? $t('library.importing') : $t('library.import_action') }}</span>
              </button>
            </div>
          </form>

          <!-- TAB 2: PDF Drag & Drop Upload Form -->
          <form v-else-if="activeTab === 'pdf'" @submit.prevent="handlePdfUpload" class="space-y-4">
            <!-- Asynchronous Ingestion Progress Card -->
            <div v-if="isProcessingPdf" class="p-5 sm:p-6 rounded-2xl bg-brand-50/70 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-800 space-y-4 animate-in fade-in duration-200">
              <div class="flex items-center justify-between gap-3">
                <div class="flex items-center gap-2 text-brand-700 dark:text-brand-300 font-bold text-xs sm:text-sm">
                  <Loader2 class="w-4 h-4 animate-spin text-brand-600 dark:text-brand-400 shrink-0" />
                  <span>{{ $t('library.processing_pdf') }}</span>
                </div>
                <span class="font-mono font-bold text-xs sm:text-sm text-brand-600 dark:text-brand-400">{{ pdfProgress }}%</span>
              </div>

              <!-- Realtime Progress Bar -->
              <div class="w-full h-2.5 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden p-0.5 border border-slate-200 dark:border-slate-700">
                <div
                  class="h-full bg-gradient-to-r from-brand-500 to-emerald-400 rounded-full transition-all duration-300 shadow-sm"
                  :style="{ width: `${pdfProgress}%` }"
                ></div>
              </div>

              <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed font-medium">
                {{ pdfStatusMessage || $t('library.processing_desc') }}
              </p>

              <div class="flex items-center justify-between text-xs text-slate-400 dark:text-slate-500 pt-2 border-t border-brand-200/50 dark:border-brand-800/50">
                <span>{{ $t('library.pdf_service_note_1') }}</span>
                <span>{{ $t('library.pdf_service_note_2') }}</span>
              </div>
            </div>

            <!-- Dropzone -->
            <div
              v-show="!isProcessingPdf"
              @dragover.prevent="isDraggingPdf = true"
              @dragleave.prevent="isDraggingPdf = false"
              @drop.prevent="onPdfDrop"
              :class="[
                'border-2 border-dashed rounded-3xl p-6 sm:p-8 text-center transition-all cursor-pointer relative',
                isDraggingPdf
                  ? 'border-brand-500 bg-brand-50/50 dark:bg-brand-950/40'
                  : 'border-slate-300 dark:border-white/[0.08] hover:border-brand-400 dark:hover:border-brand-500/30 bg-slate-50/60 dark:bg-canvas-subtle'
              ]"
              @click="($refs.pdfInput as HTMLInputElement)?.click()"
            >
              <input
                ref="pdfInput"
                type="file"
                accept=".pdf,application/pdf"
                class="hidden"
                @change="onPdfFileChange"
              />

              <div class="flex flex-col items-center justify-center space-y-3">
                <div class="w-12 h-12 sm:w-14 sm:h-14 rounded-2xl bg-brand-100 dark:bg-brand-950/80 text-brand-600 dark:text-brand-400 flex items-center justify-center border border-brand-200 dark:border-brand-900 shadow-sm">
                  <UploadCloud class="w-6 h-6 sm:w-7 sm:h-7" />
                </div>

                <div v-if="!pdfFile" class="space-y-1">
                  <h4 class="text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200">
                    {{ $t('library.pdf_drop_title') }}
                  </h4>
                  <p class="text-sm text-slate-500 dark:text-slate-400">
                    {{ $t('library.pdf_drop_desc') }}
                  </p>
                  <p class="text-xs sm:text-sm text-brand-600 dark:text-brand-400 font-mono pt-1">
                    {{ $t('library.pdf_size_limit') }}
                  </p>
                </div>

                <div v-else class="space-y-1">
                  <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-900 text-emerald-700 dark:text-emerald-300 text-xs font-bold">
                    <CheckCircle2 class="w-4 h-4" />
                    <span>{{ pdfFile.name }} ({{ (pdfFile.size / (1024 * 1024)).toFixed(1) }} MB)</span>
                  </div>
                  <p class="text-xs text-slate-400">{{ $t('library.pdf_replace_hint') }}</p>
                </div>
              </div>
            </div>

            <div v-show="!isProcessingPdf" class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.title_label') }}</label>
                <input
                  v-model="pdfTitle"
                  type="text"
                  :placeholder="$t('library.title_placeholder')"
                  class="w-full px-4 py-3 bg-slate-50 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.category_label') }}</label>
                <AppSelect
                  v-model="pdfCategory"
                  :options="formCategoryOptions"
                  :aria-label="$t('library.category_label')"
                />
              </div>
            </div>

            <div v-show="!isProcessingPdf" class="p-3 rounded-xl bg-amber-50/80 dark:bg-amber-950/30 border border-amber-200/80 dark:border-amber-900/50 flex items-center gap-2.5 text-xs text-amber-800 dark:text-amber-300">
              <Lightbulb class="w-4 h-4 shrink-0 text-amber-500" />
              <span>{{ $t('library.verbatim_category_hint') }}</span>
            </div>

            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-2.5 sm:gap-3 pt-3 sm:pt-4">
              <button
                type="button"
                @click="isImportModalOpen = false; if (pollInterval) { clearInterval(pollInterval); pollInterval = null; isProcessingPdf = false; }"
                class="w-full sm:w-auto px-5 py-2.5 sm:py-3 rounded-xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-slate-200 dark:border-white/[0.08] sm:border-transparent transition-colors text-center"
              >
                {{ $t('library.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="!pdfFile || isUploadingPdf || isProcessingPdf"
                class="w-full sm:w-auto flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md transition-all active:scale-95 disabled:opacity-50"
              >
                <span v-if="isUploadingPdf || isProcessingPdf" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <span>{{ isProcessingPdf ? `${$t('library.processing_pdf')} (${pdfProgress}%)` : (isUploadingPdf ? $t('library.parsing_pdf') : $t('library.upload_pdf_action')) }}</span>
              </button>
            </div>
          </form>

          <!-- TAB 3: Web URL Crawler Form -->
          <div v-else-if="activeTab === 'url'" class="space-y-4">
            <div>
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">
                {{ $t('library.url_crawler_title') }}
              </label>
              <div class="flex flex-col sm:flex-row gap-2">
                <input
                  v-model="crawlUrlInput"
                  type="url"
                  :placeholder="$t('library.url_input_placeholder')"
                  class="flex-1 px-4 py-3 bg-slate-50 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none"
                  @keyup.enter="handleCrawlUrl"
                />
                <button
                  type="button"
                  :disabled="!crawlUrlInput || isCrawling"
                  @click="handleCrawlUrl"
                  class="w-full sm:w-auto flex items-center justify-center gap-2 px-5 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md transition-all active:scale-95 disabled:opacity-50 shrink-0"
                >
                  <span v-if="isCrawling" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <Globe v-else class="w-4 h-4" />
                  <span>{{ isCrawling ? $t('library.fetching_url') : $t('library.fetch_url_btn') }}</span>
                </button>
              </div>
            </div>

            <!-- Category Selector for URL import -->
            <div>
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.category_label') }}</label>
              <AppSelect
                v-model="importCategory"
                :options="formCategoryOptions"
                :aria-label="$t('library.category_label')"
              />
              <div class="mt-2.5 p-3 rounded-xl bg-amber-50/80 dark:bg-amber-950/30 border border-amber-200/80 dark:border-amber-900/50 flex items-center gap-2.5 text-xs text-amber-800 dark:text-amber-300">
                <Lightbulb class="w-4 h-4 shrink-0 text-amber-500" />
                <span>{{ $t('library.verbatim_category_hint') }}</span>
              </div>
            </div>

            <!-- Embedded PDF Preview Card -->
            <div v-if="isPdfDetected && detectedPdfUrl" class="p-4 rounded-2xl bg-indigo-50/70 dark:bg-indigo-950/40 border border-indigo-200 dark:border-indigo-800/80 space-y-3">
              <div class="flex items-center gap-2 text-indigo-700 dark:text-indigo-300 font-bold text-xs sm:text-sm">
                <FileUp class="w-4 h-4 shrink-0" />
                <span>{{ $t('library.embedded_pdf_detected') }}</span>
              </div>
              <p class="text-xs text-slate-600 dark:text-slate-400 font-mono truncate">{{ detectedPdfUrl }}</p>
              <button
                type="button"
                @click="handleImportRemotePdf"
                :disabled="isProcessingPdf"
                class="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs sm:text-sm shadow-md transition-all active:scale-95 disabled:opacity-50 whitespace-nowrap shrink-0"
              >
                <FileUp class="w-4 h-4 shrink-0" />
                <span class="whitespace-nowrap">{{ $t('library.import_detected_pdf') }}</span>
              </button>
            </div>

            <div class="p-4 rounded-2xl bg-slate-50 dark:bg-slate-950/60 border border-slate-200 dark:border-slate-800 text-xs text-slate-500 dark:text-slate-400 space-y-2">
              <h5 class="font-bold text-slate-700 dark:text-slate-300">Supported Sources:</h5>
              <ul class="list-disc list-inside space-y-1">
                <li><strong>GitHub Repositories:</strong> Links to <code>README.md</code> or any <code>.md</code> file in a repository.</li>
                <li><strong>Technical Blogs & RFCs:</strong> Microsoft Learn, Martin Fowler, Dev.to, Medium, Substack architecture posts.</li>
              </ul>
            </div>

            <div class="flex justify-end pt-2">
              <button
                type="button"
                @click="isImportModalOpen = false"
                class="w-full sm:w-auto px-5 py-2.5 sm:py-3 rounded-xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-slate-200 dark:border-white/[0.08] sm:border-transparent transition-colors text-center"
              >
                {{ $t('library.cancel') }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Delete Confirmation Modal (Teleported to Body) -->
    <Teleport to="body">
      <div v-if="isDeleteModalOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-in fade-in" @click.self="isDeleteModalOpen = false; bookToDelete = null">
        <div class="w-full max-w-md glass-panel border border-slate-200/80 dark:border-white/[0.08] rounded-3xl shadow-2xl p-6 sm:p-8 space-y-5 animate-in zoom-in-95">
          <div class="w-12 h-12 rounded-2xl bg-rose-100 dark:bg-rose-500/20 text-rose-600 dark:text-rose-400 border border-rose-200 dark:border-rose-500/30 flex items-center justify-center mx-auto">
            <AlertTriangle class="w-6 h-6" />
          </div>

          <div class="text-center space-y-2">
            <h3 class="text-lg sm:text-xl font-bold text-slate-900 dark:text-white">
              {{ $t('library.confirm_delete_title') }}
            </h3>
            <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
              {{ $t('library.confirm_delete_desc', { title: bookToDelete?.title }) }}
            </p>
          </div>

          <div class="flex items-center gap-3 pt-2">
            <button
              type="button"
              @click="isDeleteModalOpen = false; bookToDelete = null"
              class="flex-1 py-3 rounded-xl border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated font-semibold text-xs sm:text-sm transition-colors"
            >
              {{ $t('library.cancel') }}
            </button>
            <button
              type="button"
              :disabled="isDeleting"
              @click="confirmDeleteBook"
              class="flex-1 py-3 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-semibold text-xs sm:text-sm shadow-md shadow-rose-600/20 transition-all active:scale-95 disabled:opacity-50 flex items-center justify-center gap-2"
            >
              <span v-if="isDeleting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ isDeleting ? $t('library.deleting') : $t('library.confirm_delete_btn') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
