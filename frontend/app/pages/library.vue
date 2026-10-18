<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useIntervalFn } from '@vueuse/core'
import { BookOpen, Plus, FileText, Bookmark, Trash2, AlertTriangle, FileUp, Globe, CheckCircle2, UploadCloud, Loader2, Download, Lightbulb, GraduationCap, ChevronRight, Pencil, Compass } from 'lucide-vue-next'
import AppModal from '~/components/ui/AppModal.vue'
import BasePagination from '~/components/common/BasePagination.vue'
import AppSelect from '~/components/common/AppSelect.vue'
import BoardLayout from '~/components/layout/BoardLayout.vue'
import CatalogCard from '~/components/catalog/CatalogCard.vue'
import CatalogPreviewModal from '~/components/catalog/CatalogPreviewModal.vue'
import CatalogFilterModal from '~/components/catalog/CatalogFilterModal.vue'
import LibraryFilterModal from '~/components/library/LibraryFilterModal.vue'
import LibraryToolbar from '~/components/library/LibraryToolbar.vue'
import { useLibraryQueryState } from '~/composables/useLibraryQueryState'
import { useCatalogStore, type LearnerCatalogDocument } from '~/stores/useCatalogStore'
import { useLibraryStore } from '~/stores/useLibraryStore'
import { useCategoryStore, type CategoryDescriptor } from '~/stores/useCategoryStore'
const { t, locale } = useI18n()
const { formatError } = useApiError()
const libraryStore = useLibraryStore()
const catalogStore = useCatalogStore()
const categoryStore = useCategoryStore()
const assignmentAvailable = (id: string) => !categoryStore.error && !categoryStore.isLoading && categoryStore.activeCategories.some(category => category.id === id)
const categoryFeedback = computed(() => categoryStore.error
  ? t('categories.metadataUnavailable')
  : categoryStore.hasLoaded && !categoryStore.activeCategories.length ? t('categories.noActiveCategories') : '')
async function refreshRejectedCategory(error: unknown) {
  if (error && typeof error === 'object' && 'code' in error && typeof error.code === 'string' && error.code.startsWith('Category.')) {
    await categoryStore.fetchCategories(true)
  }
}
const toast = useToast()

const {
  query: libraryQuery,
  currentTab,
  searchDraft: searchQuery,
  commitSearch: handleSearch,
  clearSearch,
  selectCategory: handleCategorySelect,
  applyFilters,
  resetFilters,
  changePage: onPageChange,
  canonicalize
} = useLibraryQueryState()
const selectedCategory = computed(() => libraryQuery.value.categoryId ?? undefined)
const selectedLanguage = computed(() => libraryQuery.value.language ?? undefined)
const selectedSourceType = computed(() => libraryQuery.value.sourceType ?? undefined)
const isPreviewModalOpen = ref(false)
const isCatalogFilterModalOpen = ref(false)
const isLibraryFilterModalOpen = ref(false)
const adoptingDocId = ref<string | null>(null)
const activeLibraryFilterCount = computed(() => Number(!!selectedCategory.value) + Number(!!selectedLanguage.value))
const activeDiscoverFilterCount = computed(() => activeLibraryFilterCount.value + Number(!!selectedSourceType.value))
const quickCategories = computed(() => categoryStore.allCategories.map(category => ({
  id: category.id,
  label: categoryStore.getCategoryName(category, locale.value)
})))

async function loadDiscover(page = libraryQuery.value.page) {
  const query = libraryQuery.value
  await catalogStore.fetchPublishedDocuments({
    search: query.search,
    categoryId: query.categoryId,
    sourceType: query.sourceType,
    language: query.language,
    page,
    pageSize: 12
  })
}

async function handleOpenPreview(doc: LearnerCatalogDocument) {
  try {
    await catalogStore.fetchPublishedDocumentById(doc.id)
    isPreviewModalOpen.value = true
  } catch (err: unknown) {
    toast.error(formatError(err))
  }
}

async function handleAdoptDocument(doc: { id: string }) {
  adoptingDocId.value = doc.id
  try {
    const res = await catalogStore.adoptDocument(doc.id)
    if (res?.alreadyAdded) {
      toast.info(t('library.discover.already_added'))
    } else {
      toast.success(t('library.discover.adoption_success'))
    }
  } catch (err: unknown) {
    toast.error(formatError(err))
  } finally {
    adoptingDocId.value = null
  }
}
const bookmarks = ref<Record<string, number>>({})
const exportingBookId = ref<string | null>(null)

