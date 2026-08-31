<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import {
  BookOpen,
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  List,
  CheckCircle2,
  Clock,
  Sparkles,
  Copy,
  Bookmark,
  Share2,
  HelpCircle,
  X
} from 'lucide-vue-next'
import MarkdownIt from 'markdown-it'
import type { BookDetail, ChunkSummary } from '~/stores/useLibraryStore'
import TermExplainerModal from '~/components/today/TermExplainerModal.vue'

const route = useRoute()
const router = useRouter()
const libraryStore = useLibraryStore()
const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

const bookId = computed(() => route.params.bookId as string)
const book = ref<BookDetail | null>(null)
const activeChunkIndex = ref(0)
const isTocOpen = ref(true)
const isMobileTocOpen = ref(false)
const completedSlices = ref<Set<number>>(new Set())

// Floating Selection Toolbar State
const floatingToolbar = ref({
  visible: false,
  x: 0,
  y: 0,
  selectedText: ''
})

// Term Explainer Modal State
const isExplainerOpen = ref(false)
const currentTerm = ref('')
const currentContext = ref('')

const articleScrollContainer = ref<HTMLElement | null>(null)

const currentChunk = computed<ChunkSummary | null>(() => {
  if (!book.value?.chunks?.length) return null
  return book.value.chunks[activeChunkIndex.value] || null
})

const totalChunks = computed(() => book.value?.chunks?.length || 0)
const progressPercentage = computed(() => {
  if (!totalChunks.value) return 0
  return Math.round(((activeChunkIndex.value + 1) / totalChunks.value) * 100)
})

const renderedMarkdown = computed(() => {
  if (!currentChunk.value?.originalTextMarkdown) return ''
  return md.render(currentChunk.value.originalTextMarkdown)
})

onMounted(async () => {
  // Load saved completed slices from localStorage
  try {
    const savedCompleted = localStorage.getItem(`techdaily_completed_${bookId.value}`)
    if (savedCompleted) {
      completedSlices.value = new Set(JSON.parse(savedCompleted))
    }
  } catch (e) {
    // ignore
  }

  // Fetch book details
  try {
    const res = await libraryStore.fetchBookById(bookId.value)
    book.value = res

    // Check URL query param first, then localStorage bookmark
    const querySlice = route.query.slice ? parseInt(route.query.slice as string, 10) : undefined
    if (querySlice && querySlice >= 1 && querySlice <= res.chunks.length) {
      activeChunkIndex.value = querySlice - 1
    } else {
      const savedBookmark = localStorage.getItem(`techdaily_bookmark_${bookId.value}`)
      if (savedBookmark) {
        const bookmarkSlice = parseInt(savedBookmark, 10)
        if (bookmarkSlice >= 1 && bookmarkSlice <= res.chunks.length) {
          activeChunkIndex.value = bookmarkSlice - 1
        }
      }
    }

    markCurrentSliceCompleted()
  } catch (err) {
    // handled by store
  }

  // Attach global keyboard listener for Shift + Left/Right and Escape
  window.addEventListener('keydown', handleKeyDown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyDown)
})

function handleKeyDown(e: KeyboardEvent) {
  if (e.shiftKey && e.key === 'ArrowRight') {
    e.preventDefault()
    goToNextSlice()
  } else if (e.shiftKey && e.key === 'ArrowLeft') {
    e.preventDefault()
    goToPrevSlice()
  } else if (e.key === 'Escape' && !isExplainerOpen.value && !isMobileTocOpen.value) {
    router.push('/library')
  }
}

function markCurrentSliceCompleted() {
  if (!currentChunk.value) return
  completedSlices.value.add(currentChunk.value.chunkOrder)
  try {
    localStorage.setItem(
      `techdaily_completed_${bookId.value}`,
      JSON.stringify(Array.from(completedSlices.value))
    )
  } catch (e) {
    // ignore
  }
}

