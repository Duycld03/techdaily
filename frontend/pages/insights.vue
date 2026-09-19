<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useEventListener } from '@vueuse/core'
import {
  Sparkles,
  Shuffle,
  ChevronLeft,
  ChevronRight,
  Bookmark,
  BookmarkCheck,
  Zap,
  CheckCircle2,
  XCircle,
  Cpu,
  Layers,
  Search,
  ExternalLink,
  Plus,
  X,
  Lightbulb
} from 'lucide-vue-next'
import { useInsightsStore } from '~/stores/useInsightsStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiError } from '~/composables/useApiError'
import MarkdownIt from 'markdown-it'

const { t, locale } = useI18n()
const { formatError } = useApiError()
const authStore = useAuthStore()
const insightsStore = useInsightsStore()
const toast = useToast()
const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

const viewMode = ref<'explore' | 'saved'>('explore')
const bookmarkedCount = computed(() => insightsStore.bookmarkedInsights.length)

function renderMarkdown(raw: string | undefined | null): string {
  if (!raw) return ''
  const clean = raw.replace(/\\n/g, '\n')
  return md.render(clean)
}

const renderedSummaryHtml = computed(() => {
  return renderMarkdown(insightsStore.currentInsight?.summaryMarkdown)
})

const renderedUnderTheHoodHtml = computed(() => {
  return renderMarkdown(insightsStore.currentInsight?.underTheHoodMarkdown)
})

const isGenerateModalOpen = ref(false)
const customTopicInput = ref('')
const activeCodeTab = ref<'solution' | 'problem'>('solution')


const defaultTopics = [
  'Kestrel Socket Pipeline',
  'ArrayPool<T> Zero-Allocation',
  'PostgreSQL Index-Only Scans',
  'System.Threading.Channels',
  'Transactional Outbox Pattern'
]

const currentSuggestedTopics = computed(() => {
  const cat = insightsStore.selectedCategory
  if (cat !== null && insightsStore.suggestedTopics[cat] && insightsStore.suggestedTopics[cat].length > 0) {
    return insightsStore.suggestedTopics[cat]
  }
  return defaultTopics
})

function pickRandomTopic() {
  const topics = currentSuggestedTopics.value
  const randomChoice = topics[Math.floor(Math.random() * topics.length)]
  customTopicInput.value = randomChoice
}

const computedCategories = computed(() => {
  const list = [{ id: null as number | null, label: t('insights.all_categories') }]
  if (insightsStore.categoryMetadata && insightsStore.categoryMetadata.length > 0) {
    for (const cat of insightsStore.categoryMetadata) {
      list.push({
        id: cat.id,
        label: locale.value === 'vi' ? cat.labelVi : cat.labelEn
      })
    }
  } else {
    list.push(
      { id: 0, label: t('insights.cat_frontend') },
      { id: 1, label: t('insights.cat_backend') },
      { id: 2, label: t('insights.cat_database') },
      { id: 3, label: t('insights.cat_system') }
    )
  }
  return list
})

onMounted(async () => {
  if (!authStore.isLoggedIn) {
    return navigateTo({
      path: '/login',
      query: { redirect: '/insights' }
    })
  }
  await Promise.all([
    insightsStore.fetchFeed(),
    insightsStore.fetchMetadata(),
    insightsStore.fetchBookmarkedInsights()
  ])
})

function handleKeyDown(e: KeyboardEvent) {
  // Prevent keydown during text input in modal
  if (isGenerateModalOpen.value || ['INPUT', 'TEXTAREA'].includes((e.target as HTMLElement)?.tagName)) {
    return
  }

  if (e.code === 'Space' || e.key === 'ArrowRight') {
    e.preventDefault()
    if (insightsStore.hasNext) {
      insightsStore.nextInsight()
    }
  } else if (e.key === 'ArrowLeft') {
    e.preventDefault()
    if (insightsStore.hasPrev) {
      insightsStore.prevInsight()
    }
  }
}

useEventListener(typeof window !== 'undefined' ? window : null, 'keydown', handleKeyDown)

async function switchViewMode(mode: 'explore' | 'saved') {
  if (mode === 'saved') {
    if (!authStore.isAuthenticated) {
      toast.warning(t('insights.toast_login_required_saved'))
      return
    }
    viewMode.value = 'saved'
    await insightsStore.fetchFeed(insightsStore.selectedCategory, null, true)
  } else {
    viewMode.value = 'explore'
    await insightsStore.fetchFeed(insightsStore.selectedCategory, null, false)
  }
}

