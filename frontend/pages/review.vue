<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useEventListener, useDebounceFn } from '@vueuse/core'
import {
  CheckCircle,
  Sparkles,
  Layers,
  Clock,
  Search,
  X,
  Pencil,
  RotateCcw,
  Trash2,
  Library,
  BookOpen,
  ChevronLeft,
  ChevronRight,
  Eye,
  EyeOff,
  AlertTriangle,
  Check,
  HelpCircle,
  FileText,
  SlidersHorizontal,
  Gauge,
  Keyboard,
  Flame,
  Activity,
  Key
} from 'lucide-vue-next'
import confetti from 'canvas-confetti'
import StudioLayout from '~/components/layout/StudioLayout.vue'
import FlashcardHeroCard from '~/components/review/FlashcardHeroCard.vue'
import MasteryGaugeCard from '~/components/review/MasteryGaugeCard.vue'
import ReviewForecastChart from '~/components/review/ReviewForecastChart.vue'
import AdvancedFilterModal from '~/components/review/AdvancedFilterModal.vue'
import FlashcardBentoCard from '~/components/review/FlashcardBentoCard.vue'
import FlashcardDeck from '~/components/review/FlashcardDeck.vue'
import BasePagination from '~/components/common/BasePagination.vue'
import { useReviewStore, type ReviewCard, type ReviewFilterState } from '~/stores/useReviewStore'
import MarkdownIt from 'markdown-it'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { formatError } = useApiError()
const reviewStore = useReviewStore()
const toast = useToast()
const md = new MarkdownIt({ html: false, linkify: true, typographer: true })

function renderMarkdown(raw: string | undefined | null): string {
  if (!raw) return ''
  const clean = raw.replace(/\\n/g, '\n')
  return md.render(clean)
}

// Navigation Tab State
const activeTab = ref<'session' | 'management'>('session')

// Tab 1: Review Session Logic
const initialSessionTotal = ref(0)
const currentCard = computed(() => {
  return reviewStore.cards[0] || null
})

// Keep track of initial total due cards for progress calculation
watch(
  () => reviewStore.cards.length,
  (len) => {
    if (len > initialSessionTotal.value) {
      initialSessionTotal.value = len
    }
  },
  { immediate: true }
)

const sessionProgress = computed(() => {
  const total = Math.max(initialSessionTotal.value, reviewStore.cards.length)
  if (total === 0) return { reviewed: 0, total: 0, percentage: 100 }
  const reviewed = Math.max(0, total - reviewStore.cards.length)
  const percentage = Math.min(100, Math.round((reviewed / total) * 100))
  return { reviewed, total, percentage }
})
const cleanTopicTitle = computed(() => {
  if (!currentCard.value) return t('review.title')
  const title = currentCard.value.topicTitle?.trim() || ''
  if (!title) return t('review.title')
  if (title.length > 50) {
    if (title.toLowerCase().includes('vue 3') || title.toLowerCase().includes('proxy')) {
      return 'Vue 3 Reactivity & JavaScript Proxy Engine'
    }
    if (title.toLowerCase().includes('separation')) {
      return 'Separation of Concerns in ASP.NET Core'
    }
    if (title.toLowerCase().includes('mvcc') || title.toLowerCase().includes('vacuum')) {
      return 'PostgreSQL MVCC & VACUUM Internals'
    }
    return title.split('?')[0].slice(0, 50).trim()
  }
  return title
})

const cleanSourceSubtitle = computed(() => {
  if (!currentCard.value) return t('review.subtitle')
  const card = currentCard.value as any
  if (card.sourceBook && card.sourceContext) {
    return `${card.sourceBook} • ${card.sourceContext}`
  }
  if (cardSourceContext.value) {
    return `${cardSourceContext.value.bookTitle} • ${cardSourceContext.value.chapterTitle}`
  }
  return 'Technical Architecture Monograph • Spaced Practice'
})

const cardMasteryBadge = computed(() => {
  if (!currentCard.value) return { label: '', classes: '' }
  switch (currentCard.value.status) {
    case 1:
      return {
        label: t('review.status_reviewing'),
        classes: 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border border-sky-500/20'
      }
    case 2:
      return {
        label: t('review.status_mastered'),
        classes: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20'
      }
    case 0:
    default:
      return {
        label: t('review.status_learning'),
        classes: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20'
      }
  }
})

