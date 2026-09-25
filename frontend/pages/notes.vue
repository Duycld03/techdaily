<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useDebounceFn } from '@vueuse/core'
import { Highlighter, Trash2, BookOpen, AlertTriangle, Zap, Search, Sparkles, X, Pencil, Check } from 'lucide-vue-next'
import BasePagination from '~/components/common/BasePagination.vue'
import BoardLayout from '~/components/layout/BoardLayout.vue'
import AppModal from '~/components/ui/AppModal.vue'
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

function getPrimaryTag(item: Highlight): string {
  if (item.tags && item.tags.length > 0) {
    const first = item.tags[0].trim().replace(/^#/, '')
    if (first) return first
  }
  return 'Highlight'
}

function getSecondaryTags(item: Highlight): string[] {
  if (!item.tags || item.tags.length <= 1) return []
  const primary = getPrimaryTag(item).toLowerCase()
  return item.tags.filter((t) => t.trim().replace(/^#/, '').toLowerCase() !== primary)
}


// Detail & Edit Note Modal State
const editingHighlight = ref<Highlight | null>(null)
const isModalOpen = ref(false)
const activeModalTab = ref<'preview' | 'edit'>('preview')
const editNoteText = ref('')
const editTagInput = ref('')
const isSavingEdit = ref(false)

function openDetailModal(item: Highlight) {
  editingHighlight.value = item
  editNoteText.value = item.note || ''
  editTagInput.value = item.tags ? item.tags.join(', ') : ''
  activeModalTab.value = 'preview'
  isModalOpen.value = true
}

function openEditModal(item: Highlight) {
  editingHighlight.value = item
  editNoteText.value = item.note || ''
  editTagInput.value = item.tags ? item.tags.join(', ') : ''
  activeModalTab.value = 'edit'
  isModalOpen.value = true
}

function closeModal() {
  isModalOpen.value = false
  editingHighlight.value = null
  editNoteText.value = ''
  editTagInput.value = ''
  activeModalTab.value = 'preview'
}

async function saveModalEditing() {
  if (!editingHighlight.value) return
  isSavingEdit.value = true
  try {
    const tags = editTagInput.value
      .split(',')
      .map((t) => t.trim().replace(/^#/, ''))
      .filter((t) => t.length > 0)

    const updatedNote = editNoteText.value.trim() || undefined
    await notesStore.updateHighlight(editingHighlight.value.id, {
      note: updatedNote,
      tags: tags.length > 0 ? tags : []
    })

    editingHighlight.value.note = updatedNote
    editingHighlight.value.tags = tags

    toast.success(t('notes.toast_update_success'))
    activeModalTab.value = 'preview'
  } catch (err: unknown) {
    toast.error(formatError(err, 'notes.toast_update_error'))
  } finally {
    isSavingEdit.value = false
  }
}

// Backward-compatible wrappers for tests
function startEditing(item: Highlight) {
  openEditModal(item)
}
function cancelEditing() {
  closeModal()
}
function closeEditModal() {
  closeModal()
}
async function saveEditing(_item?: Highlight) {
  await saveModalEditing()
}
defineExpose({
  tagCounts,
  selectedTag,
  selectTag,
  filteredHighlights,
  handleLoadMore,
  onPageChange,
  isEditModalOpen: computed(() => isModalOpen.value),
  isModalOpen: computed(() => isModalOpen.value),
  activeModalTab,
  openDetailModal,
  openEditModal,
  closeEditModal: closeModal,
  closeModal
})
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
      <BoardLayout flat class="w-full">
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
            <article
              v-for="item in filteredHighlights"
              :key="item.id"
              @click="openDetailModal(item)"
              class="group glass-card rounded-2xl border border-slate-200/80 dark:border-white/[0.08] hover:border-brand-400 dark:hover:border-brand-500/30 transition-all flex flex-col justify-between shadow-xs p-4 space-y-3 bg-white dark:bg-canvas-elevated h-full cursor-pointer"
            >
              <div class="space-y-3">
                <!-- Header: Showcase Archetype (Primary Tag Pill on Left, Doc Source Context & Hover Delete on Right) -->
                <div class="flex items-center justify-between gap-2">
                  <!-- Primary tag badge pill -->
                  <button
                    type="button"
                    @click.stop="item.tags?.length ? selectTag(getPrimaryTag(item)) : null"
                    :class="[
                      'rounded-full px-2.5 py-0.5 text-[11px] font-semibold transition-colors shrink-0 cursor-pointer',
                      selectedTag === getPrimaryTag(item).toLowerCase()
                        ? 'bg-brand-600 text-white shadow-xs'
                        : 'bg-brand-500/10 text-brand-600 dark:text-brand-300 hover:bg-brand-500/20'
                    ]"
                    :title="item.tags?.length ? `Filter by #${getPrimaryTag(item)}` : 'Untagged highlight'"
                  >
                    #{{ getPrimaryTag(item) }}
                  </button>

                  <!-- Document Source Metadata & Delete Trigger -->
                  <div class="flex items-center gap-1.5 min-w-0 flex-1 justify-end text-xs text-slate-400 dark:text-slate-500 font-medium">
                    <BookOpen class="w-3.5 h-3.5 text-slate-400 dark:text-slate-500 shrink-0" />
                    <span class="truncate max-w-[140px] sm:max-w-[200px]" :title="`${item.bookTitle} • ${item.chapterTitle}`">
                      {{ item.bookTitle || 'Document' }} <span class="text-slate-300 dark:text-slate-600">•</span> {{ item.chapterTitle || 'Reading Slice' }}
                    </span>

                    <!-- Delete button (visible on mobile, reveals on hover on desktop) -->
                    <button
                      type="button"
                      @click.stop="openDeleteModal(item.id)"
                      class="p-1 rounded-md text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition-all opacity-100 sm:opacity-0 sm:group-hover:opacity-100 focus:opacity-100 shrink-0 cursor-pointer"
                      :title="$t('notes.delete_btn')"
                    >
                      <Trash2 class="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>

                <!-- Body Typography: Personal Reflection Note Priority + Clean Excerpt Quote -->
                <div class="space-y-2">
                  <template v-if="item.note">
                    <div class="space-y-1">
                      <div class="text-[10px] font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 flex items-center gap-1">
                        <Sparkles class="w-3 h-3 shrink-0" />
                        <span>Personal Note</span>
                      </div>
                      <p class="text-xs sm:text-sm font-semibold text-slate-900 dark:text-white leading-relaxed line-clamp-2">
                        {{ item.note }}
                      </p>
                    </div>
                    <!-- Clean Supporting Excerpt Quote -->
                    <p class="text-xs text-slate-500 dark:text-slate-400 leading-relaxed line-clamp-1 pl-2.5 border-l-2 border-slate-200 dark:border-white/[0.08]">
                      "{{ item.selectedText }}"
                    </p>
                  </template>

                  <template v-else>
                    <!-- Highlight quote as Hero (clamped to 3 lines) -->
                    <p class="text-xs sm:text-sm text-slate-800 dark:text-slate-200 leading-relaxed line-clamp-3 pl-2.5 border-l-2 border-brand-500/60 font-normal">
                      "{{ item.selectedText }}"
                    </p>
                  </template>
                </div>
              </div>

              <!-- Footer: Secondary Tags & Subtle Action Buttons -->
              <div class="pt-3 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-between gap-2">
                <!-- Tags List -->
                <div class="flex items-center gap-1.5 min-w-0 overflow-hidden">
                  <template v-if="getSecondaryTags(item).length > 0">
                    <button
                      v-for="(tag, i) in getSecondaryTags(item).slice(0, 1)"
                      :key="i"
                      type="button"
                      @click.stop="selectTag(tag)"
                      :class="[
                        'px-2 py-0.5 rounded-md text-[11px] font-semibold border transition-colors whitespace-nowrap shrink-0 cursor-pointer truncate max-w-[100px]',
                        selectedTag === tag.trim().replace(/^#/, '').toLowerCase()
                          ? 'bg-brand-600 text-white border-transparent shadow-xs'
                          : 'bg-slate-100 dark:bg-canvas-subtle hover:bg-brand-100 dark:hover:bg-brand-900/40 text-slate-600 dark:text-slate-400 hover:text-brand-700 dark:hover:text-brand-300 border-slate-200 dark:border-white/[0.08]'
                      ]"
                    >
                      #{{ tag.replace(/^#/, '') }}
                    </button>
                    <span
                      v-if="getSecondaryTags(item).length > 1"
                      class="px-1.5 py-0.5 rounded-md text-[10px] font-medium text-slate-400 dark:text-slate-500 bg-slate-100 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] shrink-0"
                    >
                      +{{ getSecondaryTags(item).length - 1 }}
                    </span>
                  </template>
                </div>

                <!-- Subtle Action Triggers -->
                <div class="flex items-center gap-1.5 shrink-0 ml-auto">
                  <!-- Edit Note trigger (opens AppModal in Edit mode) -->
                  <button
                    type="button"
                    @click.stop="openEditModal(item)"
                    class="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-canvas-subtle hover:bg-slate-100 dark:hover:bg-canvas-elevated hover:text-brand-600 dark:hover:text-brand-400 border-slate-200/80 dark:border-white/[0.08] active:scale-95 cursor-pointer"
                    :title="$t('notes.edit_note')"
                  >
                    <Pencil class="w-3.5 h-3.5 text-brand-500" />
                    <span>{{ $t('notes.tab_edit') }}</span>
                  </button>

                  <!-- Deliberate Flashcard SM-2 creation button -->
                  <button
                    type="button"
                    @click.stop="handleCreateFlashcard(item.id)"
                    :disabled="creatingCardHighlightId === item.id || createdCardHighlightIds.has(item.id) || item.hasFlashcard"
                    class="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 disabled:opacity-60 active:scale-95 cursor-pointer"
                    :class="(createdCardHighlightIds.has(item.id) || item.hasFlashcard)
                      ? 'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30'
                      : 'bg-brand-50/70 dark:bg-brand-500/10 text-brand-700 dark:text-brand-300 hover:bg-brand-100/80 dark:hover:bg-brand-500/20 border-brand-200/80 dark:border-brand-500/20'"
                    :title="(createdCardHighlightIds.has(item.id) || item.hasFlashcard) ? $t('notes.in_sm2') : $t('notes.create_flashcard')"
                  >
                    <Check v-if="createdCardHighlightIds.has(item.id) || item.hasFlashcard" class="w-3.5 h-3.5 text-emerald-500" />
                    <Zap v-else class="w-3.5 h-3.5 text-brand-500" />
                    <span>{{
                      (createdCardHighlightIds.has(item.id) || item.hasFlashcard)
                        ? $t('notes.in_sm2')
                        : creatingCardHighlightId === item.id
                          ? $t('notes.creating_card')
                          : $t('notes.create_flashcard')
                    }}</span>
                  </button>
                </div>
              </div>
            </article>
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
    <!-- Dual-Mode Detail & Edit Note Modal -->
    <AppModal
      :open="isModalOpen"
      :title="activeModalTab === 'preview' ? $t('notes.modal_title_details') : $t('notes.modal_title_edit')"
      max-width="max-w-2xl"
      @close="closeModal"
    >
      <template #header>
        <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2.5 w-full pr-1">
          <div class="flex items-center gap-2 min-w-0">
            <BookOpen v-if="activeModalTab === 'preview'" class="w-5 h-5 text-brand-500 shrink-0" />
            <Pencil v-else class="w-5 h-5 text-brand-500 shrink-0" />
            <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white truncate">
              {{ activeModalTab === 'preview' ? $t('notes.modal_title_details') : $t('notes.modal_title_edit') }}
            </h2>
          </div>

          <!-- Mode Switcher: Preview vs Edit -->
          <div class="flex items-center gap-1 bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] p-1 rounded-xl text-xs font-semibold shrink-0">
            <button
              type="button"
              @click="activeModalTab = 'preview'"
              :class="[
                'px-2.5 py-1 rounded-lg transition-all whitespace-nowrap cursor-pointer',
                activeModalTab === 'preview'
                  ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-xs font-bold'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              ]"
            >
              {{ $t('notes.tab_details') }}
            </button>
            <button
              type="button"
              @click="activeModalTab = 'edit'"
              :class="[
                'px-2.5 py-1 rounded-lg transition-all whitespace-nowrap cursor-pointer',
                activeModalTab === 'edit'
                  ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-xs font-bold'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              ]"
            >
              {{ $t('notes.tab_edit') }}
            </button>
          </div>
        </div>
      </template>

      <div v-if="editingHighlight" class="space-y-4">
        <!-- DETAILS / PREVIEW TAB -->
        <div v-if="activeModalTab === 'preview'" class="space-y-4">
          <!-- Document Source Metadata & Full Excerpt Quote -->
          <div class="space-y-2">
            <div class="text-[11px] font-semibold text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
              <BookOpen class="w-3.5 h-3.5 text-brand-500 shrink-0" />
              <span class="truncate">{{ editingHighlight.bookTitle || 'Document' }} <span class="text-slate-300 dark:text-slate-600">•</span> {{ editingHighlight.chapterTitle || 'Reading Slice' }}</span>
            </div>
            <blockquote class="text-xs sm:text-sm text-slate-700 dark:text-slate-200 bg-slate-50 dark:bg-canvas-subtle p-3.5 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] leading-relaxed italic border-l-4 border-brand-500">
              "{{ editingHighlight.selectedText }}"
            </blockquote>
          </div>

          <!-- Personal Reflection Note Rendered via Markdown -->
          <div class="space-y-2">
            <div class="text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 flex items-center gap-1.5">
              <Sparkles class="w-3.5 h-3.5 shrink-0" />
              <span>{{ $t('notes.reflection_note') }}</span>
            </div>

            <div
              v-if="editingHighlight.note"
              class="prose prose-sm dark:prose-invert max-w-none p-3.5 rounded-2xl bg-white dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] text-xs sm:text-sm leading-relaxed text-slate-800 dark:text-slate-200"
              v-html="renderMarkdown(editingHighlight.note)"
            ></div>

            <p v-else class="text-xs text-slate-400 dark:text-slate-500 italic p-3 bg-slate-50 dark:bg-canvas-subtle rounded-xl border border-dashed border-slate-200 dark:border-white/[0.08]">
              {{ $t('notes.no_note_written') }}
            </p>
          </div>

          <!-- Highlight Tags -->
          <div v-if="editingHighlight.tags && editingHighlight.tags.length > 0" class="space-y-1.5 pt-1">
            <div class="text-[11px] font-semibold text-slate-400 dark:text-slate-500 uppercase tracking-wider">Tags</div>
            <div class="flex flex-wrap items-center gap-1.5">
              <span
                v-for="(tag, i) in editingHighlight.tags"
                :key="i"
                class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20"
              >
                #{{ tag.replace(/^#/, '') }}
              </span>
            </div>
          </div>
        </div>

        <!-- EDIT TAB -->
        <div v-else class="space-y-4">
          <!-- Source Excerpt Context (Read-Only) -->
          <div class="space-y-1.5">
            <div class="text-[11px] font-semibold text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
              <BookOpen class="w-3.5 h-3.5 text-brand-500 shrink-0" />
              <span class="truncate">{{ editingHighlight.bookTitle || 'Document' }} • {{ editingHighlight.chapterTitle || 'Reading Slice' }}</span>
            </div>
            <blockquote class="text-xs text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-canvas-subtle p-3 rounded-xl border border-slate-200/80 dark:border-white/[0.08] leading-relaxed line-clamp-3 italic border-l-2 border-brand-500/60">
              "{{ editingHighlight.selectedText }}"
            </blockquote>
          </div>

          <!-- Reflection Note Input -->
          <div class="space-y-1.5">
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-200">
              {{ $t('notes.reflection_note') }}
            </label>
            <textarea
              v-model="editNoteText"
              rows="5"
              class="w-full text-xs sm:text-sm bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl p-3 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 transition-all resize-y"
              :placeholder="$t('reader.note_placeholder')"
              autofocus
            ></textarea>
          </div>

          <!-- Tags Input -->
          <div class="space-y-1.5">
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-200">
              Tags
            </label>
            <input
              v-model="editTagInput"
              type="text"
              class="h-9 w-full text-xs sm:text-sm bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl px-3 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 transition-all"
              :placeholder="$t('reader.tags_placeholder')"
              @keydown.enter.prevent="saveModalEditing"
            />
            <p class="text-[11px] text-slate-400 dark:text-slate-500">
              Separate tags with commas (e.g. storage, concurrency, vue)
            </p>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="flex items-center justify-between gap-2 w-full">
          <!-- In Preview Mode: SM-2 trigger on left + Edit / Close on right -->
          <template v-if="activeModalTab === 'preview'">
            <div v-if="editingHighlight">
              <button
                type="button"
                @click="handleCreateFlashcard(editingHighlight.id)"
                :disabled="creatingCardHighlightId === editingHighlight.id || createdCardHighlightIds.has(editingHighlight.id) || editingHighlight.hasFlashcard"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all disabled:opacity-60 cursor-pointer active:scale-95"
                :class="(createdCardHighlightIds.has(editingHighlight.id) || editingHighlight.hasFlashcard)
                  ? 'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30'
                  : 'bg-brand-50/70 dark:bg-brand-500/10 text-brand-700 dark:text-brand-300 hover:bg-brand-100 dark:hover:bg-brand-500/20 border-brand-200/80 dark:border-brand-500/20'"
              >
                <Check v-if="createdCardHighlightIds.has(editingHighlight.id) || editingHighlight.hasFlashcard" class="w-3.5 h-3.5 text-emerald-500" />
                <Zap v-else class="w-3.5 h-3.5 text-brand-500" />
                <span>{{
                  (createdCardHighlightIds.has(editingHighlight.id) || editingHighlight.hasFlashcard)
                    ? $t('notes.in_sm2')
                    : creatingCardHighlightId === editingHighlight.id
                      ? $t('notes.creating_card')
                      : $t('notes.create_flashcard')
                }}</span>
              </button>
            </div>
            <div v-else></div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                @click="activeModalTab = 'edit'"
                class="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold border border-slate-200/80 dark:border-white/[0.08] bg-slate-50 dark:bg-canvas-subtle hover:bg-slate-100 dark:hover:bg-canvas-elevated text-slate-700 dark:text-slate-300 transition-colors cursor-pointer"
              >
                <Pencil class="w-3.5 h-3.5 text-brand-500" />
                <span>{{ $t('notes.tab_edit') }}</span>
              </button>
              <button
                type="button"
                @click="closeModal"
                class="px-3.5 py-1.5 rounded-xl text-xs font-semibold bg-brand-600 hover:bg-brand-500 text-white transition-colors cursor-pointer"
              >
                {{ $t('notes.close_btn') }}
              </button>
            </div>
          </template>

          <!-- In Edit Mode: Cancel + Save on right -->
          <template v-else>
            <div class="flex items-center justify-end gap-2 w-full">
              <button
                type="button"
                @click="closeModal"
                class="px-3.5 py-1.5 rounded-xl text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors cursor-pointer"
              >
                {{ $t('notes.cancel_edit') }}
              </button>
              <button
                type="button"
                @click="saveModalEditing"
                :disabled="isSavingEdit"
                class="flex items-center gap-1.5 px-4 py-1.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs font-semibold shadow-sm transition-all disabled:opacity-50 active:scale-95 cursor-pointer"
              >
                <span v-if="isSavingEdit" class="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <Check v-else class="w-3.5 h-3.5" />
                <span>{{ $t('notes.save_note') }}</span>
              </button>
            </div>
          </template>
        </div>
      </template>
    </AppModal>

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