function selectChunk(index: number) {
  if (index < 0 || !book.value?.chunks?.length || index >= book.value.chunks.length) return
  activeChunkIndex.value = index
  isMobileTocOpen.value = false

  const chunkOrder = book.value.chunks[index].chunkOrder
  // Save bookmark
  try {
    localStorage.setItem(`techdaily_bookmark_${bookId.value}`, chunkOrder.toString())
  } catch (e) {
    // ignore
  }

  router.replace({ query: { slice: chunkOrder } })
  markCurrentSliceCompleted()

  // Scroll to top
  nextTick(() => {
    if (articleScrollContainer.value) {
      articleScrollContainer.value.scrollTo({ top: 0, behavior: 'smooth' })
    }
  })
}

function goToNextSlice() {
  if (activeChunkIndex.value < totalChunks.value - 1) {
    selectChunk(activeChunkIndex.value + 1)
  }
}

function goToPrevSlice() {
  if (activeChunkIndex.value > 0) {
    selectChunk(activeChunkIndex.value - 1)
  }
}

// Scoped Text Selection Listener
function handleTextSelection(event: MouseEvent) {
  const selection = window.getSelection()
  if (!selection || selection.isCollapsed) {
    floatingToolbar.value.visible = false
    return
  }

  const selectedStr = selection.toString().trim()
  if (selectedStr.length < 2 || selectedStr.length > 200) {
    floatingToolbar.value.visible = false
    return
  }

  const range = selection.getRangeAt(0)
  const rect = range.getBoundingClientRect()

  floatingToolbar.value = {
    visible: true,
    x: Math.max(16, rect.left + rect.width / 2),
    y: Math.max(70, rect.top - 46),
    selectedText: selectedStr
  }
}

function handleCopySelection() {
  if (!floatingToolbar.value.selectedText) return
  navigator.clipboard.writeText(floatingToolbar.value.selectedText)
  floatingToolbar.value.visible = false
}

function handleExplainSelection() {
  currentTerm.value = floatingToolbar.value.selectedText
  currentContext.value = currentChunk.value?.chapterTitle || ''
  floatingToolbar.value.visible = false
  isExplainerOpen.value = true
}
</script>

