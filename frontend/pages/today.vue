<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { BookOpen, Terminal, ChevronLeft, ChevronRight, Calendar, CheckCircle2, RotateCcw } from 'lucide-vue-next'
import DocReaderPane from '~/components/today/DocReaderPane.vue'
import InterviewChallengePane from '~/components/today/InterviewChallengePane.vue'

const route = useRoute()
const router = useRouter()
const focusStore = useDailyFocusStore()
const { locale } = useI18n()

const activeMobileTab = ref<'reader' | 'challenge'>('reader')
const currentDayOrder = ref<number>(1)

const isTodayScheduledDay = computed(() => {
  if (!focusStore.data?.topic) return true
  const naturalDay = ((focusStore.data.currentStreak || 0) % 30) + 1
  return focusStore.data.topic.dayOrder === naturalDay
})

onMounted(async () => {
  const queryDay = route.query.day ? parseInt(route.query.day as string, 10) : undefined
  const res = await focusStore.fetchTodayFocus(queryDay, undefined, locale.value)
  if (res?.topic) {
    currentDayOrder.value = res.topic.dayOrder
  }
})

watch(locale, (newLocale) => {
  focusStore.fetchTodayFocus(currentDayOrder.value, undefined, newLocale)
})

async function navigateDay(newDay: number) {
  if (newDay < 1 || newDay > 30) return
  currentDayOrder.value = newDay
  router.replace({ query: { ...route.query, day: newDay } })
  const res = await focusStore.fetchTodayFocus(newDay, undefined, locale.value)
  if (res?.topic) {
    currentDayOrder.value = res.topic.dayOrder
  }
}

function resetToScheduledDay() {
  currentDayOrder.value = ((focusStore.data?.currentStreak || 0) % 30) + 1
  router.replace({ query: { ...route.query, day: undefined } })
  focusStore.fetchTodayFocus(undefined, undefined, locale.value)
}
</script>

