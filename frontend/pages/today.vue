<script setup lang="ts">
import { ref, onMounted, watch, computed } from "vue";
import { onClickOutside, useTimeoutFn } from "@vueuse/core";
import {
  BookOpen,
  Terminal,
  ChevronLeft,
  ChevronRight,
  CheckCircle2,
  ChevronDown,
  ArrowUpRight,
  BookMarked,
  Loader2,
  List,
  Clock,
  X
} from "lucide-vue-next";
import DocReaderPane from "~/components/today/DocReaderPane.vue";
import InterviewChallengePane from "~/components/today/InterviewChallengePane.vue";
import { useDailyFocusStore } from "~/stores/useDailyFocusStore";
import { useLibraryStore } from "~/stores/useLibraryStore";

const route = useRoute();
const router = useRouter();
const focusStore = useDailyFocusStore();
const libraryStore = useLibraryStore();
const { locale } = useI18n();

const activeMobileTab = ref<"reader" | "challenge">("reader");
const isBookMenuOpen = ref(false);
const bookMenuRef = ref<HTMLElement | null>(null);

// Studio Panel Layout State
const isOutlineOpen = ref(false);
const isChallengeDockOpen = ref(true);

const chunks = computed(() => libraryStore.selectedBook?.chunks || []);

onClickOutside(bookMenuRef, () => {
  if (isBookMenuOpen.value) {
    isBookMenuOpen.value = false;
  }
});

const { start: scheduleNextDayPrefetch, stop: cancelPendingNextDayPrefetch } = useTimeoutFn(
  triggerNextDayPrefetch,
  2500,
  { immediate: false }
);

function triggerNextDayPrefetch() {
  if (!focusStore.data?.pacer) return;
  const pacer = focusStore.data.pacer;
  const nextChunkOrder = pacer.currentChunkOrder + 1;
  if (nextChunkOrder <= pacer.totalChunks) {
    libraryStore.curateSlice(pacer.bookId, nextChunkOrder).catch((err) => {
      console.warn("Failed to prefetch next day slice curation:", err);
    });
  }
}

async function jumpToSlice(order: number) {
  if (!focusStore.data?.pacer) return;
  if (order === focusStore.data.pacer.currentChunkOrder) return;
  cancelPendingNextDayPrefetch();
  router.replace({
    query: {
      ...route.query,
      bookId: focusStore.data.pacer.bookId,
      chunkOrder: order,
      day: undefined,
    },
  });
  await focusStore.fetchTodayFocus({
    bookId: focusStore.data.pacer.bookId,
    chunkOrder: order,
    locale: locale.value,
  });
  scheduleNextDayPrefetch();
}

async function navigatePacerSlice(direction: -1 | 1) {
  if (!focusStore.data?.pacer) return;
  cancelPendingNextDayPrefetch();
  const nextOrder = focusStore.data.pacer.currentChunkOrder + direction;
  if (nextOrder < 1 || nextOrder > focusStore.data.pacer.totalChunks) return;

  router.replace({
    query: {
      ...route.query,
      bookId: focusStore.data.pacer.bookId,
      chunkOrder: nextOrder,
      day: undefined,
    },
  });

  await focusStore.fetchTodayFocus({
    bookId: focusStore.data.pacer.bookId,
    chunkOrder: nextOrder,
    locale: locale.value,
  });
  scheduleNextDayPrefetch();
}

async function handleSwitchBook(bookId: string) {
  isBookMenuOpen.value = false;
  cancelPendingNextDayPrefetch();
  router.replace({
    query: {
      ...route.query,
      bookId,
      chunkOrder: undefined,
      day: undefined,
    },
  });
  await focusStore.switchBook(bookId, locale.value);
  if (focusStore.data?.pacer?.bookId) {
    libraryStore.fetchBookById(focusStore.data.pacer.bookId).catch(() => {});
  }
  scheduleNextDayPrefetch();
}