<template>
  <div class="h-[calc(100vh-3.5rem)] flex flex-col overflow-hidden bg-white dark:bg-slate-950 transition-colors duration-200">
    <!-- Top Sticky Reader Navigation Bar -->
    <header class="h-14 px-4 md:px-7 border-b border-slate-200 dark:border-slate-800 bg-white/95 dark:bg-slate-900/90 backdrop-blur flex items-center justify-between shrink-0 gap-4 z-20">
      <!-- Left: Back to Library & TOC Toggle -->
      <div class="flex items-center gap-3">
        <NuxtLink
          to="/library"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
        >
          <ArrowLeft class="w-4 h-4" />
          <span class="hidden sm:inline">Library</span>
        </NuxtLink>

        <!-- Desktop TOC Toggle -->
        <button
          @click="isTocOpen = !isTocOpen"
          :class="[
            'hidden md:flex items-center gap-1.5 px-3 py-1.5 rounded-xl border text-xs sm:text-sm font-semibold transition-colors',
            isTocOpen
              ? 'border-brand-300 dark:border-brand-800 bg-brand-50 dark:bg-brand-950/40 text-brand-700 dark:text-brand-400'
              : 'border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
          ]"
        >
          <List class="w-4 h-4" />
          <span>Contents</span>
        </button>

        <!-- Mobile TOC Drawer Button -->
        <button
          @click="isMobileTocOpen = true"
          class="md:hidden flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs font-semibold text-slate-700 dark:text-slate-300"
        >
          <List class="w-4 h-4" />
          <span>Chapters</span>
        </button>
      </div>

      <!-- Center: Book Title & Active Chapter Indicator -->
      <div class="flex-1 max-w-xl text-center truncate">
        <h1 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white truncate">
          {{ book?.title || 'Technical Document' }}
        </h1>
        <p v-if="currentChunk" class="text-[11px] text-slate-500 dark:text-slate-400 truncate">
          Slice {{ currentChunk.chunkOrder }} of {{ totalChunks }}: {{ currentChunk.chapterTitle }}
        </p>
      </div>

      <!-- Right: Reading Progress Bar & Next/Prev Quick Buttons -->
      <div class="flex items-center gap-3 shrink-0">
        <!-- Progress Bar (Desktop) -->
        <div class="hidden lg:flex items-center gap-2.5">
          <div class="w-28 h-2 rounded-full bg-slate-200 dark:bg-slate-800 overflow-hidden">
            <div
              class="h-full bg-brand-500 rounded-full transition-all duration-300"
              :style="{ width: `${progressPercentage}%` }"
            ></div>
          </div>
          <span class="text-xs font-bold text-brand-700 dark:text-brand-400 tabular-nums">
            {{ progressPercentage }}%
          </span>
        </div>

        <!-- Quick Slice Prev/Next -->
        <div class="flex items-center gap-1">
          <button
            @click="goToPrevSlice"
            :disabled="activeChunkIndex <= 0"
            class="p-1.5 rounded-lg border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
            title="Previous Slice (Shift + ←)"
          >
            <ChevronLeft class="w-4 h-4" />
          </button>
          <button
            @click="goToNextSlice"
            :disabled="activeChunkIndex >= totalChunks - 1"
            class="p-1.5 rounded-lg border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
            title="Next Slice (Shift + →)"
          >
            <ChevronRight class="w-4 h-4" />
          </button>
        </div>
      </div>
    </header>

    <!-- Main Body: Responsive TOC Sidebar + Reading Article Pane -->
    <div class="flex-1 flex overflow-hidden relative">
      <!-- Desktop Table of Contents Sidebar (Collapsible) -->
      <aside
        v-if="isTocOpen"
        class="hidden md:flex w-72 lg:w-80 border-r border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-900/40 flex-col shrink-0 overflow-y-auto"
      >
        <div class="p-4 border-b border-slate-200 dark:border-slate-800/80 flex items-center justify-between">
          <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            <BookOpen class="w-3.5 h-3.5" />
            <span>Table of Contents</span>
          </div>
          <span class="text-xs font-semibold text-slate-400">
            {{ completedSlices.size }}/{{ totalChunks }} done
          </span>
        </div>

        <div class="p-2 space-y-1">
          <button
            v-for="(chunk, idx) in book?.chunks"
            :key="chunk.id"
            @click="selectChunk(idx)"
            :class="[
              'w-full text-left p-3 rounded-xl text-xs sm:text-sm font-semibold transition-all flex items-start gap-2.5',
              activeChunkIndex === idx
                ? 'bg-brand-500/10 dark:bg-brand-500/20 text-brand-900 dark:text-brand-300 font-bold border-l-4 border-brand-500 shadow-sm'
                : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 border-l-4 border-transparent'
            ]"
          >
            <CheckCircle2
              v-if="completedSlices.has(chunk.chunkOrder)"
              class="w-4 h-4 text-emerald-500 shrink-0 mt-0.5"
            />
            <span
              v-else
              class="w-4 h-4 rounded-full border border-slate-300 dark:border-slate-700 flex items-center justify-center text-[10px] text-slate-500 shrink-0 mt-0.5"
            >
              {{ chunk.chunkOrder }}
            </span>

            <div class="flex-1 min-w-0">
              <div class="truncate">{{ chunk.chapterTitle }}</div>
              <div class="text-[11px] text-slate-400 mt-0.5 flex items-center gap-1 font-normal">
                <Clock class="w-3 h-3" />
                <span>{{ chunk.estimatedReadMinutes || 3 }}m read</span>
              </div>
            </div>
          </button>
        </div>
      </aside>

      <!-- Mobile Table of Contents Modal Drawer -->
      <div
        v-if="isMobileTocOpen"
        class="md:hidden fixed inset-0 z-50 bg-slate-950/70 backdrop-blur-sm flex justify-start animate-in fade-in"
      >
        <div class="w-4/5 max-w-xs bg-white dark:bg-slate-900 h-full flex flex-col shadow-2xl">
          <div class="p-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
            <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-slate-900 dark:text-white">
              <BookOpen class="w-4 h-4 text-brand-500" />
              <span>Table of Contents</span>
            </div>
            <button
              @click="isMobileTocOpen = false"
              class="p-1 rounded-lg text-slate-400 hover:text-slate-900 dark:hover:text-white"
            >
              <X class="w-5 h-5" />
            </button>
          </div>

          <div class="flex-1 overflow-y-auto p-2 space-y-1">
            <button
              v-for="(chunk, idx) in book?.chunks"
              :key="chunk.id"
              @click="selectChunk(idx)"
              :class="[
                'w-full text-left p-3 rounded-xl text-xs font-semibold transition-all flex items-start gap-2.5',
                activeChunkIndex === idx
                  ? 'bg-brand-500/10 dark:bg-brand-500/20 text-brand-900 dark:text-brand-300 font-bold'
                  : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <CheckCircle2
                v-if="completedSlices.has(chunk.chunkOrder)"
                class="w-4 h-4 text-emerald-500 shrink-0 mt-0.5"
              />
              <span v-else class="text-xs text-slate-400 shrink-0 mt-0.5">
                #{{ chunk.chunkOrder }}
              </span>
              <div class="flex-1 truncate">{{ chunk.chapterTitle }}</div>
            </button>
          </div>
        </div>
      </div>

      <!-- Main Reading Article Pane -->
      <main
        ref="articleScrollContainer"
        class="flex-1 overflow-y-auto p-6 md:p-12 lg:p-16 flex justify-center selection:bg-brand-500/30"
        @mouseup="handleTextSelection"
      >
        <!-- Loading State -->
        <div v-if="libraryStore.isLoading" class="flex flex-col items-center justify-center gap-3 py-20 text-slate-400">
          <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin"></div>
          <span class="text-sm">Loading document chapter...</span>
        </div>

        <!-- Article Content Card -->
        <div v-else-if="currentChunk" class="w-full max-w-3xl space-y-10">
          <!-- Chapter Meta Header -->
          <div class="space-y-4 pb-6 border-b border-slate-200 dark:border-slate-800/80">
            <div class="flex items-center gap-3 text-xs font-bold text-brand-700 dark:text-brand-400 uppercase tracking-wider">
              <span class="px-2.5 py-1 rounded-lg bg-brand-100 dark:bg-brand-950/70 border border-brand-200 dark:border-brand-800">
                Slice {{ currentChunk.chunkOrder }} of {{ totalChunks }}
              </span>
              <span class="flex items-center gap-1 text-slate-500 dark:text-slate-400 font-normal">
                <Clock class="w-3.5 h-3.5" />
                {{ currentChunk.estimatedReadMinutes || 3 }} min read
              </span>
            </div>

            <h1 class="text-2xl sm:text-3xl lg:text-4xl font-extrabold text-slate-900 dark:text-white tracking-tight leading-tight">
              {{ currentChunk.chapterTitle }}
            </h1>
          </div>

          <!-- Markdown Body -->
          <article
            class="prose prose-slate dark:prose-invert prose-base sm:prose-lg max-w-none prose-headings:font-bold prose-headings:tracking-tight prose-a:text-brand-600 dark:prose-a:text-brand-400 prose-pre:bg-slate-950 prose-pre:border prose-pre:border-slate-800 prose-code:text-brand-700 dark:prose-code:text-brand-300 leading-relaxed"
            v-html="renderedMarkdown"
          ></article>

          <!-- Key Takeaways Callout -->
          <div
            v-if="currentChunk.keyTakeaways?.length"
            class="p-6 rounded-3xl bg-amber-50/80 dark:bg-amber-950/30 border border-amber-200/80 dark:border-amber-900/50 space-y-3"
          >
            <div class="flex items-center gap-2 text-xs sm:text-sm font-bold text-amber-900 dark:text-amber-300 uppercase tracking-wider">
              <Sparkles class="w-4 h-4 text-amber-600 dark:text-amber-400" />
              <span>Key Takeaways</span>
            </div>
            <ul class="space-y-2">
              <li
                v-for="(takeaway, idx) in currentChunk.keyTakeaways"
                :key="idx"
                class="text-xs sm:text-sm text-slate-700 dark:text-slate-300 flex items-start gap-2.5 leading-relaxed"
              >
                <span class="w-1.5 h-1.5 rounded-full bg-amber-500 mt-2 shrink-0"></span>
                <span>{{ takeaway }}</span>
              </li>
            </ul>
          </div>

          <!-- Bottom Footer Navigation: Large Next / Previous Slice Cards -->
          <div class="pt-8 border-t border-slate-200 dark:border-slate-800/80 grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- Prev Button -->
            <button
              v-if="activeChunkIndex > 0"
              @click="goToPrevSlice"
              class="p-5 rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:border-brand-500/50 hover:bg-slate-50 dark:hover:bg-slate-850 text-left transition-all group shadow-sm flex flex-col justify-between"
            >
              <span class="text-xs font-bold text-slate-400 flex items-center gap-1 group-hover:text-brand-600 dark:group-hover:text-brand-400">
                <ChevronLeft class="w-4 h-4 transition-transform group-hover:-translate-x-1" />
                Previous Slice
              </span>
              <span class="text-sm font-bold text-slate-900 dark:text-white mt-2 line-clamp-1">
                #{{ book?.chunks[activeChunkIndex - 1]?.chunkOrder }} {{ book?.chunks[activeChunkIndex - 1]?.chapterTitle }}
              </span>
            </button>
            <div v-else class="hidden sm:block"></div>

            <!-- Next Button -->
            <button
              v-if="activeChunkIndex < totalChunks - 1"
              @click="goToNextSlice"
              class="p-5 rounded-2xl bg-brand-500 hover:bg-brand-400 text-slate-950 text-right transition-all group shadow-lg shadow-brand-500/20 active:scale-[0.99] flex flex-col justify-between"
            >
              <span class="text-xs font-bold text-slate-950/70 flex items-center justify-end gap-1">
                Next Slice
                <ChevronRight class="w-4 h-4 transition-transform group-hover:translate-x-1" />
              </span>
              <span class="text-sm font-black text-slate-950 mt-2 line-clamp-1">
                #{{ book?.chunks[activeChunkIndex + 1]?.chunkOrder }} {{ book?.chunks[activeChunkIndex + 1]?.chapterTitle }}
              </span>
            </button>

            <!-- Finish Book / Return to Library on Last Slice -->
            <NuxtLink
              v-else
              to="/library"
              class="p-5 rounded-2xl bg-emerald-600 hover:bg-emerald-500 text-white text-center transition-all shadow-lg shadow-emerald-600/20 flex flex-col items-center justify-center gap-1"
            >
              <span class="text-xs font-bold text-emerald-200">🎉 Completed Entire Document</span>
              <span class="text-sm font-black">Return to Library</span>
            </NuxtLink>
          </div>
        </div>
      </main>
    </div>

    <!-- Scoped Floating Selection Action Toolbar -->
    <div
      v-if="floatingToolbar.visible"
      class="fixed z-50 -translate-x-1/2 flex items-center gap-1 p-1 rounded-2xl bg-slate-900 dark:bg-slate-800 text-white shadow-2xl border border-slate-700 animate-in fade-in zoom-in-95 duration-150"
      :style="{ left: `${floatingToolbar.x}px`, top: `${floatingToolbar.y}px` }"
    >
      <button
        @click="handleExplainSelection"
        class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-brand-500 hover:bg-brand-400 text-slate-950 font-bold text-xs shadow transition-colors"
      >
        <Sparkles class="w-3.5 h-3.5" />
        <span>Explain with Gemini</span>
      </button>

      <button
        @click="handleCopySelection"
        class="flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold text-slate-300 hover:text-white hover:bg-slate-700/60 transition-colors"
      >
        <Copy class="w-3.5 h-3.5" />
        <span>Copy</span>
      </button>
    </div>

    <!-- Term Explainer Tooltip Modal -->
    <TermExplainerModal
      v-if="isExplainerOpen"
      :term="currentTerm"
      :context="currentContext"
      @close="isExplainerOpen = false"
    />
  </div>
</template>