const cardSourceContext = computed(() => {
  if (!currentCard.value) return null
  const card = currentCard.value as any
  const bookTitle = card.bookTitle || card.sourceBookTitle || (card.sourceType === 1 ? 'Reading Highlight' : null)
  const chapterTitle = card.chapterTitle || card.sourceChapterTitle || (card.sourceType === 1 && card.topicTitle ? card.topicTitle : null)
  if (!bookTitle && !chapterTitle) return null
  return {
    bookTitle: bookTitle || 'Technical Monograph',
    chapterTitle: chapterTitle || card.topicTitle || 'Reference Chapter'
  }
})

async function onGrade(score: number) {
  if (!currentCard.value) return

  await reviewStore.gradeCard(currentCard.value.id, score)

  if (reviewStore.cards.length === 0) {
    confetti({
      particleCount: 100,
      spread: 70,
      origin: { y: 0.6 }
    })
  }
}

// Tab 2: Deck Management State
const searchQuery = ref('')
const searchInputRef = ref<HTMLInputElement | null>(null)
const selectedStatus = ref<number | null>(null)
const selectedSource = ref<number | null>(null)
const selectedUrgency = ref<string | null>(null)
const selectedSortBy = ref<string | null>(null)
const isAdvancedFilterOpen = ref(false)

const activeFilterCount = computed(() => {
  let count = 0
  if (selectedStatus.value !== null) count++
  if (selectedSource.value !== null) count++
  if (selectedUrgency.value !== null) count++
  if (selectedSortBy.value !== null) count++
  return count
})

const totalPages = computed(() => {
  return Math.max(1, Math.ceil(reviewStore.deckTotalCount / reviewStore.deckPageSize))
})

async function fetchDeck(page = 1) {
  try {
    await reviewStore.fetchDeckCards({
      page,
      pageSize: reviewStore.deckPageSize,
      search: searchQuery.value.trim() || undefined,
      status: selectedStatus.value !== null ? selectedStatus.value : undefined,
      sourceType: selectedSource.value !== null ? selectedSource.value : undefined
    })
  } catch (err: unknown) {
    toast.error(formatError(err, 'review.toast_update_error'))
  }
}

function onDeckPageChange(newPage: number) {
  router.replace({
    query: {
      ...route.query,
      page: newPage > 1 ? newPage.toString() : undefined
    }
  })
  fetchDeck(newPage)
  if (typeof window !== 'undefined') {
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}
const debouncedFetchDeck = useDebounceFn(() => {
  fetchDeck(1)
}, 300)

watch(searchQuery, () => {
  debouncedFetchDeck()
})

function setQuickFilter(filter: 'all' | 'due' | 'mastered') {
  if (filter === 'all') {
    selectedStatus.value = null
    selectedUrgency.value = null
  } else if (filter === 'due') {
    selectedStatus.value = null
    selectedUrgency.value = 'due'
  } else if (filter === 'mastered') {
    selectedStatus.value = 2
    selectedUrgency.value = null
  }
  fetchDeck(1)
}

function onApplyAdvancedFilters(filters: ReviewFilterState) {
  selectedStatus.value = filters.status
  selectedSource.value = filters.sourceType
  selectedUrgency.value = filters.urgency
  selectedSortBy.value = filters.sortBy
  fetchDeck(1)
}

function onResetAdvancedFilters() {
  selectedStatus.value = null
  selectedSource.value = null
  selectedUrgency.value = null
  selectedSortBy.value = null
  fetchDeck(1)
}

function isDueToday(dateStr: string): boolean {
  if (!dateStr) return false
  const today = new Date().toISOString().slice(0, 10)
  return dateStr <= today
}

const displayedCards = computed(() => {
  let list = [...reviewStore.deckCards]
  const today = new Date().toISOString().slice(0, 10)
  const next7Days = new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10)

  // Urgency filter
  if (selectedUrgency.value === 'due') {
    list = list.filter((c) => isDueToday(c.nextReviewDate))
  } else if (selectedUrgency.value === 'overdue') {
    list = list.filter((c) => c.nextReviewDate && c.nextReviewDate < today)
  } else if (selectedUrgency.value === 'upcoming') {
    list = list.filter((c) => c.nextReviewDate && c.nextReviewDate > today && c.nextReviewDate <= next7Days)
  }

  // Sort options
  if (selectedSortBy.value === 'nextReviewDate_asc') {
    list.sort((a, b) => (a.nextReviewDate || '').localeCompare(b.nextReviewDate || ''))
  } else if (selectedSortBy.value === 'nextReviewDate_desc') {
    list.sort((a, b) => (b.nextReviewDate || '').localeCompare(a.nextReviewDate || ''))
  } else if (selectedSortBy.value === 'difficulty') {
    list.sort((a, b) => a.easeFactor - b.easeFactor)
  } else if (selectedSortBy.value === 'recent') {
    list.sort((a, b) => b.id.localeCompare(a.id))
  }

  return list
})

