<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Highlighter, Tag, Trash2, BookOpen, Quote, Sparkles } from 'lucide-vue-next'

const notesStore = useNotesStore()
const filterTag = ref<string | null>(null)

onMounted(() => {
  notesStore.fetchHighlights()
})

function selectTag(tag: string | null) {
  filterTag.value = tag
  notesStore.fetchHighlights(tag || undefined)
}

async function handleDelete(id: string) {
  if (confirm('Are you sure you want to delete this highlight?')) {
    await notesStore.deleteHighlight(id)
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto p-6 md:p-10 space-y-8">
    <!-- Header -->
    <div>
      <h1 class="text-2xl md:text-3xl font-extrabold text-white tracking-tight flex items-center gap-3">
        <Highlighter class="w-7 h-7 text-brand-400" />
        <span>{{ $t('notes.title') }}</span>
      </h1>
      <p class="text-xs text-slate-400 mt-1">{{ $t('notes.subtitle') }}</p>
    </div>

    <!-- Highlights List -->
    <div v-if="notesStore.isLoading" class="flex flex-col items-center justify-center py-20 text-slate-400 text-sm">
      <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin mb-3"></div>
      <span>Loading saved highlights...</span>
    </div>

    <div v-else-if="notesStore.highlights.length > 0" class="space-y-4">
      <div
        v-for="item in notesStore.highlights"
        :key="item.id"
        class="p-6 rounded-2xl bg-slate-900 border border-slate-800 hover:border-slate-700 transition-all space-y-4 shadow-sm"
      >
        <!-- Reference bar -->
        <div class="flex items-center justify-between text-xs text-slate-400">
          <div class="flex items-center gap-2">
            <BookOpen class="w-3.5 h-3.5 text-brand-400" />
            <span class="font-semibold text-slate-300">{{ item.bookTitle }}</span>
            <span class="text-slate-600">•</span>
            <span>{{ item.chapterTitle }}</span>
          </div>

          <button
            @click="handleDelete(item.id)"
            class="p-1.5 rounded-lg text-slate-500 hover:text-red-400 hover:bg-slate-800 transition-colors"
            title="Delete Highlight"
          >
            <Trash2 class="w-4 h-4" />
          </button>
        </div>

        <!-- Highlighted Text Quote -->
        <div class="p-4 rounded-xl bg-slate-950/80 border-l-4 border-brand-500 text-sm text-slate-200 leading-relaxed font-sans italic">
          "{{ item.selectedText }}"
        </div>

        <!-- Note (if any) -->
        <p v-if="item.note" class="text-xs text-slate-300 bg-slate-800/40 p-3 rounded-lg border border-slate-800">
          <strong class="text-slate-400">Note:</strong> {{ item.note }}
        </p>

        <!-- Tags -->
        <div v-if="item.tags?.length" class="flex flex-wrap gap-1.5 pt-1">
          <span
            v-for="(tag, i) in item.tags"
            :key="i"
            class="px-2.5 py-0.5 rounded-md bg-slate-800 text-[11px] font-medium text-slate-300 border border-slate-700"
          >
            #{{ tag }}
          </span>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="text-center py-16 bg-slate-900/40 rounded-3xl border border-slate-800/80 p-8">
      <Highlighter class="w-12 h-12 text-slate-600 mx-auto mb-3" />
      <h3 class="text-base font-bold text-slate-200">No Highlights Saved</h3>
      <p class="text-xs text-slate-500 mt-1 max-w-sm mx-auto">{{ $t('notes.no_notes') }}</p>
    </div>
  </div>
</template>