onMounted(async () => {

  const queryChunk = route.query.chunkOrder
    ? parseInt(route.query.chunkOrder as string, 10)
    : undefined;
  const queryBook = route.query.bookId as string | undefined;

  const res = await focusStore.fetchTodayFocus({
    bookId: queryBook,
    chunkOrder: queryChunk,
    locale: locale.value,
  });
  if (res?.pacer?.bookId) {
    libraryStore.fetchBookById(res.pacer.bookId).catch(() => {});
  }
  scheduleNextDayPrefetch();
});



watch(locale, (newLocale) => {
  cancelPendingNextDayPrefetch();
  if (focusStore.data?.pacer) {
    focusStore.fetchTodayFocus({
      bookId: focusStore.data.pacer.bookId,
      chunkOrder: focusStore.data.pacer.currentChunkOrder,
      locale: newLocale,
    });
    libraryStore.fetchBookById(focusStore.data.pacer.bookId).catch(() => {});
  }
});
</script>

<template>
  <div
    class="h-full flex flex-col overflow-hidden bg-slate-50 dark:bg-canvas transition-colors duration-200"
  >
    <!-- Studio Control Bar -->
    <div
      class="h-12 px-3 sm:px-4 md:px-6 border-b border-slate-200/80 dark:border-white/[0.08] bg-white/95 dark:bg-canvas-subtle/80 backdrop-blur flex items-center justify-between shrink-0 gap-2 sm:gap-3 relative z-30"
    >
      <!-- Left: Outline Drawer Toggle + Breadcrumb -->
      <div class="flex items-center gap-2 sm:gap-3 min-w-0">
        <!-- Outline Toggle Button -->
        <button
          @click="isOutlineOpen = !isOutlineOpen"
          type="button"
          :class="[
            'flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl border text-xs font-bold transition-all shrink-0',
            isOutlineOpen
              ? 'bg-brand-500/10 text-brand-400 border-brand-500/30 shadow-sm'
              : 'bg-slate-100 dark:bg-canvas-elevated text-slate-600 dark:text-slate-300 border-slate-200 dark:border-white/[0.08] hover:border-brand-500/30'
          ]"
          :title="isOutlineOpen ? 'Close Outline' : 'Open Outline'"
        >
          <List class="w-3.5 h-3.5" :stroke-width="1.5" />
          <span class="hidden sm:inline">{{ $t('reader.toc') || 'Outline' }}</span>
        </button>

        <!-- PACER MODE: Breadcrumbs & Book Switcher Anchor -->
        <template v-if="focusStore.data?.pacer">
          <div ref="bookMenuRef" class="relative shrink-0">
            <button
              @click="isBookMenuOpen = !isBookMenuOpen"
              class="flex items-center gap-1.5 px-2 sm:px-2.5 py-1 rounded-lg bg-slate-100 dark:bg-canvas-elevated hover:bg-slate-200 dark:hover:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08] text-xs font-bold text-slate-800 dark:text-slate-200 transition-all shrink-0"
              title="Switch Document Book"
            >
              <BookOpen class="w-3.5 h-3.5 text-brand-400 shrink-0" :stroke-width="1.5" />
              <span class="max-w-[110px] sm:max-w-[160px] truncate">
                {{ focusStore.data.pacer.bookTitle }}
              </span>
              <ChevronDown
                :class="[
                  'w-3 h-3 text-slate-400 transition-transform duration-200 shrink-0',
                  isBookMenuOpen ? 'rotate-180' : '',
                ]"
                :stroke-width="1.5"
              />
            </button>

            <!-- Book Switcher Dropdown -->
            <div
              v-if="isBookMenuOpen"
              class="absolute left-0 top-full mt-2 w-72 sm:w-80 max-w-[calc(100vw-2rem)] rounded-2xl bg-white dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] shadow-2xl p-2 z-50 animate-in fade-in zoom-in-95 duration-150 space-y-1 select-none"
            >
              <div
                class="px-3 py-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500"
              >
                {{ $t("pacer.in_progress_books") }}
              </div>

              <div class="max-h-64 overflow-y-auto space-y-1">
                <button
                  v-for="b in focusStore.data.pacer.availableBooks"
                  :key="b.id"
                  @click="handleSwitchBook(b.id)"
                  :class="[
                    'w-full text-left p-2.5 rounded-xl text-xs sm:text-sm transition-all flex flex-col gap-1.5 group',
                    b.isActive
                      ? 'bg-brand-500/10 border border-brand-500/30 text-brand-300'
                      : 'hover:bg-slate-100 dark:hover:bg-white/[0.06] text-slate-700 dark:text-slate-300',
                  ]"
                >
                  <div class="flex items-center justify-between gap-2">
                    <span class="font-bold truncate group-hover:text-brand-400">
                      {{ b.title }}
                    </span>
                    <span
                      v-if="b.isActive"
                      class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-brand-600 text-white whitespace-nowrap shrink-0"
                    >
                      {{ $t("pacer.active_badge") }}
                    </span>
                  </div>

                  <div
                    class="flex items-center justify-between gap-2 text-xs text-slate-400 dark:text-slate-500"
                  >
                    <div
                      class="flex-1 h-1.5 bg-slate-200 dark:bg-canvas-subtle rounded-full overflow-hidden"
                    >
                      <div
                        class="h-full bg-brand-500 rounded-full transition-all duration-300"
                        :style="{ width: `${b.progressPercentage}%` }"
                      ></div>
                    </div>
                    <span class="font-mono shrink-0"
                      >{{ b.currentChunkOrder }}/{{ b.totalChunks }} ({{
                        b.progressPercentage
                      }}%)</span
                    >
                  </div>
                </button>
              </div>

              <div class="pt-2 border-t border-slate-100 dark:border-white/[0.08]">
                <NuxtLink
                  to="/library"
                  @click="isBookMenuOpen = false"
                  class="flex items-center justify-between px-3 py-2 rounded-xl text-xs font-bold text-brand-400 hover:bg-brand-500/10 transition-colors"
                >
                  <span>+ {{ $t("pacer.browse_library") }}</span>
                  <ArrowUpRight class="w-3.5 h-3.5" />
                </NuxtLink>
              </div>
            </div>
          </div>

          <!-- Breadcrumb chapter trail -->
          <div class="hidden md:flex items-center gap-1.5 text-xs text-slate-400 dark:text-slate-500 truncate">
            <span>›</span>
            <span class="truncate max-w-[160px] lg:max-w-[240px] text-slate-700 dark:text-slate-300 font-medium">
              {{ focusStore.data.pacer.chapterTitle }}
            </span>
          </div>
        </template>
      </div>

      <!-- Right: Pace badge, slice switcher, and copilot dock toggle -->
      <div class="flex items-center gap-1.5 sm:gap-2 shrink-0">
        <!-- Duration badge -->
        <div class="hidden md:flex items-center gap-1 px-2.5 py-1 rounded-full bg-slate-100 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] text-[11px] font-semibold text-slate-600 dark:text-slate-300">
          <Clock class="w-3 h-3 text-brand-400" />
          <span>{{ focusStore.data?.estimatedMinutes || 4 }}m</span>
        </div>

        <!-- Pacer Prev/Next Slice -->
        <template v-if="focusStore.data?.pacer">
          <button
            @click="navigatePacerSlice(-1)"
            :disabled="!focusStore.data.pacer.hasPrevious || focusStore.isLoading"
            class="p-1.5 rounded-xl border border-slate-200 dark:border-white/[0.08] bg-slate-100 dark:bg-canvas-elevated hover:bg-slate-200 dark:hover:bg-white/[0.06] text-slate-700 dark:text-slate-300 disabled:opacity-30 disabled:cursor-not-allowed transition-all shadow-sm active:scale-95 shrink-0"
            title="Previous Slice"
          >
            <ChevronLeft class="w-3.5 h-3.5" />
          </button>

          <span class="text-xs font-mono font-bold text-brand-500 dark:text-brand-400 px-1 shrink-0">
            {{ focusStore.data.pacer.currentChunkOrder }}/{{ focusStore.data.pacer.totalChunks }}
          </span>

          <button
            @click="navigatePacerSlice(1)"
            :disabled="!focusStore.data.pacer.hasNext || focusStore.isLoading"
            class="p-1.5 rounded-xl border border-slate-200 dark:border-white/[0.08] bg-slate-100 dark:bg-canvas-elevated hover:bg-slate-200 dark:hover:bg-white/[0.06] text-slate-700 dark:text-slate-300 disabled:opacity-30 disabled:cursor-not-allowed transition-all shadow-sm active:scale-95 shrink-0"
            title="Next Slice"
          >
            <ChevronRight class="w-3.5 h-3.5" />
          </button>
        </template>

        <!-- Scenario Challenge Dock Toggle -->
        <button
          v-show="!isChallengeDockOpen"
          @click="isChallengeDockOpen = !isChallengeDockOpen"
          type="button"
          :class="[
            'flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl border text-xs font-bold transition-all shrink-0',
            isChallengeDockOpen
              ? 'bg-brand-600 hover:bg-brand-500 text-white border-transparent shadow-sm shadow-brand-500/20 active:scale-95'
              : 'bg-slate-100 dark:bg-canvas-elevated text-slate-600 dark:text-slate-300 border-slate-200 dark:border-white/[0.08] hover:border-brand-500/30'
          ]"
          :title="isChallengeDockOpen ? 'Collapse Scenario Dock (Full Immersion Reader)' : 'Open Scenario Challenge Dock'"
        >
          <Terminal class="w-3.5 h-3.5" />
          <span class="hidden sm:inline">{{ isChallengeDockOpen ? 'Dock' : 'Scenario' }}</span>
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div
      v-if="focusStore.isLoading"
      class="flex-1 flex flex-col items-center justify-center p-6 sm:p-8 text-center my-auto"
    >
      <div
        class="w-12 h-12 rounded-2xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 flex items-center justify-center shadow-sm mb-4"
      >
        <Loader2
          class="w-6 h-6 text-brand-600 dark:text-brand-400 animate-spin"
          :stroke-width="1.5"
        />
      </div>
      <p
        class="text-sm sm:text-base font-semibold text-slate-700 dark:text-slate-300 max-w-sm sm:max-w-md mx-auto leading-relaxed"
      >
        {{ $t("pacer.ai_synthesis_desc") }}
      </p>
    </div>

    <!-- Error State -->
    <div
      v-else-if="focusStore.error"
      class="flex-1 flex items-center justify-center p-6"
    >
      <div
        class="p-6 sm:p-8 rounded-3xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-center max-w-md shadow-lg space-y-4"
      >
        <p class="text-sm font-semibold text-rose-800 dark:text-rose-300">
          {{ focusStore.error }}
        </p>
      </div>
    </div>

    <!-- Main 3-Column Studio Workspace -->
    <div
      v-else-if="focusStore.data"
      class="flex-1 flex flex-col md:flex-row overflow-hidden relative"
    >
      <!-- Mobile Tab Switcher -->
      <div
        class="md:hidden flex border-b border-slate-200/80 dark:border-white/[0.08] bg-slate-100 dark:bg-canvas-subtle shrink-0"
      >
        <button
          @click="activeMobileTab = 'reader'"
          :class="[
            'flex-1 py-2.5 text-xs font-bold flex items-center justify-center gap-2 border-b-2 transition-colors',
            activeMobileTab === 'reader'
              ? 'border-brand-500 text-brand-400 bg-white dark:bg-canvas-elevated'
              : 'border-transparent text-slate-500 dark:text-slate-400',
          ]"
        >
          <BookOpen class="w-4 h-4" />
          <span>{{ $t("today.doc_reader") }}</span>
        </button>

        <button
          @click="activeMobileTab = 'challenge'"
          :class="[
            'flex-1 py-2.5 text-xs font-bold flex items-center justify-center gap-2 border-b-2 transition-colors',
            activeMobileTab === 'challenge'
              ? 'border-brand-500 text-brand-400 bg-white dark:bg-canvas-elevated'
              : 'border-transparent text-slate-500 dark:text-slate-400',
          ]"
        >
          <Terminal class="w-4 h-4" />
          <span>{{ $t("today.interview_challenge") }}</span>
        </button>
      </div>

      <!-- Left Rail: Outline & Slice Navigator -->
      <aside
        v-if="isOutlineOpen"
        class="w-full md:w-72 lg:w-80 h-full border-r border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle flex flex-col shrink-0 z-20 transition-all duration-200 select-none"
      >
        <!-- Outline Header -->
        <div class="p-3 border-b border-slate-200/80 dark:border-white/[0.08] flex items-center justify-between shrink-0">
          <div class="flex items-center gap-2">
            <BookOpen class="w-4 h-4 text-brand-400" />
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 uppercase tracking-wider">
              {{ $t('reader.toc') || 'Outline' }}
            </span>
          </div>
          <button
            @click="isOutlineOpen = false"
            class="p-1 rounded-lg text-slate-400 hover:text-slate-200 hover:bg-white/[0.06] transition-colors"
          >
            <X class="w-3.5 h-3.5" />
          </button>
        </div>

        <!-- Slices List -->
        <div class="flex-1 overflow-y-auto p-2 space-y-1">
          <button
            v-for="chunk in chunks"
            :key="chunk.id || chunk.chunkOrder"
            @click="jumpToSlice(chunk.chunkOrder)"
            :class="[
              'w-full text-left px-3 py-2.5 rounded-xl text-xs transition-colors border-l-2 flex items-center justify-between gap-2 group',
              chunk.chunkOrder === focusStore.data?.pacer?.currentChunkOrder
                ? 'bg-brand-500/10 border-brand-500 text-brand-400 font-bold shadow-sm'
                : 'border-transparent text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.04]'
            ]"
          >
            <div class="flex items-center gap-2 min-w-0">
              <span
                :class="[
                  'w-5 h-5 rounded-md flex items-center justify-center text-[10px] font-mono shrink-0',
                  chunk.chunkOrder < (focusStore.data?.pacer?.currentChunkOrder || 1)
                    ? 'bg-emerald-500/10 text-emerald-400'
                    : chunk.chunkOrder === focusStore.data?.pacer?.currentChunkOrder
                      ? 'bg-brand-500 text-white font-bold'
                      : 'bg-slate-100 dark:bg-canvas-elevated text-slate-400'
                ]"
              >
                <CheckCircle2 v-if="chunk.chunkOrder < (focusStore.data?.pacer?.currentChunkOrder || 1)" class="w-3 h-3" />
                <span v-else>{{ chunk.chunkOrder }}</span>
              </span>
              <span class="truncate">{{ chunk.chapterTitle }}</span>
            </div>

            <span class="text-[10px] text-slate-400 dark:text-slate-500 font-mono shrink-0">
              {{ chunk.estimatedReadMinutes || 4 }}m
            </span>
          </button>
        </div>
      </aside>

      <!-- Center Stage: Doc Reader (Full-width when challenge dock is collapsed) -->
      <div
        :class="[
          'h-full overflow-hidden min-w-0 transition-all duration-300',
          isChallengeDockOpen ? 'w-full md:w-1/2 md:border-r border-slate-200/80 dark:border-white/[0.08]' : 'w-full',
          activeMobileTab === 'reader'
            ? 'flex-1 flex flex-col'
            : 'hidden md:flex md:flex-col'
        ]"
      >
        <DocReaderPane
          :topic="focusStore.data.topic"
          :document-chunk="focusStore.data.documentChunk"
        />
      </div>

      <!-- Right Dock: Scenario Copilot Challenge (Collapsible) -->
      <div
        v-if="isChallengeDockOpen"
        :class="[
          'md:w-1/2 h-full overflow-hidden min-w-0 transition-all duration-300',
          activeMobileTab === 'challenge'
            ? 'flex-1 flex flex-col'
            : 'hidden md:flex md:flex-col'
        ]"
      >
        <InterviewChallengePane
          :question="focusStore.data.question"
          :drill="focusStore.data.drill"
          :chapter-title="
            focusStore.data.pacer?.chapterTitle ||
            focusStore.data.documentChunk?.chapterTitle
          "
        />
      </div>
    </div>
  </div>
</template>
