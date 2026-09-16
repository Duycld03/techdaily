<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Highlighter, Bookmark, Trash2, BookOpen, AlertTriangle, Zap, ExternalLink, ArrowRight, Search, Sparkles, X } from 'lucide-vue-next'
import { useNotesStore } from '~/stores/useNotesStore'
import { useInsightsStore } from '~/stores/useInsightsStore'
import { useReviewStore } from '~/stores/useReviewStore'
import { useApiError } from '~/composables/useApiError'
import MarkdownIt from 'markdown-it'

const { t } = useI18n()
const { formatError } = useApiError()
const notesStore = useNotesStore()
const insightsStore = useInsightsStore()
const toast = useToast()
const reviewStore = useReviewStore()
const creatingCardHighlightId = ref<string | null>(null)
const createdCardHighlightIds = ref<Set<string>>(new Set())

async function handleCreateFlashcard(highlightId: string) {
  creatingCardHighlightId.value = highlightId
  try {
    const localeVal = (useI18n().locale.value as string) || 'en'
    await reviewStore.createCardFromHighlight(highlightId, localeVal)
    createdCardHighlightIds.value.add(highlightId)
    toast.success(t('notes.toast_flashcard_success'))
  } catch (err: any) {
    toast.error(err.message || 'Failed to create flashcard.')
  } finally {
    creatingCardHighlightId.value = null
  }
}
const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

const activeTab = ref<'insights' | 'highlights'>('insights')

const highlightSearchQuery = ref('')

