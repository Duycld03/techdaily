<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { BookOpen, Clock, Tag, Sparkles, Copy, Check } from 'lucide-vue-next'
import MarkdownIt from 'markdown-it'
import type { Topic, DocumentChunk } from '~/stores/useDailyFocusStore'
import MicroQuizCard from '~/components/today/MicroQuizCard.vue'
import TermExplainerModal from '~/components/today/TermExplainerModal.vue'

const { t, locale } = useI18n()

const props = defineProps<{
  topic: Topic
  documentChunk?: DocumentChunk
}>()

const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

const renderedDeepDiveHtml = computed(() => {
  const content = props.topic.deepDiveMarkdown || props.documentChunk?.originalTextMarkdown || props.topic.summary || ''
  return md.render(content)
})

const renderedChunkHtml = computed(() => {
  if (
    props.documentChunk?.originalTextMarkdown &&
    props.topic.deepDiveMarkdown &&
    props.documentChunk.originalTextMarkdown !== props.topic.deepDiveMarkdown
  ) {
    return md.render(props.documentChunk.originalTextMarkdown)
  }
  return ''
})

// Floating Action Bar state
const readerContentRef = ref<HTMLElement | null>(null)
const floatingMenu = ref<{ visible: boolean; x: number; y: number; text: string; context: string }>({
  visible: false,
  x: 0,
  y: 0,
  text: '',
  context: ''
})

const copied = ref(false)

// Explainer Modal state
const selectedTerm = ref<string | null>(null)
const selectedCategory = ref<string>('Architecture')
const selectedContext = ref<string>('')
const isExplainerOpen = ref(false)

function handleMouseUp(e: MouseEvent) {
  // Ignore clicks inside interactive elements or quiz
  const target = e.target as HTMLElement
  if (target.closest('.micro-quiz-container') || target.closest('button') || target.closest('input')) {
    floatingMenu.value.visible = false
    return
  }

  const selection = window.getSelection()
  if (!selection || selection.isCollapsed) {
    floatingMenu.value.visible = false
    return
  }

  const text = selection.toString().trim()
  if (text.length >= 2 && text.length <= 80) {
    const range = selection.getRangeAt(0)
    const rect = range.getBoundingClientRect()

    // Ensure selection is inside reader container
    if (readerContentRef.value && readerContentRef.value.contains(range.commonAncestorContainer)) {
      floatingMenu.value = {
        visible: true,
        x: Math.max(10, rect.left + rect.width / 2),
        y: Math.max(10, rect.top - 46),
        text,
        context: selection.anchorNode?.textContent?.slice(0, 200) || text
      }
      return
    }
  }

  floatingMenu.value.visible = false
}

function handleDocumentClick(e: MouseEvent) {
  const target = e.target as HTMLElement
  if (!target.closest('.floating-selection-menu') && !target.closest('.doc-reader-content')) {
    floatingMenu.value.visible = false
  }
}

function triggerExplainWithAi() {
  if (!floatingMenu.value.text) return
  selectedTerm.value = floatingMenu.value.text
  selectedContext.value = floatingMenu.value.context
  selectedCategory.value = props.topic.title
  isExplainerOpen.value = true
  floatingMenu.value.visible = false
}

function copySelectedText() {
  if (!floatingMenu.value.text) return
  navigator.clipboard.writeText(floatingMenu.value.text)
  copied.value = true
  setTimeout(() => {
    copied.value = false
    floatingMenu.value.visible = false
  }, 1200)
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick)
})

onUnmounted(() => {
  document.removeEventListener('click', handleDocumentClick)
})
</script>

