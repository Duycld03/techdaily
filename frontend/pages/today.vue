<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, computed } from "vue";
import {
  BookOpen,
  Terminal,
  ChevronLeft,
  ChevronRight,
  CheckCircle2,
  RotateCcw,
  ChevronDown,
  ArrowUpRight,
  BookMarked,
  Sparkles,
} from "lucide-vue-next";
import DocReaderPane from "~/components/today/DocReaderPane.vue";
import InterviewChallengePane from "~/components/today/InterviewChallengePane.vue";

const route = useRoute();
const router = useRouter();
const focusStore = useDailyFocusStore();
const libraryStore = useLibraryStore();
const { locale } = useI18n();

const activeMobileTab = ref<"reader" | "challenge">("reader");
const currentDayOrder = ref<number>(1);
const isBookMenuOpen = ref(false);
const bookMenuRef = ref<HTMLElement | null>(null);

function handleClickOutside(event: MouseEvent) {
  if (bookMenuRef.value && !bookMenuRef.value.contains(event.target as Node)) {
    isBookMenuOpen.value = false;
  }
}

let nextDayPrefetchTimer: ReturnType<typeof setTimeout> | null = null;

function cancelPendingNextDayPrefetch() {
  if (nextDayPrefetchTimer) {
    clearTimeout(nextDayPrefetchTimer);
    nextDayPrefetchTimer = null;
  }
}

function scheduleNextDayPrefetch(delayMs = 2500) {
  cancelPendingNextDayPrefetch();
  nextDayPrefetchTimer = setTimeout(() => {
    nextDayPrefetchTimer = null;
    triggerNextDayPrefetch();
  }, delayMs);
}

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

onMounted(async () => {
  document.addEventListener("click", handleClickOutside);
  const queryDay = route.query.day
    ? parseInt(route.query.day as string, 10)
    : undefined;
  const queryChunk = route.query.chunkOrder
    ? parseInt(route.query.chunkOrder as string, 10)
    : undefined;
  const queryBook = route.query.bookId as string | undefined;

  const res = await focusStore.fetchTodayFocus({
    bookId: queryBook,
    chunkOrder: queryChunk,
    dayOrder: queryDay,
    locale: locale.value,
  });
  if (res?.topic) {
    currentDayOrder.value = res.topic.dayOrder;
  }
  scheduleNextDayPrefetch();
});

onUnmounted(() => {
  cancelPendingNextDayPrefetch();
  document.removeEventListener("click", handleClickOutside);
});


watch(locale, (newLocale) => {
  cancelPendingNextDayPrefetch();
  if (focusStore.data?.pacer) {
    focusStore.fetchTodayFocus({
      bookId: focusStore.data.pacer.bookId,
      chunkOrder: focusStore.data.pacer.currentChunkOrder,
      locale: newLocale,
    });
  } else {
    focusStore.fetchTodayFocus(currentDayOrder.value, undefined, newLocale);
  }
});

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
  scheduleNextDayPrefetch();
}

const isTodayScheduledDay = computed(() => {
  if (!focusStore.data?.topic) return true;
  const naturalDay = ((focusStore.data.currentStreak || 0) % 30) + 1;
  return focusStore.data.topic.dayOrder === naturalDay;
});

async function navigateDay(newDay: number) {
  if (newDay < 1 || newDay > 30) return;
  cancelPendingNextDayPrefetch();
  currentDayOrder.value = newDay;
  router.replace({ query: { ...route.query, day: newDay } });
  const res = await focusStore.fetchTodayFocus(newDay, undefined, locale.value);
  if (res?.topic) {
    currentDayOrder.value = res.topic.dayOrder;
  }
}

function resetToScheduledDay() {
  currentDayOrder.value = ((focusStore.data?.currentStreak || 0) % 30) + 1;
  router.replace({ query: { ...route.query, day: undefined } });
  focusStore.fetchTodayFocus(undefined, undefined, locale.value);
}
</script>

