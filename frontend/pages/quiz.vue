<script lang="ts">
export const seniorityLevels = [
  { id: 0, key: 'level_fresher', label: 'Fresher / Entry', desc: 'Core syntax, OOP, basic algorithms' },
  { id: 1, key: 'level_junior', label: 'Junior', desc: 'Framework APIs, standard libraries, debugging' },
  { id: 2, key: 'level_middle', label: 'Mid-Level', desc: 'Design patterns, concurrency, SQL tuning' },
  { id: 3, key: 'level_senior', label: 'Senior / Staff', desc: 'Under-the-hood runtime, memory trade-offs' }
]

export function formatSeniorityLevel(level: string | number) {
  if (typeof level === 'number') {
    return seniorityLevels[level] || seniorityLevels[3]
  }
  const levelStr = String(level).trim().toLowerCase()
  if (levelStr === 'fresher' || levelStr === '0') return seniorityLevels[0]
  if (levelStr === 'junior' || levelStr === '1') return seniorityLevels[1]
  if (levelStr === 'middle' || levelStr === 'mid' || levelStr === '2') return seniorityLevels[2]
  if (levelStr === 'senior' || levelStr === '3') return seniorityLevels[3]
  return seniorityLevels[3]
}
</script>

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
  Target,
  Check,
  AlertCircle,
  Swords
} from 'lucide-vue-next'
import { useReviewStore } from '~/stores/useReviewStore'
import { useInterviewQuizStore, type QuizQuestion } from '~/stores/useInterviewQuizStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { useProfileStore } from '~/stores/useProfileStore'
import { useLibraryStore } from '~/stores/useLibraryStore'
import BasePagination from '~/components/common/BasePagination.vue'
import AppSelect from '~/components/common/AppSelect.vue'
import { useMarkdownRenderer } from '~/composables/useMarkdownRenderer'

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const authStore = useAuthStore()
const profileStore = useProfileStore()
const quizStore = useInterviewQuizStore()
const libraryStore = useLibraryStore()
const reviewStore = useReviewStore()
const toast = useToast()

const sessionMistakes = computed(() => {
  return quizStore.questions.filter(q => {
    const sub = quizStore.submissions[q.id]
    return sub && !sub.isCorrect
  })
})

const pushedQuestionIds = ref<Set<string>>(new Set())
const pushingQuestionId = ref<string | null>(null)

async function handlePushToReview(questionId: string) {
  pushingQuestionId.value = questionId
  try {
    await reviewStore.createCardFromQuizMistake(questionId)
    pushedQuestionIds.value.add(questionId)
    toast.success(t('quiz.toast_pushed_to_sm2'))
  } catch (err: any) {
    toast.error(err.message || t('quiz.toast_push_sm2_failed'))
  } finally {
    pushingQuestionId.value = null
  }
}
const { render: renderMarkdownRaw, isHighlighterReady } = useMarkdownRenderer()

function renderMarkdown(raw: string | undefined | null): string {
  if (!raw) return ''
  const clean = raw.replace(/\\n/g, '\n')
  const _ = isHighlighterReady.value
  return renderMarkdownRaw(clean)
}

const selectedOptionIndex = ref<number | null>(null)
const customTopicInput = ref('')
const selectedLevel = ref(3) // 3 = Senior
const selectedCount = ref(5)
const isGrounded = ref(false)
const selectedBookId = ref<string | null>(null)
const groundedBookOptions = computed(() => [
  { value: '', label: t('quiz.any_book_in_library') },
  ...(libraryStore.books || []).map(b => ({ value: b.id, label: b.title }))
])
const computedQuickTopics = computed(() => {
  const list: string[] = []

  // 1. Prioritize active technical books from user's library
  if (libraryStore.books && libraryStore.books.length > 0) {
    libraryStore.books.slice(0, 3).forEach((b) => {
      if (b.title && !list.includes(b.title)) {
        list.push(b.title)
      }
    })
  }

  // 2. Prioritize target role specialization if set in profile
  const role = profileStore.profile?.targetRole?.toLowerCase() || ''
  if (role.includes('backend') || role.includes('.net') || role.includes('c#')) {
    const backendTopics = ['.NET 10 Runtime & Memory', 'PostgreSQL MVCC & Indexing', 'Distributed Systems & Raft']
    backendTopics.forEach((t) => {
      if (!list.includes(t)) list.push(t)
    })
  } else if (role.includes('frontend') || role.includes('react') || role.includes('web')) {
    const feTopics = ['React 19 Concurrency', 'CSS Engine & Layout Performance', 'Web Vitals & Browser Runtime']
    feTopics.forEach((t) => {
      if (!list.includes(t)) list.push(t)
    })
  } else if (role.includes('devops') || role.includes('cloud') || role.includes('infra')) {
    const devopsTopics = ['Kubernetes Scheduling & Pods', 'Docker Storage Drivers', 'Linux Kernel & cgroups']
    devopsTopics.forEach((t) => {
      if (!list.includes(t)) list.push(t)
    })
  }

  // 3. Fallback to foundational senior engineering topics
  const fallbacks = [
    'PostgreSQL MVCC & Indexing',
    'Distributed Consensus & Raft',
    'Redis Caching & Lock Strategies',
    'Docker & Kubernetes Architecture',
    '.NET 10 Internals & Memory',
    'React 19 Concurrency & Server Components',
    'Go Routines, Channels & Memory Model'
  ]
  for (const item of fallbacks) {
    if (list.length >= 7) break
    if (!list.includes(item)) {
      list.push(item)
    }
  }

  return list.slice(0, 7)
})

