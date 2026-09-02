<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import {
  HelpCircle,
  Sparkles,
  Zap,
  RotateCcw,
  BarChart3,
  CheckCircle2,
  XCircle,
  ArrowRight,
  ArrowLeft,
  Flame,
  Award,
  BookOpen,
  Loader2,
  Layers,
  ChevronRight,
  TrendingUp,
  Target
} from 'lucide-vue-next'
import { useInterviewQuizStore, type QuizQuestion } from '~/stores/useInterviewQuizStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { useProfileStore } from '~/stores/useProfileStore'
import MarkdownIt from 'markdown-it'

const { t, locale } = useI18n()
const authStore = useAuthStore()
const profileStore = useProfileStore()
const quizStore = useInterviewQuizStore()
const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

function renderMarkdown(raw: string | undefined | null): string {
  if (!raw) return ''
  const clean = raw.replace(/\\n/g, '\n')
  return md.render(clean)
}

const selectedOptionIndex = ref<number | null>(null)
const customTopicInput = ref('')
const selectedLevel = ref(3) // 3 = Senior
const selectedCount = ref(5)

const quickTopics = [
  '.NET 10 Internals & Memory',
  'PostgreSQL MVCC & Indexing',
  'React 19 Concurrency & Server Components',
  'Distributed Consensus & Raft',
  'Redis Caching & Lock Strategies',
  'Docker & Kubernetes Architecture',
  'Go Routines, Channels & Memory Model'
]

const seniorityLevels = [
  { id: 0, key: 'level_fresher', label: 'Fresher / Entry', desc: 'Core syntax, OOP, basic algorithms' },
  { id: 1, key: 'level_junior', label: 'Junior', desc: 'Framework APIs, standard libraries, debugging' },
  { id: 2, key: 'level_middle', label: 'Mid-Level', desc: 'Design patterns, concurrency, SQL tuning' },
  { id: 3, key: 'level_senior', label: 'Senior / Staff', desc: 'Under-the-hood runtime, memory trade-offs' }
]

onMounted(async () => {
  if (!authStore.isLoggedIn) {
    return navigateTo({
      path: '/login',
      query: { redirect: '/quiz' }
    })
  }

  // Pre-fill level based on user profile if available
  if (profileStore.profile?.targetRole) {
    const roleLower = profileStore.profile.targetRole.toLowerCase()
    if (roleLower.includes('senior') || roleLower.includes('lead') || roleLower.includes('staff')) {
      selectedLevel.value = 3
    } else if (roleLower.includes('middle') || roleLower.includes('mid')) {
      selectedLevel.value = 2
    } else if (roleLower.includes('junior')) {
      selectedLevel.value = 1
    } else {
      selectedLevel.value = 0
    }
  }

  await quizStore.fetchReviewQueue()
  await quizStore.fetchStats()
})

const renderedExplanationHtml = computed(() => {
  return renderMarkdown(quizStore.currentSubmission?.explanationMarkdown || quizStore.currentQuestion?.explanationMarkdown)
})

const currentQ = computed(() => quizStore.currentQuestion)
const currentSub = computed(() => quizStore.currentSubmission)

function handleSelectOption(index: number) {
  if (quizStore.isCurrentAnswered || quizStore.isSubmitting) return
  selectedOptionIndex.value = index
}

async function handleGenerateQuiz(topic?: string) {
  const chosenTopic = topic || customTopicInput.value || quickTopics[0]
  if (!chosenTopic.trim()) return

  selectedOptionIndex.value = null
  await quizStore.generateQuiz(chosenTopic.trim(), selectedLevel.value, selectedCount.value, null, locale.value)
}

async function handleSubmitAnswer() {
  if (selectedOptionIndex.value === null || !currentQ.value) return
  await quizStore.submitAnswer(currentQ.value.id, selectedOptionIndex.value)
}

function handleNextQuestion() {
  selectedOptionIndex.value = null
  quizStore.nextQuestion()
}

function handlePrevQuestion() {
  selectedOptionIndex.value = null
  quizStore.prevQuestion()
}

function handleRetryMistakes() {
  const mistakes = quizStore.questions.filter(q => {
    const sub = quizStore.submissions[q.id]
    return sub && !sub.isCorrect
  })
  if (mistakes.length > 0) {
    quizStore.startReviewSession(mistakes)
    selectedOptionIndex.value = null
  } else {
    quizStore.startReviewSession()
    selectedOptionIndex.value = null
  }
}

