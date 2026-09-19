<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { BookOpen, Clock, Tag, Sparkles, Copy, Check, Highlighter } from 'lucide-vue-next'
import type { Topic, DocumentChunk } from '~/stores/useDailyFocusStore'
import { useNotesStore } from '~/stores/useNotesStore'
import { useToast } from '~/composables/useToast'
import TermExplainerModal from '~/components/today/TermExplainerModal.vue'
import { useMarkdownRenderer } from '~/composables/useMarkdownRenderer'
import { useApiError } from '~/composables/useApiError'
import { useReaderTypography } from '~/composables/useReaderTypography'
const { t, locale } = useI18n()
const { formatError } = useApiError()
const notesStore = useNotesStore()
const toast = useToast()
const { render: renderMarkdown, isHighlighterReady } = useMarkdownRenderer()

const props = defineProps<{
  topic: Topic
  documentChunk?: DocumentChunk
}>()
const {
  typography,
  fontSizes,
  fontScalePercentages,
  currentFontSizeIndex,
  canDecreaseFontSize,
  canIncreaseFontSize,
  decreaseFontSize,
  increaseFontSize,
  fontSizePx,
  lineHeightValue,
  fontFamilyClass
} = useReaderTypography()

const isTypographyOpen = ref(false)
const typographyDropdownRef = ref<HTMLElement | null>(null)

function handleKeyDown(e: KeyboardEvent) {
  if (e.key === 'Escape' && isTypographyOpen.value) {
    isTypographyOpen.value = false
  }
}