function onReviewPageChange(newPage: number) {
  router.replace({
    query: {
      ...route.query,
      tab: 'review',
      page: newPage > 1 ? newPage.toString() : undefined
    }
  })
  quizStore.fetchReviewQueue({ page: newPage, pageSize: quizStore.reviewPageSize })
  if (typeof window !== 'undefined') {
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

function handlePracticeCurrentBatch() {
  quizStore.startReviewSession(quizStore.reviewQueue)
}

async function handlePracticeAllMistakes() {
  try {
    const res = await quizStore.fetchReviewQueue({ page: 1, pageSize: 100 })
    if (res && res.questions && res.questions.length > 0) {
      quizStore.startReviewSession(res.questions)
    } else {
      quizStore.startReviewSession()
    }
  } catch {
    quizStore.startReviewSession()
  }
}
onMounted(async () => {
  if (!authStore.isLoggedIn) {
    return navigateTo({
      path: '/login',
      query: { redirect: '/quiz' }
    })
  }

  // Pre-fill topic from query param (e.g. from Reader)
  if (route.query.topic) {
    customTopicInput.value = (route.query.topic as string).trim()
  }

  // Pre-fill grounded book mode if query params present
  if (route.query.bookId) {
    selectedBookId.value = route.query.bookId as string
    isGrounded.value = true
  } else if (route.query.grounded === 'true') {
    isGrounded.value = true
  }

  if (route.query.tab === 'review') {
    quizStore.activeTab = 'review'
    const queryPage = route.query.page ? parseInt(route.query.page as string, 10) : 1
    const initialPage = isNaN(queryPage) || queryPage < 1 ? 1 : queryPage
    quizStore.fetchReviewQueue({ page: initialPage, pageSize: quizStore.reviewPageSize })
  }
  libraryStore.fetchBooks().catch((err) => {
    console.warn('Failed to load books for quiz dropdown:', err)
  })
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
  if (quizStore.isGenerating) return
  const chosenTopic = topic || customTopicInput.value || quickTopics[0]
  if (!chosenTopic.trim()) return

  selectedOptionIndex.value = null
  await quizStore.generateQuiz(
    chosenTopic.trim(),
    selectedLevel.value,
    selectedCount.value,
    null,
    locale.value,
    isGrounded.value ? (selectedBookId.value || null) : null,
    isGrounded.value
  )
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
      return 'border-brand-500 bg-brand-500/10 text-brand-600 dark:text-white ring-1 ring-brand-500/40 shadow-sm'
    }
    return 'border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-800 dark:text-slate-200 hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50/70 dark:hover:bg-white/[0.04]'
  }

  // Answered state
  const isCorrectOption = idx === currentSub.value?.correctOptionIndex
  const isSelectedByMe = idx === (selectedOptionIndex.value ?? currentQ.value?.lastSelectedOptionIndex)

  if (isCorrectOption) {
    return 'border-brand-500/60 bg-brand-50/80 dark:bg-brand-500/10 text-brand-900 dark:text-brand-300 ring-1 ring-brand-500/30'
  }
  if (isSelectedByMe && !currentSub.value?.isCorrect) {
    return 'border-rose-500/60 bg-rose-50/80 dark:bg-rose-500/10 text-rose-900 dark:text-rose-300 ring-1 ring-rose-500/30'
  }
  return 'opacity-40 border-slate-200/60 dark:border-white/[0.04] bg-slate-50/40 dark:bg-white/[0.01] text-slate-400 dark:text-slate-500'
}

// Readiness Tier Calculation for Bento Hero Card
const readinessInfo = computed(() => {
  const acc = quizStore.stats?.accuracyRate ?? 0
  if (acc >= 75) {
    return {
      labelKey: 'quiz.readiness_ready',
      badgeClass: 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20'
    }
  }
  if (acc >= 50) {
    return {
      labelKey: 'quiz.readiness_building',
      badgeClass: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20'
    }
  }
  return {
    labelKey: 'quiz.readiness_starting',
    badgeClass: 'bg-slate-100 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-white/[0.08]'
  }
})

// Spaced Mastery Gauge Calculation for Bento Card 2
const masteryRate = computed(() => {
  const total = quizStore.stats?.totalAnswered ?? 0
  const mastered = quizStore.stats?.masteredCount ?? 0
  if (total <= 0) return 0
  const rate = (mastered / total) * 100
  return Math.min(100, Math.max(0, Math.round(rate)))
})

const arcCircumference = 141.37
const strokeDashoffset = computed(() => {
  const percent = masteryRate.value / 100
  return arcCircumference - percent * arcCircumference
})

const masteryTierInfo = computed(() => {
  const rate = masteryRate.value
  if (rate <= 25) {
    return {
      labelKey: 'review.tier_starting',
      badgeClass: 'bg-slate-100 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-white/[0.08]',
      arcColor: 'text-slate-300 dark:text-white/[0.12]'
    }
  }
  if (rate <= 50) {
    return {
      labelKey: 'review.tier_building',
      badgeClass: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20',
      arcColor: 'text-amber-500'
    }
  }
  if (rate <= 75) {
    return {
      labelKey: 'review.tier_solid',
      badgeClass: 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border border-sky-500/20',
      arcColor: 'text-sky-500'
    }
  }
  return {
    labelKey: 'review.tier_mastered',
    badgeClass: 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20',
    arcColor: 'text-brand-500'
  }
})

// Seniority Progress Bar Styling for Bento Card 3
function getSeniorityColor(levelId: number) {
  switch (levelId) {
    case 0:
      return {
        bar: 'bg-brand-500',
        text: 'text-brand-600 dark:text-brand-400'
      }
    case 1:
      return {
        bar: 'bg-sky-500',
        text: 'text-sky-600 dark:text-sky-400'
      }
    case 2:
      return {
        bar: 'bg-amber-500',
        text: 'text-amber-600 dark:text-amber-400'
      }
    default:
      return {
        bar: 'bg-brand-500',
        text: 'text-brand-600 dark:text-brand-400'
      }
  }
}

// Topic Accuracy Badge Styling for Bento Card 4
function getTopicBadge(rate: number) {
  if (rate >= 80) {
    return {
      class: 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20'
    }
  }
  if (rate >= 50) {
    return {
      class: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20'
    }
  }
  return {
    class: 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20'
  }
}

defineExpose({
  formatSeniorityLevel,
  seniorityLevels
})
</script>

<template>
  <div class="max-w-6xl mx-auto px-4 sm:px-6 py-6 space-y-6">
    <!-- Header Section -->
    <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 border-b border-slate-200 dark:border-white/[0.08] pb-4">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
          <HelpCircle class="w-5 h-5 sm:w-6 sm:h-6" />
        </div>
        <div class="space-y-0.5">
          <h1 class="text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white">
            {{ $t('quiz.title') }}
          </h1>
          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium">
            {{ $t('quiz.subtitle') }}
          </p>
        </div>
      </div>

      <!-- Tab Buttons -->
      <div class="flex items-center gap-1.5 p-1 bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-2xl shrink-0 overflow-x-auto">
        <button
          data-testid="generate-tab-btn"
          @click="quizStore.activeTab = 'generate'"
          :class="[
            'px-3.5 py-2 rounded-xl text-sm font-semibold transition-colors border whitespace-nowrap shrink-0 flex items-center gap-1.5',
            quizStore.activeTab === 'generate'
              ? 'bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border-transparent dark:border-white/[0.06]'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          ]"
        >
          <Sparkles class="w-4 h-4" />
          {{ $t('quiz.tab_generate') }}
        </button>

        <button
          data-testid="arena-tab-btn"
          @click="quizStore.activeTab = 'arena'"
          :disabled="quizStore.questions.length === 0"
          :class="[
            'px-3.5 py-2 rounded-xl text-sm font-semibold transition-colors border whitespace-nowrap shrink-0 flex items-center gap-1.5',
            quizStore.questions.length === 0 ? 'opacity-40 cursor-not-allowed' : '',
            quizStore.activeTab === 'arena'
              ? 'bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border-transparent dark:border-white/[0.06]'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          ]"
        >
          <Swords class="w-4 h-4" />
          {{ $t('quiz.tab_arena') }}
          <span
            v-if="quizStore.questions.length > 0"
            class="px-1.5 py-0.5 rounded-md text-xs font-bold bg-brand-500/15 text-brand-600 dark:text-brand-400 border border-brand-500/20"
          >
            {{ quizStore.currentIndex + 1 }}/{{ quizStore.questions.length }}
          </span>
        </button>

        <button
          data-testid="review-tab-btn"
          @click="quizStore.activeTab = 'review'; quizStore.fetchReviewQueue()"
          :class="[
            'px-3.5 py-2 rounded-xl text-sm font-semibold transition-colors border whitespace-nowrap shrink-0 flex items-center gap-1.5',
            quizStore.activeTab === 'review'
              ? 'bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border-transparent dark:border-white/[0.06]'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          ]"
        >
          <RotateCcw class="w-4 h-4" />
          {{ $t('quiz.tab_review_queue') }}
          <span
            v-if="quizStore.reviewTotalCount > 0"
            class="px-1.5 py-0.5 rounded-md text-xs font-bold bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/20"
          >
            {{ quizStore.reviewTotalCount }}
          </span>
        </button>

        <button
          data-testid="stats-tab-btn"
          @click="quizStore.activeTab = 'stats'; quizStore.fetchStats()"
          :class="[
            'px-3.5 py-2 rounded-xl text-sm font-semibold transition-colors border whitespace-nowrap shrink-0 flex items-center gap-1.5',
            quizStore.activeTab === 'stats'
              ? 'bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border-transparent dark:border-white/[0.06]'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          ]"
        >
          <BarChart3 class="w-4 h-4" />
          {{ $t('quiz.tab_stats') }}
        </button>
      </div>
    </div>

    <!-- TAB 1: GENERATE QUIZ (BENTO STUDIO) -->
    <div v-if="quizStore.activeTab === 'generate'" class="grid grid-cols-1 lg:grid-cols-2 gap-6 items-stretch">
      <!-- LEFT BENTO: Topic & Context Hub -->
      <div class="glass-card p-5 sm:p-7 space-y-6 flex flex-col justify-between">
        <div class="space-y-6">
          <!-- Topic Selection -->
          <div class="space-y-2.5">
            <label class="block text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200">
              {{ $t('quiz.topic_label') }}
            </label>
            <input
              v-model="customTopicInput"
              type="text"
              :placeholder="$t('quiz.topic_placeholder')"
              class="w-full px-4 py-3 rounded-xl border border-slate-300 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 text-sm sm:text-base transition-all shadow-sm"
              @keyup.enter="handleGenerateQuiz()"
            />

            <!-- Quick Topic Chips -->
            <div class="space-y-2 pt-1">
              <span class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                {{ $t('quiz.quick_topics') }}
              </span>
              <div class="flex flex-wrap gap-2">
                <button
                  v-for="topic in computedQuickTopics"
                  :key="topic"
                  @click="customTopicInput = topic"
                  :class="[
                    'px-3 py-1.5 rounded-lg text-xs sm:text-sm font-medium border transition-colors whitespace-nowrap shrink-0 max-w-[220px] truncate',
                    customTopicInput === topic
                      ? 'border-brand-500 bg-brand-500/10 text-brand-600 dark:text-brand-300 font-bold shadow-sm'
                      : 'bg-slate-100 dark:bg-white/[0.03] hover:bg-brand-50 dark:hover:bg-white/[0.06] text-slate-700 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-300 border-slate-200 dark:border-white/[0.06]'
                  ]"
                  :title="topic"
                >
                  {{ topic }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Grounded in Book Toggle Card -->
        <div class="p-4 sm:p-5 rounded-2xl border border-slate-200/80 dark:border-white/[0.06] bg-slate-50/70 dark:bg-white/[0.02] space-y-3 mt-4">
          <div class="flex items-center justify-between gap-3">
            <div class="flex items-center gap-3">
              <div class="w-8 h-8 rounded-xl bg-brand-500/10 dark:bg-brand-500/20 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
                <BookOpen class="w-4 h-4" />
              </div>
              <div>
                <span class="text-sm sm:text-base font-bold text-slate-900 dark:text-white block">
                  {{ $t('quiz.grounded_toggle') }}
                </span>
                <span class="text-xs text-slate-500 dark:text-slate-400">
                  {{ $t('quiz.grounded_desc') }}
                </span>
              </div>
            </div>
            <button
              type="button"
              @click="isGrounded = !isGrounded"
              :class="[
                'relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-brand-500',
                isGrounded ? 'bg-brand-600' : 'bg-slate-300 dark:bg-canvas-elevated'
              ]"
            >
              <span
                :class="[
                  'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out',
                  isGrounded ? 'translate-x-5' : 'translate-x-0'
                ]"
              />
            </button>
          </div>

          <!-- Select Book dropdown when Grounded is active -->
          <div v-if="isGrounded" class="pt-3 border-t border-slate-200/80 dark:border-white/[0.06] space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">
              {{ $t('quiz.select_book') }}
            </label>
            <AppSelect
              v-model="selectedBookId"
              :options="groundedBookOptions"
              :icon="BookOpen"
              :aria-label="$t('quiz.select_book')"
            />
          </div>
        </div>
      </div>

      <!-- RIGHT BENTO: Seniority & Generation Controls -->
      <div class="glass-card p-5 sm:p-7 space-y-6 flex flex-col justify-between">
        <div class="space-y-6">
          <!-- Level Picker (Purely Typographic, 2x2 Grid, No Emojis/Icons) -->
          <div class="space-y-2.5">
            <label class="block text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200">
              {{ $t('quiz.level_label') }}
            </label>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <button
                v-for="lvl in seniorityLevels"
                :key="lvl.id"
                @click="selectedLevel = lvl.id"
                :class="[
                  'p-3.5 sm:p-4 rounded-xl border text-left transition-all relative flex flex-col justify-between',
                  selectedLevel === lvl.id
                    ? 'border-brand-500 bg-brand-500/10 text-brand-900 dark:text-white ring-1 ring-brand-500/30 shadow-sm'
                    : 'border-slate-200/80 dark:border-white/[0.06] bg-white/60 dark:bg-white/[0.02] hover:border-slate-300 dark:hover:border-white/[0.15]'
                ]"
              >
                <div class="flex items-center justify-between mb-1.5 gap-2">
                  <span class="font-bold text-sm sm:text-base text-slate-900 dark:text-white truncate">{{ $t(`quiz.${lvl.key}`) }}</span>
                  <span
                    v-if="selectedLevel === lvl.id"
                    class="w-2 h-2 rounded-full bg-brand-500 shrink-0"
                  ></span>
                </div>
                <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-normal leading-relaxed">
                  {{ lvl.desc }}
                </p>
              </button>
            </div>
          </div>

          <!-- Question Count (3-Tier Segmented Pill Container) -->
          <div class="space-y-2.5">
            <label class="block text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200">
              {{ $t('quiz.count_label') }}
            </label>
            <div class="flex items-center p-1 rounded-xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08]">
              <button
                @click="selectedCount = 3"
                :class="[
                  'flex-1 py-2 px-2.5 rounded-lg text-xs sm:text-sm font-semibold transition-all text-center whitespace-nowrap shrink-0',
                  selectedCount === 3
                    ? 'bg-brand-600 text-white font-bold shadow-sm'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                ]"
              >
                {{ $t('quiz.count_3') }}
              </button>
              <button
                @click="selectedCount = 5"
                :class="[
                  'flex-1 py-2 px-2.5 rounded-lg text-xs sm:text-sm font-semibold transition-all text-center whitespace-nowrap shrink-0',
                  selectedCount === 5
                    ? 'bg-brand-600 text-white font-bold shadow-sm'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                ]"
              >
                {{ $t('quiz.count_5') }}
              </button>
              <button
                @click="selectedCount = 10"
                :class="[
                  'flex-1 py-2 px-2.5 rounded-lg text-xs sm:text-sm font-semibold transition-all text-center whitespace-nowrap shrink-0',
                  selectedCount === 10
                    ? 'bg-brand-600 text-white font-bold shadow-sm'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                ]"
              >
                {{ $t('quiz.count_10') }}
              </button>
            </div>
          </div>
        </div>

        <!-- Generate Button -->
        <div class="pt-4">
          <button
            data-testid="generate-quiz-btn"
            @click="handleGenerateQuiz()"
            :disabled="quizStore.isGenerating"
            class="w-full px-6 py-3.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm sm:text-base shadow-md shadow-brand-500/20 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2.5 transition-all cursor-pointer whitespace-nowrap shrink-0"
          >
            <Loader2 v-if="quizStore.isGenerating" class="w-5 h-5 animate-spin shrink-0" />
            <Sparkles v-else class="w-5 h-5 shrink-0" />
            <span>{{ quizStore.isGenerating ? $t('quiz.generating_loader') : $t('quiz.btn_generate') }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- TAB 2: QUIZ ARENA -->
    <div v-if="quizStore.activeTab === 'arena' && currentQ" class="space-y-6">
      <!-- Stepper & Topic Bar -->
      <div class="glass-card p-4 sm:p-5 space-y-3">
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
            <span class="px-2.5 py-1 rounded-lg text-xs font-bold bg-slate-100 dark:bg-white/[0.04] text-slate-700 dark:text-slate-300 border border-slate-200/60 dark:border-white/[0.06]">
              {{ formatSeniorityLevel(currentQ.level).label }}
            </span>
            <span
              v-if="currentQ.isMastered"
              class="px-2.5 py-1 rounded-lg text-xs font-bold bg-brand-500/15 text-brand-600 dark:text-brand-400 border border-brand-500/30 flex items-center gap-1"
            >
              <CheckCircle2 class="w-3.5 h-3.5" />
              Mastered
            </span>
          </div>
        </div>

        <!-- Progress Bar -->
        <div class="w-full h-2 bg-slate-100 dark:bg-canvas-elevated rounded-full overflow-hidden">
          <div
            class="h-full bg-gradient-to-r from-brand-600 to-brand-400 transition-all duration-300"
            :style="{ width: `${quizStore.progressPercentage}%` }"
          ></div>
        </div>
      </div>

      <!-- Question Card -->
      <div class="glass-card p-5 sm:p-7 space-y-6">
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
                quizStore.isCurrentAnswered
                  ? (idx === currentSub?.correctOptionIndex
                      ? 'bg-brand-600 text-white'
                      : (idx === (selectedOptionIndex ?? currentQ.lastSelectedOptionIndex) && !currentSub?.isCorrect
                          ? 'bg-rose-600 text-white'
                          : 'bg-slate-100 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 border border-slate-200/60 dark:border-white/[0.08]'))
                  : (selectedOptionIndex === idx
                      ? 'bg-brand-600 text-white'
                      : 'bg-slate-100 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 border border-slate-200/60 dark:border-white/[0.08]')
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
            class="px-6 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm sm:text-base shadow-md shadow-brand-500/20 disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-2 transition-all whitespace-nowrap shrink-0 cursor-pointer"
          >
            <Loader2 v-if="quizStore.isSubmitting" class="w-4 h-4 animate-spin" />
            <span>{{ quizStore.isSubmitting ? $t('quiz.submitting_loader') : $t('quiz.btn_submit_choice') }}</span>
          </button>
        </div>

        <!-- Explanation & Feedback (Shown after submitting) -->
        <div v-if="quizStore.isCurrentAnswered && currentSub" class="space-y-4 pt-4 border-t border-slate-200/80 dark:border-white/[0.06]">
          <!-- Banner -->
          <div
            :class="[
              'p-4 rounded-xl border flex items-center gap-3',
              currentSub.isCorrect
                ? 'bg-brand-500/10 border-brand-500/30 text-brand-700 dark:text-brand-300'
                : 'bg-rose-500/10 border-rose-500/30 text-rose-700 dark:text-rose-300'
            ]"
          >
            <CheckCircle2 v-if="currentSub.isCorrect" class="w-5 h-5 text-brand-500 shrink-0" />
            <XCircle v-else class="w-5 h-5 text-rose-500 shrink-0" />
            <span class="font-bold text-sm sm:text-base">
              {{ currentSub.isCorrect ? $t('quiz.correct_banner') : $t('quiz.incorrect_banner') }}
            </span>
          </div>

          <!-- Deep-dive Markdown -->
          <div class="bg-slate-50 dark:bg-canvas-subtle rounded-xl p-5 border border-slate-200/80 dark:border-white/[0.06] space-y-2">
            <h3 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {{ $t('quiz.explanation_header') }}
            </h3>
            <div
              class="prose prose-sm sm:prose-base dark:prose-invert max-w-none text-slate-700 dark:text-slate-300 leading-relaxed min-w-0 max-w-full"
              v-html="renderedExplanationHtml"
            ></div>
          </div>

          <!-- Next / Finish Control -->
          <div class="flex items-center justify-between pt-2">
            <button
              @click="handlePrevQuestion()"
              :disabled="quizStore.currentIndex === 0"
              class="px-4 py-2.5 rounded-xl border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-white/[0.04] text-slate-700 dark:text-slate-300 font-semibold text-sm disabled:opacity-30 disabled:cursor-not-allowed flex items-center gap-1.5 transition-all whitespace-nowrap shrink-0 cursor-pointer"
            >
              <ArrowLeft class="w-4 h-4" />
              {{ $t('quiz.btn_prev') }}
            </button>

            <button
              @click="handleNextQuestion()"
              class="px-6 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm sm:text-base shadow-sm flex items-center gap-2 transition-all whitespace-nowrap shrink-0 cursor-pointer"
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
      <div class="glass-card p-6 sm:p-8 text-center space-y-6">
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
        <div class="inline-block p-6 rounded-2xl bg-slate-50 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.06]">
          <span class="text-3xl sm:text-4xl font-black text-brand-600 dark:text-brand-400">
            {{ quizStore.sessionScore.correct }} / {{ quizStore.sessionScore.total }}
          </span>
          <p class="text-sm font-bold text-slate-500 mt-1">
            {{ quizStore.sessionScore.percentage }}% {{ $t('quiz.stats_accuracy') }}
          </p>
        </div>

        <!-- Actions -->
        <div class="flex flex-wrap items-center justify-center gap-3 pt-2">
          <button
            @click="handleGenerateQuiz(quizStore.currentTopic)"
            :disabled="quizStore.isGenerating"
            class="px-5 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-sm disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 transition-all whitespace-nowrap shrink-0 cursor-pointer"
          >
            <Loader2 v-if="quizStore.isGenerating" class="w-4 h-4 animate-spin" />
            <Sparkles v-else class="w-4 h-4" />
            <span>{{ quizStore.isGenerating ? $t('quiz.generating_loader') : $t('quiz.btn_generate_more') }}</span>
          </button>

          <button
            v-if="quizStore.sessionScore.correct < quizStore.sessionScore.total"
            @click="handleRetryMistakes()"
            :disabled="quizStore.isGenerating"
            class="px-5 py-3 rounded-xl bg-amber-500 hover:bg-amber-600 text-white font-bold text-sm shadow-sm disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 transition-all whitespace-nowrap shrink-0 cursor-pointer"
          >
            <RotateCcw class="w-4 h-4" />
            {{ $t('quiz.btn_retry_mistakes', { count: quizStore.sessionScore.total - quizStore.sessionScore.correct }) }}
          </button>

          <button
            @click="quizStore.resetSession()"
            :disabled="quizStore.isGenerating"
            class="px-5 py-3 rounded-xl border border-slate-300 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-white/[0.04] text-slate-700 dark:text-slate-300 font-bold text-sm disabled:opacity-50 disabled:cursor-not-allowed transition-all whitespace-nowrap shrink-0 cursor-pointer"
          >
            {{ $t('quiz.btn_choose_new') }}
          </button>
        </div>
      </div>
        <!-- Session Mistakes List with Push to SM-2 -->
        <div v-if="sessionMistakes.length > 0" class="text-left pt-6 border-t border-slate-200/80 dark:border-white/[0.06] space-y-3">
          <h3 class="text-sm font-bold text-slate-900 dark:text-white flex items-center gap-2">
            <AlertCircle class="w-4 h-4 text-rose-500" />
            <span>{{ $t('quiz.mistakes_to_review') }} ({{ sessionMistakes.length }})</span>
          </h3>
          <div class="space-y-3">
            <div
              v-for="q in sessionMistakes"
              :key="q.id"
              class="p-4 rounded-xl bg-slate-50 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.06] space-y-2"
            >
              <div class="flex items-center justify-between text-xs">
                <span class="font-bold text-brand-600 dark:text-brand-400">{{ q.topic }}</span>
                <span class="text-rose-500 font-semibold">{{ $t('quiz.incorrect_badge') }}</span>
              </div>
              <p class="text-sm font-bold text-slate-900 dark:text-white">
                {{ q.questionText }}
              </p>
              <div class="flex items-center justify-end pt-1">
                <button
                  @click="handlePushToReview(q.id)"
                  :disabled="pushedQuestionIds.has(q.id) || pushingQuestionId === q.id"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-bold transition-all shadow-sm disabled:opacity-60 cursor-pointer"
                  :class="pushedQuestionIds.has(q.id)
                    ? 'bg-brand-50 dark:bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-200 dark:border-brand-500/20'
                    : 'bg-brand-600 hover:bg-brand-500 text-white'"
                >
                  <Check v-if="pushedQuestionIds.has(q.id)" class="w-3.5 h-3.5" />
                  <Loader2 v-else-if="pushingQuestionId === q.id" class="w-3.5 h-3.5 animate-spin" />
                  <RotateCcw v-else class="w-3.5 h-3.5" />
                  <span>{{ pushedQuestionIds.has(q.id) ? $t('quiz.btn_pushed_sm2') : $t('quiz.btn_push_sm2') }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
    </div>

    <!-- TAB 4: MISTAKE REVIEW QUEUE -->
    <div v-if="quizStore.activeTab === 'review'" class="space-y-6">
      <div v-if="quizStore.reviewQueue.length === 0" class="glass-card p-8 text-center space-y-4">
        <div class="inline-flex p-4 rounded-full bg-brand-500/10 text-brand-500 border border-brand-500/20">
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
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <span class="text-sm font-bold text-slate-700 dark:text-slate-300">
            {{ $t('quiz.review_count_badge', { count: quizStore.reviewTotalCount }) }}
          </span>
          <div class="flex items-center gap-2 flex-wrap">
            <button
              @click="handlePracticeCurrentBatch"
              class="px-4 py-2 sm:py-2.5 rounded-xl bg-brand-500/10 hover:bg-brand-500/20 text-brand-700 dark:text-brand-300 border border-brand-500/20 font-bold text-xs sm:text-sm shadow-sm flex items-center gap-1.5 transition-all whitespace-nowrap shrink-0 cursor-pointer"
            >
              <RotateCcw class="w-4 h-4" />
              {{ $t('quiz.practice_current_batch', { count: quizStore.reviewQueue.length }) }}
            </button>
            <button
              @click="handlePracticeAllMistakes"
              class="px-4 py-2 sm:py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-sm flex items-center gap-1.5 transition-all whitespace-nowrap shrink-0 cursor-pointer"
            >
              <RotateCcw class="w-4 h-4" />
              {{ $t('quiz.practice_all_mistakes', { count: quizStore.reviewTotalCount }) }}
            </button>
          </div>
        </div>

        <!-- Question Cards List -->
        <div class="space-y-3">
          <div
            v-for="q in quizStore.reviewQueue"
            :key="q.id"
            class="glass-card p-5 space-y-2 hover:border-slate-300 dark:hover:border-white/[0.15] transition-all"
          >
            <div class="flex items-center justify-between text-xs">
              <span class="font-bold text-brand-600 dark:text-brand-400">{{ q.topic }}</span>
              <span class="text-rose-500 font-semibold">{{ $t('quiz.incorrect_attempts', { count: q.incorrectCount }) }}</span>
            </div>
            <p class="text-sm sm:text-base font-bold text-slate-900 dark:text-white line-clamp-2">
              {{ q.questionText }}
            </p>
            <div class="flex items-center justify-end pt-1">
              <button
                @click="handlePushToReview(q.id)"
                :disabled="pushedQuestionIds.has(q.id) || pushingQuestionId === q.id"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-bold transition-all shadow-sm disabled:opacity-60 cursor-pointer"
                :class="pushedQuestionIds.has(q.id)
                  ? 'bg-brand-50 dark:bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-200 dark:border-brand-500/20'
                  : 'bg-brand-600 hover:bg-brand-500 text-white'"
              >
                <Check v-if="pushedQuestionIds.has(q.id)" class="w-3.5 h-3.5" />
                <Loader2 v-else-if="pushingQuestionId === q.id" class="w-3.5 h-3.5 animate-spin" />
                <RotateCcw v-else class="w-3.5 h-3.5" />
                <span>{{ pushedQuestionIds.has(q.id) ? $t('quiz.btn_pushed_sm2') : $t('quiz.btn_push_sm2') }}</span>
              </button>
            </div>
          </div>
        </div>
        <!-- Pagination -->
        <BasePagination
          :current-page="quizStore.reviewPage"
          :total-pages="quizStore.reviewTotalPages"
          :total-count="quizStore.reviewTotalCount"
          :page-size="quizStore.reviewPageSize"
          show-summary
          @change="onReviewPageChange"
        />
      </div>
    </div>

    <!-- TAB 5: MASTERY STATS -->
    <div v-if="quizStore.activeTab === 'stats' && quizStore.stats" class="space-y-6">
      <!-- 4-Card Bento Grid Dashboard -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
        <!-- Bento Card 1: Hero Performance Card -->
        <div class="glass-card p-5 sm:p-6 flex flex-col justify-between space-y-5">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2.5">
              <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
                <Target class="w-4 h-4" />
              </div>
              <div>
                <h3 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                  {{ $t('quiz.bento_hero_title') }}
                </h3>
                <p class="text-xs text-slate-500 dark:text-slate-400 font-medium">
                  {{ quizStore.stats.totalAnswered }} {{ $t('quiz.stats_total').toLowerCase() }}
                </p>
              </div>
            </div>

            <!-- Readiness Badge -->
            <span
              :class="[
                'px-2.5 py-1 rounded-full text-[11px] font-bold border transition-colors whitespace-nowrap shrink-0',
                readinessInfo.badgeClass
              ]"
            >
              {{ $t(readinessInfo.labelKey) }}
            </span>
          </div>

          <div class="flex flex-col sm:flex-row sm:items-end justify-between gap-4 pt-1">
            <div class="space-y-1">
              <span class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                {{ $t('quiz.stats_accuracy') }}
              </span>
              <div class="flex items-baseline gap-2">
                <span class="text-3xl sm:text-4xl font-black text-brand-600 dark:text-brand-400 tracking-tight">
                  {{ quizStore.stats.accuracyRate }}%
                </span>
                <span class="text-xs font-semibold text-slate-500 dark:text-slate-400">
                  ({{ quizStore.stats.masteredCount }}/{{ quizStore.stats.totalAnswered }} {{ $t('quiz.stats_mastered').toLowerCase() }})
                </span>
              </div>
            </div>

            <!-- 1-Click Mistake Review CTA -->
            <button
              @click="quizStore.activeTab = 'review'"
              :disabled="quizStore.stats.reviewQueueCount === 0"
              class="inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-2xl bg-brand-600 hover:bg-brand-500 disabled:opacity-50 disabled:hover:bg-brand-600 text-white font-semibold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all active:scale-95 whitespace-nowrap shrink-0 cursor-pointer"
            >
              <RotateCcw class="w-4 h-4" />
              <span>{{ $t('quiz.btn_review_mistakes', { count: quizStore.stats.reviewQueueCount }) }}</span>
            </button>
          </div>
        </div>

        <!-- Bento Card 2: Spaced Mastery Gauge Card -->
        <div class="glass-card text-slate-900 dark:text-white p-5 sm:p-6 flex flex-col justify-between">
          <!-- Header -->
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2.5">
              <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
                <Award class="w-4 h-4" />
              </div>
              <div>
                <h3 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                  {{ $t('quiz.bento_mastery_title') }}
                </h3>
                <p class="text-xs text-slate-500 dark:text-slate-400 font-medium">
                  {{ quizStore.stats.masteredCount }} / {{ quizStore.stats.totalAnswered }} {{ $t('quiz.stats_mastered').toLowerCase() }}
                </p>
              </div>
            </div>

            <!-- Dynamic Proficiency Tier Badge -->
            <span
              :class="[
                'px-2.5 py-1 rounded-full text-[11px] font-bold border transition-colors whitespace-nowrap shrink-0',
                masteryTierInfo.badgeClass
              ]"
            >
              {{ $t(masteryTierInfo.labelKey) }}
            </span>
          </div>

          <!-- Semi-Circular Radial Arc Gauge -->
          <div class="relative flex flex-col items-center justify-center my-auto pt-3">
            <div class="relative w-40 h-24 flex items-end justify-center">
              <svg
                class="w-full h-full overflow-visible"
                viewBox="0 0 120 70"
                aria-label="Quiz mastery rate gauge"
              >
                <!-- Background Arc (180deg) -->
                <path
                  d="M 15 60 A 45 45 0 0 1 105 60"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="10"
                  stroke-linecap="round"
                  class="text-slate-100 dark:text-white/[0.08]"
                />

                <!-- Foreground Value Arc -->
                <path
                  d="M 15 60 A 45 45 0 0 1 105 60"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="10"
                  stroke-linecap="round"
                  :stroke-dasharray="arcCircumference"
                  :stroke-dashoffset="strokeDashoffset"
                  :class="['transition-all duration-700 ease-out', masteryTierInfo.arcColor]"
                />
              </svg>

              <!-- Center Numerical Label -->
              <div class="absolute inset-x-0 bottom-0 text-center flex flex-col items-center pointer-events-none">
                <span class="text-2xl sm:text-3xl font-black tracking-tight text-slate-900 dark:text-white leading-none">
                  {{ masteryRate }}%
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Bento Card 3: Seniority Matrix Card -->
        <div class="glass-card p-5 sm:p-6 space-y-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2.5">
              <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
                <TrendingUp class="w-4 h-4" />
              </div>
              <div>
                <h3 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                  {{ $t('quiz.bento_seniority_title') }}
                </h3>
                <p class="text-xs text-slate-500 dark:text-slate-400 font-medium">
                  {{ $t('quiz.stats_level_breakdown') }}
                </p>
              </div>
            </div>
          </div>

          <div class="space-y-3.5 pt-1">
            <div
              v-for="lvl in quizStore.stats.levelBreakdown"
              :key="String(lvl.level)"
              class="space-y-1.5"
            >
              <div class="flex items-center justify-between text-xs sm:text-sm">
                <div class="flex items-center gap-1.5 min-w-0">
                  <span class="font-bold text-slate-800 dark:text-slate-200 truncate">
                    {{ formatSeniorityLevel(lvl.level).label }}
                  </span>
                  <span class="text-xs text-slate-400 dark:text-slate-500 truncate hidden sm:inline">
                    • {{ formatSeniorityLevel(lvl.level).desc }}
                  </span>
                  <span class="text-xs text-slate-400 dark:text-slate-500 whitespace-nowrap">
                    ({{ lvl.masteredCount }}/{{ lvl.answeredCount }})
                  </span>
                </div>
                <span :class="['font-black whitespace-nowrap shrink-0 ml-2', getSeniorityColor(formatSeniorityLevel(lvl.level).id).text]">
                  {{ lvl.accuracyRate }}%
                </span>
              </div>
              <div class="w-full h-2.5 bg-slate-100 dark:bg-canvas-elevated rounded-full overflow-hidden">
                <div
                  :class="['h-full rounded-full transition-all duration-500', getSeniorityColor(formatSeniorityLevel(lvl.level).id).bar]"
                  :style="{ width: `${lvl.accuracyRate}%` }"
                ></div>
              </div>
            </div>
          </div>
        </div>

        <!-- Bento Card 4: Topic Strengths & Weaknesses Radar Card -->
        <div class="glass-card p-5 sm:p-6 space-y-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2.5">
              <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
                <BarChart3 class="w-4 h-4" />
              </div>
              <div>
                <h3 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                  {{ $t('quiz.bento_topic_title') }}
                </h3>
                <p class="text-xs text-slate-500 dark:text-slate-400 font-medium">
                  {{ quizStore.stats.topicBreakdown?.length || 0 }} {{ $t('insights.all_categories').toLowerCase() }}
                </p>
              </div>
            </div>
          </div>

          <!-- Topic List -->
          <div v-if="quizStore.stats.topicBreakdown?.length" class="space-y-2.5 pt-1 max-h-72 overflow-y-auto pr-1">
            <div
              v-for="topic in quizStore.stats.topicBreakdown"
              :key="topic.topic"
              class="flex items-center justify-between p-2.5 rounded-xl bg-slate-50 dark:bg-canvas-subtle border border-slate-100 dark:border-white/[0.06] gap-2"
            >
              <div class="min-w-0 flex-1">
                <p class="text-xs sm:text-sm font-semibold text-slate-800 dark:text-slate-200 truncate">
                  {{ topic.topic }}
                </p>
                <p class="text-[11px] text-slate-400 dark:text-slate-500">
                  {{ topic.answeredCount }} {{ $t('quiz.stats_total').toLowerCase() }} • {{ topic.masteredCount }} {{ $t('quiz.stats_mastered').toLowerCase() }}
                </p>
              </div>

              <span
                :class="[
                  'px-2.5 py-1 rounded-full text-xs font-bold border whitespace-nowrap shrink-0',
                  getTopicBadge(topic.accuracyRate).class
                ]"
              >
                {{ topic.accuracyRate }}%
              </span>
            </div>
          </div>

          <div v-else class="text-center py-8 text-slate-400 dark:text-slate-500 text-xs">
            {{ $t('insights.empty_title') }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