function getOptionLetter(idx: number): string {
  return ['A', 'B', 'C', 'D'][idx] || `${idx + 1}`
}

function getOptionClass(idx: number): string {
  if (!quizStore.isCurrentAnswered) {
    if (selectedOptionIndex.value === idx) {
      return 'border-brand-500 dark:border-brand-400 bg-brand-50/70 dark:bg-brand-950/40 text-brand-900 dark:text-brand-100 ring-2 ring-brand-500/30 shadow-sm'
    }
    return 'border-slate-200 dark:border-slate-800 bg-white/90 dark:bg-slate-900/60 text-slate-800 dark:text-slate-200 hover:border-slate-300 dark:hover:border-slate-700 hover:bg-slate-50/70 dark:hover:bg-slate-800/40'
  }

  // Answered state
  const isCorrectOption = idx === currentSub.value?.correctOptionIndex
  const isSelectedByMe = idx === (currentQ.value?.lastSelectedOptionIndex ?? selectedOptionIndex.value)

  if (isCorrectOption) {
    return 'border-emerald-500 dark:border-emerald-400 bg-emerald-50/80 dark:bg-emerald-950/40 text-emerald-950 dark:text-emerald-100 ring-2 ring-emerald-500/40'
  }
  if (isSelectedByMe && !currentSub.value?.isCorrect) {
    return 'border-rose-500 dark:border-rose-400 bg-rose-50/80 dark:bg-rose-950/40 text-rose-950 dark:text-rose-100 ring-2 ring-rose-500/40'
  }
  return 'opacity-50 border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/30 text-slate-500 dark:text-slate-500'
}
</script>