async function handleExportBook(book: { id: string; slug?: string }) {
  exportingBookId.value = book.id
  try {
    await libraryStore.exportBookMarkdown(book.id, book.slug)
    toast.success(t('reader.toast_export_success') || 'Notes exported successfully!')
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Failed to export notes.'
    toast.error(message)
  } finally {
    exportingBookId.value = null
  }
}

// Import modal state
const isImportModalOpen = ref(false)
const activeTab = ref<'markdown' | 'pdf' | 'url'>('markdown')

// Tab 1: Markdown form state
const importTitle = ref('')
const importCategory = ref('')
const importSourceUrl = ref('')
const importContent = ref('')

// Tab 2: PDF Upload state
const pdfFile = ref<File | null>(null)
const pdfTitle = ref('')
const pdfCategory = ref('')
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

// Document language selection (defaults to the interface locale, user-overridable)
const uploadLanguage = ref(locale.value || 'vi')
const importLanguage = ref(locale.value || 'vi')

// Delete modal state
const bookToDelete = ref<{ id: string; title: string } | null>(null)
const isDeleteModalOpen = ref(false)
const isDeleting = ref(false)

const formCategoryOptions = computed(() => categoryStore.activeCategories.map(category => ({
  value: category.id,
  label: categoryStore.getCategoryName(category, locale.value)
})))
const languageOptions = computed(() => [
  { value: 'en', label: t('library.language_english') },
  { value: 'vi', label: t('library.language_vietnamese') }
])
function getCategoryLabel(category: CategoryDescriptor | null | undefined): string {
  return categoryStore.getCategoryName(categoryStore.getCategoryById(category?.id) ?? category, locale.value)
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
    await loadBooksWithPagination()
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
        await loadBooksWithPagination()
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

onMounted(() => {
  watch(libraryQuery, async (query, _previous, onCleanup) => {
    let current = true
    onCleanup(() => {
      current = false
      libraryStore.cancelInFlight()
      catalogStore.cancelInFlight()
    })
    stopBackgroundPolling()
    if (await canonicalize() || !current) return
    void categoryStore.ensureCategories()
    if (!current) return
    if (query.tab === 'discover') {
      await loadDiscover(query.page)
      return
    }
    await loadBooksWithPagination(query.page)
    if (!current) return
    checkBackgroundPolling()
    loadBookmarks()
  }, { immediate: true, flush: 'post' })
})


function loadBookmarks() {
  if (typeof window === 'undefined') return
  try {
    const loaded: Record<string, number> = {}
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i)
      if (key?.startsWith('deeppace_bookmark_')) {
        const bookId = key.replace('deeppace_bookmark_', '')
        const slice = parseInt(localStorage.getItem(key) || '1', 10)
        loaded[bookId] = slice
      }
    }
    bookmarks.value = loaded
  } catch (e) {
    // ignore
  }
}

async function loadBooksWithPagination(page = libraryQuery.value.page) {
  const query = libraryQuery.value
  await libraryStore.fetchBooks({
    categoryId: query.categoryId,
    search: query.search,
    language: query.language,
    page,
    pageSize: libraryStore.pageSize
  })
}