<template>
  <div
    class="h-full flex flex-col overflow-hidden bg-slate-50 dark:bg-slate-950 transition-colors duration-200"
  >
    <!-- Top Pacer Navigation Bar -->
    <div
      class="h-13 px-3 sm:px-6 md:px-8 border-b border-slate-200 dark:border-slate-800/80 bg-white/95 dark:bg-slate-900/60 backdrop-blur flex items-center justify-between shrink-0 gap-2 sm:gap-3 relative z-30"
    >
      <!-- PACER MODE: Active Document Book -->
      <template v-if="focusStore.data?.pacer">
        <!-- Left: Previous Slice button -->
        <button
          @click="navigatePacerSlice(-1)"
          :disabled="!focusStore.data.pacer.hasPrevious || focusStore.isLoading"
          class="flex items-center gap-1 sm:gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-sm active:scale-95 whitespace-nowrap shrink-0"
        >
          <ChevronLeft class="w-3.5 h-3.5 sm:w-4 sm:h-4" />
          <span class="hidden sm:inline">{{ $t("pacer.prev_slice") }}</span>
          <span class="sm:hidden">{{ $t("today.prev") }}</span>
        </button>

        <!-- Center: 1-Click Book Switcher & Progress Pill -->
        <div class="flex items-center gap-2 sm:gap-3 min-w-0 max-w-xl">
          <!-- Book Switcher Dropdown Anchor -->
          <div ref="bookMenuRef" class="relative">
            <button
              @click="isBookMenuOpen = !isBookMenuOpen"
              class="flex items-center gap-1.5 sm:gap-2 px-2.5 sm:px-3.5 py-1.5 rounded-xl bg-slate-100 dark:bg-slate-800/90 hover:bg-slate-200 dark:hover:bg-slate-700 border border-slate-200 dark:border-slate-700 text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 transition-all shadow-sm active:scale-95"
            >
              <BookOpen
                class="w-3.5 h-3.5 sm:w-4 sm:h-4 text-brand-600 dark:text-brand-400 shrink-0"
              />
              <span
                class="max-w-[120px] sm:max-w-[200px] md:max-w-[240px] truncate"
              >
                {{ focusStore.data.pacer.bookTitle }}
              </span>
              <ChevronDown
                :class="[
                  'w-3.5 h-3.5 text-slate-400 transition-transform duration-200 shrink-0',
                  isBookMenuOpen ? 'rotate-180' : '',
                ]"
              />
            </button>

            <!-- Book Switcher Menu Dropdown -->
            <div
              v-if="isBookMenuOpen"
              class="absolute left-0 sm:left-1/2 sm:-translate-x-1/2 top-full mt-2 w-72 sm:w-84 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl p-2 z-50 animate-in fade-in zoom-in-95 duration-150 space-y-1"
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
                      ? 'bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-800/80 text-brand-950 dark:text-brand-100'
                      : 'hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300',
                  ]"
                >
                  <div class="flex items-center justify-between gap-2">
                    <span
                      class="font-bold truncate group-hover:text-brand-600 dark:group-hover:text-brand-400"
                    >
                      {{ b.title }}
                    </span>
                    <span
                      v-if="b.isActive"
                      class="px-2 py-0.5 rounded-full text-xs font-bold bg-brand-600 text-white whitespace-nowrap shrink-0"
                    >
                      {{ $t("pacer.active_badge") }}
                    </span>
                  </div>

                  <div
                    class="flex items-center justify-between gap-2 text-xs text-slate-400 dark:text-slate-500"
                  >
                    <div
                      class="flex-1 h-1.5 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden"
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

              <div class="pt-2 border-t border-slate-100 dark:border-slate-800">
                <NuxtLink
                  to="/library"
                  @click="isBookMenuOpen = false"
                  class="flex items-center justify-between px-3 py-2 rounded-xl text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 hover:bg-brand-50 dark:hover:bg-brand-950/40 transition-colors"
                >
                  <span>+ {{ $t("pacer.browse_library") }}</span>
                  <ArrowUpRight class="w-3.5 h-3.5" />
                </NuxtLink>
              </div>
            </div>
          </div>

          <!-- Progress Pill -->
          <div
            class="hidden sm:inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-100 dark:bg-slate-800/80 border border-slate-200/80 dark:border-slate-700/80 text-xs font-semibold text-slate-700 dark:text-slate-300 max-w-xs truncate shadow-sm whitespace-nowrap shrink-0"
          >
            <span class="truncate max-w-[140px] md:max-w-[180px]">{{
              focusStore.data.pacer.chapterTitle
            }}</span>
            <span class="text-slate-400 dark:text-slate-500">•</span>
            <span class="font-bold text-brand-600 dark:text-brand-400">
              {{ $t("pacer.slice") }}
              {{ focusStore.data.pacer.currentChunkOrder }}/{{
                focusStore.data.pacer.totalChunks
              }}
            </span>
            <span class="text-slate-400 font-mono"
              >({{ focusStore.data.pacer.progressPercentage }}%)</span
            >
          </div>
        </div>

        <!-- Right: Next Slice button -->
        <div class="flex items-center gap-1.5 sm:gap-2 shrink-0">
          <button
            @click="navigatePacerSlice(1)"
            :disabled="!focusStore.data.pacer.hasNext || focusStore.isLoading"
            class="flex items-center gap-1 sm:gap-1.5 px-2.5 sm:px-3.5 py-1.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-semibold disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-md shadow-brand-500/20 active:scale-95 whitespace-nowrap shrink-0"
          >
            <span class="hidden sm:inline">{{ $t("pacer.next_slice") }}</span>
            <span class="sm:hidden">{{ $t("today.next") }}</span>
            <ChevronRight class="w-3.5 h-3.5 sm:w-4 sm:h-4" />
          </button>
        </div>
      </template>

      <!-- LEGACY TOPIC MODE: 30-Day Curriculum Fallback -->
      <template v-else>
        <!-- Left: Previous button -->
        <button
          @click="navigateDay(currentDayOrder - 1)"
          :disabled="currentDayOrder <= 1 || focusStore.isLoading"
          class="flex items-center gap-1 sm:gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-sm active:scale-95 shrink-0"
        >
          <ChevronLeft class="w-3.5 h-3.5 sm:w-4 sm:h-4" />
          <span class="hidden sm:inline">{{
            currentDayOrder > 1
              ? `${$t("today.day")} ${currentDayOrder - 1}`
              : $t("today.prev")
          }}</span>
          <span class="sm:hidden">{{ $t("today.prev") }}</span>
        </button>

        <!-- Center: Day Selector & Topic Title -->
        <div class="flex items-center gap-2 max-w-lg truncate">
          <div class="relative">
            <select
              v-model="currentDayOrder"
              @change="navigateDay(currentDayOrder)"
              class="appearance-none pl-2.5 sm:pl-3 pr-7 sm:pr-8 py-1.5 rounded-xl bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-800 dark:text-slate-200 text-xs sm:text-sm font-bold focus:outline-none focus:border-brand-500 cursor-pointer shadow-sm"
            >
              <option v-for="d in 30" :key="d" :value="d">
                {{ $t("today.day") }} {{ d }} / 30
              </option>
            </select>
            <div
              class="pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-500 dark:text-slate-400 text-xs"
            >
              ▼
            </div>
          </div>

          <span
            v-if="focusStore.data?.drill?.status === 2"
            class="hidden md:inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-emerald-100 dark:bg-emerald-950 border border-emerald-300 dark:border-emerald-800 text-emerald-800 dark:text-emerald-300 text-xs font-bold"
          >
            <CheckCircle2 class="w-3.5 h-3.5" />
            <span>{{ $t("today.completed") }}</span>
          </span>

          <button
            v-if="!isTodayScheduledDay"
            @click="resetToScheduledDay"
            class="hidden sm:inline-flex items-center gap-1 px-2.5 py-1 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-xs font-semibold transition-colors shrink-0"
            title="Back to today's scheduled curriculum"
          >
            <RotateCcw class="w-3 h-3 text-brand-600 dark:text-brand-400" />
            <span>{{ $t("today.today_focus") }}</span>
          </button>
        </div>

        <!-- Right: Next button -->
        <button
          @click="navigateDay(currentDayOrder + 1)"
          :disabled="currentDayOrder >= 30 || focusStore.isLoading"
          class="flex items-center gap-1 sm:gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-semibold disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-md shadow-brand-500/20 active:scale-95 shrink-0"
        >
          <span class="hidden sm:inline"
            >{{ $t("today.day") }} {{ currentDayOrder + 1 }}</span
          >
          <span class="sm:hidden">{{ $t("today.next") }}</span>
          <ChevronRight class="w-3.5 h-3.5 sm:w-4 sm:h-4" />
        </button>
      </template>
    </div>

    <!-- Loading State -->
    <div
      v-if="focusStore.isLoading"
      class="flex-1 flex flex-col items-center justify-center p-6 sm:p-8 text-center my-auto"
    >
      <div
        class="w-12 h-12 rounded-2xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 flex items-center justify-center shadow-sm mb-4"
      >
        <Sparkles
          class="w-6 h-6 text-brand-600 dark:text-brand-400 animate-spin"
        />
      </div>
      <p
        class="text-sm sm:text-base font-semibold text-slate-700 dark:text-slate-300 max-w-sm sm:max-w-md mx-auto leading-relaxed"
      >
        {{
          focusStore.data?.pacer
            ? $t("pacer.ai_synthesis_desc")
            : $t("today.loading_curriculum", { day: currentDayOrder })
        }}
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
        <button
          @click="navigateDay(currentDayOrder)"
          class="px-5 py-2.5 rounded-xl bg-slate-900 dark:bg-slate-800 hover:bg-slate-800 text-white text-xs font-bold transition-colors"
        >
          {{ $t("today.retry_day", { day: currentDayOrder }) }}
        </button>
      </div>
    </div>

    <!-- Main Dual-Pane Content -->
    <div
      v-else-if="focusStore.data"
      class="flex-1 flex flex-col md:flex-row overflow-hidden"
    >
      <!-- Mobile Tab Switcher -->
      <div
        class="md:hidden flex border-b border-slate-200 dark:border-slate-800 bg-slate-100 dark:bg-slate-950 shrink-0"
      >
        <button
          @click="activeMobileTab = 'reader'"
          :class="[
            'flex-1 py-3 text-xs font-bold flex items-center justify-center gap-2 border-b-2 transition-colors',
            activeMobileTab === 'reader'
              ? 'border-brand-500 text-brand-700 dark:text-brand-400 bg-white dark:bg-slate-900/40'
              : 'border-transparent text-slate-500 dark:text-slate-400',
          ]"
        >
          <BookOpen class="w-4 h-4" />
          <span>{{ $t("today.doc_reader") }}</span>
        </button>

        <button
          @click="activeMobileTab = 'challenge'"
          :class="[
            'flex-1 py-3 text-xs font-bold flex items-center justify-center gap-2 border-b-2 transition-colors',
            activeMobileTab === 'challenge'
              ? 'border-brand-500 text-brand-700 dark:text-brand-400 bg-white dark:bg-slate-900/40'
              : 'border-transparent text-slate-500 dark:text-slate-400',
          ]"
        >
          <Terminal class="w-4 h-4" />
          <span>{{ $t("today.interview_challenge") }}</span>
        </button>
      </div>

      <!-- Left Pane: Doc Reader & Source Context (50% on Desktop) -->
      <div
        :class="[
          'md:w-1/2 md:border-r border-slate-200 dark:border-slate-800/80 h-full overflow-hidden min-w-0',
          activeMobileTab === 'reader'
            ? 'flex-1 flex flex-col'
            : 'hidden md:flex md:flex-col',
        ]"
      >
        <DocReaderPane
          :topic="focusStore.data.topic"
          :document-chunk="focusStore.data.documentChunk"
        />
      </div>

      <!-- Right Pane: Interview Scenario Challenge & AI Evaluator (50% on Desktop) -->
      <div
        :class="[
          'md:w-1/2 h-full overflow-hidden min-w-0',
          activeMobileTab === 'challenge'
            ? 'flex-1 flex flex-col'
            : 'hidden md:flex md:flex-col',
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