<template>
  <div class="max-w-5xl mx-auto px-4 sm:px-6 py-6 space-y-6">
    <!-- Header Section -->
    <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 border-b border-slate-200 dark:border-slate-800 pb-4">
      <div class="flex items-center gap-2.5">
        <div class="p-2 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20">
          <HelpCircle class="w-6 h-6" />
        </div>
        <h1 class="text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white">
          {{ $t('quiz.title') }}
        </h1>
      </div>

      <!-- Tab Buttons -->
      <div class="flex items-center gap-1.5 p-1 bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shrink-0 overflow-x-auto">
        <button
          data-testid="generate-tab-btn"
          @click="quizStore.activeTab = 'generate'"
          :class="[
            'px-3.5 py-2 rounded-xl text-sm font-semibold transition-all whitespace-nowrap shrink-0 flex items-center gap-1.5',
            quizStore.activeTab === 'generate'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
          ]"
        >
          <Sparkles class="w-4 h-4" />
          {{ $t('quiz.tab_generate') }}
        </button>

        <button
          v-if="quizStore.questions.length > 0"
          data-testid="arena-tab-btn"
          @click="quizStore.activeTab = 'arena'"
          :class="[
            'px-3.5 py-2 rounded-xl text-sm font-semibold transition-all whitespace-nowrap shrink-0 flex items-center gap-1.5',
            quizStore.activeTab === 'arena'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
          ]"
        >
          <Zap class="w-4 h-4" />
          {{ $t('quiz.tab_arena') }}
        </button>

        <button
          data-testid="review-tab-btn"
          @click="quizStore.activeTab = 'review'; quizStore.fetchReviewQueue()"
          :class="[
            'px-3.5 py-2 rounded-xl text-sm font-semibold transition-all whitespace-nowrap shrink-0 flex items-center gap-1.5',
            quizStore.activeTab === 'review'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
          ]"
        >
          <RotateCcw class="w-4 h-4" />
          {{ $t('quiz.tab_review') }}
          <span
            v-if="quizStore.reviewQueueTotal > 0"
            class="px-1.5 py-0.5 rounded-full text-xs font-black bg-amber-500/20 text-amber-600 dark:text-amber-400"
          >
            {{ quizStore.reviewQueueTotal }}
          </span>
        </button>

        <button
          data-testid="stats-tab-btn"
          @click="quizStore.activeTab = 'stats'; quizStore.fetchStats()"
          :class="[
            'px-3.5 py-2 rounded-xl text-sm font-semibold transition-all whitespace-nowrap shrink-0 flex items-center gap-1.5',
            quizStore.activeTab === 'stats'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
          ]"
        >
          <BarChart3 class="w-4 h-4" />
          {{ $t('quiz.tab_stats') }}
        </button>
      </div>
    </div>

    <!-- TAB 1: GENERATE QUIZ -->
    <div v-if="quizStore.activeTab === 'generate'" class="space-y-6">
      <div class="bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800/80 rounded-2xl p-5 sm:p-7 shadow-sm space-y-6">
        <!-- Topic Selection -->
        <div class="space-y-2.5">
          <label class="block text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200">
            {{ $t('quiz.topic_label') }}
          </label>
          <input
            v-model="customTopicInput"
            type="text"
            :placeholder="$t('quiz.topic_placeholder')"
            class="w-full px-4 py-3 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 text-sm sm:text-base transition-all"
            @keyup.enter="handleGenerateQuiz()"
          />

          <!-- Quick Topic Chips -->
          <div class="space-y-1.5 pt-1">
            <span class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {{ $t('quiz.quick_topics') }}
            </span>
            <div class="flex flex-wrap gap-2">
              <button
                v-for="topic in quickTopics"
                :key="topic"
                @click="customTopicInput = topic; handleGenerateQuiz(topic)"
                class="px-3 py-1.5 rounded-lg text-xs sm:text-sm font-medium bg-slate-100 dark:bg-slate-800/70 hover:bg-brand-50 dark:hover:bg-brand-950/40 text-slate-700 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 border border-slate-200 dark:border-slate-700/60 transition-colors whitespace-nowrap"
              >
                {{ topic }}
              </button>
            </div>
          </div>
        </div>

        <!-- Level Picker -->
        <div class="space-y-2.5">
          <label class="block text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200">
            {{ $t('quiz.level_label') }}
          </label>
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <button
              v-for="lvl in seniorityLevels"
              :key="lvl.id"
              @click="selectedLevel = lvl.id"
              :class="[
                'p-4 rounded-xl border text-left transition-all relative',
                selectedLevel === lvl.id
                  ? 'border-brand-500 bg-brand-50/70 dark:bg-brand-950/40 text-brand-900 dark:text-brand-100 ring-2 ring-brand-500/30'
                  : 'border-slate-200 dark:border-slate-800 bg-white/60 dark:bg-slate-950/40 hover:border-slate-300 dark:hover:border-slate-700'
              ]"
            >
              <div class="flex items-center justify-between mb-1">
                <span class="font-bold text-sm sm:text-base">{{ $t(`quiz.${lvl.key}`) }}</span>
                <span
                  v-if="selectedLevel === lvl.id"
                  class="w-2 h-2 rounded-full bg-brand-500"
                ></span>
              </div>
              <p class="text-xs text-slate-500 dark:text-slate-400">
                {{ lvl.desc }}
              </p>
            </button>
          </div>
        </div>

        <!-- Question Count -->
        <div class="space-y-2.5">
          <label class="block text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200">
            {{ $t('quiz.count_label') }}
          </label>
          <div class="flex gap-3">
            <button
              @click="selectedCount = 5"
              :class="[
                'px-4 py-2.5 rounded-xl border text-sm font-semibold transition-all',
                selectedCount === 5
                  ? 'border-brand-500 bg-brand-500 text-white shadow-sm'
                  : 'border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 text-slate-700 dark:text-slate-300 hover:border-slate-300'
              ]"
            >
              {{ $t('quiz.count_5') }}
            </button>
            <button
              @click="selectedCount = 10"
              :class="[
                'px-4 py-2.5 rounded-xl border text-sm font-semibold transition-all',
                selectedCount === 10
                  ? 'border-brand-500 bg-brand-500 text-white shadow-sm'
                  : 'border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 text-slate-700 dark:text-slate-300 hover:border-slate-300'
              ]"
            >
              {{ $t('quiz.count_10') }}
            </button>
          </div>
        </div>

        <!-- Generate Button -->
        <div class="pt-3">
          <button
            data-testid="generate-quiz-btn"
            @click="handleGenerateQuiz()"
            :disabled="quizStore.isGenerating"
            class="w-full sm:w-auto px-7 py-3.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-base shadow-md shadow-brand-500/20 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2.5 transition-all"
          >
            <Loader2 v-if="quizStore.isGenerating" class="w-5 h-5 animate-spin" />
            <Sparkles v-else class="w-5 h-5" />
            <span>{{ quizStore.isGenerating ? $t('quiz.generating_loader') : $t('quiz.btn_generate') }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- TAB 2: QUIZ ARENA -->
    <div v-if="quizStore.activeTab === 'arena' && currentQ" class="space-y-6">
      <!-- Stepper & Topic Bar -->
      <div class="bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800/80 rounded-2xl p-4 sm:p-5 shadow-sm space-y-3">
        <div class="flex flex-wrap items-center justify-between gap-3 text-sm">
          <div class="flex items-center gap-2">
            <span class="px-2.5 py-1 rounded-lg text-xs font-black uppercase tracking-wider bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20">
              {{ $t('quiz.question_counter', { current: quizStore.currentIndex + 1, total: quizStore.questions.length }) }}
            </span>
            <span class="font-bold text-slate-900 dark:text-white text-sm sm:text-base">
              {{ currentQ.topic }}
            </span>
          </div>

          <div class="flex items-center gap-2">
            <span class="px-2.5 py-1 rounded-lg text-xs font-bold bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
              {{ seniorityLevels[currentQ.level]?.label || 'Senior' }}
            </span>
            <span
              v-if="currentQ.isMastered"
              class="px-2.5 py-1 rounded-lg text-xs font-bold bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 flex items-center gap-1"
            >
              <CheckCircle2 class="w-3.5 h-3.5" />
              Mastered
            </span>
          </div>
        </div>

        <!-- Progress Bar -->
        <div class="w-full h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
          <div
            class="h-full bg-gradient-to-r from-brand-600 to-brand-400 transition-all duration-300"
            :style="{ width: `${quizStore.progressPercentage}%` }"
          ></div>
        </div>
      </div>

      <!-- Question Card -->
      <div class="bg-white/95 dark:bg-slate-900/70 border border-slate-200 dark:border-slate-800 rounded-2xl p-5 sm:p-7 shadow-sm space-y-6">
        <!-- Question Text -->
        <div class="space-y-2">
          <h2 class="text-base sm:text-lg md:text-xl font-bold text-slate-900 dark:text-white leading-relaxed break-words">
            {{ currentQ.questionText }}
          </h2>
          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400">
            {{ $t('quiz.choose_optimal_answer') }}
          </p>
        </div>

        <!-- Options Grid -->
        <div class="grid grid-cols-1 gap-3.5">
          <button
            v-for="(opt, idx) in currentQ.options"
            :key="idx"
            data-testid="quiz-option"
            @click="handleSelectOption(idx)"
            :disabled="quizStore.isCurrentAnswered"
            :class="[
              'p-4 sm:p-5 rounded-2xl border text-left transition-all flex items-start gap-3.5',
              getOptionClass(idx),
              quizStore.isCurrentAnswered ? 'cursor-default' : 'cursor-pointer'
            ]"
          >
            <span
              class="w-7 h-7 rounded-xl font-black text-xs sm:text-sm shrink-0 flex items-center justify-center transition-colors shadow-sm"
              :class="[
                selectedOptionIndex === idx || currentQ.lastSelectedOptionIndex === idx
                  ? 'bg-brand-600 text-white'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300'
              ]"
            >
              {{ getOptionLetter(idx) }}
            </span>
            <span class="text-sm sm:text-base font-medium leading-relaxed break-words flex-1">
              {{ opt }}
            </span>
          </button>
        </div>

        <!-- Action / Submit Button -->
        <div v-if="!quizStore.isCurrentAnswered" class="pt-2 flex justify-end">
          <button
            data-testid="submit-answer-btn"
            @click="handleSubmitAnswer()"
            :disabled="selectedOptionIndex === null || quizStore.isSubmitting"
            class="px-6 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm sm:text-base shadow-md shadow-brand-500/20 disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-2 transition-all whitespace-nowrap shrink-0"
          >
            <Loader2 v-if="quizStore.isSubmitting" class="w-4 h-4 animate-spin" />
            <span>{{ quizStore.isSubmitting ? $t('quiz.submitting_loader') : $t('quiz.btn_submit_choice') }}</span>
          </button>
        </div>

        <!-- Explanation & Feedback (Shown after submitting) -->
        <div v-if="quizStore.isCurrentAnswered && currentSub" class="space-y-4 pt-4 border-t border-slate-200 dark:border-slate-800">
          <!-- Banner -->
          <div
            :class="[
              'p-4 rounded-xl border flex items-center gap-3',
              currentSub.isCorrect
                ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-950 dark:text-emerald-200'
                : 'bg-amber-500/10 border-amber-500/30 text-amber-950 dark:text-amber-200'
            ]"
          >
            <CheckCircle2 v-if="currentSub.isCorrect" class="w-5 h-5 text-emerald-500 shrink-0" />
            <XCircle v-else class="w-5 h-5 text-rose-500 shrink-0" />
            <span class="font-bold text-sm sm:text-base">
              {{ currentSub.isCorrect ? $t('quiz.correct_banner') : $t('quiz.incorrect_banner') }}
            </span>
          </div>

          <!-- Deep-dive Markdown -->
          <div class="bg-slate-50 dark:bg-slate-950/60 rounded-xl p-5 border border-slate-200 dark:border-slate-800/80 space-y-2">
            <h3 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {{ $t('quiz.explanation_header') }}
            </h3>
            <div
              class="prose prose-sm sm:prose-base dark:prose-invert max-w-none text-slate-700 dark:text-slate-300 leading-relaxed"
              v-html="renderedExplanationHtml"
            ></div>
          </div>

          <!-- Next / Finish Control -->
          <div class="flex items-center justify-between pt-2">
            <button
              @click="handlePrevQuestion()"
              :disabled="quizStore.currentIndex === 0"
              class="px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300 font-semibold text-sm disabled:opacity-30 disabled:cursor-not-allowed flex items-center gap-1.5 transition-all whitespace-nowrap shrink-0"
            >
              <ArrowLeft class="w-4 h-4" />
              {{ $t('quiz.btn_prev') }}
            </button>

            <button
              @click="handleNextQuestion()"
              class="px-6 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm sm:text-base shadow-sm flex items-center gap-2 transition-all whitespace-nowrap shrink-0"
            >
              <span>{{ quizStore.currentIndex === quizStore.questions.length - 1 ? $t('quiz.btn_finish') : $t('quiz.btn_next') }}</span>
              <ArrowRight class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB 3: SESSION SUMMARY -->
    <div v-if="quizStore.activeTab === 'summary'" class="space-y-6">
      <div class="bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800/80 rounded-2xl p-6 sm:p-8 text-center space-y-6 shadow-sm">
        <div class="inline-flex p-4 rounded-full bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20">
          <Award class="w-12 h-12" />
        </div>

        <div class="space-y-2">
          <h2 class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white">
            {{ $t('quiz.score_title') }}
          </h2>
          <p class="text-sm sm:text-base text-slate-600 dark:text-slate-400">
            {{ $t('quiz.score_subtitle') }}
          </p>
        </div>

        <!-- Score Badge -->
        <div class="inline-block p-6 rounded-2xl bg-slate-50 dark:bg-slate-950/60 border border-slate-200 dark:border-slate-800">
          <span class="text-3xl sm:text-4xl font-black text-brand-600 dark:text-brand-400">
            {{ quizStore.sessionScore.correct }} / {{ quizStore.sessionScore.total }}
          </span>
          <p class="text-sm font-bold text-slate-500 mt-1">
            {{ quizStore.sessionScore.percentage }}% Accuracy
          </p>
        </div>

        <!-- Actions -->
        <div class="flex flex-wrap items-center justify-center gap-3 pt-2">
          <button
            @click="handleGenerateQuiz(quizStore.currentTopic)"
            class="px-5 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-sm flex items-center gap-2 transition-all whitespace-nowrap shrink-0"
          >
            <Sparkles class="w-4 h-4" />
            {{ $t('quiz.btn_generate_more') }}
          </button>

          <button
            v-if="quizStore.sessionScore.correct < quizStore.sessionScore.total"
            @click="handleRetryMistakes()"
            class="px-5 py-3 rounded-xl bg-amber-500 hover:bg-amber-600 text-white font-bold text-sm shadow-sm flex items-center gap-2 transition-all whitespace-nowrap shrink-0"
          >
            <RotateCcw class="w-4 h-4" />
            {{ $t('quiz.btn_retry_mistakes', { count: quizStore.sessionScore.total - quizStore.sessionScore.correct }) }}
          </button>

          <button
            @click="quizStore.resetSession()"
            class="px-5 py-3 rounded-xl border border-slate-300 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300 font-bold text-sm transition-all whitespace-nowrap shrink-0"
          >
            {{ $t('quiz.btn_choose_new') }}
          </button>
        </div>
      </div>
    </div>

    <!-- TAB 4: MISTAKE REVIEW QUEUE -->
    <div v-if="quizStore.activeTab === 'review'" class="space-y-6">
      <div v-if="quizStore.reviewQueue.length === 0" class="bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800 rounded-2xl p-8 text-center space-y-4">
        <div class="inline-flex p-4 rounded-full bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
          <CheckCircle2 class="w-10 h-10" />
        </div>
        <h3 class="text-lg font-bold text-slate-900 dark:text-white">
          {{ $t('quiz.review_empty_title') }}
        </h3>
        <p class="text-sm text-slate-600 dark:text-slate-400 max-w-md mx-auto">
          {{ $t('quiz.review_empty_desc') }}
        </p>
      </div>

      <div v-else class="space-y-4">
        <!-- Review Header Action -->
        <div class="flex items-center justify-between">
          <span class="text-sm font-bold text-slate-700 dark:text-slate-300">
            {{ $t('quiz.review_count_badge', { count: quizStore.reviewQueueTotal }) }}
          </span>
          <button
            @click="quizStore.startReviewSession()"
            class="px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-sm flex items-center gap-2 transition-all whitespace-nowrap shrink-0"
          >
            <RotateCcw class="w-4 h-4" />
            {{ $t('quiz.btn_start_review', { count: quizStore.reviewQueueTotal }) }}
          </button>
        </div>

        <!-- Question Cards List -->
        <div class="space-y-3">
          <div
            v-for="q in quizStore.reviewQueue"
            :key="q.id"
            class="p-5 rounded-2xl bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800 space-y-2 hover:border-slate-300 dark:hover:border-slate-700 transition-all"
          >
            <div class="flex items-center justify-between text-xs">
              <span class="font-bold text-brand-600 dark:text-brand-400">{{ q.topic }}</span>
              <span class="text-rose-500 font-semibold">{{ q.incorrectCount }} incorrect attempts</span>
            </div>
            <p class="text-sm sm:text-base font-bold text-slate-900 dark:text-white line-clamp-2">
              {{ q.questionText }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB 5: MASTERY STATS -->
    <div v-if="quizStore.activeTab === 'stats' && quizStore.stats" class="space-y-6">
      <!-- 4 Stat Metric Cards -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div class="p-5 rounded-2xl bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800 space-y-1">
          <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">{{ $t('quiz.stats_total') }}</span>
          <p class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white">{{ quizStore.stats.totalAnswered }}</p>
        </div>

        <div class="p-5 rounded-2xl bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800 space-y-1">
          <span class="text-xs font-bold text-emerald-500 uppercase tracking-wider">{{ $t('quiz.stats_mastered') }}</span>
          <p class="text-2xl sm:text-3xl font-black text-emerald-600 dark:text-emerald-400">{{ quizStore.stats.masteredCount }}</p>
        </div>

        <div class="p-5 rounded-2xl bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800 space-y-1">
          <span class="text-xs font-bold text-amber-500 uppercase tracking-wider">{{ $t('quiz.stats_review_queue') }}</span>
          <p class="text-2xl sm:text-3xl font-black text-amber-600 dark:text-amber-400">{{ quizStore.stats.reviewQueueCount }}</p>
        </div>

        <div class="p-5 rounded-2xl bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800 space-y-1">
          <span class="text-xs font-bold text-brand-500 uppercase tracking-wider">{{ $t('quiz.stats_accuracy') }}</span>
          <p class="text-2xl sm:text-3xl font-black text-brand-600 dark:text-brand-400">{{ quizStore.stats.accuracyRate }}%</p>
        </div>
      </div>

      <!-- Breakdown by Seniority Level -->
      <div class="bg-white/90 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800 rounded-2xl p-5 sm:p-6 space-y-4">
        <h3 class="text-sm font-bold text-slate-900 dark:text-white flex items-center gap-2">
          <TrendingUp class="w-4 h-4 text-brand-500" />
          {{ $t('quiz.stats_level_breakdown') }}
        </h3>
        <div class="space-y-3">
          <div
            v-for="lvl in quizStore.stats.levelBreakdown"
            :key="lvl.level"
            class="space-y-1.5"
          >
            <div class="flex items-center justify-between text-xs sm:text-sm">
              <span class="font-semibold text-slate-700 dark:text-slate-300">
                {{ seniorityLevels[lvl.level]?.label || 'Senior' }} ({{ lvl.masteredCount }}/{{ lvl.answeredCount }} mastered)
              </span>
              <span class="font-black text-brand-600 dark:text-brand-400">{{ lvl.accuracyRate }}%</span>
            </div>
            <div class="w-full h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
              <div
                class="h-full bg-brand-500 rounded-full transition-all"
                :style="{ width: `${lvl.accuracyRate}%` }"
              ></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
