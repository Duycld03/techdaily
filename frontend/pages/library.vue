<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { BookOpen, Search, Plus, ExternalLink, Layers, X, FileText, Bookmark } from 'lucide-vue-next'

const libraryStore = useLibraryStore()

const searchQuery = ref('')
const selectedCategory = ref<number | undefined>(undefined)
const bookmarks = ref<Record<string, number>>({})

// Import modal state
const isImportModalOpen = ref(false)
const importTitle = ref('')
const importCategory = ref(0)
const importSourceUrl = ref('')
const importContent = ref('')

const categories = [
  { id: undefined, label: 'All Categories' },
  { id: 0, label: 'Frontend & Web' },
  { id: 1, label: 'Backend & Distributed' },
  { id: 2, label: 'Database & Storage' },
  { id: 3, label: 'Cloud & DevOps' },
  { id: 4, label: 'System Design' }
]

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
    // error handled
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
            'px-4 py-2 rounded-xl text-xs sm:text-sm font-semibold border transition-all',
            selectedCategory === cat.id
              ? 'bg-brand-600 text-white border-brand-500 shadow-sm font-semibold'
              : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:border-slate-300 dark:hover:border-slate-700'
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
      <span>Loading Technical Library...</span>
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
            <span>Resumes at Slice {{ bookmarks[book.id] }}</span>
          </div>
        </div>

        <div class="pt-4 border-t border-slate-100 dark:border-slate-800/80 flex items-center justify-between">
          <span class="text-xs text-slate-400">GitBook Reader</span>
          <NuxtLink
            :to="`/read/${book.id}`"
            class="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs transition-transform active:scale-95 shadow-sm"
          >
            <span>{{ bookmarks[book.id] ? 'Continue Reading' : $t('library.read_book') }}</span>
            <ExternalLink class="w-3.5 h-3.5" />
          </NuxtLink>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-else class="text-center py-16 bg-white dark:bg-slate-900/40 rounded-3xl border border-slate-200 dark:border-slate-800/80 p-8 shadow-sm">
      <FileText class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto mb-3" />
      <h3 class="text-base font-bold text-slate-800 dark:text-slate-200">{{ $t('library.no_books') }}</h3>
      <p class="text-xs text-slate-500 mt-1">Import your first Markdown document or article to get started.</p>
    </div>

    <!-- Import Document Modal -->
    <div v-if="isImportModalOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div class="w-full max-w-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-2xl p-6 sm:p-9 space-y-6 animate-in zoom-in-95">
        <div class="flex items-center justify-between pb-3 border-b border-slate-200 dark:border-slate-800">
          <div>
            <h3 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('library.import_modal_title') }}</h3>
            <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">{{ $t('library.import_modal_desc') }}</p>
          </div>
          <button @click="isImportModalOpen = false" class="p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800">
            <X class="w-5 h-5" />
          </button>
        </div>

        <form @submit.prevent="handleImportSubmit" class="space-y-4">
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
              <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('library.source_url_label') }}</label>
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
              rows="8"
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
              Cancel
            </button>
            <button
              type="submit"
              :disabled="libraryStore.isImporting"
              class="flex items-center gap-2 px-6 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm shadow-md transition-colors"
            >
              <span v-if="libraryStore.isImporting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ libraryStore.isImporting ? $t('library.importing') : $t('library.import_action') }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