async function handleCategorySelect(catId: number | null) {
  const onlySaved = viewMode.value === 'saved'
  await insightsStore.fetchFeed(catId, null, onlySaved)
}

async function handleToggleBookmark(id: string) {
  try {
    const res = await insightsStore.toggleBookmark(id)
    if (res?.isBookmarked) {
      toast.success(t('insights.toast_bookmark_added'))
    } else {
      toast.info(t('insights.toast_bookmark_removed'))
    }
  } catch (err: unknown) {
    const apiErr = err as { message?: string; status?: number; response?: { status?: number } }
    if (apiErr?.message === 'UNAUTHENTICATED' || apiErr?.response?.status === 401 || apiErr?.status === 401) {
      toast.warning(t('insights.toast_login_required_bookmark'))
    } else {
      toast.error(formatError(err, 'insights.toast_bookmark_failed'))
    }
  }
}


async function handleGenerateSubmit() {
  if (insightsStore.isGenerating) return
  try {
    await insightsStore.generateWithAi(customTopicInput.value, locale.value)
    toast.success(t('insights.toast_generate_success'))
    isGenerateModalOpen.value = false
    customTopicInput.value = ''
  } catch (err: unknown) {
    toast.error(formatError(err, 'insights.toast_generate_failed'))
  }
}

