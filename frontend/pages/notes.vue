<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Highlighter, Trash2, BookOpen, AlertTriangle } from 'lucide-vue-next'

const notesStore = useNotesStore()
const toast = useToast()
const filterTag = ref<string | null>(null)

// Delete Modal State
const highlightToDelete = ref<string | null>(null)
const isDeleteModalOpen = ref(false)
const isDeleting = ref(false)

onMounted(() => {
  notesStore.fetchHighlights()
})

function openDeleteModal(id: string) {
  highlightToDelete.value = id
  isDeleteModalOpen.value = true
}

async function confirmDeleteHighlight() {
  if (!highlightToDelete.value) return
  isDeleting.value = true
  try {
    await notesStore.deleteHighlight(highlightToDelete.value)
    toast.success('Đã xóa đoạn ghi chú.')
    isDeleteModalOpen.value = false
    highlightToDelete.value = null
  } catch (err: any) {
    toast.error(err.message || 'Không thể xóa ghi chú.')
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto p-4 sm:p-6 md:p-10 space-y-6 sm:space-y-8 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Header -->
    <div>
      <h1 class="text-xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2.5 sm:gap-3">
        <Highlighter class="w-6 h-6 sm:w-7 sm:h-7 text-brand-600 dark:text-brand-400" />
        <span>{{ $t('notes.title') }}</span>
      </h1>
      <p class="text-sm md:text-lg text-slate-500 dark:text-slate-400 mt-1 font-medium">{{ $t('notes.subtitle') }}</p>
    </div>

    <!-- Highlights List -->
    <div v-if="notesStore.isLoading" class="flex flex-col items-center justify-center py-20 text-slate-500 dark:text-slate-400 text-sm">
      <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin mb-3"></div>
      <span>Loading saved highlights...</span>
    </div>

    <div v-else-if="notesStore.highlights.length > 0" class="space-y-4">
      <div
        v-for="item in notesStore.highlights"
        :key="item.id"
        class="p-5 sm:p-7 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 hover:border-brand-400 dark:hover:border-slate-700 transition-all space-y-3.5 sm:space-y-4 shadow-md dark:shadow-sm"
      >
        <!-- Reference bar -->
        <div class="flex items-center justify-between text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-semibold">
          <div class="flex items-center gap-2">
            <BookOpen class="w-4 h-4 text-brand-600 dark:text-brand-400" />
            <span class="text-slate-800 dark:text-slate-200">{{ item.bookTitle }}</span>
            <span class="text-slate-400 dark:text-slate-600">•</span>
            <span>{{ item.chapterTitle }}</span>
          </div>

          <button
            @click="openDeleteModal(item.id)"
            class="p-2 rounded-xl text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-slate-800 transition-colors"
            title="Delete Highlight"
          >
            <Trash2 class="w-4 h-4" />
          </button>
        </div>

        <!-- Highlighted Text Quote -->
        <div class="p-4 sm:p-5 rounded-2xl bg-slate-50 dark:bg-slate-950/80 border-l-4 border-brand-500 text-sm md:text-lg text-slate-800 dark:text-slate-200 leading-relaxed font-sans italic">
          "{{ item.selectedText }}"
        </div>

        <!-- Note (if any) -->
        <p v-if="item.note" class="text-sm md:text-lg text-slate-700 dark:text-slate-300 bg-slate-100 dark:bg-slate-800/40 p-3.5 sm:p-4 rounded-xl border border-slate-200 dark:border-slate-800">
          <strong class="text-slate-900 dark:text-slate-400">Note:</strong> {{ item.note }}
        </p>

        <!-- Tags -->
        <div v-if="item.tags?.length" class="flex flex-wrap gap-1.5 pt-1">
          <span
            v-for="(tag, i) in item.tags"
            :key="i"
            class="px-3 py-1 rounded-lg bg-slate-100 dark:bg-slate-800 text-xs font-semibold text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-700"
          >
            #{{ tag }}
          </span>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="text-center py-16 bg-white dark:bg-slate-900/40 rounded-3xl border border-slate-200 dark:border-slate-800/80 p-8 shadow-sm">
      <Highlighter class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto mb-3" />
      <h3 class="text-base font-bold text-slate-800 dark:text-slate-200">No Highlights Saved</h3>
      <p class="text-sm text-slate-500 mt-1 max-w-sm mx-auto">{{ $t('notes.no_notes') }}</p>
    </div>

    <!-- Delete Confirmation Modal (Teleported to Body) -->
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
              <span>{{ isDeleting ? 'Đang xóa...' : $t('notes.confirm_delete_btn') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