const cleanSummary = computed(() => {
  if (!props.topic.summary) return ''
  let s = props.topic.summary
  // Strip leading '#+ Heading' if present
  s = s.replace(/^\s*#{1,6}\s+[^\n\r]+(?:\r?\n)*/gm, '').trim()
  // Strip pre-release notices if present
  s = s.replace(/(?:Important\s+)?This information relates to a pre-release product[^\n.]*\.[^\n.]*\./gi, '').trim()
  return s || props.topic.title
})

function suppressDuplicateHeading(text: string, title?: string): string {
  if (!text || !title) return text
  const titleNorm = title.trim().toLowerCase()
  const match = text.match(/^\s*#{1,6}\s+([^\n\r]+)/)
  if (match) {
    const headingText = match[1].trim().toLowerCase()
    if (headingText === titleNorm || titleNorm.includes(headingText) || headingText.includes(titleNorm)) {
      return text.replace(/^\s*#{1,6}\s+[^\n\r]+(\r?\n)+/, '')
    }
  }
  return text
}

const renderedDeepDiveHtml = computed(() => {
  const rawContent = props.topic.deepDiveMarkdown || props.documentChunk?.originalTextMarkdown || props.topic.summary || ''
  const title = props.topic.title || props.documentChunk?.chapterTitle
  const content = suppressDuplicateHeading(rawContent, title)
  const _ = isHighlighterReady.value
  return renderMarkdown(content)
})

const renderedChunkHtml = computed(() => {
  if (
    props.documentChunk?.originalTextMarkdown &&
    props.topic.deepDiveMarkdown &&
    props.documentChunk.originalTextMarkdown !== props.topic.deepDiveMarkdown
  ) {
    const _ = isHighlighterReady.value
    const title = props.documentChunk.chapterTitle || props.topic.title
    const content = suppressDuplicateHeading(props.documentChunk.originalTextMarkdown, title)
    return renderMarkdown(content)
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

let selectionDebounceTimer: ReturnType<typeof setTimeout> | null = null

function handleMouseUp(e: MouseEvent) {
  if (selectionDebounceTimer) {
    clearTimeout(selectionDebounceTimer)
    selectionDebounceTimer = null
  }

  selectionDebounceTimer = setTimeout(() => {
    // Ignore clicks inside interactive elements
    const target = e.target as HTMLElement
    if (target.closest('button') || target.closest('input')) {
      floatingMenu.value.visible = false
      return
    }

    const selection = window.getSelection()
    if (!selection || selection.isCollapsed) {
      floatingMenu.value.visible = false
      return
    }

    const text = selection.toString().trim()
    if (text.length >= 2 && text.length <= 500) {
      const range = selection.getRangeAt(0)
      const rect = range.getBoundingClientRect()

      // Ensure selection is inside reader container
      if (readerContentRef.value && readerContentRef.value.contains(range.commonAncestorContainer)) {
        floatingMenu.value = {
          visible: true,
          x: Math.max(10, rect.left + rect.width / 2),
          y: Math.max(10, rect.top - 46),
          text,
          context: selection.anchorNode?.textContent?.slice(0, 300) || text
        }
        return
      }
    }

    floatingMenu.value.visible = false
  }, 400)
}

function handleDocumentClick(e: MouseEvent) {
  const target = e.target as HTMLElement
  if (!target.closest('.floating-selection-menu') && !target.closest('.doc-reader-content')) {
    floatingMenu.value.visible = false
  }
  if (
    isTypographyOpen.value &&
    typographyDropdownRef.value &&
    !typographyDropdownRef.value.contains(target)
  ) {
    isTypographyOpen.value = false
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
  toast.info(t('today.toast_copied_text'))
  setTimeout(() => {
    copied.value = false
    floatingMenu.value.visible = false
  }, 1200)
}

async function handleHighlightSelection() {
  const chunkId = props.documentChunk?.id
  if (!floatingMenu.value.text || !chunkId) {
    toast.error(t('today.toast_no_document_chunk'))
    return
  }
  try {
    await notesStore.createHighlight({
      documentChunkId: chunkId,
      selectedText: floatingMenu.value.text.trim()
    })
    toast.success(t('today.toast_highlight_saved'))
  } catch (err: any) {
    toast.error(formatError(err, 'today.toast_highlight_failed'))
  } finally {
    floatingMenu.value.visible = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick)
  document.addEventListener('keydown', handleKeyDown)
})

onUnmounted(() => {
  if (selectionDebounceTimer) {
    clearTimeout(selectionDebounceTimer)
    selectionDebounceTimer = null
  }
  document.removeEventListener('click', handleDocumentClick)
  document.removeEventListener('keydown', handleKeyDown)
})
</script>

<template>
  <div class="h-full bg-white dark:bg-canvas overflow-y-auto p-4 sm:p-6 md:p-8 transition-colors duration-200 min-w-0 max-w-full" @mouseup="handleMouseUp">
    <!-- Header info -->
    <div class="mb-5 sm:mb-6">
      <div class="flex items-center justify-between gap-2 mb-2">
        <div class="flex items-center gap-2 text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 uppercase tracking-wider">
          <BookOpen class="w-4 h-4 shrink-0" />
          <span>{{ $t('today.doc_reader') }}</span>
          <span class="text-slate-400 dark:text-slate-600">•</span>
          <span class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400">
            <Clock class="w-3.5 h-3.5" />
            {{ documentChunk?.estimatedReadMinutes || 3 }} {{ $t('today.estimated_read') }}
          </span>
        </div>

        <!-- Typography Settings Popover -->
        <div ref="typographyDropdownRef" class="relative shrink-0">
          <button
            @click.stop="isTypographyOpen = !isTypographyOpen"
            :class="[
              'px-2.5 sm:px-3 py-1.5 rounded-xl border text-xs font-bold transition-all flex items-center gap-1.5 shrink-0 whitespace-nowrap',
              isTypographyOpen
                ? 'bg-brand-600 text-white border-transparent shadow-sm'
                : 'bg-slate-100 dark:bg-canvas-elevated text-slate-700 dark:text-slate-300 border-slate-200 dark:border-white/[0.08] hover:bg-slate-200 dark:hover:bg-white/[0.06]'
            ]"
            :title="$t('reader.typography_settings')"
          >
            <span class="font-serif text-sm font-black">Aa</span>
          </button>

          <!-- Typography Popover Dropdown (click-outside dismissed) -->
          <div
            v-if="isTypographyOpen"
            class="absolute right-0 mt-2 w-80 sm:w-84 p-4 bg-white dark:bg-canvas-elevated rounded-2xl border border-slate-200 dark:border-white/[0.08] shadow-2xl z-50 space-y-4 text-xs select-none"
          >
            <!-- Section 1: Font Size -->
            <div class="space-y-2">
              <div class="flex items-center justify-between text-slate-500 dark:text-slate-400 font-semibold">
                <span>{{ $t('reader.font_size') }}</span>
                <span class="font-mono text-xs font-bold text-slate-700 dark:text-slate-200">
                  {{ fontScalePercentages[typography.fontSize] }}
                </span>
              </div>
            <div class="flex items-center justify-between gap-2 p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/60 dark:border-white/[0.04]">
                <button
                  type="button"
                  @click="decreaseFontSize"
                  :disabled="!canDecreaseFontSize"
                  class="flex-1 py-1.5 px-3 rounded-lg font-serif font-bold text-xs flex items-center justify-center gap-1 transition-all disabled:opacity-30 disabled:cursor-not-allowed hover:bg-white dark:hover:bg-canvas-elevated text-slate-700 dark:text-slate-300 shadow-none hover:shadow-sm whitespace-nowrap shrink-0"
                  title="Smaller Font"
                >
                  <span class="text-xs font-bold">A</span>
                  <span class="text-[10px] font-mono">−</span>
                </button>
                <div class="flex items-center gap-1.5 px-2">
                  <span
                    v-for="(size, idx) in fontSizes"
                    :key="size"
                    class="w-1.5 h-1.5 rounded-full transition-all"
                    :class="[
                      typography.fontSize === size
                        ? 'w-2 h-2 bg-brand-500 scale-110'
                        : (idx < currentFontSizeIndex ? 'bg-slate-400 dark:bg-slate-500' : 'bg-slate-300 dark:bg-slate-700')
                    ]"
                  />
                </div>
                <button
                  type="button"
                  @click="increaseFontSize"
                  :disabled="!canIncreaseFontSize"
                  class="flex-1 py-1.5 px-3 rounded-lg font-serif font-bold text-sm flex items-center justify-center gap-1 transition-all disabled:opacity-30 disabled:cursor-not-allowed hover:bg-white dark:hover:bg-canvas-elevated text-slate-700 dark:text-slate-300 shadow-none hover:shadow-sm whitespace-nowrap shrink-0"
                  title="Larger Font"
                >
                  <span class="text-sm font-black">A</span>
                  <span class="text-[10px] font-mono">+</span>
                </button>
              </div>
            </div>

            <!-- Section 2: Font Family -->
            <div class="space-y-2">
              <span class="text-slate-500 dark:text-slate-400 font-semibold block">{{ $t('reader.font_family') }}</span>
            <div class="grid grid-cols-3 gap-1.5 p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/60 dark:border-white/[0.04]">
                <button
                  type="button"
                  @click="typography.fontFamily = 'sans'"
                  class="py-2 px-2 rounded-lg font-sans font-medium text-xs transition-all text-center truncate whitespace-nowrap shrink-0"
                  :class="[
                    typography.fontFamily === 'sans'
                      ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                  ]"
                >
                  Sans
                </button>
                <button
                  type="button"
                  @click="typography.fontFamily = 'serif'"
                  class="py-2 px-2 rounded-lg font-serif font-medium text-xs transition-all text-center truncate whitespace-nowrap shrink-0"
                  :class="[
                    typography.fontFamily === 'serif'
                      ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                  ]"
                >
                  Serif
                </button>
                <button
                  type="button"
                  @click="typography.fontFamily = 'mono'"
                  class="py-2 px-2 rounded-lg font-mono font-medium text-xs transition-all text-center truncate whitespace-nowrap shrink-0"
                  :class="[
                    typography.fontFamily === 'mono'
                      ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                  ]"
                >
                  Mono
                </button>
              </div>
            </div>

            <!-- Section 3: Line Spacing -->
            <div class="space-y-2">
              <span class="text-slate-500 dark:text-slate-400 font-semibold block">{{ $t('reader.line_spacing') }}</span>
            <div class="grid grid-cols-3 gap-1.5 p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/60 dark:border-white/[0.04]">
                <button
                  type="button"
                  @click="typography.lineSpacing = 'normal'"
                  class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate whitespace-nowrap shrink-0"
                  :class="[
                    typography.lineSpacing === 'normal'
                      ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                  ]"
                >
                  {{ $t('reader.spacing_normal') }}
                </button>
                <button
                  type="button"
                  @click="typography.lineSpacing = 'relaxed'"
                  class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate whitespace-nowrap shrink-0"
                  :class="[
                    typography.lineSpacing === 'relaxed'
                      ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                  ]"
                >
                  {{ $t('reader.spacing_relaxed') }}
                </button>
                <button
                  type="button"
                  @click="typography.lineSpacing = 'loose'"
                  class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate whitespace-nowrap shrink-0"
                  :class="[
                    typography.lineSpacing === 'loose'
                      ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                  ]"
                >
                  {{ $t('reader.spacing_loose') }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <h1 class="text-xl sm:text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight leading-snug mb-3">
        {{ topic.title }}
      </h1>

      <p class="text-sm md:text-lg text-slate-700 dark:text-slate-300 leading-relaxed bg-slate-100/90 dark:bg-canvas-subtle/80 p-4 sm:p-5 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] font-normal">
        {{ cleanSummary }}
      </p>

      <!-- Key Takeaways -->
      <div v-if="documentChunk?.keyTakeaways?.length" class="mt-3.5 sm:mt-4 flex flex-wrap gap-1.5 sm:gap-2">
        <span
          v-for="(takeaway, i) in documentChunk.keyTakeaways"
          :key="i"
          class="inline-flex items-center gap-1.5 px-3 py-1 sm:px-3.5 sm:py-1.5 rounded-xl bg-slate-100 dark:bg-canvas-elevated text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 border border-slate-200/80 dark:border-white/[0.08] shadow-sm"
        >
          <Tag class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400 shrink-0" />
          <span>{{ takeaway }}</span>
        </span>
      </div>
    </div>

    <div class="w-full h-px bg-slate-200/80 dark:bg-white/[0.08] mb-5 sm:mb-6"></div>

    <!-- Reading Content (Rendered Architectural Deep Dive) -->
    <div
      ref="readerContentRef"
      class="doc-reader-content markdown-body text-slate-800 dark:text-slate-200 min-w-0 max-w-full break-words space-y-4"
      :class="[fontFamilyClass]"
      :style="{ fontSize: fontSizePx, lineHeight: lineHeightValue }"
      v-html="renderedDeepDiveHtml"
    ></div>

    <!-- Authoritative Source Excerpt (if distinct) -->
    <div v-if="renderedChunkHtml" class="mt-6 p-4 sm:p-5 rounded-2xl glass-panel dark:bg-canvas-subtle/80 border border-slate-200/80 dark:border-white/[0.08] space-y-2">
      <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400">
        <BookOpen class="w-3.5 h-3.5" />
        <span>{{ $t('today.source_context') }}</span>
      </div>
      <div class="markdown-body text-sm md:text-lg text-slate-700 dark:text-slate-300 leading-relaxed min-w-0 max-w-full" v-html="renderedChunkHtml"></div>
    </div>

    <!-- Benchmark Snippet (if available) -->
    <div v-if="topic.benchmarkSnippet" class="mt-6 p-4 sm:p-5 rounded-2xl bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 font-mono text-xs sm:text-sm text-brand-700 dark:text-brand-300 shadow-sm">
      <div class="text-slate-700 dark:text-slate-400 font-bold mb-2 font-sans flex items-center gap-1.5">
        <Sparkles class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>Performance Benchmark Context:</span>
      </div>
      <pre class="overflow-x-auto p-0 m-0 bg-transparent border-0">{{ topic.benchmarkSnippet }}</pre>
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
          @click.stop="handleHighlightSelection"
          class="flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold bg-amber-500/20 text-amber-300 hover:bg-amber-500 hover:text-slate-950 transition-colors"
          :title="$t('today.highlight_save_tooltip')"
        >
          <Highlighter class="w-3.5 h-3.5" />
          <span>Highlight</span>
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

<style scoped>
:deep(.markdown-body p),
:deep(.markdown-body li),
:deep(.markdown-body blockquote),
:deep(.prose p),
:deep(.prose li) {
  font-size: inherit !important;
  line-height: inherit !important;
}
</style>