<template>
  <div class="h-[calc(100vh-3.75rem)] flex flex-col overflow-hidden bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Top Day Navigation Bar -->
    <div class="h-13 px-4 md:px-8 border-b border-slate-200 dark:border-slate-800/80 bg-white/95 dark:bg-slate-900/60 backdrop-blur flex items-center justify-between shrink-0 gap-3">
      <!-- Left: Previous button -->
      <button
        @click="navigateDay(currentDayOrder - 1)"
        :disabled="currentDayOrder <= 1 || focusStore.isLoading"
        class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-sm active:scale-95"
      >
        <ChevronLeft class="w-4 h-4" />
        <span class="hidden sm:inline">{{ currentDayOrder > 1 ? `Day ${currentDayOrder - 1}` : 'Prev' }}</span>
        <span class="sm:hidden">Prev</span>
      </button>

      <!-- Center: Day Selector & Topic Title -->
      <div class="flex items-center gap-2.5 max-w-lg truncate">
        <div class="relative">
          <select
            v-model="currentDayOrder"
            @change="navigateDay(currentDayOrder)"
            class="appearance-none pl-3 pr-8 py-1.5 rounded-xl bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-800 dark:text-slate-200 text-xs sm:text-sm font-bold focus:outline-none focus:border-brand-500 cursor-pointer shadow-sm"
          >
            <option v-for="d in 30" :key="d" :value="d">
              Day {{ d }} / 30
            </option>
          </select>
          <div class="pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-500 dark:text-slate-400 text-xs">▼</div>
        </div>

        <span v-if="focusStore.data?.drill?.status === 2" class="hidden md:inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-emerald-100 dark:bg-emerald-950 border border-emerald-300 dark:border-emerald-800 text-emerald-800 dark:text-emerald-300 text-xs font-bold">
          <CheckCircle2 class="w-3.5 h-3.5" />
          <span>Completed</span>
        </span>

        <button
          v-if="!isTodayScheduledDay"
          @click="resetToScheduledDay"
          class="hidden sm:inline-flex items-center gap-1 px-2.5 py-1 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-xs font-semibold transition-colors"
          title="Back to today's scheduled curriculum"
        >
          <RotateCcw class="w-3 h-3 text-brand-600 dark:text-brand-400" />
          <span>Today's Focus</span>
        </button>
      </div>

      <!-- Right: Next button -->
      <button
        @click="navigateDay(currentDayOrder + 1)"
        :disabled="currentDayOrder >= 30 || focusStore.isLoading"
        class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-semibold disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-md shadow-brand-500/20 active:scale-95"
      >
        <span class="hidden sm:inline">Day {{ currentDayOrder + 1 }}</span>
        <span class="sm:hidden">Next</span>
        <ChevronRight class="w-4 h-4" />
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="focusStore.isLoading" class="flex-1 flex items-center justify-center">
      <div class="flex flex-col items-center gap-3 text-slate-500 dark:text-slate-400 text-sm">
        <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin"></div>
        <span>Loading Curriculum Day {{ currentDayOrder }}...</span>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="focusStore.error" class="flex-1 flex items-center justify-center p-6">
      <div class="p-6 sm:p-8 rounded-3xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-center max-w-md shadow-lg space-y-4">
        <p class="text-sm font-semibold text-rose-800 dark:text-rose-300">{{ focusStore.error }}</p>
        <button
          @click="navigateDay(currentDayOrder)"
          class="px-5 py-2.5 rounded-xl bg-slate-900 dark:bg-slate-800 hover:bg-slate-800 text-white text-xs font-bold transition-colors"
        >
          Retry Day {{ currentDayOrder }}
        </button>
      </div>
    </div>

    <!-- Main Dual-Pane Content -->
    <div v-else-if="focusStore.data" class="flex-1 flex flex-col md:flex-row overflow-hidden">
      <!-- Mobile Tab Switcher -->
      <div class="md:hidden flex border-b border-slate-200 dark:border-slate-800 bg-slate-100 dark:bg-slate-950 shrink-0">
        <button
          @click="activeMobileTab = 'reader'"
          :class="[
            'flex-1 py-3 text-xs font-bold flex items-center justify-center gap-2 border-b-2 transition-colors',
            activeMobileTab === 'reader'
              ? 'border-brand-500 text-brand-700 dark:text-brand-400 bg-white dark:bg-slate-900/40'
              : 'border-transparent text-slate-500 dark:text-slate-400'
          ]"
        >
          <BookOpen class="w-4 h-4" />
          <span>{{ $t('today.doc_reader') }}</span>
        </button>

        <button
          @click="activeMobileTab = 'challenge'"
          :class="[
            'flex-1 py-3 text-xs font-bold flex items-center justify-center gap-2 border-b-2 transition-colors',
            activeMobileTab === 'challenge'
              ? 'border-brand-500 text-brand-700 dark:text-brand-400 bg-white dark:bg-slate-900/40'
              : 'border-transparent text-slate-500 dark:text-slate-400'
          ]"
        >
          <Terminal class="w-4 h-4" />
          <span>{{ $t('today.interview_challenge') }}</span>
        </button>
      </div>

      <!-- Left Pane: Doc Reader & Source Context (50% on Desktop) -->
      <div
        :class="[
          'md:w-1/2 md:border-r border-slate-200 dark:border-slate-800/80 h-full overflow-hidden',
          activeMobileTab === 'reader' ? 'flex-1 flex flex-col' : 'hidden md:flex md:flex-col'
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
          'md:w-1/2 h-full overflow-hidden',
          activeMobileTab === 'challenge' ? 'flex-1 flex flex-col' : 'hidden md:flex md:flex-col'
        ]"
      >
        <InterviewChallengePane
          :question="focusStore.data.question"
          :drill="focusStore.data.drill"
        />
      </div>
    </div>
  </div>
</template>