function isInteractiveElement(target: EventTarget | null): boolean {
  if (!target || !(target instanceof HTMLElement)) return false
  const tag = target.tagName.toLowerCase()
  return (
    tag === 'input' ||
    tag === 'textarea' ||
    tag === 'select' ||
    target.isContentEditable ||
    target.getAttribute('contenteditable') === 'true'
  )
}

function handleKeydown(e: KeyboardEvent) {
  if (isInteractiveElement(e.target) || (typeof document !== 'undefined' && isInteractiveElement(document.activeElement))) {
    return
  }

  if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
    e.preventDefault()
    searchInputRef.value?.focus()
    return
  }

  // Active review session shortcuts: [E] Edit active card
  if (activeTab.value === 'session' && currentCard.value && !cardToEdit.value && !cardToReset.value && !cardToDelete.value) {
    if (e.key === 'e' || e.key === 'E') {
      e.preventDefault()
      openEditModal(currentCard.value)
    }
  }
}

// Edit Modal State
const cardToEdit = ref<ReviewCard | null>(null)
const editFrontMarkdown = ref('')
const editBackMarkdown = ref('')
const editActiveTab = ref<'edit' | 'preview'>('edit')
const isSavingCard = ref(false)

function openEditModal(card: ReviewCard) {
  cardToEdit.value = card
  editFrontMarkdown.value = card.frontMarkdown || card.topicTitle || ''
  editBackMarkdown.value = card.backMarkdown || card.topicSummary || ''
  editActiveTab.value = 'edit'
}

function closeEditModal() {
  cardToEdit.value = null
  editFrontMarkdown.value = ''
  editBackMarkdown.value = ''
}

async function confirmSaveCard() {
  if (!cardToEdit.value) return
  if (!editFrontMarkdown.value.trim() || !editBackMarkdown.value.trim()) {
    toast.error('Both front and back markdown cannot be empty.')
    return
  }

  isSavingCard.value = true
  try {
    await reviewStore.updateCard(cardToEdit.value.id, {
      frontMarkdown: editFrontMarkdown.value.trim(),
      backMarkdown: editBackMarkdown.value.trim()
    })
    toast.success(t('review.toast_update_success'))
    closeEditModal()
  } catch (err: unknown) {
    toast.error(formatError(err, 'review.toast_update_error'))
  } finally {
    isSavingCard.value = false
  }
}

// Reset Modal State
const cardToReset = ref<ReviewCard | null>(null)
const isResetting = ref(false)

function openResetModal(card: ReviewCard) {
  cardToReset.value = card
}

function closeResetModal() {
  cardToReset.value = null
}

async function confirmResetCard() {
  if (!cardToReset.value) return
  isResetting.value = true
  try {
    await reviewStore.resetCardProgress(cardToReset.value.id)
    toast.success(t('review.toast_reset_success'))
    closeResetModal()
    await reviewStore.fetchReviewDeck()
  } catch (err: unknown) {
    toast.error(formatError(err, 'review.toast_reset_error'))
  } finally {
    isResetting.value = false
  }
}

// Delete Modal State
const cardToDelete = ref<ReviewCard | null>(null)
const isDeleting = ref(false)

function openDeleteModal(card: ReviewCard) {
  cardToDelete.value = card
}

function closeDeleteModal() {
  cardToDelete.value = null
}

async function confirmDeleteCard() {
  if (!cardToDelete.value) return
  isDeleting.value = true
  try {
    await reviewStore.deleteCard(cardToDelete.value.id)
    toast.success(t('review.toast_delete_success'))
    closeDeleteModal()
    await reviewStore.fetchReviewDeck()
  } catch (err: unknown) {
    toast.error(formatError(err, 'review.toast_delete_error'))
  } finally {
    isDeleting.value = false
  }
}

onMounted(() => {

  reviewStore.fetchReviewDeck()

  if (route.query.tab === 'management') {
    activeTab.value = 'management'
  }
  const queryPage = route.query.page ? parseInt(route.query.page as string, 10) : 1
  const initialPage = isNaN(queryPage) || queryPage < 1 ? 1 : queryPage
  fetchDeck(initialPage)
})


useEventListener(typeof window !== 'undefined' ? window : null, 'keydown', handleKeydown)
</script>

