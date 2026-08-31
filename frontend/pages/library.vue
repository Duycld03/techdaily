<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { BookOpen, Search, Plus, ExternalLink, Layers, X, FileText, Bookmark, Trash2, AlertTriangle, FileUp, Globe, CheckCircle2, UploadCloud } from 'lucide-vue-next'

const { t, locale } = useI18n()
const libraryStore = useLibraryStore()

const searchQuery = ref('')
const selectedCategory = ref<number | undefined>(undefined)
const bookmarks = ref<Record<string, number>>({})

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
const pdfError = ref<string | null>(null)

// Tab 3: URL Crawler state
const crawlUrlInput = ref('')
const isCrawling = ref(false)
const crawlError = ref<string | null>(null)
const crawlSuccess = ref(false)

// Delete modal state
const bookToDelete = ref<{ id: string; title: string } | null>(null)
const isDeleteModalOpen = ref(false)
const isDeleting = ref(false)

const categories = computed(() => [
  { id: undefined, label: t('library.categories.all') },
  { id: 0, label: t('library.categories.frontend') },
  { id: 1, label: t('library.categories.backend') },
  { id: 2, label: t('library.categories.database') },
  { id: 3, label: t('library.categories.cloud') },
  { id: 4, label: t('library.categories.system_design') }
])

