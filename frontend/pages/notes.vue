<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useDebounceFn } from '@vueuse/core'
import { Highlighter, Trash2, BookOpen, AlertTriangle, Zap, Search, Sparkles, X, Pencil, Check } from 'lucide-vue-next'
import BasePagination from '~/components/common/BasePagination.vue'
import BoardLayout from '~/components/layout/BoardLayout.vue'
import { useNotesStore, type Highlight } from '~/stores/useNotesStore'
import { useReviewStore } from '~/stores/useReviewStore'
import { useApiError } from '~/composables/useApiError'
import MarkdownIt from 'markdown-it'

const { t, locale } = useI18n()
const route = useRoute()
const router = useRouter()
const { formatError } = useApiError()
const notesStore = useNotesStore()
const reviewStore = useReviewStore()
const toast = useToast()
const creatingCardHighlightId = ref<string | null>(null)
const createdCardHighlightIds = ref<Set<string>>(new Set())

async function handleCreateFlashcard(highlightId: string) {
  creatingCardHighlightId.value = highlightId
  try {
    const localeVal = (locale.value as string) || 'en'
    await reviewStore.createCardFromHighlight(highlightId, localeVal)
    createdCardHighlightIds.value.add(highlightId)
    toast.success(t('notes.toast_flashcard_success'))
  } catch (err: unknown) {
    toast.error(formatError(err, 'notes.toast_flashcard_error'))
  } finally {
    creatingCardHighlightId.value = null
  }
}

function syncFlashcardState() {
  notesStore.highlights.forEach(h => {
    if (h.hasFlashcard) {
      createdCardHighlightIds.value.add(h.id)
    }
  })
}

watch(
  () => notesStore.highlights,
  () => {
    syncFlashcardState()
  },
  { deep: true }
)

const md = new MarkdownIt({ html: false, linkify: true, typographer: true })

const highlightSearchQuery = ref('')

interface TagCount {
  tag: string
  count: number
}