<template>
  <div class="min-h-[calc(100vh-3.5rem)] sm:min-h-[calc(100vh-3.75rem)] p-4 sm:p-6 md:p-8 flex flex-col items-center bg-slate-50 dark:bg-canvas transition-colors duration-200">
    <!-- Top-Level Tab Switcher -->
    <div class="w-full max-w-5xl flex items-center justify-between border-b border-slate-200 dark:border-white/[0.08] pb-3 mb-4 sm:mb-6">
      <div class="flex items-center gap-1.5 p-1 bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-2xl shrink-0 overflow-x-auto">
        <!-- Tab 1: Review Session -->
        <button
          @click="activeTab = 'session'"
          :class="[
            'px-3.5 sm:px-4 py-2 sm:py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-colors border inline-flex items-center gap-2 whitespace-nowrap shrink-0 cursor-pointer',
            activeTab === 'session'
              ? 'bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border-transparent dark:border-white/[0.06]'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          ]"
        >
          <Layers class="w-4 h-4" />
          <span>{{ $t('review.tab_session') }}</span>
          <span
            v-if="reviewStore.cards.length > 0"
            class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-brand-500/15 text-brand-600 dark:text-brand-400 border border-brand-500/20 ml-0.5"
          >
            {{ reviewStore.cards.length }}
          </span>
        </button>

        <!-- Tab 2: Deck Management -->
        <button
          @click="activeTab = 'management'"
          :class="[
            'px-3.5 sm:px-4 py-2 sm:py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-colors border inline-flex items-center gap-2 whitespace-nowrap shrink-0 cursor-pointer',
            activeTab === 'management'
              ? 'bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border-transparent dark:border-white/[0.06]'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          ]"
        >
          <Library class="w-4 h-4" />
          <span>{{ $t('review.tab_management') }}</span>
          <span
            v-if="reviewStore.deckStatistics.totalCards > 0"
            class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-slate-200/60 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 ml-0.5 border border-slate-300/40 dark:border-white/[0.08]"
          >
            {{ reviewStore.deckStatistics.totalCards }}
          </span>
        </button>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- TAB 1: REVIEW SESSION                                                     -->
    <!-- ========================================================================= -->
    <div v-if="activeTab === 'session'" class="w-full flex flex-col items-center justify-center flex-1">
      <!-- Loading State -->
      <div v-if="reviewStore.isLoading" class="flex flex-col items-center gap-3 text-slate-500 dark:text-slate-400 text-sm py-16">
        <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin"></div>
        <span>Loading Spaced Repetition Deck...</span>
      </div>

      <!-- Active Review Deck with Unboxed Direct-Canvas Architecture -->
      <div v-else-if="currentCard" class="w-full max-w-6xl mx-auto flex flex-col flex-1 min-h-0 space-y-4">
        <!-- Direct-Canvas Breadcrumb & Status Header (No Enclosing Card) -->
        <div class="flex flex-wrap items-center justify-between gap-2 px-1">
          <div class="flex items-center gap-2.5 min-w-0">
            <div class="w-7 h-7 sm:w-8 sm:h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
              <Layers class="w-4 h-4" />
            </div>
            <div class="min-w-0">
              <h1 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white truncate">
                {{ cleanTopicTitle }}
              </h1>
              <p class="text-[11px] sm:text-xs text-slate-500 dark:text-slate-400 truncate">
                {{ cleanSourceSubtitle }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2 shrink-0">
            <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 shadow-sm">
              <span class="w-1.5 h-1.5 rounded-full bg-brand-500 animate-pulse"></span>
              <span>{{ reviewStore.cards.length }} {{ $t('review.cards_remaining') }}</span>
            </span>
          </div>
        </div>

        <!-- 2-Column Unboxed Cockpit (Stage 68% + Dock 32%) -->
        <div class="flex flex-col lg:flex-row gap-4 items-start">
          <!-- LEFT: Flashcard Hero (The ONLY Card!) -->
          <div class="w-full lg:w-[68%]">
            <FlashcardDeck
              :card="currentCard"
              :remaining-count="reviewStore.cards.length"
              @grade="onGrade"
            />
          </div>

          <!-- RIGHT: Companion Telemetry Dock (Cards sit directly on canvas) -->
          <aside class="w-full lg:w-[32%] space-y-3 sm:space-y-4">
            <!-- 1. Session Progress -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-2.5">
              <div class="flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
                <span class="flex items-center gap-1.5">
                  <Clock class="w-3.5 h-3.5 text-brand-500" />
                  <span>{{ $t('review.session_progress') }}</span>
                </span>
                <span class="font-mono text-xs font-bold text-slate-700 dark:text-slate-300">
                  {{ sessionProgress.reviewed }} / {{ sessionProgress.total }}
                </span>
              </div>

              <div class="h-2 w-full bg-slate-100 dark:bg-white/[0.06] rounded-full overflow-hidden">
                <div
                  class="h-full bg-gradient-to-r from-brand-600 to-brand-400 transition-all duration-300"
                  :style="{ width: `${sessionProgress.percentage}%` }"
                ></div>
              </div>

              <div class="flex items-center justify-between text-[11px] text-slate-400">
                <span>{{ sessionProgress.percentage }}% {{ $t('review.complete') }}</span>
                <span>{{ reviewStore.cards.length }} {{ $t('review.remaining') }}</span>
              </div>
            </div>

            <!-- 2. SM-2 Algorithm Telemetry Readout -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-3">
              <div class="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                <span class="flex items-center gap-1.5">
                  <Activity class="w-3.5 h-3.5 text-brand-500" />
                  <span>{{ $t('review.sm2_telemetry') }}</span>
                </span>
              </div>

              <div class="grid grid-cols-2 gap-2 text-xs">
                <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-white/[0.02] border border-slate-100 dark:border-white/[0.04]">
                  <span class="text-[11px] text-slate-400 block">{{ $t('review.ease_factor_label') || 'Ease Factor' }}</span>
                  <span class="font-mono font-bold text-slate-800 dark:text-slate-200 text-sm">
                    {{ currentCard.easeFactor.toFixed(2) }}
                  </span>
                </div>
                <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-white/[0.02] border border-slate-100 dark:border-white/[0.04]">
                  <span class="text-[11px] text-slate-400 block">{{ $t('review.interval_label') || 'Current Interval' }}</span>
                  <span class="font-mono font-bold text-slate-800 dark:text-slate-200 text-sm">
                    {{ currentCard.intervalDays }} {{ $t('review.days') }}
                  </span>
                </div>
              </div>

              <p class="text-[11px] text-slate-400 leading-relaxed font-mono">
                Formula: $I_n = I_{n-1} \times \text{EF}$. Next interval scales from {{ currentCard.intervalDays }}d up to {{ Math.round(currentCard.intervalDays * Number(currentCard.easeFactor || 2.5)) }}d.
              </p>
            </div>

            <!-- 3. Keyboard Shortcuts Guide -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-2">
              <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                <Key class="w-3.5 h-3.5 text-brand-500" />
                <span>{{ $t('review.shortcuts_title') }}</span>
              </div>
              <div class="space-y-1.5 text-xs text-slate-600 dark:text-slate-400">
                <div class="flex items-center justify-between">
                  <span>{{ $t('review.flip_unflip') }}</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">Space</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>{{ $t('review.grade_sm2') }}</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">1, 2, 3, 4</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>{{ $t('review.edit_card_shortcut') }}</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">E</span>
                </div>
              </div>
            </div>
          </aside>
        </div>
      </div>

      <!-- Empty / Completed State (Unboxed Direct-Canvas Refinement) -->
      <div
        v-else
        class="w-full max-w-lg mx-auto text-center p-6 sm:p-8 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle shadow-sm my-auto space-y-4 sm:space-y-5 animate-in zoom-in-95 duration-200"
      >
        <div class="w-12 h-12 sm:w-14 sm:h-14 rounded-2xl bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 flex items-center justify-center mx-auto shadow-sm">
          <CheckCircle class="w-6 h-6 sm:w-7 sm:h-7" />
        </div>

        <div class="space-y-1.5">
          <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white tracking-tight">
            {{ $t('review.no_cards') }}
          </h2>
          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed max-w-sm mx-auto">
            {{ $t('review.no_cards_desc') }}
          </p>
        </div>

        <!-- Session Summary Badge -->
        <div class="flex items-center justify-center gap-3 py-2 px-3.5 rounded-xl bg-slate-50 dark:bg-white/[0.02] border border-slate-100 dark:border-white/[0.04] text-xs max-w-xs mx-auto">
          <div class="flex items-center gap-1.5 text-slate-600 dark:text-slate-300">
            <Layers class="w-3.5 h-3.5 text-brand-500" />
            <span class="font-bold">{{ sessionProgress.reviewed || reviewStore.deckStatistics.totalCards }}</span>
            <span class="text-slate-400">{{ $t('review.cards_unit') }}</span>
          </div>
          <span class="text-slate-300 dark:text-white/10">•</span>
          <div class="flex items-center gap-1.5 text-emerald-600 dark:text-emerald-400 font-semibold">
            <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
            <span>100% {{ $t('review.complete') }}</span>
          </div>
        </div>

        <!-- Action CTAs -->
        <div class="flex flex-col sm:flex-row items-center justify-center gap-2.5 pt-2">
          <button
            @click="activeTab = 'management'"
            type="button"
            class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs sm:text-sm shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0 cursor-pointer"
          >
            <Library class="w-4 h-4" />
            <span>{{ $t('review.browse_deck_btn') }} ({{ reviewStore.deckStatistics.totalCards }} {{ $t('review.cards_unit') }})</span>
          </button>
          <NuxtLink
            to="/today"
            class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 hover:bg-slate-200 dark:bg-white/[0.06] dark:hover:bg-white/[0.1] text-slate-700 dark:text-slate-200 font-semibold text-xs sm:text-sm border border-slate-200 dark:border-white/[0.08] transition-all whitespace-nowrap shrink-0"
          >
            <Sparkles class="w-4 h-4 text-brand-500" />
            <span>{{ $t('review.cram_practice_btn') }}</span>
          </NuxtLink>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- TAB 2: DECK MANAGEMENT                                                    -->
    <!-- ========================================================================= -->
    <div v-else-if="activeTab === 'management'" class="w-full max-w-7xl mx-auto space-y-5">
      <!-- 1. Header: Bento Overview (3 Cards) -->
          <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 w-full">
            <FlashcardHeroCard
              :due-count="reviewStore.totalCardsDue"
              @start-review="activeTab = 'session'"
            />
            <MasteryGaugeCard
              :mastered-count="reviewStore.deckStatistics.masteredCount"
              :total-count="reviewStore.deckStatistics.totalCards"
            />
            <ReviewForecastChart
              :cards="reviewStore.deckCards.length > 0 ? reviewStore.deckCards : reviewStore.cards"
            />
          </div>

      <!-- 2. Filters & Search Bar (Trực tiếp trên nền Canvas) -->
      <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 w-full">
        <!-- Quick Filter Chips -->
        <div class="flex items-center gap-1.5 overflow-x-auto pb-0.5 sm:pb-0">
          <button
            type="button"
            @click="setQuickFilter('all')"
            :class="[
              'flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 cursor-pointer',
              selectedStatus === null && selectedUrgency === null
                ? 'bg-brand-600 text-white border-transparent shadow-sm'
                : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08]'
            ]"
          >
            <Layers class="w-3.5 h-3.5" />
            <span>{{ $t('review.quick_filter_all') }} ({{ reviewStore.deckTotalCount || reviewStore.deckStatistics.totalCards }})</span>
          </button>
          <button
            type="button"
            @click="setQuickFilter('due')"
            :class="[
              'flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 cursor-pointer',
              selectedUrgency === 'due'
                ? 'bg-amber-600 text-white border-transparent shadow-sm'
                : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08]'
            ]"
          >
            <Flame class="w-3.5 h-3.5" :class="selectedUrgency === 'due' ? 'text-white' : 'text-amber-500'" />
            <span>{{ $t('review.quick_filter_due') }} ({{ reviewStore.totalCardsDue }})</span>
          </button>
          <button
            type="button"
            @click="setQuickFilter('mastered')"
            :class="[
              'flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 cursor-pointer',
              selectedStatus === 2
                ? 'bg-emerald-600 text-white border-transparent shadow-sm'
                : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08]'
            ]"
          >
            <Check class="w-3.5 h-3.5" :class="selectedStatus === 2 ? 'text-white' : 'text-emerald-500'" />
            <span>{{ $t('review.quick_filter_mastered') }} ({{ reviewStore.deckStatistics.masteredCount }})</span>
          </button>
          <!-- Advanced Filter Trigger Button -->
          <button
            type="button"
            @click="isAdvancedFilterOpen = true"
            class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold bg-white dark:bg-canvas-subtle text-slate-700 dark:text-slate-300 border border-slate-200/80 dark:border-white/[0.08] hover:border-brand-500/40 transition-colors whitespace-nowrap shrink-0 cursor-pointer"
          >
            <SlidersHorizontal class="w-3.5 h-3.5" />
            <span>{{ $t('review.advanced_filter_btn') }}</span>
            <span
              v-if="activeFilterCount > 0"
              class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] font-bold bg-brand-600 text-white leading-tight"
            >
              {{ activeFilterCount }}
            </span>
          </button>
        </div>

        <!-- Search input with ⌘K -->
        <div class="relative w-full sm:w-72 shrink-0">
          <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
          <input
            ref="searchInputRef"
            v-model="searchQuery"
            type="text"
            :placeholder="$t('review.search_placeholder')"
            class="w-full pl-9 pr-14 py-1.5 text-xs rounded-xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 shadow-sm"
          />
          <div class="absolute right-2.5 top-1/2 -translate-y-1/2 flex items-center gap-1">
            <button
              v-if="searchQuery"
              type="button"
              @click="searchQuery = ''"
              class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 p-0.5 rounded-full cursor-pointer"
            >
              <X class="w-3.5 h-3.5" />
            </button>
            <span class="px-1.5 py-0.5 rounded text-[10px] font-mono text-slate-400 bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">⌘K</span>
          </div>
        </div>
      </div>


      <!-- 3. Content: Flashcard Inventory Cards -->
      <!-- Loading state -->
          <div v-if="reviewStore.isDeckLoading" class="flex flex-col items-center justify-center py-20 text-slate-500 dark:text-slate-400 text-sm">
            <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin mb-3"></div>
            <span>{{ $t('review.loading_deck') }}</span>
          </div>

          <!-- Cards Bento Grid -->
          <div v-else-if="displayedCards.length > 0" class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 items-start">
            <FlashcardBentoCard
              v-for="card in displayedCards"
              :key="card.id"
              :card="card"
              @edit="openEditModal"
              @reset="openResetModal"
              @delete="openDeleteModal"
            />
          </div>

          <!-- Empty Deck State -->
          <div v-else class="text-center py-16 glass-card p-8 space-y-3">
            <Layers class="w-12 h-12 text-slate-400 dark:text-slate-600 mx-auto" />
            <h3 class="text-base font-bold text-slate-800 dark:text-slate-200">{{ $t('review.empty_deck') }}</h3>
            <p v-if="searchQuery || selectedStatus !== null || selectedSource !== null || selectedUrgency !== null" class="text-xs text-slate-500 max-w-sm mx-auto">
              {{ $t('review.empty_deck_hint') }}
            </p>
          </div>
      <!-- 4. Pagination -->
      <div v-if="displayedCards.length > 0" class="w-full pt-1">
            <BasePagination
              :current-page="reviewStore.deckCurrentPage"
              :total-pages="totalPages"
              :total-count="reviewStore.deckTotalCount"
              :page-size="reviewStore.deckPageSize"
              show-summary
              @change="onDeckPageChange"
            />
          </div>
      </div>
    <!-- ========================================================================= -->
    <!-- MODALS (Teleported to Body)                                               -->
    <!-- ========================================================================= -->

    <!-- 1. Edit Flashcard Modal -->
    <Teleport to="body">
      <div
        v-if="cardToEdit"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200"
      >
        <div class="w-full max-w-2xl p-6 rounded-3xl glass-panel shadow-2xl space-y-4 animate-in zoom-in-95 duration-200 max-h-[90vh] overflow-y-auto">
          <div class="flex items-center justify-between border-b border-slate-100 dark:border-white/[0.08] pb-3">
            <h3 class="text-lg font-bold text-slate-900 dark:text-white flex items-center gap-2">
              <Pencil class="w-5 h-5 text-brand-500" />
              <span>{{ $t('review.edit_card') }}</span>
            </h3>

            <!-- Mode Switcher: Edit vs Preview -->
            <div class="flex items-center gap-1 bg-slate-100 dark:bg-white/[0.06] p-1 rounded-xl text-xs font-semibold">
              <button
                @click="editActiveTab = 'edit'"
                :class="[
                  'px-3 py-1 rounded-lg transition-all cursor-pointer',
                  editActiveTab === 'edit'
                    ? 'bg-white dark:bg-white/[0.1] text-brand-600 dark:text-brand-300 font-bold shadow-sm'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
                ]"
              >
                {{ $t('review.tab_edit') }}
              </button>
              <button
                @click="editActiveTab = 'preview'"
                :class="[
                  'px-3 py-1 rounded-lg transition-all cursor-pointer',
                  editActiveTab === 'preview'
                    ? 'bg-white dark:bg-white/[0.1] text-brand-600 dark:text-brand-300 font-bold shadow-sm'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
                ]"
              >
                {{ $t('review.tab_preview') }}
              </button>
            </div>
          </div>

          <!-- Edit Mode -->
          <div v-if="editActiveTab === 'edit'" class="space-y-4">
            <div class="space-y-1.5">
              <label class="text-xs font-bold text-slate-700 dark:text-slate-300">
                {{ $t('review.front_label') }}
              </label>
              <textarea
                v-model="editFrontMarkdown"
                rows="4"
                class="w-full text-xs sm:text-sm bg-slate-50 dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl p-3 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all font-mono"
                placeholder="Front prompt markdown..."
              ></textarea>
            </div>

            <div class="space-y-1.5">
              <label class="text-xs font-bold text-slate-700 dark:text-slate-300">
                {{ $t('review.back_label') }}
              </label>
              <textarea
                v-model="editBackMarkdown"
                rows="6"
                class="w-full text-xs sm:text-sm bg-slate-50 dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl p-3 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all font-mono"
                placeholder="Back explanation markdown..."
              ></textarea>
            </div>
          </div>

          <!-- Live Preview Mode -->
          <div v-else class="space-y-4">
            <div class="space-y-1.5">
              <div class="text-xs font-bold text-slate-700 dark:text-slate-300">{{ $t('review.front_label') }}</div>
              <div
                class="p-4 rounded-xl bg-slate-50 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.06] text-xs sm:text-sm prose dark:prose-invert max-w-none"
                v-html="renderMarkdown(editFrontMarkdown)"
              ></div>
            </div>

            <div class="space-y-1.5">
              <div class="text-xs font-bold text-slate-700 dark:text-slate-300">{{ $t('review.back_label') }}</div>
              <div
                class="p-4 rounded-xl bg-brand-500/10 border border-brand-500/20 text-xs sm:text-sm prose dark:prose-invert max-w-none"
                v-html="renderMarkdown(editBackMarkdown)"
              ></div>
            </div>
          </div>

          <!-- Modal Action Buttons -->
          <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-white/[0.08]">
            <button
              type="button"
              @click="closeEditModal"
              class="px-4 py-2 rounded-xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors whitespace-nowrap shrink-0"
            >
              {{ $t('review.cancel') }}
            </button>
            <button
              type="button"
              :disabled="isSavingCard"
              @click="confirmSaveCard"
              class="inline-flex items-center gap-1.5 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-brand-500/20 active:scale-95 transition-all whitespace-nowrap shrink-0 disabled:opacity-50"
            >
              <span v-if="isSavingCard" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <Check v-else class="w-4 h-4" />
              <span>{{ isSavingCard ? $t('review.saving') : $t('review.save_changes') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- 2. Reset Progression Confirmation Modal -->
    <Teleport to="body">
      <div
        v-if="cardToReset"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200"
      >
        <div class="w-full max-w-md p-6 rounded-3xl glass-panel shadow-2xl space-y-4 animate-in zoom-in-95 duration-200">
          <div class="w-12 h-12 rounded-2xl bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 flex items-center justify-center">
            <RotateCcw class="w-6 h-6" />
          </div>

          <div class="space-y-1.5">
            <h3 class="text-lg font-bold text-slate-900 dark:text-white">
              {{ $t('review.reset_confirm_title') }}
            </h3>
            <p class="text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
              {{ $t('review.reset_confirm_desc') }}
            </p>
          </div>

          <div class="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              @click="closeResetModal"
              class="px-4 py-2 rounded-xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors whitespace-nowrap shrink-0"
            >
              {{ $t('review.cancel') }}
            </button>
            <button
              type="button"
              :disabled="isResetting"
              @click="confirmResetCard"
              class="inline-flex items-center gap-1.5 px-5 py-2.5 rounded-xl bg-amber-600 hover:bg-amber-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-amber-600/20 active:scale-95 transition-all whitespace-nowrap shrink-0 disabled:opacity-50"
            >
              <span v-if="isResetting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ isResetting ? $t('review.resetting') : $t('review.confirm_reset') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- 3. Delete Confirmation Modal -->
    <Teleport to="body">
      <div
        v-if="cardToDelete"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200"
      >
        <div class="w-full max-w-md p-6 rounded-3xl glass-panel shadow-2xl space-y-4 animate-in zoom-in-95 duration-200">
          <div class="w-12 h-12 rounded-2xl bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20 flex items-center justify-center">
            <AlertTriangle class="w-6 h-6" />
          </div>

          <div class="space-y-1.5">
            <h3 class="text-lg font-bold text-slate-900 dark:text-white">
              {{ $t('review.delete_confirm_title') }}
            </h3>
            <p class="text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
              {{ $t('review.delete_confirm_desc') }}
            </p>
          </div>

          <div class="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              @click="closeDeleteModal"
              class="px-4 py-2 rounded-xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors whitespace-nowrap shrink-0"
            >
              {{ $t('review.cancel') }}
            </button>
            <button
              type="button"
              :disabled="isDeleting"
              @click="confirmDeleteCard"
              class="inline-flex items-center gap-1.5 px-5 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-rose-600/20 active:scale-95 transition-all whitespace-nowrap shrink-0 disabled:opacity-50"
            >
              <span v-if="isDeleting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ isDeleting ? $t('review.deleting') : $t('review.confirm_delete') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- 4. Advanced Filter Modal -->
    <AdvancedFilterModal
      :is-open="isAdvancedFilterOpen"
      :current-filters="{
        status: selectedStatus,
        sourceType: selectedSource,
        urgency: selectedUrgency,
        sortBy: selectedSortBy
      }"
      @close="isAdvancedFilterOpen = false"
      @apply="onApplyAdvancedFilters"
      @reset="onResetAdvancedFilters"
    />
  </div>
</template>