function getCategoryBadge(cat: number) {
  switch (cat) {
    case 0:
      return { text: 'Frontend & Browser', color: 'bg-sky-50 dark:bg-sky-950/50 text-sky-700 dark:text-sky-300 border-sky-200 dark:border-sky-800' }
    case 1:
      return { text: '.NET 10 & C# 13', color: 'bg-purple-50 dark:bg-purple-950/50 text-purple-700 dark:text-purple-300 border-purple-200 dark:border-purple-800' }
    case 2:
      return { text: 'PostgreSQL 17 Engine', color: 'bg-blue-50 dark:bg-blue-950/50 text-blue-700 dark:text-blue-300 border-blue-200 dark:border-blue-800' }
    case 3:
      return { text: 'System Design & Distributed', color: 'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800' }
    default:
      return { text: 'Core Architecture', color: 'bg-slate-50 dark:bg-slate-900 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-800' }
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto p-4 sm:p-6 md:p-8 space-y-6 sm:space-y-8 animate-in fade-in duration-300">
    <!-- Header Banner -->
    <div class="p-4 sm:p-7 rounded-2xl sm:rounded-3xl glass-panel border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-white shadow-md dark:shadow-none relative overflow-hidden transition-all">
      <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4 sm:gap-6 relative z-10">
        <div class="space-y-1 sm:space-y-2 flex-1 min-w-0">
          <div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20 text-xs font-bold uppercase tracking-wider">
            <Sparkles class="w-3.5 h-3.5" />
            <span>{{ $t('insights.badge') }}</span>
          </div>

          <h1 class="text-2xl sm:text-4xl font-black text-slate-900 dark:text-white tracking-tight leading-tight">
            {{ $t('insights.title') }}
          </h1>

          <p class="text-slate-600 dark:text-slate-300 text-sm md:text-lg leading-relaxed max-w-2xl">
            {{ $t('insights.subtitle') }}
          </p>
        </div>

        <div class="flex items-center gap-2.5 sm:gap-3 shrink-0 flex-wrap sm:flex-nowrap w-full sm:w-auto">
          <button
            @click="insightsStore.shuffle()"
            class="flex-1 sm:flex-initial flex items-center justify-center gap-2 px-4 sm:px-5 py-2.5 sm:py-3 rounded-2xl bg-white dark:bg-canvas-subtle hover:bg-slate-50 dark:hover:bg-canvas-elevated text-slate-700 dark:text-slate-200 text-sm md:text-base font-bold transition-all border border-slate-200/80 dark:border-white/[0.08] shadow-sm active:scale-95"
            :title="$t('insights.shuffle')"
          >
            <Shuffle class="w-4 h-4 text-slate-500 dark:text-slate-400" />
            <span>{{ $t('insights.shuffle') }}</span>
          </button>

          <button
            @click="isGenerateModalOpen = true"
            class="flex-1 sm:flex-initial flex items-center justify-center gap-2 px-4 sm:px-5 py-2.5 sm:py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white text-sm md:text-base font-bold transition-all shadow-md shadow-brand-500/20 active:scale-95"
          >
            <Plus class="w-4 h-4" />
            <span>{{ $t('insights.generate_ai') }}</span>
          </button>
        </div>
      </div>
    </div>
    <!-- View Mode Switcher -->
    <div class="flex items-center gap-2 p-1 bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-2xl w-fit">
      <button
        @click="switchViewMode('explore')"
        :class="[
          'px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-colors inline-flex items-center gap-2 whitespace-nowrap shrink-0 border',
          viewMode === 'explore'
            ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 border-slate-200/80 dark:border-white/[0.12] shadow-sm'
            : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
        ]"
      >
        <Sparkles class="w-4 h-4 text-brand-500" />
        <span>{{ $t('insights.view_explore') }}</span>
      </button>
      <button
        @click="switchViewMode('saved')"
        :class="[
          'px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-colors inline-flex items-center gap-2 whitespace-nowrap shrink-0 border',
          viewMode === 'saved'
            ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 border-slate-200/80 dark:border-white/[0.12] shadow-sm'
            : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
        ]"
      >
        <BookmarkCheck class="w-4 h-4 text-brand-500" />
        <span>{{ $t('insights.view_saved', { count: bookmarkedCount }) }}</span>
      </button>
    </div>

    <!-- Category Filter Bar -->
    <div class="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none -mx-4 px-4 sm:mx-0 sm:px-0">
      <button
        v-for="cat in computedCategories"
        :key="String(cat.id)"
        @click="handleCategorySelect(cat.id)"
        :class="[
          'px-3.5 sm:px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all shrink-0 border whitespace-nowrap',
          insightsStore.selectedCategory === cat.id
            ? 'bg-brand-600 text-white border-transparent shadow-sm shadow-brand-500/20'
            : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-50 dark:hover:bg-canvas-elevated hover:border-slate-300 dark:hover:border-white/[0.16]'
        ]"
      >
        {{ cat.label }}
      </button>
    </div>

    <!-- Main Card Container -->
    <div v-if="insightsStore.isLoading" class="flex flex-col items-center justify-center py-20 space-y-4">
      <div class="w-10 h-10 border-4 border-brand-500/20 border-t-brand-500 rounded-full animate-spin"></div>
      <p class="text-sm font-semibold text-slate-500 dark:text-slate-400">Loading senior technical insights...</p>
    </div>

    <!-- Empty State: Saved Mode -->
    <div
      v-else-if="!insightsStore.currentInsight && viewMode === 'saved'"
      class="p-8 sm:p-12 text-center rounded-2xl sm:rounded-3xl glass-card border border-slate-200/80 dark:border-white/[0.08] space-y-4"
    >
      <div class="w-12 h-12 rounded-2xl bg-brand-500/10 flex items-center justify-center mx-auto text-brand-500 border border-brand-500/20">
      </div>
      <h3 class="text-lg font-bold text-slate-900 dark:text-white">{{ $t('insights.saved_empty_title') }}</h3>
      <p class="text-sm text-slate-500 dark:text-slate-400 max-w-md mx-auto">{{ $t('insights.saved_empty_desc') }}</p>
      <button
        @click="switchViewMode('explore')"
        class="px-5 py-2.5 rounded-xl bg-brand-600 text-white text-sm font-bold hover:bg-brand-500 shadow-md shadow-brand-500/20 transition-all inline-flex items-center gap-2 whitespace-nowrap shrink-0"
      >
        <Sparkles class="w-4 h-4" />
        <span>{{ $t('insights.saved_empty_cta') }}</span>
      </button>
    </div>

    <!-- Empty State: Explore Mode -->
    <div
      v-else-if="!insightsStore.currentInsight"
      class="p-8 sm:p-12 text-center rounded-2xl sm:rounded-3xl glass-card border border-slate-200/80 dark:border-white/[0.08] space-y-4"
    >
      <div class="w-12 h-12 rounded-2xl bg-brand-500/10 flex items-center justify-center mx-auto text-brand-500 border border-brand-500/20">
      </div>
      <h3 class="text-lg font-bold text-slate-900 dark:text-white">{{ $t('insights.empty_title') }}</h3>
      <p class="text-sm text-slate-500 dark:text-slate-400 max-w-md mx-auto">{{ $t('insights.empty_desc') }}</p>
      <button
        @click="isGenerateModalOpen = true"
        class="px-5 py-2.5 rounded-xl bg-brand-600 text-white text-sm font-bold hover:bg-brand-500 shadow-md shadow-brand-500/20 transition-all whitespace-nowrap shrink-0"
      >
        {{ $t('insights.generate_ai') }}
      </button>
    </div>

    <!-- Active Insight Card -->
    <div
      v-else
      class="rounded-2xl sm:rounded-3xl glass-card border border-slate-200/80 dark:border-white/[0.08] shadow-xl overflow-hidden transition-all duration-300"
    >
      <!-- Card Top Header -->
      <div class="p-4 sm:p-7 md:p-8 border-b border-slate-100 dark:border-white/[0.06] space-y-3.5 sm:space-y-4">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 sm:gap-3">
          <div class="flex flex-wrap items-center gap-1.5 sm:gap-2">
            <span :class="['px-2.5 sm:px-3 py-1 rounded-full text-xs sm:text-sm font-bold border shrink-0', getCategoryBadge(insightsStore.currentInsight.category).color]">
              {{ getCategoryBadge(insightsStore.currentInsight.category).text }}
            </span>

            <span
              v-for="tag in insightsStore.currentInsight.tags"
              :key="tag"
              class="px-2 py-0.5 rounded-md bg-slate-100 dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] text-xs font-mono font-medium"
            >
              #{{ tag }}
            </span>
          </div>

          <div class="flex items-center justify-between sm:justify-end gap-2 w-full sm:w-auto">
            <!-- Benchmark Badge -->
            <div class="inline-flex items-center gap-1.5 px-2.5 sm:px-3 py-1 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 border border-emerald-200 dark:border-emerald-800/80 text-emerald-700 dark:text-emerald-400 text-xs sm:text-sm font-bold break-words min-w-0 max-w-full">
              <Zap class="w-3.5 h-3.5 fill-emerald-500 text-emerald-500 shrink-0" />
              <span class="truncate sm:whitespace-normal">{{ insightsStore.currentInsight.benchmarkStats }}</span>
            </div>

            <!-- Bookmark Button -->
            <button
              @click="handleToggleBookmark(insightsStore.currentInsight.id)"
              :class="[
                'p-1.5 sm:p-2 rounded-xl transition-all shrink-0 flex items-center gap-1.5 border active:scale-95',
                insightsStore.currentInsight.isBookmarkedByUser
                  ? 'bg-brand-500/10 text-brand-400 border-brand-500/20 shadow-sm'
                  : 'bg-white dark:bg-canvas-subtle text-slate-400 hover:text-brand-400 hover:bg-slate-100 dark:hover:bg-canvas-elevated border-slate-200/80 dark:border-white/[0.08]'
              ]"
              :title="insightsStore.currentInsight.isBookmarkedByUser ? $t('insights.saved') : $t('insights.save_bookmark')"
            >
              <BookmarkCheck v-if="insightsStore.currentInsight.isBookmarkedByUser" class="w-4 h-4 text-brand-400 fill-brand-400/20" />
              <Bookmark v-else class="w-4 h-4" />
              <span class="text-xs font-bold font-mono">{{ insightsStore.currentInsight.bookmarksCount }}</span>
            </button>
          </div>
        </div>

        <h2 class="text-lg sm:text-2xl font-black text-slate-900 dark:text-white tracking-tight leading-snug">
          {{ insightsStore.currentInsight.title }}
        </h2>

        <div
          class="prose dark:prose-invert max-w-none text-sm md:text-lg text-slate-700 dark:text-slate-200 leading-relaxed font-normal"
          v-html="renderedSummaryHtml"
        ></div>
      </div>

      <!-- Code Snippets Showcase -->
      <div class="p-4 sm:p-7 md:p-8 bg-slate-50/60 dark:bg-canvas-subtle/50 border-b border-slate-100 dark:border-white/[0.06] space-y-4 sm:space-y-5">
        <div class="flex items-center justify-between border-b border-slate-200/80 dark:border-white/[0.08] pb-3">
          <div class="flex items-center gap-2">
            <button
              @click="activeCodeTab = 'solution'"
              :class="[
                'flex items-center gap-1.5 sm:gap-2 px-3 sm:px-3.5 py-1.5 rounded-xl text-xs sm:text-sm font-bold transition-all',
                activeCodeTab === 'solution'
                  ? 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300 border border-emerald-500/30'
                  : 'text-slate-500 hover:text-slate-900 dark:hover:text-slate-200'
              ]"
            >
              <CheckCircle2 class="w-3.5 h-3.5 sm:w-4 sm:h-4 text-emerald-500" />
              <span>{{ $t('insights.solution_tab') }}</span>
            </button>

            <button
              @click="activeCodeTab = 'problem'"
              :class="[
                'flex items-center gap-1.5 sm:gap-2 px-3 sm:px-3.5 py-1.5 rounded-xl text-xs sm:text-sm font-bold transition-all',
                activeCodeTab === 'problem'
                  ? 'bg-rose-500/15 text-rose-700 dark:text-rose-300 border border-rose-500/30'
                  : 'text-slate-500 hover:text-slate-900 dark:hover:text-slate-200'
              ]"
            >
              <XCircle class="w-3.5 h-3.5 sm:w-4 sm:h-4 text-rose-500" />
              <span>{{ $t('insights.problem_tab') }}</span>
            </button>
          </div>

          <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider hidden sm:inline">
            Side-by-side Architectural Comparison
          </span>
        </div>

        <!-- Code Block Render -->
        <div class="max-w-full">
          <CommonShikiCodeBlock
            v-if="activeCodeTab === 'solution'"
            :code="insightsStore.currentInsight.solutionSnippet"
            :category="insightsStore.currentInsight.category"
            :tags="insightsStore.currentInsight.tags"
          />
          <CommonShikiCodeBlock
            v-else
            :code="insightsStore.currentInsight.problemSnippet"
            :category="insightsStore.currentInsight.category"
            :tags="insightsStore.currentInsight.tags"
          />
        </div>
      </div>

      <!-- Under The Hood Deep Dive -->
      <div class="p-4 sm:p-8 space-y-3.5 sm:space-y-4">
        <div class="flex items-center gap-2 text-slate-900 dark:text-white font-bold text-sm sm:text-base">
          <Cpu class="w-5 h-5 text-brand-600 dark:text-brand-400" />
          <span>{{ $t('insights.underthehood_title') }}</span>
        </div>

        <div
          class="prose dark:prose-invert max-w-none text-sm md:text-lg text-slate-700 dark:text-slate-300 leading-relaxed"
          v-html="renderedUnderTheHoodHtml"
        ></div>

        <div v-if="insightsStore.currentInsight.sourceUrl" class="pt-3 border-t border-slate-100 dark:border-slate-800 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-1 text-xs sm:text-sm text-slate-400">
          <span>Official Documentation & Architecture Benchmark:</span>
          <a
            :href="insightsStore.currentInsight.sourceUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-1 text-brand-600 dark:text-brand-400 hover:underline font-semibold"
          >
            <span>Learn More</span>
            <ExternalLink class="w-3.5 h-3.5" />
          </a>
        </div>
      </div>

      <!-- Navigation & Action Footer -->
      <div class="p-4 sm:p-6 bg-slate-50 dark:bg-canvas-subtle/70 border-t border-slate-200/80 dark:border-white/[0.06] flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-4">
        <!-- Counter and Keyboard Hint -->
        <div class="flex items-center gap-3 text-xs sm:text-sm font-semibold text-slate-500 dark:text-slate-400">
          <span>
            {{ $t('insights.card_counter', { current: insightsStore.currentIndex + 1, total: insightsStore.insights.length }) }}
          </span>
          <span class="hidden sm:inline-block w-1 h-1 rounded-full bg-slate-300 dark:bg-slate-700"></span>
          <span class="hidden sm:inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-slate-200 dark:bg-slate-800 text-xs font-mono text-slate-600 dark:text-slate-300">
            {{ $t('insights.keyboard_hint') }}
          </span>
        </div>

        <!-- Prev / Next Controls -->
        <div class="flex items-center gap-2 sm:gap-3 w-full sm:w-auto justify-between sm:justify-end">
          <button
            @click="insightsStore.prevInsight()"
            class="flex-1 sm:flex-none flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-700 dark:text-slate-200 text-xs sm:text-sm font-bold hover:bg-slate-50 dark:hover:bg-canvas-elevated transition-colors shadow-sm active:scale-95"
          >
            <ChevronLeft class="w-4 h-4" />
            <span>{{ $t('insights.prev') }}</span>
          </button>

          <button
            @click="insightsStore.nextInsight()"
            class="flex-1 sm:flex-none flex items-center justify-center gap-1.5 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-bold transition-all shadow-md shadow-brand-500/20 active:scale-95"
          >
            <span>{{ $t('insights.next') }}</span>
            <ChevronRight class="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>

    <!-- AI Generator Modal Dialog (Teleported to Body) -->
    <Teleport to="body">
      <div
        v-if="isGenerateModalOpen"
        class="fixed inset-0 z-50 bg-slate-950/75 backdrop-blur-sm flex items-center justify-center p-3 sm:p-4 animate-in fade-in"
        @click.self="isGenerateModalOpen = false"
      >
        <div class="glass-panel rounded-3xl border border-slate-200/80 dark:border-white/[0.08] max-w-lg w-full p-5 sm:p-7 space-y-5 shadow-2xl animate-in zoom-in-95">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2 font-black text-base sm:text-lg text-slate-900 dark:text-white">
              <Sparkles class="w-5 h-5 text-brand-500" />
              <span>{{ $t('insights.generate_modal_title') }}</span>
            </div>
            <button
              @click="isGenerateModalOpen = false"
              class="p-2 rounded-xl text-slate-400 hover:text-slate-700 dark:hover:text-slate-200"
              aria-label="Close generator modal"
            >
              <X class="w-5 h-5" />
            </button>
          </div>

          <p class="text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
            {{ $t('insights.generate_modal_desc') }}
          </p>

          <div class="space-y-3">
            <input
              v-model="customTopicInput"
              @keyup.enter="handleGenerateSubmit"
              type="text"
              :placeholder="$t('insights.generate_topic_placeholder')"
              class="w-full px-4 py-3 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50 dark:bg-canvas-elevated text-sm text-slate-900 dark:text-white placeholder:text-slate-400 focus:outline-none focus:border-brand-500"
              autofocus
            />

            <!-- Topic Inspiration Chips (Only populates input per Rule 11) -->
            <div class="space-y-1.5 pt-1">
              <div class="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
                <span class="font-medium">{{ $t('insights.suggested_topics') }}</span>
                <button
                  type="button"
                  @click="pickRandomTopic"
                  class="inline-flex items-center gap-1 text-brand-600 dark:text-brand-400 hover:underline font-semibold"
                >
                  <Shuffle class="w-3 h-3" />
                  <span>{{ $t('insights.surprise_me') }}</span>
                </button>
              </div>
              <div class="flex flex-wrap gap-1.5 pt-0.5">
                <button
                  v-for="chip in currentSuggestedTopics"
                  :key="chip"
                  type="button"
                  @click="customTopicInput = chip"
                  :class="[
                    'px-2.5 py-1 rounded-lg text-xs font-medium transition-all border text-left',
                    customTopicInput === chip
                      ? 'bg-brand-500/15 text-brand-400 border-brand-500/30 font-bold'
                      : 'bg-slate-100 dark:bg-canvas-subtle text-slate-600 dark:text-slate-300 border-transparent hover:bg-slate-200 dark:hover:bg-canvas-elevated'
                  ]"
                >
                  {{ chip }}
                </button>
              </div>
            </div>
          </div>

          <div class="flex items-center justify-end gap-3 pt-2">
            <button
              @click="isGenerateModalOpen = false"
              class="px-4 py-2.5 rounded-xl text-xs font-bold text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-canvas-elevated transition-colors"
            >
              Cancel
            </button>

            <button
              @click="handleGenerateSubmit"
              :disabled="insightsStore.isGenerating"
              class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs font-bold shadow-md shadow-brand-500/20 disabled:opacity-50 transition-all"
            >
              <div v-if="insightsStore.isGenerating" class="w-3.5 h-3.5 border-2 border-white/20 border-t-white rounded-full animate-spin"></div>
              <span>{{ insightsStore.isGenerating ? $t('insights.generating_btn') : $t('insights.generate_btn') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
