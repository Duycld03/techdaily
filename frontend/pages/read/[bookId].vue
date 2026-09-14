<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from "vue";
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
  Highlighter,
  X,
  AlertTriangle,
  AlertCircle,
  RefreshCw,
} from "lucide-vue-next";
import type { BookDetail, ChunkSummary } from "~/stores/useLibraryStore";
import TermExplainerModal from "~/components/today/TermExplainerModal.vue";
import ThemeToggle from "~/components/common/ThemeToggle.vue";

const { t } = useI18n();
const toast = useToast();
const route = useRoute();
const router = useRouter();
const libraryStore = useLibraryStore();
const notesStore = useNotesStore();
const {
  render: renderMarkdown,
  initHighlighter,
  isHighlighterReady,
} = useMarkdownRenderer();

const bookId = computed(() => route.params.bookId as string);
const book = ref<BookDetail | null>(null);
const activeChunkIndex = ref(0);
const isTocOpen = ref(true);
const isMobileTocOpen = ref(false);
const completedSlices = ref<Set<number>>(new Set());

// Floating Selection Toolbar State
const floatingToolbar = ref({
  visible: false,
  x: 0,
  y: 0,
  selectedText: "",
});

// Term Explainer Modal State
const isExplainerOpen = ref(false);
const currentTerm = ref("");
const currentContext = ref("");

const articleScrollContainer = ref<HTMLElement | null>(null);

const currentChunk = computed<ChunkSummary | null>(() => {
  if (!book.value?.chunks?.length) return null;
  return book.value.chunks[activeChunkIndex.value] || null;
});

const nextChunk = computed<ChunkSummary | null>(() => {
  if (!book.value?.chunks?.length) return null;
  if (activeChunkIndex.value < book.value.chunks.length - 1) {
    return book.value.chunks[activeChunkIndex.value + 1];
  }
  return null;
});

const prevChunk = computed<ChunkSummary | null>(() => {
  if (!book.value?.chunks?.length) return null;
  if (activeChunkIndex.value > 0) {
    return book.value.chunks[activeChunkIndex.value - 1];
  }
  return null;
});

const hasValidTakeaways = computed(() => {
  if (!currentChunk.value?.keyTakeaways?.length) return false;
  const isDefaultPlaceholder =
    currentChunk.value.keyTakeaways.length === 2 &&
    currentChunk.value.keyTakeaways[0] === "Core Architecture Principle" &&
    currentChunk.value.keyTakeaways[1] === "System Invariant";
  return !isDefaultPlaceholder;
});

const totalChunks = computed(() => book.value?.chunks?.length || 0);
const progressPercentage = computed(() => {
  if (!totalChunks.value) return 0;
  return Math.round(((activeChunkIndex.value + 1) / totalChunks.value) * 100);
});