async function handleImportSubmit() {
  if (!importTitle.value || !importContent.value || !assignmentAvailable(importCategory.value)) return

  try {
    await libraryStore.importDocument({
      title: importTitle.value,
      markdownContent: importContent.value,
      categoryId: importCategory.value,
      sourceUrl: importSourceUrl.value || undefined,
      language: importLanguage.value
    })

    toast.success(t('library.toast_import_success'))
    importTitle.value = ''
    importContent.value = ''
    importSourceUrl.value = ''
    isImportModalOpen.value = false
  } catch (err: any) {
    await refreshRejectedCategory(err)
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
}

async function handlePdfUpload() {
  if (!pdfFile.value || !assignmentAvailable(pdfCategory.value)) return
  isUploadingPdf.value = true
  isProcessingPdf.value = true
  pdfProgress.value = 5
  pdfStatusMessage.value = t('library.parsing_pdf')

  try {
    const formData = new FormData()
    formData.append('file', pdfFile.value, pdfFile.value.name)
    if (pdfTitle.value) formData.append('title', pdfTitle.value)
    formData.append('categoryId', pdfCategory.value)
    formData.append('language', uploadLanguage.value)

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
    await refreshRejectedCategory(err)
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


  try {
    const result = await libraryStore.crawlUrl(crawlUrlInput.value)
    importTitle.value = result.title
    importSourceUrl.value = result.sourceUrl
    importContent.value = result.markdownContent
    crawlSuccess.value = true

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
  if (!detectedPdfUrl.value || !assignmentAvailable(importCategory.value)) return
  isProcessingPdf.value = true
  try {
    await libraryStore.importRemotePdf({
      pdfUrl: detectedPdfUrl.value,
      title: importTitle.value || 'PDF Document',
      categoryId: importCategory.value,
      language: importLanguage.value
    })
    isImportModalOpen.value = false
    toast.success(t('library.upload_success_async'))
    if (libraryQuery.value.page !== 1) await onPageChange(1)
    else await loadBooksWithPagination()
    checkBackgroundPolling()
  } catch (err: any) {
    await refreshRejectedCategory(err)
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
  } catch (err: unknown) {
    toast.error(formatError(err, 'library.toast_delete_failed'))
  } finally {
    isDeleting.value = false
  }
}

// Edit book modal state
const isEditModalOpen = ref(false)
const isSavingBook = ref(false)
const editingBook = ref<{
  id: string
  title: string
  authorOrSourceUrl?: string | null
  categoryId: string
  category: CategoryDescriptor
  language?: string
} | null>(null)
const editTitle = ref('')
const editAuthor = ref('')
const editCategory = ref('')
const editLanguage = ref('en')
const editCategoryOptions = computed(() => {
  const retained = editingBook.value?.category
  const current = categoryStore.getCategoryById(editingBook.value?.categoryId) ?? retained
  const options = formCategoryOptions.value
  if (!current || current.isActive) return options
  return [...options, { value: current.id, label: `${categoryStore.getCategoryName(current, locale.value)} (${t('categories.inactive')})` }]
})
function openEditModal(book: { id: string; title: string; authorOrSourceUrl?: string | null; categoryId: string; category: CategoryDescriptor; language?: string }) {
  editingBook.value = { ...book, language: book.language || 'en' }
  editTitle.value = book.title || ''
  editAuthor.value = book.authorOrSourceUrl || ''
  editCategory.value = book.categoryId
  editLanguage.value = book.language || 'en'
  isEditModalOpen.value = true
}

async function handleSaveEditBook() {
  if (!editingBook.value) return
  if (editCategory.value !== editingBook.value.categoryId && !assignmentAvailable(editCategory.value)) return
  if (!editTitle.value.trim()) {
    toast.error(t('library.edit_title_required'))
    return
  }

  isSavingBook.value = true
  try {
    await libraryStore.updateBook(editingBook.value.id, {
      title: editTitle.value.trim(),
      authorOrSourceUrl: editAuthor.value.trim() || null,
      categoryId: editCategory.value,
      language: editLanguage.value
    })
    toast.success(t('library.toast_edit_success'))
    isEditModalOpen.value = false
    editingBook.value = null
    await loadBooksWithPagination(libraryStore.currentPage)
  } catch (err: unknown) {
    await refreshRejectedCategory(err)
    const message = err instanceof Error ? err.message : t('library.toast_edit_error')
    toast.error(message)
  } finally {
    isSavingBook.value = false
  }
}
</script>

<template>
  <div class="py-4 sm:py-6 px-4 sm:px-6 lg:px-8 bg-slate-50 dark:bg-canvas min-h-[calc(100vh-3.5rem)] sm:min-h-[calc(100vh-3.75rem)] transition-colors duration-200">
    <div class="max-w-7xl mx-auto">
      <BoardLayout flat class="w-full">
        <!-- Header -->
        <template #header>
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 w-full">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
                <BookOpen class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />
              </div>
              <div>
                <h1 class="text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white flex items-center gap-2">
                  <span>{{ currentTab === 'discover' ? $t('library.discover.title') : $t('library.title') }}</span>
                </h1>
                <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium">
                  {{ currentTab === 'discover' ? $t('library.discover.subtitle') : $t('library.subtitle') }}
                </p>
              </div>
            </div>

            <div class="flex flex-wrap items-center gap-3">
              <!-- Tab Switcher -->
              <div class="flex items-center p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/80 dark:border-white/[0.08]">
                <button
                  type="button"
                  @click="currentTab = 'mine'"
                  :class="[
                    'px-3.5 py-1.5 rounded-lg text-xs sm:text-sm font-semibold transition-all whitespace-nowrap shrink-0 cursor-pointer',
                    currentTab === 'mine'
                      ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                  ]"
                >
                  {{ $t('library.tabs.mine') }}
                </button>
                <button
                  type="button"
                  @click="currentTab = 'discover'"
                  :class="[
                    'px-3.5 py-1.5 rounded-lg text-xs sm:text-sm font-semibold transition-all whitespace-nowrap shrink-0 cursor-pointer',
                    currentTab === 'discover'
                      ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                  ]"
                >
                  {{ $t('library.tabs.discover') }}
                </button>
              </div>

              <!-- Import Document Button (My Library tab) -->
              <button
                v-if="currentTab === 'mine'"
                @click="isImportModalOpen = true"
                class="h-10 flex items-center gap-2 px-4 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs sm:text-sm transition-all shadow-md shadow-brand-500/20 active:scale-[0.98] whitespace-nowrap shrink-0 cursor-pointer"
              >
                <Plus class="w-4 h-4" :stroke-width="2" />
                <span>{{ $t('library.import_btn') }}</span>
              </button>
            </div>
          </div>
        </template>

        <!-- Filters Bar -->
        <template #filters>
          <LibraryToolbar
            v-model="searchQuery"
            :scope="currentTab === 'mine' ? 'library' : 'catalog'"
            :search-placeholder="$t(currentTab === 'mine' ? 'library.search_placeholder' : 'library.discover.search_placeholder')"
            :categories="quickCategories"
            :selected-category="libraryQuery.categoryId"
            :active-filter-count="currentTab === 'mine' ? activeLibraryFilterCount : activeDiscoverFilterCount"
            :filters-open="currentTab === 'mine' ? isLibraryFilterModalOpen : isCatalogFilterModalOpen"
            @search="handleSearch"
            @clear="clearSearch"
            @category="handleCategorySelect"
            @filters="isLibraryFilterModalOpen = currentTab === 'mine'; isCatalogFilterModalOpen = currentTab === 'discover'"
          />
        </template>

        <!-- Content Slot -->
        <template #content>
          <!-- My Library Content -->
          <div v-if="currentTab === 'mine'">

      <!-- Content Grid -->
      <!-- Books Grid Loading -->
          <div v-if="libraryStore.isLoading" class="flex flex-col items-center justify-center py-20 text-slate-500 dark:text-slate-400 text-sm">
            <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin mb-3"></div>
            <span>{{ $t('library.loading') }}</span>
          </div>

          <div v-else-if="libraryStore.books.length > 0" class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            <div
              v-for="book in libraryStore.books"
              :key="book.id"
              class="p-5 rounded-2xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40 transition-all flex flex-col justify-between space-y-4 shadow-sm"
            >
              <div class="flex flex-col flex-1 space-y-2">
                <div class="flex items-center justify-between gap-2 mb-1">
                  <span class="px-2.5 py-0.5 rounded-md text-[10px] font-bold uppercase tracking-wider bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20 whitespace-nowrap shrink-0">
                    {{ getCategoryLabel(book.category) }}
                  </span>
                  <span class="text-xs text-slate-400 font-mono flex items-center gap-1">
                    <Bookmark class="w-3.5 h-3.5 text-brand-400" />
                    {{ book.totalChunks }} {{ $t('library.chunks') }}
                  </span>
                </div>

                <h3 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white line-clamp-2 leading-snug">
                  {{ book.title }}
                </h3>

                <p v-if="book.authorOrSourceUrl" class="text-xs text-slate-500 dark:text-slate-400 font-mono truncate">
                  {{ book.authorOrSourceUrl }}
                </p>

                <div class="mt-auto pt-3 space-y-2">
                  <!-- Bookmark Badge if exists -->
                  <div v-if="bookmarks[book.id]" class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-brand-50 dark:bg-brand-500/10 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-300 text-xs font-semibold">
                    <Bookmark class="w-3.5 h-3.5 text-brand-500 fill-brand-500" />
                    <span>{{ $t('library.resumes_at', { slice: bookmarks[book.id] }) }}</span>
                  </div>
                  <!-- Ready Badge if no bookmark -->
                  <div v-else-if="book.status === 'Ready' || (book.status as unknown as number) === 2" class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 text-xs font-semibold">
                    <BookOpen class="w-3.5 h-3.5 text-slate-500 dark:text-slate-400" />
                    <span>{{ $t('library.ready_to_read') }}</span>
                  </div>
                  <!-- In-Progress Ingestion Indicator -->
                  <div v-if="book.status === 'Processing' || (book.status as any) === 1" class="p-3 rounded-2xl bg-brand-500/10 dark:bg-brand-500/15 border border-brand-500/20">
                    <div class="flex items-center gap-2 text-xs font-semibold text-brand-700 dark:text-brand-300">
                      <Loader2 class="w-3.5 h-3.5 text-brand-500 animate-spin shrink-0" />
                      <span class="truncate">{{ getStatusMessage(book) }}</span>
                    </div>
                  </div>

                  <!-- Progress Bar Row -->
                  <div class="space-y-1 pt-1">
                    <div class="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
                      <span class="inline-flex items-center gap-1.5 font-semibold text-emerald-600 dark:text-emerald-400 text-[11px]">
                        <template v-if="book.status === 'Processing' || (book.status as any) === 1">
                          <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                          {{ $t('library.status_processing') }}
                        </template>
                        <template v-else-if="bookmarks[book.id]">
                          <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                          {{ (bookmarks[book.id] ?? 0) >= book.totalChunks ? $t('library.status_completed') : $t('library.status_in_progress') }}
                        </template>
                      </span>
                      <span class="font-mono text-[11px]">
                        {{ bookmarks[book.id] ? $t('library.slice_progress', { current: bookmarks[book.id], total: book.totalChunks, percent: Math.round(((bookmarks[book.id] ?? 0) / (book.totalChunks || 1)) * 100) }) : $t('library.slices_total', { count: book.totalChunks }) }}
                      </span>
                    </div>
                    <div class="w-full h-1 bg-slate-100 dark:bg-canvas-elevated rounded-full overflow-hidden">
                      <div
                        class="h-full bg-brand-500 rounded-full transition-all"
                        :style="{ width: `${bookmarks[book.id] ? Math.min(100, Math.round(((bookmarks[book.id] ?? 0) / (book.totalChunks || 1)) * 100)) : 0}%` }"
                      ></div>
                    </div>
                  </div>
                </div>
              </div>

              <div class="pt-2.5 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-between gap-2">
                <div class="flex items-center gap-1.5 min-w-0">
                  <span class="text-[11px] text-slate-400 font-mono truncate max-w-[80px]">
                    {{ book.sourceType === 2 ? 'PDF' : book.sourceType === 3 ? 'Web Doc' : 'Markdown' }}
                  </span>
                  <button
                    @click.stop="openEditModal(book)"
                    class="p-1.5 rounded-lg text-slate-400 hover:text-brand-500 hover:bg-brand-500/10 transition-colors cursor-pointer"
                    :title="$t('library.edit_book')"
                    :aria-label="$t('library.edit_book')"
                  >
                    <Pencil class="w-3.5 h-3.5" />
                  </button>
                  <button
                    @click.stop="openDeleteModal(book)"
                    class="p-1.5 rounded-lg text-slate-400 hover:text-rose-500 hover:bg-rose-500/10 transition-colors cursor-pointer"
                    :title="$t('library.delete_doc')"
                    :aria-label="$t('library.delete_doc')"
                  >
                    <Trash2 class="w-3.5 h-3.5" />
                  </button>
                  <button
                    @click.stop="handleExportBook(book)"
                    :disabled="exportingBookId === book.id"
                    class="p-1.5 rounded-lg text-slate-400 hover:text-brand-500 hover:bg-brand-500/10 transition-colors cursor-pointer disabled:opacity-50"
                    :title="$t('reader.export_obsidian')"
                    :aria-label="$t('reader.export_obsidian')"
                  >
                    <Download class="w-3.5 h-3.5" />
                  </button>
                </div>

                <NuxtLink
                  :to="{ path: `/read/${book.id}`, query: { from: '/library' } }"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-brand-500/10 text-brand-600 dark:text-brand-300 hover:bg-brand-500/20 border border-brand-500/30 whitespace-nowrap shrink-0 transition-colors"
                >
                  <GraduationCap class="w-3.5 h-3.5" />
                  <span>{{ bookmarks[book.id] ? $t('library.continue_reading') : $t('library.read_book') }}</span>
                  <ChevronRight class="w-3.5 h-3.5" />
                </NuxtLink>
              </div>
            </div>
          </div>

          <!-- Empty state -->
          <div v-else class="text-center py-16 glass-card rounded-2xl border border-slate-200/80 dark:border-white/[0.08] p-8 shadow-sm max-w-lg mx-auto space-y-4">
            <FileText class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto" :stroke-width="1.5" />
            <div class="space-y-1">
              <h3 class="text-base sm:text-lg font-bold text-slate-800 dark:text-slate-200">{{ $t('library.no_books') }}</h3>
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400">{{ $t('library.empty_desc') }}</p>
            </div>
            <div class="flex flex-wrap items-center justify-center gap-3 pt-2">
              <button
                type="button"
                @click="isImportModalOpen = true"
                class="inline-flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 dark:bg-white/[0.06] dark:hover:bg-white/[0.1] text-slate-800 dark:text-slate-200 transition-colors whitespace-nowrap shrink-0"
              >
                <Plus class="w-4 h-4" />
                <span>{{ $t('library.import_btn') }}</span>
              </button>
              <button
                type="button"
                @click="currentTab = 'discover'"
                class="inline-flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl bg-brand-500 hover:bg-brand-600 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0 cursor-pointer"
              >
                <Compass class="w-4 h-4" />
                <span>{{ $t('library.discover.browse_discover') }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Discover Content -->
        <div v-else>
          <div v-if="catalogStore.isLoading" class="flex flex-col items-center justify-center py-20 text-slate-500 dark:text-slate-400 text-sm">
            <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin mb-3"></div>
            <span>{{ $t('library.loading') }}</span>
          </div>

          <div v-else-if="catalogStore.documents.length > 0" class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            <CatalogCard
              v-for="doc in catalogStore.documents"
              :key="doc.id"
              :document="doc"
              :is-adopting="adoptingDocId === doc.id"
              @preview="handleOpenPreview"
              @adopt="handleAdoptDocument"
            />
          </div>

          <!-- Empty Discover State -->
          <div v-else class="text-center py-16 glass-card rounded-2xl border border-slate-200/80 dark:border-white/[0.08] p-8 shadow-sm max-w-lg mx-auto space-y-4">
            <BookOpen class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto" :stroke-width="1.5" />
            <div class="space-y-1">
              <h3 class="text-base sm:text-lg font-bold text-slate-800 dark:text-slate-200">{{ $t('library.discover.no_published') }}</h3>
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed">{{ $t('library.discover.no_published_desc') }}</p>
            </div>
            <div class="flex flex-wrap items-center justify-center gap-3 pt-2">
              <button
                type="button"
                @click="isImportModalOpen = true; currentTab = 'mine'"
                class="inline-flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl bg-brand-500 hover:bg-brand-600 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0 cursor-pointer"
              >
                <Plus class="w-4 h-4" />
                <span>{{ $t('library.import_btn') }}</span>
              </button>
            </div>
          </div>
        </div>
      </template>

      <!-- Pagination -->
      <template #pagination>
        <div v-if="currentTab === 'mine' && libraryStore.books.length > 0" class="w-full">
          <BasePagination
            :current-page="libraryStore.currentPage"
            :total-pages="libraryStore.totalPages"
            :total-count="libraryStore.totalCount"
            :page-size="libraryStore.pageSize"
            show-summary
            @change="onPageChange"
          />
        </div>
        <div v-else-if="currentTab === 'discover' && catalogStore.documents.length > 0" class="w-full">
          <BasePagination
            :current-page="catalogStore.currentPage"
            :total-pages="catalogStore.totalPages"
            :total-count="catalogStore.totalCount"
            :page-size="catalogStore.pageSize"
            show-summary
            @change="onPageChange"
          />
        </div>
      </template>
    </BoardLayout>

    <!-- Catalog Preview Modal -->
    <CatalogPreviewModal
      :open="isPreviewModalOpen"
      :document="catalogStore.selectedDocument"
      :is-adopting="adoptingDocId === catalogStore.selectedDocument?.id"
      @close="isPreviewModalOpen = false"
      @adopt="(id) => handleAdoptDocument({ id })"
    />

    <!-- Catalog Advanced Filter Modal -->
    <CatalogFilterModal
      :is-open="isCatalogFilterModalOpen"
      :current-filters="{
        categoryId: selectedCategory,
        sourceType: selectedSourceType,
        language: selectedLanguage
      }"
      @close="isCatalogFilterModalOpen = false"
      @apply="applyFilters"
      @reset="resetFilters"
    />

    <!-- Library Advanced Filter Modal -->
    <LibraryFilterModal
      :is-open="isLibraryFilterModalOpen"
      :current-filters="{
        categoryId: selectedCategory,
        language: selectedLanguage
      }"
      @close="isLibraryFilterModalOpen = false"
      @apply="applyFilters"
      @reset="resetFilters"
    />
  </div>

  <!-- Existing Modals -->
  <div class="max-w-7xl mx-auto">
    <!-- Import Document Modal -->
    <AppModal
      :open="isImportModalOpen"
      :title="$t('library.import_modal_title')"
      max-width="max-w-2xl"
      @close="isImportModalOpen = false"
    >
      <div v-if="categoryFeedback" role="status" class="text-sm sm:text-base text-amber-700 dark:text-amber-300">
        <p>{{ categoryFeedback }}</p>
        <button type="button" class="mt-2 font-semibold whitespace-nowrap" :disabled="categoryStore.isLoading" @click="categoryStore.fetchCategories(true)">{{ $t('categories.retry') }}</button>
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
            :placeholder="$t('library.markdown_title_placeholder')"
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
              :placeholder="$t('categories.selectCategory')"
              :disabled="categoryStore.isLoading || !!categoryStore.error"
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
            'border-2 border-dashed rounded-2xl p-4 sm:p-5 text-center transition-all cursor-pointer relative',
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

          <div class="flex flex-col items-center justify-center space-y-2">
            <div class="w-12 h-12 rounded-xl bg-brand-100 dark:bg-brand-950/80 text-brand-600 dark:text-brand-400 flex items-center justify-center border border-brand-200 dark:border-brand-900 shadow-sm">
              <UploadCloud class="h-8 w-8" />
            </div>

            <div v-if="!pdfFile" class="space-y-1">
              <h4 class="text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200">
                {{ $t('library.pdf_drop_title') }}
              </h4>
              <p class="text-xs text-slate-500 dark:text-slate-400">
                {{ $t('library.pdf_drop_desc') }}
              </p>
              <p class="text-xs text-brand-600 dark:text-brand-400 font-mono">
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
              :placeholder="$t('categories.selectCategory')"
              :disabled="categoryStore.isLoading || !!categoryStore.error"
            />
          </div>

          <div>
            <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.document_language') }}</label>
            <AppSelect
              v-model="uploadLanguage"
              :options="languageOptions"
              :aria-label="$t('library.document_language')"
            />
          </div>
        </div>

        <div v-show="!isProcessingPdf" class="p-3 rounded-xl bg-amber-50/80 dark:bg-amber-950/30 border border-amber-200/80 dark:border-amber-900/50 flex items-center gap-2.5 text-xs text-amber-800 dark:text-amber-300">
          <Lightbulb class="w-4 h-4 shrink-0 text-amber-500" />
          <span>{{ $t('library.verbatim_category_hint') }}</span>
        </div>

        <!-- Hidden accessible submit button for keyboard & test automation -->
        <button type="submit" :disabled="!assignmentAvailable(pdfCategory)" class="hidden">{{ $t('library.upload_pdf_action') }}</button>
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
            :placeholder="$t('categories.selectCategory')"
            :disabled="categoryStore.isLoading || !!categoryStore.error"
          />
          <div class="mt-2.5 p-3 rounded-xl bg-amber-50/80 dark:bg-amber-950/30 border border-amber-200/80 dark:border-amber-900/50 flex items-center gap-2.5 text-xs text-amber-800 dark:text-amber-300">
            <Lightbulb class="w-4 h-4 shrink-0 text-amber-500" />
            <span>{{ $t('library.verbatim_category_hint') }}</span>
          </div>
        </div>

        <!-- Document Language Selector for URL import -->
        <div>
          <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.document_language') }}</label>
          <AppSelect
            v-model="importLanguage"
            :options="languageOptions"
            :aria-label="$t('library.document_language')"
          />
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
            :disabled="isProcessingPdf || !assignmentAvailable(importCategory)"
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
      </div>

      <!-- Pinned Sticky Footer -->
      <template #footer>
        <button
          type="button"
          class="h-9 rounded-xl border border-slate-300 dark:border-white/[0.08] px-4 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 transition-colors hover:bg-slate-100 dark:hover:bg-white/[0.06] cursor-pointer"
          @click="isImportModalOpen = false"
        >
          {{ $t('library.cancel') }}
        </button>

        <button
          v-if="activeTab === 'markdown'"
          type="button"
          :disabled="libraryStore.isImporting || !importTitle || !importContent || !assignmentAvailable(importCategory)"
          class="h-9 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-sm transition-all active:scale-95 disabled:opacity-50 px-4 flex items-center justify-center gap-1.5 cursor-pointer"
          @click="handleImportSubmit"
        >
          <span v-if="libraryStore.isImporting" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <span>{{ libraryStore.isImporting ? $t('library.importing') : $t('library.import_action') }}</span>
        </button>

        <button
          v-else-if="activeTab === 'pdf'"
          type="button"
          :disabled="!pdfFile || isUploadingPdf || isProcessingPdf || !assignmentAvailable(pdfCategory)"
          class="h-9 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-sm transition-all active:scale-95 disabled:opacity-50 px-4 flex items-center justify-center gap-1.5 cursor-pointer"
          @click="handlePdfUpload"
        >
          <span v-if="isUploadingPdf || isProcessingPdf" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <span>{{ isProcessingPdf ? `${$t('library.processing_pdf')} (${pdfProgress}%)` : (isUploadingPdf ? $t('library.parsing_pdf') : $t('library.upload_pdf_action')) }}</span>
        </button>

        <button
          v-else-if="activeTab === 'url'"
          type="button"
          :disabled="!crawlUrlInput || isCrawling"
          class="h-9 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-sm transition-all active:scale-95 disabled:opacity-50 px-4 flex items-center justify-center gap-1.5 cursor-pointer"
          @click="handleCrawlUrl"
        >
          <span v-if="isCrawling" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <Globe v-else class="w-3.5 h-3.5" />
          <span>{{ isCrawling ? $t('library.fetching_url') : $t('library.fetch_url_btn') }}</span>
        </button>
      </template>
    </AppModal>

    <!-- Edit Book Modal -->
    <AppModal
      :open="isEditModalOpen"
      :title="$t('library.edit_modal_title')"
      max-width="max-w-xl"
      @close="isEditModalOpen = false; editingBook = null"
    >
      <form id="edit-book-form" @submit.prevent="handleSaveEditBook" class="space-y-4">
        <!-- Knowledge Graph & Taxonomy Synchronization Callout Banner -->
        <div class="p-3.5 rounded-2xl bg-brand-500/10 dark:bg-brand-500/15 border border-brand-500/20 flex items-start gap-3">
          <Lightbulb class="w-5 h-5 text-brand-600 dark:text-brand-400 shrink-0 mt-0.5" />
          <div class="text-xs sm:text-sm text-slate-700 dark:text-slate-300">
            <span class="font-bold block mb-0.5 text-brand-700 dark:text-brand-300">{{ $t('library.edit_taxonomy_callout_title') }}</span>
            <span class="leading-relaxed">{{ $t('library.edit_taxonomy_callout_desc') }}</span>
          </div>
        </div>

        <!-- Book Title Field -->
        <div>
          <label for="edit-book-title" class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">
            {{ $t('library.edit_title_label') }} <span class="text-rose-500">*</span>
          </label>
          <input
            id="edit-book-title"
            v-model="editTitle"
            type="text"
            required
            maxlength="255"
            :placeholder="$t('library.edit_title_placeholder')"
            class="w-full px-3.5 py-2.5 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 text-sm sm:text-base focus:border-brand-500 focus:ring-1 focus:ring-brand-500 transition-colors"
          />
        </div>

        <!-- Category assignment, including retained inactive metadata -->
        <div>
          <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">
            {{ $t('library.category_label') }}
          </label>
          <AppSelect
            v-model="editCategory"
            :options="editCategoryOptions"
            :aria-label="$t('library.category_label')"
          />
        </div>

        <!-- Document Language Field (Accessible AppSelect with en / vi) -->
        <div>
          <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">
            {{ $t('library.document_language') }}
          </label>
          <AppSelect
            v-model="editLanguage"
            :options="languageOptions"
            :aria-label="$t('library.document_language')"
          />
        </div>
        <!-- Author / Source URL Field -->
        <div>
          <label for="edit-book-author" class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">
            {{ $t('library.edit_author_label') }}
          </label>
          <input
            id="edit-book-author"
            v-model="editAuthor"
            type="text"
            maxlength="500"
            :placeholder="$t('library.edit_author_placeholder')"
            class="w-full px-3.5 py-2.5 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 text-sm sm:text-base focus:border-brand-500 focus:ring-1 focus:ring-brand-500 transition-colors"
          />
        </div>
      </form>

      <template #footer>
        <div class="flex items-center justify-end gap-3 w-full">
          <button
            type="button"
            @click="isEditModalOpen = false; editingBook = null"
            class="px-4 py-2.5 rounded-xl border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated font-semibold text-xs sm:text-sm whitespace-nowrap shrink-0 transition-colors cursor-pointer"
          >
            {{ $t('library.cancel') }}
          </button>
          <button
            type="submit"
            form="edit-book-form"
            :disabled="isSavingBook || !editTitle.trim()"
            class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 disabled:opacity-50 text-white font-semibold text-xs sm:text-sm whitespace-nowrap shrink-0 transition-all shadow-md shadow-brand-500/20 active:scale-[0.98] cursor-pointer"
          >
            <Loader2 v-if="isSavingBook" class="w-4 h-4 animate-spin shrink-0" />
            <span>{{ isSavingBook ? $t('library.saving') : $t('library.save_changes') }}</span>
          </button>
        </div>
      </template>
    </AppModal>

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
  </div>
</template>