const filteredHighlights = computed(() => {
  const q = highlightSearchQuery.value.trim().toLowerCase()
  if (!q) return notesStore.highlights
  return notesStore.highlights.filter(h =>
    h.selectedText.toLowerCase().includes(q) ||
    (h.note && h.note.toLowerCase().includes(q)) ||
    h.bookTitle.toLowerCase().includes(q) ||
    h.chapterTitle.toLowerCase().includes(q) ||
    h.tags?.some(tag => tag.toLowerCase().includes(q.replace(/^#/, '')))
  )
})
// Delete Highlight Modal State
const highlightToDelete = ref<string | null>(null)
const isDeleteModalOpen = ref(false)
const isDeleting = ref(false)

// Unbookmark Modal State
const insightToUnbookmark = ref<string | null>(null)
const isUnbookmarkModalOpen = ref(false)
const isUnbookmarking = ref(false)

onMounted(async () => {
  await Promise.all([
    notesStore.fetchHighlights(),
    insightsStore.fetchBookmarkedInsights()
  ])
})

function renderMarkdown(raw: string | undefined | null): string {
  if (!raw) return ''
  const clean = raw.replace(/\\n/g, '\n')
  return md.render(clean)
}

function getCategoryBadge(cat: number) {
  switch (cat) {
    case 0:
      return { text: 'Frontend & Browser', color: 'bg-sky-50 dark:bg-sky-950/50 text-sky-700 dark:text-sky-300 border-sky-200 dark:border-sky-800' }
    case 1:
      return { text: '.NET 10 & C# 13', color: 'bg-purple-50 dark:bg-purple-950/50 text-purple-700 dark:text-purple-300 border-purple-200 dark:border-purple-800' }
    case 2:
      return { text: 'PostgreSQL 17 Engine', color: 'bg-blue-50 dark:bg-blue-950/50 text-blue-700 dark:text-blue-300 border-blue-200 dark:border-blue-800' }
    case 3:
      return { text: 'System Design & Distributed', color: 'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800' }
    default:
      return { text: 'Core Architecture', color: 'bg-slate-50 dark:bg-slate-900 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-800' }
  }
}

function openDeleteModal(id: string) {
  highlightToDelete.value = id
  isDeleteModalOpen.value = true
}

async function confirmDeleteHighlight() {
  if (!highlightToDelete.value) return
  isDeleting.value = true
  try {
    await notesStore.deleteHighlight(highlightToDelete.value)
    toast.success(t('notes.toast_delete_success'))
    isDeleteModalOpen.value = false
    highlightToDelete.value = null
  } catch (err: any) {
    toast.error(formatError(err, 'notes.toast_delete_error'))
  } finally {
    isDeleting.value = false
  }
}

function openUnbookmarkModal(id: string) {
  insightToUnbookmark.value = id
  isUnbookmarkModalOpen.value = true
}

async function confirmUnbookmark() {
  if (!insightToUnbookmark.value) return
  isUnbookmarking.value = true
  try {
    await insightsStore.toggleBookmark(insightToUnbookmark.value)
    toast.success(t('notes.toast_unbookmark_success'))
    isUnbookmarkModalOpen.value = false
    insightToUnbookmark.value = null
  } catch (err: any) {
    toast.error(formatError(err, 'notes.toast_unbookmark_error'))
  } finally {
    isUnbookmarking.value = false
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto p-4 sm:p-6 md:p-10 space-y-6 sm:space-y-8 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Header -->
    <div class="space-y-1 sm:space-y-2">
      <h1 class="text-xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2.5 sm:gap-3">
        <Highlighter class="w-6 h-6 sm:w-7 sm:h-7 text-indigo-600 dark:text-indigo-400" />
        <span>{{ $t('notes.title') }}</span>
      </h1>
      <p class="text-sm md:text-lg text-slate-500 dark:text-slate-400 font-medium leading-relaxed">{{ $t('notes.subtitle') }}</p>
    </div>

    <!-- Tab Switcher Bar -->
    <div class="flex items-center gap-2 border-b border-slate-200 dark:border-slate-800 pb-3">
      <button
        @click="activeTab = 'insights'"
        :class="[
          'px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all inline-flex items-center gap-2 border',
          activeTab === 'insights'
            ? 'bg-indigo-600 text-white dark:bg-indigo-500 dark:text-white border-transparent shadow-sm'
            : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/60'
        ]"
      >
        <Bookmark class="w-4 h-4" />
        <span>{{ $t('notes.tab_saved_insights', { count: insightsStore.bookmarkedInsights.length }) }}</span>
      </button>

      <button
        @click="activeTab = 'highlights'"
        :class="[
          'px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all inline-flex items-center gap-2 border',
          activeTab === 'highlights'
            ? 'bg-indigo-600 text-white dark:bg-indigo-500 dark:text-white border-transparent shadow-sm'
            : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/60'
        ]"
      >
        <BookOpen class="w-4 h-4" />
        <span>{{ $t('notes.tab_highlights', { count: notesStore.highlights.length }) }}</span>
      </button>
    </div>

    <!-- TAB 1: Saved Insights List -->
    <div v-if="activeTab === 'insights'">
      <div v-if="insightsStore.isLoadingBookmarks" class="flex flex-col items-center justify-center py-20 text-slate-500 dark:text-slate-400 text-sm">
        <div class="w-8 h-8 rounded-full border-2 border-indigo-500 border-t-transparent animate-spin mb-3"></div>
        <span>{{ $t('notes.loading_insights') }}</span>
      </div>

      <div v-else-if="insightsStore.bookmarkedInsights.length > 0" class="space-y-4">
        <div
          v-for="item in insightsStore.bookmarkedInsights"
          :key="item.id"
          class="p-5 sm:p-7 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 hover:border-indigo-400 dark:hover:border-slate-700 transition-all space-y-3.5 sm:space-y-4 shadow-md dark:shadow-sm"
        >
          <!-- Card Header Info -->
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 sm:gap-3">
            <div class="flex flex-wrap items-center gap-1.5 sm:gap-2">
              <span :class="['px-2.5 py-0.5 rounded-full text-xs font-bold border shrink-0', getCategoryBadge(item.category).color]">
                {{ getCategoryBadge(item.category).text }}
              </span>
              <span
                v-for="tag in item.tags"
                :key="tag"
                class="px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 text-xs font-mono font-medium"
              >
                #{{ tag }}
              </span>
            </div>

            <div class="flex items-center gap-2 shrink-0">
              <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 border border-emerald-200 dark:border-emerald-800/80 text-emerald-700 dark:text-emerald-400 text-xs font-bold">
                <Zap class="w-3.5 h-3.5 fill-emerald-500 text-emerald-500 shrink-0" />
                <span>{{ item.benchmarkStats }}</span>
              </div>

              <button
                @click="openUnbookmarkModal(item.id)"
                class="p-1.5 rounded-xl text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-slate-800 transition-colors"
                :title="$t('notes.unbookmark_btn')"
              >
                <Trash2 class="w-4 h-4" />
              </button>
            </div>
          </div>

          <!-- Title -->
          <h2 class="text-base sm:text-xl font-bold text-slate-900 dark:text-white tracking-tight leading-snug">
            {{ item.title }}
          </h2>

          <!-- Summary Snippet -->
          <div
            class="prose dark:prose-invert max-w-none text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed line-clamp-3"
            v-html="renderMarkdown(item.summaryMarkdown)"
          ></div>

          <!-- Card Actions Footer -->
          <div class="pt-2 flex items-center justify-between border-t border-slate-100 dark:border-slate-800/80 text-xs text-slate-500 dark:text-slate-400">
            <NuxtLink
              to="/insights"
              class="inline-flex items-center gap-1 text-indigo-600 dark:text-indigo-400 hover:underline font-bold text-xs sm:text-sm"
            >
              <span>{{ $t('notes.view_insight_btn') }}</span>
              <ArrowRight class="w-3.5 h-3.5" />
            </NuxtLink>

            <button
              @click="openUnbookmarkModal(item.id)"
              class="text-xs text-rose-500 hover:text-rose-600 font-semibold"
            >
              {{ $t('notes.unbookmark_btn') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Empty State for Insights -->
      <div v-else class="text-center py-16 bg-white dark:bg-slate-900/40 rounded-3xl border border-slate-200 dark:border-slate-800/80 p-8 shadow-sm space-y-3">
        <Bookmark class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto" />
        <h3 class="text-base font-bold text-slate-800 dark:text-slate-200">{{ $t('insights.empty_title') }}</h3>
        <p class="text-sm text-slate-500 max-w-sm mx-auto">{{ $t('notes.no_saved_insights') }}</p>
        <NuxtLink
          to="/insights"
          class="inline-flex items-center gap-1.5 px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-indigo-500/20 transition-all"
        >
          <span>{{ $t('notes.explore_insights_btn') }}</span>
          <ArrowRight class="w-4 h-4" />
        </NuxtLink>
      </div>
    </div>

    <!-- TAB 2: Highlights List -->
    <div v-else-if="activeTab === 'highlights'" class="space-y-4">
      <!-- Search & Filter Bar -->
      <div v-if="notesStore.highlights.length > 0" class="relative">
        <Search class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
        <input
          v-model="highlightSearchQuery"
          type="text"
          class="w-full pl-10 pr-9 py-2.5 text-xs sm:text-sm rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all shadow-sm"
          :placeholder="$t('notes.search_placeholder')"
        />
        <button
          v-if="highlightSearchQuery"
          @click="highlightSearchQuery = ''"
          class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 p-0.5 rounded-full"
        >
          <X class="w-4 h-4" />
        </button>
      </div>

      <div v-if="notesStore.isLoading" class="flex flex-col items-center justify-center py-20 text-slate-500 dark:text-slate-400 text-sm">
        <div class="w-8 h-8 rounded-full border-2 border-indigo-500 border-t-transparent animate-spin mb-3"></div>
        <span>Loading saved highlights...</span>
      </div>

      <div v-else-if="filteredHighlights.length > 0" class="space-y-4">
        <div
          v-for="item in filteredHighlights"
          :key="item.id"
          class="p-5 sm:p-7 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 hover:border-indigo-400 dark:hover:border-slate-700 transition-all space-y-3.5 sm:space-y-4 shadow-md dark:shadow-sm"
        >
          <!-- Reference bar -->
          <div class="flex items-center justify-between text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-semibold">
            <div class="flex items-center gap-2 min-w-0">
              <BookOpen class="w-4 h-4 text-indigo-600 dark:text-indigo-400 shrink-0" />
              <span class="text-slate-800 dark:text-slate-200 truncate">{{ item.bookTitle }}</span>
              <span class="text-slate-400 dark:text-slate-600 shrink-0">•</span>
              <span class="truncate">{{ item.chapterTitle }}</span>
            </div>

            <div class="flex items-center gap-1.5 shrink-0">
              <button
                @click="handleCreateFlashcard(item.id)"
                :disabled="creatingCardHighlightId === item.id || createdCardHighlightIds.has(item.id)"
                class="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl text-xs font-semibold border transition-all disabled:opacity-60"
                :class="createdCardHighlightIds.has(item.id)
                  ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 border-emerald-200 dark:border-emerald-800'
                  : 'bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 hover:bg-amber-100 dark:hover:bg-amber-900/50 border-amber-200 dark:border-amber-800/60'"
                :title="$t('notes.create_flashcard')"
              >
                <Zap class="w-3.5 h-3.5 text-amber-500" />
                <span class="hidden sm:inline">{{
                  createdCardHighlightIds.has(item.id)
                    ? 'In SM-2'
                    : creatingCardHighlightId === item.id
                      ? $t('notes.creating_card')
                      : $t('notes.create_flashcard')
                }}</span>
              </button>

              <button
                @click="openDeleteModal(item.id)"
                class="p-2 rounded-xl text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-slate-800 transition-colors shrink-0"
                title="Delete Highlight"
              >
                <Trash2 class="w-4 h-4" />
              </button>
            </div>
          </div>

          <!-- Highlighted Text Quote -->
          <div class="p-4 sm:p-5 rounded-2xl bg-slate-50 dark:bg-slate-950/80 border-l-4 border-indigo-500 text-sm md:text-base text-slate-800 dark:text-slate-200 leading-relaxed font-sans italic">
            "{{ item.selectedText }}"
          </div>

          <!-- Personal Reflection Note Block -->
          <div
            v-if="item.note"
            class="p-4 rounded-2xl bg-indigo-50/60 dark:bg-indigo-950/30 border border-indigo-100 dark:border-indigo-900/50 space-y-1.5"
          >
            <div class="flex items-center gap-1.5 text-xs font-bold text-indigo-700 dark:text-indigo-400">
              <Sparkles class="w-3.5 h-3.5" />
              <span>Personal Reflection</span>
            </div>
            <p class="text-xs sm:text-sm text-slate-700 dark:text-slate-300 whitespace-pre-wrap leading-relaxed">
              {{ item.note }}
            </p>
          </div>

          <!-- Tags -->
          <div v-if="item.tags?.length" class="flex flex-wrap gap-1.5 pt-1">
            <button
              v-for="(tag, i) in item.tags"
              :key="i"
              @click="highlightSearchQuery = tag"
              class="px-2.5 py-1 rounded-lg bg-slate-100 dark:bg-slate-800 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-indigo-700 dark:hover:text-indigo-300 border border-slate-200 dark:border-slate-700 transition-colors"
            >
              #{{ tag.replace(/^#/, '') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Empty State for Highlights -->
      <div v-else class="text-center py-16 bg-white dark:bg-slate-900/40 rounded-3xl border border-slate-200 dark:border-slate-800/80 p-8 shadow-sm">
        <Highlighter class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto mb-3" />
        <h3 class="text-base font-bold text-slate-800 dark:text-slate-200">{{ highlightSearchQuery ? 'No highlights match your search.' : $t('notes.no_notes') }}</h3>
        <p v-if="highlightSearchQuery" class="text-xs text-slate-500 mt-1">Try searching for a different keyword or tag.</p>
      </div>
    </div>

    <!-- Delete Highlight Confirmation Modal (Teleported to Body) -->
    <Teleport to="body">
      <div
        v-if="isDeleteModalOpen && highlightToDelete"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200"
      >
        <div
          class="w-full max-w-md p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl space-y-4 animate-in zoom-in-95 duration-200"
        >
          <div class="w-12 h-12 rounded-2xl bg-rose-100 dark:bg-rose-950/60 text-rose-600 dark:text-rose-400 border border-rose-200 dark:border-rose-900 flex items-center justify-center">
            <AlertTriangle class="w-6 h-6" />
          </div>

          <div class="space-y-1.5">
            <h3 class="text-lg font-bold text-slate-900 dark:text-white">
              {{ $t('notes.delete_confirm_title') }}
            </h3>
            <p class="text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
              {{ $t('notes.delete_confirm_desc') }}
            </p>
          </div>

          <div class="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              @click="isDeleteModalOpen = false; highlightToDelete = null"
              class="px-5 py-2.5 rounded-xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors"
            >
              {{ $t('notes.cancel_btn') }}
            </button>

            <button
              type="button"
              :disabled="isDeleting"
              @click="confirmDeleteHighlight"
              class="flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-rose-600/20 active:scale-95 transition-all disabled:opacity-50"
            >
              <span v-if="isDeleting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ isDeleting ? $t('notes.deleting') : $t('notes.confirm_delete_btn') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Unbookmark Insight Confirmation Modal (Teleported to Body) -->
    <Teleport to="body">
      <div
        v-if="isUnbookmarkModalOpen && insightToUnbookmark"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200"
      >
        <div
          class="w-full max-w-md p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl space-y-4 animate-in zoom-in-95 duration-200"
        >
          <div class="w-12 h-12 rounded-2xl bg-rose-100 dark:bg-rose-950/60 text-rose-600 dark:text-rose-400 border border-rose-200 dark:border-rose-900 flex items-center justify-center">
            <AlertTriangle class="w-6 h-6" />
          </div>

          <div class="space-y-1.5">
            <h3 class="text-lg font-bold text-slate-900 dark:text-white">
              {{ $t('notes.unbookmark_title') }}
            </h3>
            <p class="text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
              {{ $t('notes.unbookmark_desc') }}
            </p>
          </div>

          <div class="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              @click="isUnbookmarkModalOpen = false; insightToUnbookmark = null"
              class="px-5 py-2.5 rounded-xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors"
            >
              {{ $t('notes.cancel_btn') }}
            </button>

            <button
              type="button"
              :disabled="isUnbookmarking"
              @click="confirmUnbookmark"
              class="flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-rose-600/20 active:scale-95 transition-all disabled:opacity-50"
            >
              <span v-if="isUnbookmarking" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ isUnbookmarking ? $t('notes.unbookmarking') : $t('notes.confirm_unbookmark_btn') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