<template>
  <div class="h-full bg-white/60 dark:bg-slate-950/40 overflow-y-auto p-4 sm:p-6 md:p-9 transition-colors duration-200" @mouseup="handleMouseUp">
    <!-- Header info -->
    <div class="mb-5 sm:mb-6">
      <div class="flex items-center gap-2 text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 uppercase tracking-wider mb-2">
        <BookOpen class="w-4 h-4 shrink-0" />
        <span>{{ $t('today.doc_reader') }}</span>
        <span class="text-slate-400 dark:text-slate-600">•</span>
        <span class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400">
          <Clock class="w-3.5 h-3.5" />
          {{ documentChunk?.estimatedReadMinutes || 3 }} {{ $t('today.estimated_read') }}
        </span>
      </div>

      <h1 class="text-xl sm:text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight leading-snug mb-3">
        {{ topic.title }}
      </h1>

      <p class="text-xs sm:text-sm md:text-base text-slate-700 dark:text-slate-300 leading-relaxed bg-slate-100/90 dark:bg-slate-900/60 p-3.5 sm:p-4 rounded-2xl border border-slate-200 dark:border-slate-800/80 font-normal">
        {{ topic.summary }}
      </p>

      <!-- Key Takeaways -->
      <div v-if="documentChunk?.keyTakeaways?.length" class="mt-3.5 sm:mt-4 flex flex-wrap gap-1.5 sm:gap-2">
        <span
          v-for="(takeaway, i) in documentChunk.keyTakeaways"
          :key="i"
          class="inline-flex items-center gap-1.5 px-3 py-1 sm:px-3.5 sm:py-1.5 rounded-xl bg-slate-100 dark:bg-slate-900 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <Tag class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400 shrink-0" />
          <span>{{ takeaway }}</span>
        </span>
      </div>
    </div>

    <div class="w-full h-px bg-slate-200 dark:bg-slate-800/80 mb-5 sm:mb-6"></div>

    <!-- Reading Content (Rendered Architectural Deep Dive) -->
    <div ref="readerContentRef" class="doc-reader-content markdown-body text-slate-800 dark:text-slate-200 max-w-full overflow-x-hidden break-words space-y-4" v-html="renderedDeepDiveHtml"></div>

    <!-- Authoritative Source Excerpt (if distinct) -->
    <div v-if="renderedChunkHtml" class="mt-6 p-4 sm:p-5 rounded-2xl bg-emerald-500/5 dark:bg-emerald-950/20 border border-emerald-500/20 space-y-2">
      <div class="flex items-center gap-2 text-xs font-bold tracking-wider text-emerald-700 dark:text-emerald-400">
        <BookOpen class="w-3.5 h-3.5" />
        <span>{{ t('today.source_context') || (locale === 'vi' ? 'Ngữ Cảnh Trích Xuất Gốc' : 'Authoritative Source Context') }}</span>
      </div>
      <div class="markdown-body text-xs sm:text-sm text-slate-700 dark:text-slate-300" v-html="renderedChunkHtml"></div>
    </div>

    <!-- Benchmark Snippet (if available) -->
    <div v-if="topic.benchmarkSnippet" class="mt-6 p-4 sm:p-5 rounded-2xl bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 font-mono text-xs sm:text-sm text-brand-700 dark:text-brand-300 shadow-sm">
      <div class="text-slate-700 dark:text-slate-400 font-bold mb-2 font-sans flex items-center gap-1.5">
        <Sparkles class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>Performance Benchmark Context:</span>
      </div>
      <pre class="overflow-x-auto p-0 m-0 bg-transparent border-0">{{ topic.benchmarkSnippet }}</pre>
    </div>

    <!-- Interactive Micro Quiz Check -->
    <div class="micro-quiz-container mt-8">
      <MicroQuizCard v-if="documentChunk?.microQuiz" :quiz="documentChunk.microQuiz" />
    </div>

    <!-- Discreet Floating Action Bar on Selection -->
    <Teleport to="body">
      <div
        v-if="floatingMenu.visible"
        :style="{ left: `${floatingMenu.x}px`, top: `${floatingMenu.y}px`, transform: 'translateX(-50%)' }"
        class="floating-selection-menu fixed z-50 flex items-center gap-1.5 p-1 rounded-2xl bg-slate-900/95 dark:bg-slate-900/95 text-white border border-slate-700 shadow-2xl backdrop-blur-md animate-in fade-in zoom-in-95 duration-150"
      >
        <button
          @click.stop="triggerExplainWithAi"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs shadow transition-all active:scale-95"
        >
          <Sparkles class="w-3.5 h-3.5" />
          <span>{{ $t('today.explain_term_tooltip') || 'Explain with Gemini' }}</span>
        </button>

        <button
          @click.stop="copySelectedText"
          class="flex items-center gap-1 px-2.5 py-1.5 rounded-xl hover:bg-slate-800 text-slate-300 hover:text-white text-xs font-semibold transition-colors"
          title="Copy to Clipboard"
        >
          <Check v-if="copied" class="w-3.5 h-3.5 text-emerald-400" />
          <Copy v-else class="w-3.5 h-3.5" />
          <span>{{ copied ? 'Copied' : 'Copy' }}</span>
        </button>
      </div>
    </Teleport>

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
