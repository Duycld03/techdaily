<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { Highlighter, Trash2, BookOpen, AlertTriangle, Zap, Search, Sparkles, X, Pencil, Check } from 'lucide-vue-next'
import { useNotesStore, type Highlight } from '~/stores/useNotesStore'
import { useReviewStore } from '~/stores/useReviewStore'
import { useApiError } from '~/composables/useApiError'
import MarkdownIt from 'markdown-it'

const { t, locale } = useI18n()
const { formatError } = useApiError()
const notesStore = useNotesStore()
const toast = useToast()
const reviewStore = useReviewStore()
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

const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

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
  await notesStore.fetchHighlights()
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
  <div class="max-w-4xl mx-auto p-4 sm:p-6 md:p-10 space-y-6 sm:space-y-8 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Header -->
    <div class="space-y-1 sm:space-y-2">
      <h1 class="text-xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2.5 sm:gap-3">
        <Highlighter class="w-6 h-6 sm:w-7 sm:h-7 text-indigo-600 dark:text-indigo-400" />
        <span>{{ $t('notes.title') }}</span>
      </h1>
      <p class="text-sm md:text-lg text-slate-500 dark:text-slate-400 font-medium leading-relaxed">{{ $t('notes.subtitle') }}</p>
    </div>

    <!-- Highlights Hub -->
    <div class="space-y-4">
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
              <!-- Edit Note action button -->
              <button
                @click="startEditing(item)"
                :disabled="editingHighlightId === item.id"
                class="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 border-slate-200 dark:border-slate-700 disabled:opacity-50"
                :title="$t('notes.edit_note')"
              >
                <Pencil class="w-3.5 h-3.5 text-indigo-500" />
                <span class="hidden sm:inline">{{ $t('notes.edit_note') }}</span>
              </button>

              <!-- Deliberate Flashcard SM-2 creation button -->
              <button
                @click="handleCreateFlashcard(item.id)"
                :disabled="creatingCardHighlightId === item.id || createdCardHighlightIds.has(item.id)"
                class="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 disabled:opacity-60"
                :class="createdCardHighlightIds.has(item.id)
                  ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 border-emerald-200 dark:border-emerald-800'
                  : 'bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 hover:bg-amber-100 dark:hover:bg-amber-900/50 border-amber-200 dark:border-amber-800/60'"
                :title="createdCardHighlightIds.has(item.id) ? $t('notes.in_sm2') : $t('notes.create_flashcard')"
              >
                <Check v-if="createdCardHighlightIds.has(item.id)" class="w-3.5 h-3.5 text-emerald-500" />
                <Zap v-else class="w-3.5 h-3.5 text-amber-500" />
                <span class="hidden sm:inline">{{
                  createdCardHighlightIds.has(item.id)
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

          <!-- Inline Editing Form -->
          <div
            v-if="editingHighlightId === item.id"
            class="p-4 rounded-2xl bg-slate-100 dark:bg-slate-800/80 border border-indigo-300 dark:border-indigo-600 space-y-3 animate-in fade-in duration-150"
          >
            <div class="text-xs font-bold text-indigo-700 dark:text-indigo-400 flex items-center gap-1.5">
              <Pencil class="w-3.5 h-3.5" />
              <span>{{ $t('notes.edit_note') }}</span>
            </div>

            <textarea
              v-model="editNoteText"
              rows="3"
              class="w-full text-xs sm:text-sm bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-xl p-3 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all resize-none"
              :placeholder="$t('reader.note_placeholder')"
              autofocus
            ></textarea>

            <input
              v-model="editTagInput"
              type="text"
              class="w-full text-xs sm:text-sm bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all"
              :placeholder="$t('reader.tags_placeholder')"
              @keydown.enter.prevent="saveEditing(item)"
            />

            <div class="flex items-center justify-end gap-2 pt-1 border-t border-slate-200 dark:border-slate-700">
              <button
                @click="cancelEditing"
                class="px-3 py-1.5 rounded-xl text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors whitespace-nowrap shrink-0"
              >
                {{ $t('notes.cancel_edit') }}
              </button>
              <button
                @click="saveEditing(item)"
                :disabled="isSavingEdit"
                class="flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-semibold shadow-sm transition-all whitespace-nowrap shrink-0 disabled:opacity-50"
              >
                <span v-if="isSavingEdit" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <Check v-else class="w-3.5 h-3.5" />
                <span>{{ $t('notes.save_note') }}</span>
              </button>
            </div>
          </div>

          <!-- Normal Display Mode -->
          <template v-else>
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
                class="px-2.5 py-1 rounded-lg bg-slate-100 dark:bg-slate-800 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-indigo-700 dark:hover:text-indigo-300 border border-slate-200 dark:border-slate-700 transition-colors whitespace-nowrap shrink-0"
              >
                #{{ tag.replace(/^#/, '') }}
              </button>
            </div>
          </template>
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
  </div>
</template>