const renderedMarkdown = computed(() => {
  if (!currentChunk.value?.originalTextMarkdown) return "";
  const _ = isHighlighterReady.value;
  let text = currentChunk.value.originalTextMarkdown;

  // Heading deduplication: suppress redundant initial heading if it matches the current chapterTitle
  if (currentChunk.value.chapterTitle) {
    const titleNorm = currentChunk.value.chapterTitle.trim().toLowerCase();
    const match = text.match(/^\s*#{1,6}\s+([^\n\r]+)/);
    if (match) {
      const headingText = match[1].trim().toLowerCase();
      if (
        headingText === titleNorm ||
        titleNorm.includes(headingText) ||
        headingText.includes(titleNorm)
      ) {
        text = text.replace(/^\s*#{1,6}\s+[^\n\r]+(\r?\n)+/, "");
      }
    }
  }

  return renderMarkdown(text, book.value?.authorOrSourceUrl);
});

onMounted(async () => {
  // Load saved completed slices from localStorage
  try {
    const savedCompleted = localStorage.getItem(
      `techdaily_completed_${bookId.value}`,
    );
    if (savedCompleted) {
      completedSlices.value = new Set(JSON.parse(savedCompleted));
    }
  } catch (e) {
    // ignore
  }

  // Fetch book details
  try {
    const res = await libraryStore.fetchBookById(bookId.value);
    book.value = res;

    // Check URL query param first, then localStorage bookmark
    const querySlice = route.query.slice
      ? parseInt(route.query.slice as string, 10)
      : undefined;
    if (querySlice && querySlice >= 1 && querySlice <= res.chunks.length) {
      activeChunkIndex.value = querySlice - 1;
    } else {
      const savedBookmark = localStorage.getItem(
        `techdaily_bookmark_${bookId.value}`,
      );
      if (savedBookmark) {
        const bookmarkSlice = parseInt(savedBookmark, 10);
        if (bookmarkSlice >= 1 && bookmarkSlice <= res.chunks.length) {
          activeChunkIndex.value = bookmarkSlice - 1;
        }
      }
    }

    markCurrentSliceCompleted();
    checkAndCurateSlice();
  } catch (err) {
    // handled by store
  }

  // Attach global keyboard listener for Shift + Left/Right and Escape
  window.addEventListener("keydown", handleKeyDown);
});

const isCuratingCurrentSlice = ref(false);
const curationError = ref<string | null>(null);
const isViewingRawTemporarily = ref(false);
const prefetchedChunkOrders = ref<Set<number>>(new Set());
let prefetchTimeoutId: ReturnType<typeof setTimeout> | null = null;

function cancelPendingPrefetch() {
  if (prefetchTimeoutId) {
    clearTimeout(prefetchTimeoutId);
    prefetchTimeoutId = null;
  }
}

function scheduleLookaheadPrefetch(delayMs = 2500) {
  cancelPendingPrefetch();
  prefetchTimeoutId = setTimeout(() => {
    prefetchTimeoutId = null;
    triggerLookaheadPrefetch();
  }, delayMs);
}

async function checkAndCurateSlice() {
  const chunk = currentChunk.value;
  if (!chunk) return;

  // If already formatted, schedule lookahead prefetch for next slice and exit
  if (chunk.isAiFormatted) {
    curationError.value = null;
    scheduleLookaheadPrefetch();
    return;
  }

  // If viewing raw temporarily or already curating, skip
  if (isViewingRawTemporarily.value || isCuratingCurrentSlice.value) return;

  isCuratingCurrentSlice.value = true;
  curationError.value = null;

  try {
    const updated = await libraryStore.curateSlice(
      bookId.value,
      chunk.chunkOrder,
    );
    if (updated && book.value?.chunks) {
      const idx = book.value.chunks.findIndex(
        (c) => c.chunkOrder === updated.chunkOrder,
      );
      if (idx !== -1) {
        book.value.chunks[idx] = updated;
      }
      curationError.value = null;
      scheduleLookaheadPrefetch();
    } else {
      curationError.value = "Failed to curate slice";
    }
  } catch (err: any) {
    curationError.value = err.message || "Failed to curate slice";
  } finally {
    isCuratingCurrentSlice.value = false;
  }
}

async function retryCurateCurrentSlice() {
  isViewingRawTemporarily.value = false;
  curationError.value = null;
  await checkAndCurateSlice();
}

function handleViewRawTemporarily() {
  isViewingRawTemporarily.value = true;
  curationError.value = null;
}

async function triggerLookaheadPrefetch() {
  if (!book.value?.chunks?.length) return;
  const nextIndex = activeChunkIndex.value + 1;
  if (nextIndex >= book.value.chunks.length) return;

  const nextSlice = book.value.chunks[nextIndex];
  if (
    !nextSlice ||
    nextSlice.isAiFormatted ||
    prefetchedChunkOrders.value.has(nextSlice.chunkOrder)
  ) {
    return;
  }

  prefetchedChunkOrders.value.add(nextSlice.chunkOrder);
  try {
    const updated = await libraryStore.curateSlice(
      bookId.value,
      nextSlice.chunkOrder,
    );
    if (updated && book.value?.chunks) {
      const idx = book.value.chunks.findIndex(
        (c) => c.chunkOrder === updated.chunkOrder,
      );
      if (idx !== -1) {
        book.value.chunks[idx] = updated;
      }
    }
  } catch {
    prefetchedChunkOrders.value.delete(nextSlice.chunkOrder);
  }
}

watch(activeChunkIndex, () => {
  cancelPendingPrefetch();
  isViewingRawTemporarily.value = false;
  curationError.value = null;
  checkAndCurateSlice();
});

onUnmounted(() => {
  cancelPendingPrefetch();
  window.removeEventListener("keydown", handleKeyDown);
});

function handleKeyDown(e: KeyboardEvent) {
  if (e.shiftKey && e.key === "ArrowRight") {
    e.preventDefault();
    goToNextSlice();
  } else if (e.shiftKey && e.key === "ArrowLeft") {
    e.preventDefault();
    goToPrevSlice();
  } else if (
    e.key === "Escape" &&
    !isExplainerOpen.value &&
    !isMobileTocOpen.value
  ) {
    router.push("/library");
  }
}

function markCurrentSliceCompleted() {
  if (!currentChunk.value) return;
  completedSlices.value.add(currentChunk.value.chunkOrder);
  try {
    localStorage.setItem(
      `techdaily_completed_${bookId.value}`,
      JSON.stringify(Array.from(completedSlices.value)),
    );
  } catch (e) {
    // ignore
  }
}

function selectChunk(index: number) {
  if (
    index < 0 ||
    !book.value?.chunks?.length ||
    index >= book.value.chunks.length
  )
    return;
  isViewingRawTemporarily.value = false;
  curationError.value = null;
  activeChunkIndex.value = index;
  isMobileTocOpen.value = false;

  const chunkOrder = book.value.chunks[index].chunkOrder;
  // Save bookmark
  try {
    localStorage.setItem(
      `techdaily_bookmark_${bookId.value}`,
      chunkOrder.toString(),
    );
  } catch (e) {
    // ignore
  }

  router.replace({ query: { slice: chunkOrder } });
  markCurrentSliceCompleted();

  // Scroll to top
  nextTick(() => {
    if (articleScrollContainer.value) {
      articleScrollContainer.value.scrollTo({ top: 0, behavior: "smooth" });
    }
  });
}

function goToNextSlice() {
  if (activeChunkIndex.value < totalChunks.value - 1) {
    selectChunk(activeChunkIndex.value + 1);
  }
}

function goToPrevSlice() {
  if (activeChunkIndex.value > 0) {
    selectChunk(activeChunkIndex.value - 1);
  }
}

// Scoped Text Selection Listener
function handleTextSelection(event: MouseEvent) {
  const selection = window.getSelection();
  if (!selection || selection.isCollapsed) {
    floatingToolbar.value.visible = false;
    return;
  }

  const selectedStr = selection.toString().trim();
  if (selectedStr.length < 2 || selectedStr.length > 500) {
    floatingToolbar.value.visible = false;
    return;
  }

  const range = selection.getRangeAt(0);
  const rect = range.getBoundingClientRect();

  floatingToolbar.value = {
    visible: true,
    x: Math.max(16, rect.left + rect.width / 2),
    y: Math.max(70, rect.top - 46),
    selectedText: selectedStr,
  };
}

function handleCopySelection() {
  if (!floatingToolbar.value.selectedText) return;
  navigator.clipboard.writeText(floatingToolbar.value.selectedText);
  toast.info(t("reader.toast_copy"));
  floatingToolbar.value.visible = false;
}

function handleExplainSelection() {
  currentTerm.value = floatingToolbar.value.selectedText;
  currentContext.value = currentChunk.value?.chapterTitle || "";
  floatingToolbar.value.visible = false;
  isExplainerOpen.value = true;
}

async function handleHighlightSelection() {
  if (!floatingToolbar.value.selectedText || !currentChunk.value?.id) return;
  try {
    await notesStore.createHighlight({
      documentChunkId: currentChunk.value.id,
      selectedText: floatingToolbar.value.selectedText,
    });
    toast.success(t("reader.toast_highlight_success"));
  } catch (err: any) {
    toast.error(err.message || t("reader.toast_highlight_error"));
  } finally {
    floatingToolbar.value.visible = false;
  }
}
</script>

<template>
  <div
    class="h-screen flex flex-col overflow-hidden bg-white dark:bg-slate-950 transition-colors duration-200"
  >
    <!-- Top Sticky Reader Navigation Bar -->
    <header
      class="h-14 sm:h-15 px-3 sm:px-6 border-b border-slate-200 dark:border-slate-800 bg-white/95 dark:bg-slate-900/90 backdrop-blur flex items-center justify-between shrink-0 gap-2 sm:gap-4 z-20"
    >
      <!-- Left: Back to Library & TOC Toggle -->
      <div class="flex items-center gap-1.5 sm:gap-2.5 shrink-0">
        <NuxtLink
          to="/library"
          class="flex items-center gap-1 sm:gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors shrink-0"
          :title="$t('reader.return_library')"
        >
          <ArrowLeft class="w-4 h-4 shrink-0" />
          <span class="hidden sm:inline">{{ $t("reader.library") }}</span>
        </NuxtLink>

        <!-- Desktop TOC Toggle -->
        <button
          @click="isTocOpen = !isTocOpen"
          :class="[
            'hidden md:flex items-center gap-1.5 px-3 py-1.5 rounded-xl border text-xs sm:text-sm font-semibold transition-colors shrink-0',
            isTocOpen
              ? 'border-brand-300 dark:border-brand-800 bg-brand-50 dark:bg-brand-950/40 text-brand-700 dark:text-brand-400 font-bold'
              : 'border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800',
          ]"
          :title="isTocOpen ? $t('reader.close_toc') : $t('reader.open_toc')"
        >
          <List class="w-4 h-4 shrink-0" />
          <span>{{ $t("reader.contents") }}</span>
        </button>

        <!-- Mobile TOC Drawer Button -->
        <button
          @click="isMobileTocOpen = true"
          class="md:hidden flex items-center gap-1 px-2.5 py-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors shrink-0"
          :title="$t('reader.open_toc')"
        >
          <List class="w-3.5 h-3.5 shrink-0" />
          <span>{{ $t("reader.contents") }}</span>
        </button>
      </div>

      <!-- Center: Book Title & Active Chapter Indicator -->
      <div class="flex-1 min-w-0 text-center px-1 sm:px-3">
        <h1
          class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white truncate max-w-[140px] sm:max-w-xs md:max-w-md mx-auto"
        >
          {{ book?.title || "Technical Document" }}
        </h1>
        <p
          v-if="currentChunk"
          class="text-[11px] sm:text-xs text-slate-500 dark:text-slate-400 truncate max-w-[180px] sm:max-w-sm mx-auto"
        >
          <span class="hidden sm:inline">{{
            $t("reader.slice_of", {
              current: currentChunk.chunkOrder,
              total: totalChunks,
              chapter: currentChunk.chapterTitle,
            })
          }}</span>
          <span
            class="sm:hidden font-semibold text-brand-600 dark:text-brand-400"
            >{{
              $t("reader.slice_badge", {
                current: currentChunk.chunkOrder,
                total: totalChunks,
              })
            }}</span
          >
        </p>
      </div>

      <!-- Right: Quiz Chapter, ThemeToggle, Progress & Quick Nav -->
      <div class="flex items-center gap-1.5 sm:gap-2.5 shrink-0">
        <!-- 1-Click Quiz Chapter Action -->
        <NuxtLink
          v-if="currentChunk"
          :to="{
            path: '/quiz',
            query: { topic: currentChunk.chapterTitle || book?.title },
          }"
          class="hidden sm:inline-flex items-center gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl bg-purple-50 dark:bg-purple-950/50 border border-purple-200 dark:border-purple-800 text-purple-700 dark:text-purple-300 hover:bg-purple-100 dark:hover:bg-purple-900/50 text-xs font-bold transition-colors shrink-0"
          :title="$t('reader.quiz_chapter_btn')"
        >
          <HelpCircle class="w-3.5 h-3.5 shrink-0" />
          <span class="hidden md:inline">{{
            $t("reader.quiz_chapter_btn")
          }}</span>
        </NuxtLink>

        <!-- Theme Toggle -->
        <ThemeToggle />

        <!-- Progress Bar (Desktop) -->
        <div class="hidden lg:flex items-center gap-2">
          <div
            class="w-20 xl:w-28 h-2 rounded-full bg-slate-200 dark:bg-slate-800 overflow-hidden"
          >
            <div
              class="h-full bg-brand-500 rounded-full transition-all duration-300"
              :style="{ width: `${progressPercentage}%` }"
            ></div>
          </div>
          <span
            class="text-xs font-bold text-brand-700 dark:text-brand-400 tabular-nums"
          >
            {{ progressPercentage }}%
          </span>
        </div>

        <!-- Quick Slice Prev/Next -->
        <div class="flex items-center gap-0.5 sm:gap-1">
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
        <div
          class="p-4 border-b border-slate-200 dark:border-slate-800/80 flex items-center justify-between"
        >
          <div
            class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400"
          >
            <BookOpen class="w-3.5 h-3.5" />
            <span>{{ $t("reader.toc") }}</span>
          </div>
          <span class="text-xs font-semibold text-slate-400">
            {{
              $t("reader.done", {
                count: completedSlices.size,
                total: totalChunks,
              })
            }}
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
                : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 border-l-4 border-transparent',
            ]"
          >
            <CheckCircle2
              v-if="completedSlices.has(chunk.chunkOrder)"
              class="w-4 h-4 text-emerald-500 shrink-0 mt-0.5"
            />
            <span
              v-else
              class="w-4 h-4 rounded-full border border-slate-300 dark:border-slate-700 flex items-center justify-center text-xs text-slate-500 shrink-0 mt-0.5"
            >
              {{ chunk.chunkOrder }}
            </span>

            <div class="flex-1 min-w-0">
              <div class="truncate">{{ chunk.chapterTitle }}</div>
              <div
                class="text-xs text-slate-400 mt-0.5 flex items-center gap-1 font-normal"
              >
                <Clock class="w-3 h-3" />
                <span>{{
                  $t("reader.read_min", {
                    minutes: chunk.estimatedReadMinutes || 3,
                  })
                }}</span>
              </div>
            </div>
          </button>
        </div>
      </aside>

      <!-- Mobile Table of Contents Modal Drawer (Teleported to Body) -->
      <Teleport to="body">
        <div
          v-if="isMobileTocOpen"
          class="md:hidden fixed inset-0 z-50 bg-slate-950/75 backdrop-blur-sm flex justify-start animate-in fade-in"
          @click.self="isMobileTocOpen = false"
        >
          <div
            class="w-4/5 max-w-xs bg-white dark:bg-slate-900 text-slate-900 dark:text-white h-full flex flex-col shadow-2xl border-r border-slate-200 dark:border-slate-800 animate-in slide-in-from-left"
          >
            <div
              class="p-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between"
            >
              <div
                class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-slate-900 dark:text-white"
              >
                <BookOpen class="w-4 h-4 text-brand-500" />
                <span>{{ $t("reader.toc") }}</span>
              </div>
              <button
                @click="isMobileTocOpen = false"
                class="p-1.5 rounded-lg text-slate-400 hover:text-slate-900 dark:hover:text-white"
                aria-label="Close contents"
              >
                <X class="w-5 h-5" />
              </button>
            </div>

            <div class="flex-1 overflow-y-auto p-2 space-y-1">
              <button
                v-for="(chunk, idx) in book?.chunks"
                :key="chunk.id"
                @click="
                  selectChunk(idx);
                  isMobileTocOpen = false;
                "
                :class="[
                  'w-full text-left p-3 rounded-xl text-xs sm:text-sm font-semibold transition-all flex items-start gap-2.5',
                  activeChunkIndex === idx
                    ? 'bg-brand-500/10 dark:bg-brand-500/20 text-brand-900 dark:text-brand-300 font-bold border-l-4 border-brand-500 shadow-sm'
                    : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 border-l-4 border-transparent',
                ]"
              >
                <CheckCircle2
                  v-if="completedSlices.has(chunk.chunkOrder)"
                  class="w-4 h-4 text-emerald-500 shrink-0 mt-0.5"
                />
                <span
                  v-else
                  class="w-4 h-4 rounded-full border border-slate-300 dark:border-slate-700 flex items-center justify-center text-xs text-slate-500 shrink-0 mt-0.5"
                >
                  {{ chunk.chunkOrder }}
                </span>

                <div class="flex-1 min-w-0">
                  <div class="truncate">{{ chunk.chapterTitle }}</div>
                  <div
                    class="text-[11px] text-slate-400 mt-0.5 flex items-center gap-1 font-normal"
                  >
                    <Clock class="w-3 h-3" />
                    <span>{{
                      $t("reader.read_min", {
                        minutes: chunk.estimatedReadMinutes || 3,
                      })
                    }}</span>
                  </div>
                </div>
              </button>
            </div>
          </div>
        </div>
      </Teleport>

      <!-- Main Reading Article Pane -->
      <main
        ref="articleScrollContainer"
        class="flex-1 overflow-y-auto p-4 sm:p-8 md:p-12 lg:p-16 flex justify-center selection:bg-brand-500/30"
        @mouseup="handleTextSelection"
      >
        <!-- Loading State -->
        <div
          v-if="libraryStore.isLoading"
          class="flex flex-col items-center justify-center gap-3 py-20 text-slate-400"
        >
          <div
            class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin"
          ></div>
          <span class="text-sm">Loading document chapter...</span>
        </div>

        <!-- JIT Curating State (Current Slice is uncurated & actively being formatted by AI) -->
        <div
          v-else-if="
            currentChunk &&
            !currentChunk.isAiFormatted &&
            !isViewingRawTemporarily &&
            isCuratingCurrentSlice
          "
          class="py-24 flex flex-col items-center justify-center text-center space-y-4 max-w-md mx-auto my-auto"
        >
          <div
            class="w-14 h-14 rounded-2xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 flex items-center justify-center shadow-sm"
          >
            <Sparkles
              class="w-7 h-7 text-brand-600 dark:text-brand-400 animate-spin"
            />
          </div>
          <div class="space-y-2">
            <h3
              class="text-base sm:text-lg font-bold text-slate-900 dark:text-white"
            >
              {{ $t("reader.curating_title") }}
            </h3>
            <p
              class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed"
            >
              {{ $t("reader.curating_desc") }}
            </p>
          </div>
        </div>

        <!-- AI Curation Error State (Retry or View Raw Temporarily) -->
        <div
          v-else-if="
            currentChunk &&
            !currentChunk.isAiFormatted &&
            !isViewingRawTemporarily &&
            curationError
          "
          class="py-20 flex flex-col items-center justify-center text-center space-y-5 max-w-md mx-auto my-auto"
        >
          <div
            class="w-14 h-14 rounded-2xl bg-amber-50 dark:bg-amber-950/60 border border-amber-200 dark:border-amber-800/60 flex items-center justify-center shadow-sm"
          >
            <AlertCircle class="w-7 h-7 text-amber-600 dark:text-amber-400" />
          </div>
          <div class="space-y-2">
            <h3
              class="text-base sm:text-lg font-bold text-slate-900 dark:text-white"
            >
              {{ $t("reader.curate_error_title") }}
            </h3>
            <p
              class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed"
            >
              {{ $t("reader.curate_error_desc") }}
            </p>
          </div>
          <div class="flex flex-wrap items-center justify-center gap-3 pt-2">
            <button
              @click="retryCurateCurrentSlice"
              class="px-4 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-semibold transition-all shadow-sm active:scale-95 flex items-center gap-1.5 whitespace-nowrap shrink-0"
            >
              <RefreshCw class="w-4 h-4" />
              <span>{{ $t("reader.retry_ai") }}</span>
            </button>
            <button
              @click="handleViewRawTemporarily"
              class="px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300 text-xs sm:text-sm font-semibold transition-all active:scale-95 whitespace-nowrap shrink-0"
            >
              <span>{{ $t("reader.view_raw_temporary") }}</span>
            </button>
          </div>
        </div>

        <!-- Article Content Card -->
        <div
          v-else-if="currentChunk"
          class="w-full max-w-3xl space-y-8 sm:space-y-10"
        >
          <!-- Ephemeral Raw Text Fallback Amber Banner -->
          <div
            v-if="isViewingRawTemporarily"
            class="p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800/80 flex items-center justify-between gap-3 text-xs sm:text-sm text-amber-800 dark:text-amber-300 shadow-sm"
          >
            <div class="flex items-center gap-2.5 min-w-0">
              <AlertTriangle
                class="w-4 h-4 shrink-0 text-amber-600 dark:text-amber-400"
              />
              <span class="leading-snug">{{
                $t("reader.viewing_raw_notice")
              }}</span>
            </div>
            <button
              @click="retryCurateCurrentSlice"
              class="px-3 py-1.5 rounded-xl bg-amber-600 hover:bg-amber-700 text-white font-semibold text-xs transition-colors shrink-0 whitespace-nowrap shadow-sm"
            >
              {{ $t("reader.retry_ai") }}
            </button>
          </div>
          <!-- Chapter Meta Header -->
          <div
            class="space-y-3 sm:space-y-4 pb-5 sm:pb-6 border-b border-slate-200 dark:border-slate-800/80"
          >
            <div
              class="flex items-center gap-2 sm:gap-3 text-xs font-bold text-brand-700 dark:text-brand-400 uppercase tracking-wider"
            >
              <span
                class="px-2.5 py-1 rounded-lg bg-brand-100 dark:bg-brand-950/70 border border-brand-200 dark:border-brand-800"
              >
                {{
                  $t("reader.slice_badge", {
                    current: currentChunk.chunkOrder,
                    total: totalChunks,
                  })
                }}
              </span>
              <span
                class="flex items-center gap-1 text-slate-500 dark:text-slate-400 font-normal"
              >
                <Clock class="w-3.5 h-3.5" />
                {{
                  $t("reader.reading_time", {
                    minutes: currentChunk.estimatedReadMinutes || 3,
                  })
                }}
              </span>
            </div>

            <h1
              class="text-xl sm:text-3xl lg:text-4xl font-extrabold text-slate-900 dark:text-white tracking-tight leading-tight"
            >
              {{ currentChunk.chapterTitle }}
            </h1>
          </div>

          <!-- Markdown Body -->
          <article
            class="markdown-body prose prose-slate dark:prose-invert max-w-full min-w-0 break-words prose-headings:font-bold prose-headings:tracking-tight prose-headings:text-slate-900 dark:prose-headings:text-white prose-a:text-emerald-500 hover:prose-a:underline prose-code:font-mono prose-code:text-emerald-600 dark:prose-code:text-emerald-400 prose-code:bg-slate-100 dark:prose-code:bg-slate-800/80 prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded-md prose-code:text-sm prose-code:before:content-none prose-code:after:content-none prose-blockquote:not-italic prose-blockquote:before:content-none prose-blockquote:after:content-none prose-p:before:content-none prose-p:after:content-none leading-relaxed text-sm md:text-lg"
            v-html="renderedMarkdown"
          ></article>

          <!-- Key Takeaways Callout -->
          <div
            v-if="hasValidTakeaways"
            class="p-4 sm:p-6 rounded-3xl bg-amber-50/80 dark:bg-amber-950/30 border border-amber-200/80 dark:border-amber-900/50 space-y-3"
          >
            <div
              class="flex items-center gap-2 text-xs sm:text-sm font-bold text-amber-900 dark:text-amber-300 uppercase tracking-wider"
            >
              <Sparkles class="w-4 h-4 text-amber-600 dark:text-amber-400" />
              <span>{{ $t("reader.key_takeaways") }}</span>
            </div>
            <ul class="space-y-2">
              <li
                v-for="(takeaway, idx) in currentChunk.keyTakeaways"
                :key="idx"
                class="text-sm md:text-lg text-slate-700 dark:text-slate-300 flex items-start gap-2.5 leading-relaxed"
              >
                <span
                  class="w-1.5 h-1.5 rounded-full bg-amber-500 mt-2 shrink-0"
                ></span>
                <span>{{ takeaway }}</span>
              </li>
            </ul>
          </div>

          <!-- Bottom Symmetrical Navigation Cards & Progress Footer -->
          <div
            class="pt-6 mt-8 sm:mt-12 border-t border-slate-200 dark:border-slate-800/80 space-y-3"
          >
            <!-- Progress Meta Bar -->
            <div
              class="flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400 px-1"
            >
              <div class="flex items-center gap-1.5">
                <BookOpen class="w-3.5 h-3.5 text-brand-500/80" />
                <span>{{ $t("reader.progress_label") }}</span>
              </div>
              <div class="flex items-center gap-2">
                <span>{{
                  $t("reader.done", {
                    count: activeChunkIndex + 1,
                    total: totalChunks,
                  })
                }}</span>
                <span class="text-slate-300 dark:text-slate-700">•</span>
                <span class="font-bold text-slate-700 dark:text-slate-300"
                  >{{ progressPercentage }}%</span
                >
              </div>
            </div>

            <!-- Symmetrical 2-Column Card Grid -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <!-- Left Card: Previous Slice -->
              <button
                v-if="prevChunk"
                @click="goToPrevSlice"
                class="group flex flex-col items-start p-4 sm:p-5 rounded-2xl border border-slate-200 dark:border-slate-800/80 bg-white/80 dark:bg-slate-900/60 hover:bg-slate-50 dark:hover:bg-slate-800/60 hover:border-slate-300 dark:hover:border-slate-700 transition-all text-left shadow-sm hover:shadow-md active:scale-[0.99] min-w-0"
                :title="prevChunk.chapterTitle"
              >
                <div
                  class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 group-hover:text-slate-700 dark:group-hover:text-slate-200 transition-colors"
                >
                  <ChevronLeft
                    class="w-3.5 h-3.5 transition-transform group-hover:-translate-x-1 shrink-0"
                  />
                  <span class="whitespace-nowrap shrink-0">{{
                    $t("reader.prev_slice_card_label")
                  }}</span>
                </div>
                <div
                  class="w-full text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200 group-hover:text-brand-600 dark:group-hover:text-brand-400 truncate mt-1.5 transition-colors"
                >
                  {{ prevChunk.chapterTitle }}
                </div>
              </button>

              <!-- Right Card: Next Slice -->
              <button
                v-if="activeChunkIndex < totalChunks - 1"
                @click="goToNextSlice"
                :class="[
                  'group flex flex-col items-end p-4 sm:p-5 rounded-2xl border border-brand-500/30 dark:border-brand-500/20 bg-brand-50/30 dark:bg-brand-950/20 hover:bg-brand-50/60 dark:hover:bg-brand-950/40 hover:border-brand-500/60 dark:hover:border-brand-500/50 transition-all text-right shadow-sm hover:shadow-md active:scale-[0.99] min-w-0',
                  { 'sm:col-start-2': !prevChunk },
                ]"
                :title="nextChunk?.chapterTitle"
              >
                <div
                  class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 transition-colors"
                >
                  <span class="whitespace-nowrap shrink-0">{{
                    $t("reader.next_slice_card_label")
                  }}</span>
                  <ChevronRight
                    class="w-3.5 h-3.5 transition-transform group-hover:translate-x-1 shrink-0"
                  />
                </div>
                <div
                  class="w-full text-sm sm:text-base font-bold text-slate-900 dark:text-white group-hover:text-brand-600 dark:group-hover:text-brand-400 truncate mt-1.5 transition-colors"
                >
                  {{ nextChunk?.chapterTitle || $t("reader.next_slice") }}
                </div>
              </button>

              <!-- Right Card Alternative: Return to Library on Final Slice -->
              <NuxtLink
                v-else
                to="/library"
                :class="[
                  'group flex flex-col items-end p-4 sm:p-5 rounded-2xl border border-emerald-500/30 dark:border-emerald-500/20 bg-emerald-50/30 dark:bg-emerald-950/20 hover:bg-emerald-50/60 dark:hover:bg-emerald-950/40 hover:border-emerald-500/60 dark:hover:border-emerald-500/50 transition-all text-right shadow-sm hover:shadow-md active:scale-[0.99] min-w-0',
                  { 'sm:col-start-2': !prevChunk },
                ]"
              >
                <div
                  class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400 transition-colors"
                >
                  <span class="whitespace-nowrap shrink-0">{{
                    $t("reader.completed_card_label")
                  }}</span>
                  <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500 shrink-0" />
                </div>
                <div
                  class="w-full text-sm sm:text-base font-bold text-emerald-900 dark:text-emerald-200 group-hover:underline truncate mt-1.5 transition-colors"
                >
                  {{ $t("reader.return_library") }}
                </div>
              </NuxtLink>
            </div>
          </div>
        </div>
      </main>
    </div>

    <!-- Scoped Floating Selection Action Toolbar (Teleported to Body) -->
    <Teleport to="body">
      <div
        v-if="floatingToolbar.visible"
        class="fixed z-50 -translate-x-1/2 flex items-center gap-1 p-1 rounded-2xl bg-slate-900 dark:bg-slate-800 text-white shadow-2xl border border-slate-700 animate-in fade-in zoom-in-95 duration-150"
        :style="{
          left: `${floatingToolbar.x}px`,
          top: `${floatingToolbar.y}px`,
        }"
      >
        <button
          @click="handleExplainSelection"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs shadow transition-colors"
        >
          <Sparkles class="w-3.5 h-3.5" />
          <span>{{ $t("reader.explain_with_gemini") }}</span>
        </button>

        <button
          @click="handleHighlightSelection"
          class="flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold bg-amber-500/20 text-amber-300 hover:bg-amber-500 hover:text-slate-950 transition-colors"
          :title="$t('reader.highlight_save_tooltip')"
        >
          <Highlighter class="w-3.5 h-3.5" />
          <span>Highlight</span>
        </button>

        <button
          @click="handleCopySelection"
          class="flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold text-slate-300 hover:text-white hover:bg-slate-700/60 transition-colors"
        >
          <Copy class="w-3.5 h-3.5" />
          <span>{{ $t("reader.copy") }}</span>
        </button>
      </div>
    </Teleport>

    <!-- Term Explainer Tooltip Modal -->
    <TermExplainerModal
      v-if="isExplainerOpen"
      :term="currentTerm"
      :category="book?.title || 'System Architecture'"
      :context="currentContext"
      @close="isExplainerOpen = false"
    />
  </div>
</template>