onMounted(() => {
  libraryStore.fetchBooks()
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

function handleCategorySelect(catId?: number) {
  selectedCategory.value = catId
  libraryStore.fetchBooks(selectedCategory.value, searchQuery.value)
}

function handleSearch() {
  libraryStore.fetchBooks(selectedCategory.value, searchQuery.value)
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

    // Reset & close
    importTitle.value = ''
    importContent.value = ''
    importSourceUrl.value = ''
    isImportModalOpen.value = false
  } catch (err) {
    // error handled in store
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
    pdfError.value = 'Only .pdf files are supported.'
    return
  }
  if (file.size > 209_715_200) {
    pdfError.value = 'File exceeds the maximum limit of 200 MB.'
    return
  }
  pdfError.value = null
  pdfFile.value = file
  if (!pdfTitle.value) {
    pdfTitle.value = file.name.replace(/\.pdf$/i, '')
  }
}

async function handlePdfUpload() {
  if (!pdfFile.value) return
  isUploadingPdf.value = true
  pdfError.value = null

  try {
    const formData = new FormData()
    formData.append('file', pdfFile.value, pdfFile.value.name)
    if (pdfTitle.value) formData.append('title', pdfTitle.value)
    formData.append('category', pdfCategory.value.toString())
    formData.append('language', locale.value || 'vi')

    await libraryStore.uploadPdf(formData)

    // Reset & close
    pdfFile.value = null
    pdfTitle.value = ''
    isImportModalOpen.value = false
  } catch (err: any) {
    pdfError.value = err.message || 'Failed to process PDF.'
  } finally {
    isUploadingPdf.value = false
  }
}

async function handleCrawlUrl() {
  if (!crawlUrlInput.value) return
  isCrawling.value = true
  crawlError.value = null
  crawlSuccess.value = false

  try {
    const result = await libraryStore.crawlUrl(crawlUrlInput.value)
    importTitle.value = result.title
    importSourceUrl.value = result.sourceUrl
    importContent.value = result.markdownContent
    crawlSuccess.value = true
    // Switch to markdown tab for preview & confirmation
    activeTab.value = 'markdown'
  } catch (err: any) {
    crawlError.value = err.message || 'Failed to crawl article from URL.'
  } finally {
    isCrawling.value = false
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
    isDeleteModalOpen.value = false
    bookToDelete.value = null
  } catch (err) {
    // error handled in store
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div class="max-w-6xl mx-auto p-6 md:p-10 space-y-8 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-3">
          <BookOpen class="w-7 h-7 text-brand-600 dark:text-brand-400" />
          <span>{{ $t('library.title') }}</span>
        </h1>
        <p class="text-sm text-slate-500 dark:text-slate-400 mt-1.5 font-medium">{{ $t('library.subtitle') }}</p>
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
            'px-4 py-2 rounded-xl text-xs sm:text-sm border transition-colors outline-none focus:outline-none',
            selectedCategory === cat.id
              ? 'bg-slate-100 dark:bg-slate-800 border-slate-300 dark:border-slate-700 text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:border-slate-300 dark:hover:border-slate-700 font-medium'
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
          class="w-full pl-10 pr-4 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-200 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:border-brand-500 transition-colors shadow-sm"
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
        class="p-6 sm:p-7 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 hover:border-brand-400 dark:hover:border-slate-700 transition-all flex flex-col justify-between space-y-4 group shadow-md dark:shadow-sm"
      >
        <div>
          <div class="flex items-center justify-between gap-2 mb-3.5">
            <span class="px-3 py-1 rounded-lg bg-brand-100 dark:bg-brand-950/80 border border-brand-200 dark:border-brand-800/60 text-brand-800 dark:text-brand-300 text-xs font-bold">
              {{ categories.find((c) => c.id === book.category)?.label || 'Engineering' }}
            </span>
            <span class="text-xs text-slate-500 font-mono flex items-center gap-1">
              <Layers class="w-3.5 h-3.5" />
              {{ book.totalChunks }} {{ $t('library.chunks') }}
            </span>
          </div>

          <h3 class="text-lg font-bold text-slate-900 dark:text-white group-hover:text-brand-600 dark:group-hover:text-brand-300 transition-colors line-clamp-2 leading-snug">
            {{ book.title }}
          </h3>

          <p v-if="book.authorOrSourceUrl" class="text-xs text-slate-500 mt-2 truncate font-mono">
            {{ book.authorOrSourceUrl }}
          </p>

          <!-- Bookmark Badge if exists -->
          <div v-if="bookmarks[book.id]" class="mt-3 inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-brand-50 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-900 text-brand-700 dark:text-brand-300 text-xs font-semibold">
            <Bookmark class="w-3 h-3 text-brand-500 fill-brand-500" />
            <span>{{ $t('library.resumes_at', { slice: bookmarks[book.id] }) }}</span>
          </div>
        </div>

        <div class="pt-4 border-t border-slate-100 dark:border-slate-800/80 flex items-center justify-between">
          <div class="flex items-center gap-2">
            <span class="text-xs text-slate-400">GitBook Reader</span>
            <button
              @click.stop="openDeleteModal(book)"
              class="p-1.5 rounded-lg text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 border border-transparent hover:border-rose-200 dark:hover:border-rose-900/50 transition-colors"
              :title="$t('library.delete_doc')"
              :aria-label="$t('library.delete_doc')"
            >
              <Trash2 class="w-3.5 h-3.5" />
            </button>
          </div>

          <NuxtLink
            :to="`/read/${book.id}`"
            class="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs transition-transform active:scale-95 shadow-sm"
          >
            <span>{{ bookmarks[book.id] ? $t('library.continue_reading') : $t('library.read_book') }}</span>
            <ExternalLink class="w-3.5 h-3.5" />
          </NuxtLink>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-else class="text-center py-16 bg-white dark:bg-slate-900/40 rounded-3xl border border-slate-200 dark:border-slate-800/80 p-8 shadow-sm">
      <FileText class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto mb-3" />
      <h3 class="text-base font-bold text-slate-800 dark:text-slate-200">{{ $t('library.no_books') }}</h3>
      <p class="text-xs text-slate-500 mt-1">{{ $t('library.empty_desc') }}</p>
    </div>

    <!-- Import Document Modal -->
    <div v-if="isImportModalOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-in fade-in">
      <div class="w-full max-w-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-2xl p-6 sm:p-9 space-y-6 animate-in zoom-in-95 max-h-[90vh] overflow-y-auto">
        <div class="flex items-center justify-between pb-3 border-b border-slate-200 dark:border-slate-800">
          <div>
            <h3 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('library.import_modal_title') }}</h3>
            <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">{{ $t('library.import_modal_desc') }}</p>
          </div>
          <button @click="isImportModalOpen = false" class="p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800">
            <X class="w-5 h-5" />
          </button>
        </div>

        <!-- 3-Tab Selector -->
        <div class="flex items-center gap-2 p-1.5 bg-slate-100 dark:bg-slate-950/80 rounded-2xl border border-slate-200/80 dark:border-slate-800/80">
          <button
            type="button"
            @click="activeTab = 'markdown'"
            :class="[
              'flex-1 py-2.5 px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-2 transition-all',
              activeTab === 'markdown'
                ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm border border-slate-200 dark:border-slate-700'
                : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            ]"
          >
            <FileText class="w-4 h-4" />
            <span>{{ $t('library.tab_markdown') }}</span>
          </button>

          <button
            type="button"
            @click="activeTab = 'pdf'"
            :class="[
              'flex-1 py-2.5 px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-2 transition-all',
              activeTab === 'pdf'
                ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm border border-slate-200 dark:border-slate-700'
                : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            ]"
          >
            <FileUp class="w-4 h-4" />
            <span>{{ $t('library.tab_pdf') }}</span>
          </button>

          <button
            type="button"
            @click="activeTab = 'url'"
            :class="[
              'flex-1 py-2.5 px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-2 transition-all',
              activeTab === 'url'
                ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm border border-slate-200 dark:border-slate-700'
                : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            ]"
          >
            <Globe class="w-4 h-4" />
            <span>{{ $t('library.tab_url') }}</span>
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
              class="w-full px-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none"
            />
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.category_label') }}</label>
              <select
                v-model="importCategory"
                class="w-full px-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none"
              >
                <option :value="0">Frontend & Web</option>
                <option :value="1">Backend & Distributed</option>
                <option :value="2">Database & Storage</option>
                <option :value="3">Cloud & DevOps</option>
                <option :value="4">System Design</option>
              </select>
            </div>

            <div>
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.url_label') }}</label>
              <input
                v-model="importSourceUrl"
                type="url"
                placeholder="https://..."
                class="w-full px-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none"
              />
            </div>
          </div>

          <div>
            <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.content_label') }}</label>
            <textarea
              v-model="importContent"
              required
              rows="7"
              placeholder="Paste Markdown document with # and ## headers here..."
              class="w-full p-4 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm font-mono text-slate-800 dark:text-slate-200 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none resize-none"
            ></textarea>
          </div>

          <div class="flex justify-end gap-3 pt-2">
            <button
              type="button"
              @click="isImportModalOpen = false"
              class="px-5 py-2.5 rounded-xl text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
            >
              {{ $t('library.cancel') }}
            </button>
            <button
              type="submit"
              :disabled="libraryStore.isImporting"
              class="flex items-center gap-2 px-6 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm shadow-md transition-colors disabled:opacity-50"
            >
              <span v-if="libraryStore.isImporting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ libraryStore.isImporting ? $t('library.importing') : $t('library.import_action') }}</span>
            </button>
          </div>
        </form>

        <!-- TAB 2: PDF Drag & Drop Upload Form -->
        <form v-else-if="activeTab === 'pdf'" @submit.prevent="handlePdfUpload" class="space-y-4">
          <!-- Dropzone -->
          <div
            @dragover.prevent="isDraggingPdf = true"
            @dragleave.prevent="isDraggingPdf = false"
            @drop.prevent="onPdfDrop"
            :class="[
              'border-2 border-dashed rounded-3xl p-8 text-center transition-all cursor-pointer relative',
              isDraggingPdf
                ? 'border-brand-500 bg-brand-50/50 dark:bg-brand-950/40'
                : 'border-slate-300 dark:border-slate-800 hover:border-brand-400 dark:hover:border-slate-700 bg-slate-50/60 dark:bg-slate-950/40'
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
              <div class="w-14 h-14 rounded-2xl bg-brand-100 dark:bg-brand-950/80 text-brand-600 dark:text-brand-400 flex items-center justify-center border border-brand-200 dark:border-brand-900 shadow-sm">
                <UploadCloud class="w-7 h-7" />
              </div>

              <div v-if="!pdfFile" class="space-y-1">
                <h4 class="text-sm font-bold text-slate-800 dark:text-slate-200">
                  {{ $t('library.pdf_drop_title') }}
                </h4>
                <p class="text-xs text-slate-500 dark:text-slate-400">
                  {{ $t('library.pdf_drop_desc') }}
                </p>
                <p class="text-[11px] text-brand-600 dark:text-brand-400 font-mono pt-1">
                  {{ $t('library.pdf_size_limit') }}
                </p>
              </div>

              <div v-else class="space-y-1">
                <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-900 text-emerald-700 dark:text-emerald-300 text-xs font-bold">
                  <CheckCircle2 class="w-4 h-4" />
                  <span>{{ pdfFile.name }} ({{ (pdfFile.size / (1024 * 1024)).toFixed(1) }} MB)</span>
                </div>
                <p class="text-xs text-slate-400">Click or drop another file to replace</p>
              </div>
            </div>
          </div>

          <div v-if="pdfError" class="p-3 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-rose-600 dark:text-rose-400 text-xs font-medium">
            {{ pdfError }}
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.title_label') }}</label>
              <input
                v-model="pdfTitle"
                type="text"
                placeholder="Optional: Auto-extracted from PDF if blank"
                class="w-full px-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none"
              />
            </div>

            <div>
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.category_label') }}</label>
              <select
                v-model="pdfCategory"
                class="w-full px-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none"
              >
                <option :value="0">Frontend & Web</option>
                <option :value="1">Backend & Distributed</option>
                <option :value="2">Database & Storage</option>
                <option :value="3">Cloud & DevOps</option>
                <option :value="4">System Design</option>
              </select>
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-2">
            <button
              type="button"
              @click="isImportModalOpen = false"
              class="px-5 py-2.5 rounded-xl text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
            >
              {{ $t('library.cancel') }}
            </button>
            <button
              type="submit"
              :disabled="!pdfFile || isUploadingPdf"
              class="flex items-center gap-2 px-6 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm shadow-md transition-colors disabled:opacity-50"
            >
              <span v-if="isUploadingPdf" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ isUploadingPdf ? $t('library.parsing_pdf') : $t('library.upload_pdf_action') }}</span>
            </button>
          </div>
        </form>

        <!-- TAB 3: Web URL Crawler Form -->
        <div v-else-if="activeTab === 'url'" class="space-y-4">
          <div>
            <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">
              {{ $t('library.url_crawler_title') }}
            </label>
            <div class="flex gap-2">
              <input
                v-model="crawlUrlInput"
                type="url"
                :placeholder="$t('library.url_input_placeholder')"
                class="flex-1 px-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none"
                @keyup.enter="handleCrawlUrl"
              />
              <button
                type="button"
                :disabled="!crawlUrlInput || isCrawling"
                @click="handleCrawlUrl"
                class="flex items-center gap-2 px-5 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm shadow-md transition-colors disabled:opacity-50 shrink-0"
              >
                <span v-if="isCrawling" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <Globe v-else class="w-4 h-4" />
                <span>{{ isCrawling ? $t('library.fetching_url') : $t('library.fetch_url_btn') }}</span>
              </button>
            </div>
          </div>

          <div v-if="crawlError" class="p-3 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-rose-600 dark:text-rose-400 text-xs font-medium">
            {{ crawlError }}
          </div>

          <div class="p-4 rounded-2xl bg-slate-50 dark:bg-slate-950/60 border border-slate-200 dark:border-slate-800 text-xs text-slate-500 dark:text-slate-400 space-y-2">
            <h5 class="font-bold text-slate-700 dark:text-slate-300">Supported Sources:</h5>
            <ul class="list-disc list-inside space-y-1">
              <li><strong>GitHub Repositories:</strong> Links to <code>README.md</code> or any <code>.md</code> file in a repository.</li>
              <li><strong>Technical Blogs & RFCs:</strong> Microsoft Learn, Martin Fowler, Dev.to, Medium, Substack architecture posts.</li>
            </ul>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="isDeleteModalOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-in fade-in">
      <div class="w-full max-w-md bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-2xl p-6 sm:p-8 space-y-5 animate-in zoom-in-95">
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
            class="flex-1 py-3 rounded-xl border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 font-semibold text-xs sm:text-sm transition-colors"
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
  </div>
</template>
