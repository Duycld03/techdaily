<script setup lang="ts">
import { ref, computed } from 'vue'
import { BookOpen, Clock, Tag, Sparkles } from 'lucide-vue-next'
import MarkdownIt from 'markdown-it'
import type { Topic, DocumentChunk } from '~/stores/useDailyFocusStore'
import MicroQuizCard from '~/components/today/MicroQuizCard.vue'
import TermExplainerModal from '~/components/today/TermExplainerModal.vue'

const props = defineProps<{
  topic: Topic
  documentChunk?: DocumentChunk
}>()

const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

const renderedDocHtml = computed(() => {
  const content = props.documentChunk?.originalTextMarkdown || props.topic.deepDiveMarkdown || props.topic.summary
  return md.render(content)
})

// Highlight / Floating Explainer state
const selectedTerm = ref<string | null>(null)
const selectedCategory = ref<string>('Architecture')
const selectedContext = ref<string>('')
const isExplainerOpen = ref(false)

function handleTextSelection() {
  const selection = window.getSelection()
  if (selection && !selection.isCollapsed) {
    const text = selection.toString().trim()
    if (text.length > 2 && text.length < 50) {
      selectedTerm.value = text
      selectedContext.value = selection.anchorNode?.textContent?.slice(0, 200) || text
      selectedCategory.value = props.topic.title
      isExplainerOpen.value = true
    }
  }
}
</script>

<template>
  <div class="h-full flex flex-col bg-white/60 dark:bg-slate-950/40 overflow-y-auto p-6 md:p-9 transition-colors duration-200" @mouseup="handleTextSelection">
    <!-- Header info -->
    <div class="mb-6">
      <div class="flex items-center gap-2 text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 uppercase tracking-wider mb-2.5">
        <BookOpen class="w-4 h-4" />
        <span>{{ $t('today.doc_reader') }}</span>
        <span class="text-slate-400 dark:text-slate-600">•</span>
        <span class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400">
          <Clock class="w-3.5 h-3.5" />
          {{ documentChunk?.estimatedReadMinutes || 3 }} {{ $t('today.estimated_read') }}
        </span>
      </div>

      <h1 class="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight leading-snug mb-3.5">
        {{ topic.title }}
      </h1>

      <p class="text-sm md:text-base text-slate-700 dark:text-slate-300 leading-relaxed bg-slate-100/90 dark:bg-slate-900/60 p-4.5 rounded-2xl border border-slate-200 dark:border-slate-800/80 font-normal">
        {{ topic.summary }}
      </p>

      <!-- Key Takeaways -->
      <div v-if="documentChunk?.keyTakeaways?.length" class="mt-4 flex flex-wrap gap-2">
        <span
          v-for="(takeaway, i) in documentChunk.keyTakeaways"
          :key="i"
          class="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-slate-100 dark:bg-slate-900 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <Tag class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400 shrink-0" />
          {{ takeaway }}
        </span>
      </div>
    </div>

    <div class="w-full h-px bg-slate-200 dark:bg-slate-800/80 mb-6"></div>

    <!-- Reading Content (Rendered Markdown) -->
    <div class="markdown-body text-slate-800 dark:text-slate-200" v-html="renderedDocHtml"></div>

    <!-- Benchmark Snippet (if available) -->
    <div v-if="topic.benchmarkSnippet" class="mt-6 p-4 rounded-2xl bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 font-mono text-xs sm:text-sm text-brand-700 dark:text-brand-300 shadow-sm">
      <div class="text-slate-700 dark:text-slate-400 font-bold mb-2 font-sans flex items-center gap-1.5">
        <Sparkles class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>Performance Benchmark Context:</span>
      </div>
      <pre class="overflow-x-auto p-0 m-0 bg-transparent border-0">{{ topic.benchmarkSnippet }}</pre>
    </div>

    <!-- Interactive Micro Quiz Check -->
    <MicroQuizCard v-if="documentChunk?.microQuiz" :quiz="documentChunk.microQuiz" />

    <!-- Term Explainer Modal -->
    <TermExplainerModal
      v-if="isExplainerOpen && selectedTerm"
      :term="selectedTerm"
      :category="selectedCategory"
      :context="selectedContext"
      @close="isExplainerOpen = false"
    />
  </div>
</template>