const tagCounts = computed<TagCount[]>(() => {
  if (notesStore.tagCounts && notesStore.tagCounts.length > 0) {
    return notesStore.tagCounts
  }

  const counts: Record<string, number> = {}
  notesStore.highlights.forEach((h) => {
    if (h.tags && Array.isArray(h.tags)) {
      h.tags.forEach((rawTag) => {
        const clean = rawTag.trim().replace(/^#/, '').toLowerCase()
        if (clean.length > 0) {
          counts[clean] = (counts[clean] || 0) + 1
        }
      })
    }
  })

  return Object.entries(counts)
    .map(([tag, count]) => ({ tag, count }))
    .sort((a, b) => b.count - a.count || a.tag.localeCompare(b.tag))
})

const selectedTag = ref<string | null>(null)

function selectTag(tag: string | null) {
  if (!tag) {
    selectedTag.value = null
  } else {
    const clean = tag.trim().replace(/^#/, '').toLowerCase()
    if (selectedTag.value === clean) {
      selectedTag.value = null
    } else {
      selectedTag.value = clean
    }
  }

  router.replace({
    query: {
      ...route.query,
      tag: selectedTag.value ? selectedTag.value : undefined,
      page: undefined
    }
  })
  loadNotes(1, false)
}

const filteredHighlights = computed(() => {
  const q = highlightSearchQuery.value.trim().toLowerCase()
  const activeTag = selectedTag.value ? selectedTag.value.toLowerCase() : null

  return notesStore.highlights.filter((h) => {
    // 1. Tag matching
    if (activeTag) {
      const hasTag = h.tags?.some((t) => t.trim().replace(/^#/, '').toLowerCase() === activeTag)
      if (!hasTag) return false
    }

    // 2. Keyword matching
    if (q) {
      const matchText = h.selectedText.toLowerCase().includes(q)
      const matchNote = h.note ? h.note.toLowerCase().includes(q) : false
      const matchBook = h.bookTitle.toLowerCase().includes(q)
      const matchChapter = h.chapterTitle.toLowerCase().includes(q)
      const matchTags = h.tags?.some((t) => t.toLowerCase().includes(q.replace(/^#/, '')))
      if (!matchText && !matchNote && !matchBook && !matchChapter && !matchTags) {
        return false
      }
    }

    return true
  })
})

async function loadNotes(page = 1, append = false) {
  await notesStore.fetchHighlights({
    tag: selectedTag.value || undefined,
    search: highlightSearchQuery.value.trim() || undefined,
    page,
    pageSize: notesStore.pageSize,
    append
  })
}

function onPageChange(newPage: number) {
  router.replace({
    query: {
      ...route.query,
      page: newPage > 1 ? newPage.toString() : undefined
    }
  })
  loadNotes(newPage, false)
  if (typeof window !== 'undefined') {
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

async function handleLoadMore() {
  if (notesStore.currentPage < notesStore.totalPages) {
    await loadNotes(notesStore.currentPage + 1, true)
  }
}

const debouncedSyncSearch = useDebounceFn((newVal: string) => {
  router.replace({
    query: {
      ...route.query,
      search: newVal.trim() ? newVal.trim() : undefined,
      page: undefined
    }
  })
  loadNotes(1, false)
}, 300)

watch(highlightSearchQuery, (newVal) => {
  debouncedSyncSearch(newVal)
})

defineExpose({
  tagCounts,
  selectedTag,
  selectTag,
  filteredHighlights,
  handleLoadMore,
  onPageChange
})

// Inline Editing State
const editingHighlightId = ref<string | null>(null)
const editNoteText = ref('')
const editTagInput = ref('')
const isSavingEdit = ref(false)

function startEditing(item: Highlight) {
  editingHighlightId.value = item.id
  editNoteText.value = item.note || ''
  editTagInput.value = item.tags ? item.tags.join(', ') : ''
}

function cancelEditing() {
  editingHighlightId.value = null
  editNoteText.value = ''
  editTagInput.value = ''
}

async function saveEditing(item: Highlight) {
  isSavingEdit.value = true
  try {
    const tags = editTagInput.value
      .split(',')
      .map((t) => t.trim().replace(/^#/, ''))
      .filter((t) => t.length > 0)

    await notesStore.updateHighlight(item.id, {
      note: editNoteText.value.trim() || undefined,
      tags: tags.length > 0 ? tags : []
    })
    toast.success(t('notes.toast_update_success'))
    editingHighlightId.value = null
    editNoteText.value = ''
    editTagInput.value = ''
  } catch (err: unknown) {
    toast.error(formatError(err, 'notes.toast_update_error'))
  } finally {
    isSavingEdit.value = false
  }
}

// Delete Highlight Modal State
const highlightToDelete = ref<string | null>(null)
const isDeleteModalOpen = ref(false)
const isDeleting = ref(false)

onMounted(async () => {
  const queryPage = route.query.page ? parseInt(route.query.page as string, 10) : 1
  const initialPage = isNaN(queryPage) || queryPage < 1 ? 1 : queryPage

  if (typeof route.query.tag === 'string') {
    selectedTag.value = route.query.tag.trim().replace(/^#/, '').toLowerCase()
  }
  if (typeof route.query.search === 'string') {
    highlightSearchQuery.value = route.query.search
  }

  await loadNotes(initialPage, false)
  syncFlashcardState()
})

function renderMarkdown(raw: string | undefined | null): string {
  if (!raw) return ''
  const clean = raw.replace(/\\n/g, '\n')
  return md.render(clean)
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
  } catch (err: unknown) {
    toast.error(formatError(err, 'notes.toast_delete_error'))
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div class="py-4 sm:py-6 px-4 sm:px-6 lg:px-8 bg-slate-50 dark:bg-canvas min-h-[calc(100vh-3.5rem)] sm:min-h-[calc(100vh-3.75rem)] transition-colors duration-200">
    <div class="max-w-7xl mx-auto">
      <BoardLayout class="w-full">
        <!-- Header -->
        <template #header>
          <div class="flex items-center gap-3">
            <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
              <Highlighter class="w-4 h-4" :stroke-width="1.5" />
            </div>
            <div>
              <h1 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white">
                {{ $t('notes.title') }}
              </h1>
              <p class="text-xs text-slate-500 dark:text-slate-400">
                {{ $t('notes.subtitle') }}
              </p>
            </div>
          </div>
        </template>

        <!-- Filters Bar -->
        <template #filters>
          <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 w-full">
            <!-- Search Input -->
            <div v-if="notesStore.highlights.length > 0" class="relative max-w-md w-full">
              <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
              <input
                v-model="highlightSearchQuery"
                type="text"
                class="h-9 w-full pl-9 pr-8 text-xs sm:text-sm rounded-lg bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 transition-all shadow-sm"
                :placeholder="$t('notes.search_placeholder')"
              />
              <button
                v-if="highlightSearchQuery"
                @click="highlightSearchQuery = ''"
                class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 p-0.5 rounded-full"
              >
                <X class="w-3.5 h-3.5" :stroke-width="1.5" />
              </button>
            </div>

            <!-- Tag Chips -->
            <div v-if="notesStore.highlights.length > 0" class="flex items-center gap-1.5 overflow-x-auto no-scrollbar py-0.5">
              <button
                @click="selectTag(null)"
                :class="[
                  'px-2.5 py-1 rounded-full text-xs font-bold transition-all whitespace-nowrap shrink-0 border inline-flex items-center gap-1',
                  selectedTag === null
                    ? 'bg-brand-600 text-white border-transparent shadow-sm'
                    : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-50 dark:hover:bg-canvas-elevated'
                ]"
              >
                <span>{{ $t('notes.tag_all') }}</span>
                <span :class="selectedTag === null ? 'text-white/80' : 'text-slate-400 dark:text-slate-500'">
                  ({{ notesStore.totalCount || notesStore.highlights.length }})
                </span>
              </button>

              <button
                v-for="item in tagCounts"
                :key="item.tag"
                @click="selectTag(item.tag)"
                :class="[
                  'px-2.5 py-1 rounded-full text-xs font-semibold transition-all whitespace-nowrap shrink-0 border inline-flex items-center gap-1',
                  selectedTag === item.tag
                    ? 'bg-brand-600 text-white border-transparent shadow-sm'
                    : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-50 dark:hover:bg-canvas-elevated'
                ]"
              >
                <span>#{{ item.tag }}</span>
                <span :class="selectedTag === item.tag ? 'text-white/80' : 'text-slate-400 dark:text-slate-500'">
                  ({{ item.count }})
                </span>
              </button>
            </div>
          </div>
        </template>

        <!-- Content Grid (2-to-3 columns auto-flow) -->
        <template #content>
          <div v-if="notesStore.isLoading" class="flex flex-col items-center justify-center py-20 text-slate-500 dark:text-slate-400 text-sm">
            <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin mb-3"></div>
            <span>Loading saved highlights...</span>
          </div>

          <div v-else-if="filteredHighlights.length > 0" class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            <div
              v-for="item in filteredHighlights"
              :key="item.id"
              class="glass-card p-4 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] hover:border-brand-400 dark:hover:border-brand-500/30 transition-all flex flex-col justify-between shadow-xs space-y-3"
            >
              <div class="space-y-3">
                <!-- Reference bar -->
                <div class="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 font-semibold gap-2">
                  <div class="flex items-center gap-1.5 min-w-0">
                    <BookOpen class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400 shrink-0" />
                    <span class="text-slate-800 dark:text-slate-200 truncate">{{ item.bookTitle }}</span>
                    <span class="text-slate-400 dark:text-slate-600 shrink-0">•</span>
                    <span class="truncate">{{ item.chapterTitle }}</span>
                  </div>

                  <div class="flex items-center gap-1.5 shrink-0">
                    <!-- Edit Note action button -->
                    <button
                      @click="startEditing(item)"
                      :disabled="editingHighlightId === item.id"
                      class="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-canvas-subtle hover:bg-slate-100 dark:hover:bg-canvas-elevated border-slate-200/80 dark:border-white/[0.08] disabled:opacity-50"
                      :title="$t('notes.edit_note')"
                    >
                      <Pencil class="w-3.5 h-3.5 text-brand-500" />
                      <span class="hidden sm:inline">{{ $t('notes.edit_note') }}</span>
                    </button>

                    <!-- Deliberate Flashcard SM-2 creation button -->
                    <button
                      @click="handleCreateFlashcard(item.id)"
                      :disabled="creatingCardHighlightId === item.id || createdCardHighlightIds.has(item.id) || item.hasFlashcard"
                      class="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 disabled:opacity-60"
                      :class="(createdCardHighlightIds.has(item.id) || item.hasFlashcard)
                        ? 'bg-brand-500/15 text-brand-600 dark:text-brand-400 border-brand-500/30'
                        : 'bg-brand-50/70 dark:bg-brand-500/10 text-brand-700 dark:text-brand-300 hover:bg-brand-100/80 dark:hover:bg-brand-500/20 border-brand-200/80 dark:border-brand-500/20'"
                      :title="(createdCardHighlightIds.has(item.id) || item.hasFlashcard) ? $t('notes.in_sm2') : $t('notes.create_flashcard')"
                    >
                      <Check v-if="createdCardHighlightIds.has(item.id) || item.hasFlashcard" class="w-3.5 h-3.5 text-brand-500" />
                      <Zap v-else class="w-3.5 h-3.5 text-brand-500" />
                      <span class="hidden sm:inline">{{
                        (createdCardHighlightIds.has(item.id) || item.hasFlashcard)
                          ? $t('notes.in_sm2')
                          : creatingCardHighlightId === item.id
                            ? $t('notes.creating_card')
                            : $t('notes.create_flashcard')
                      }}</span>
                    </button>

                    <!-- Delete button -->
                    <button
                      @click="openDeleteModal(item.id)"
                      class="p-2 rounded-xl text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-slate-800 transition-colors shrink-0"
                      :title="$t('notes.delete_btn')"
                    >
                      <Trash2 class="w-4 h-4" />
                    </button>
                  </div>
                </div>

                <!-- Highlight Quote -->
                <blockquote class="border-l-2 border-brand-500/60 pl-3 py-0.5 text-xs sm:text-sm text-slate-800 dark:text-slate-200 italic leading-relaxed line-clamp-4">
                  "{{ item.selectedText }}"
                </blockquote>

                <!-- Inline Editing Form -->
                <div
                  v-if="editingHighlightId === item.id"
                  class="glass-panel p-3 rounded-xl border border-brand-300/80 dark:border-brand-500/30 space-y-2.5 animate-in fade-in duration-150"
                >
                  <div class="text-xs font-bold text-brand-600 dark:text-brand-400 flex items-center gap-1.5">
                    <Pencil class="w-3.5 h-3.5" />
                    <span>{{ $t('notes.edit_note') }}</span>
                  </div>

                  <textarea
                    v-model="editNoteText"
                    rows="2"
                    class="w-full text-xs bg-white dark:bg-canvas-elevated border border-slate-300 dark:border-white/[0.08] rounded-lg p-2.5 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 transition-all resize-none"
                    :placeholder="$t('reader.note_placeholder')"
                    autofocus
                  ></textarea>

                  <input
                    v-model="editTagInput"
                    type="text"
                    class="h-8 w-full text-xs bg-white dark:bg-canvas-elevated border border-slate-300 dark:border-white/[0.08] rounded-lg px-2.5 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 transition-all"
                    :placeholder="$t('reader.tags_placeholder')"
                    @keydown.enter.prevent="saveEditing(item)"
                  />

                  <div class="flex items-center justify-end gap-1.5 pt-1 border-t border-slate-200/80 dark:border-white/[0.08]">
                    <button
                      @click="cancelEditing"
                      class="px-2.5 py-1 rounded-lg text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors"
                    >
                      {{ $t('notes.cancel_edit') }}
                    </button>
                    <button
                      @click="saveEditing(item)"
                      :disabled="isSavingEdit"
                      class="flex items-center gap-1 px-3 py-1 rounded-lg bg-brand-600 hover:bg-brand-500 text-white text-xs font-semibold shadow-sm transition-all disabled:opacity-50"
                    >
                      <span v-if="isSavingEdit" class="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                      <Check v-else class="w-3 h-3" />
                      <span>{{ $t('notes.save_note') }}</span>
                    </button>
                  </div>
                </div>

                <!-- Normal Display Mode -->
                <template v-else>
                  <div
                    v-if="item.note"
                    class="p-3 rounded-xl bg-brand-50/60 dark:bg-brand-950/30 border border-brand-100 dark:border-brand-900/50 space-y-1"
                  >
                    <div class="text-[10px] font-bold uppercase tracking-wider text-brand-700 dark:text-brand-400">
                      Personal Note
                    </div>
                    <p class="text-xs text-slate-700 dark:text-slate-300 whitespace-pre-wrap leading-relaxed">
                      {{ item.note }}
                    </p>
                  </div>
                </template>
              </div>

              <!-- Card Footer: Tags -->
              <div v-if="item.tags?.length" class="flex flex-wrap gap-1.5 pt-2 border-t border-slate-100 dark:border-white/[0.04]">
                <button
                  v-for="(tag, i) in item.tags"
                  :key="i"
                  @click="selectTag(tag)"
                  :class="[
                    'px-2.5 py-1 rounded-lg text-xs font-semibold border transition-colors whitespace-nowrap shrink-0',
                    selectedTag === tag.trim().replace(/^#/, '').toLowerCase()
                      ? 'bg-brand-600 text-white border-transparent shadow-sm'
                      : 'bg-slate-100 dark:bg-canvas-subtle hover:bg-brand-100 dark:hover:bg-brand-900/40 text-slate-600 dark:text-slate-400 hover:text-brand-700 dark:hover:text-brand-300 border-slate-200 dark:border-white/[0.08]'
                  ]"
                >
                  #{{ tag.replace(/^#/, '') }}
                </button>
              </div>
            </div>
          </div>

          <!-- Empty State -->
          <div v-else class="text-center py-16">
            <Highlighter class="w-10 h-10 text-slate-400 dark:text-slate-600 mx-auto mb-2" />
            <h3 class="text-sm font-bold text-slate-800 dark:text-slate-200">
              {{ (highlightSearchQuery || selectedTag) ? 'No highlights match your search.' : $t('notes.no_notes') }}
            </h3>
            <p v-if="highlightSearchQuery || selectedTag" class="text-xs text-slate-500 mt-1">
              Try searching for a different keyword or tag.
            </p>
          </div>
        </template>

        <!-- Pagination -->
        <template #pagination>
          <div v-if="filteredHighlights.length > 0" class="w-full">
            <BasePagination
              :current-page="notesStore.currentPage"
              :total-pages="notesStore.totalPages"
              :total-count="notesStore.totalCount"
              :page-size="notesStore.pageSize"
              show-summary
              @change="onPageChange"
            />
          </div>
        </template>
      </BoardLayout>
    </div>
  </div>
    <!-- Delete Highlight Confirmation Modal (Teleported to Body) -->
    <Teleport to="body">
      <div
        v-if="isDeleteModalOpen && highlightToDelete"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200"
      >
        <div
          class="w-full max-w-md p-6 rounded-3xl glass-panel border border-slate-200/80 dark:border-white/[0.08] shadow-2xl space-y-4 animate-in zoom-in-95 duration-200"
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
</template>
