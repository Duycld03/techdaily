<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
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
import MarkdownIt from 'markdown-it'

const { locale } = useI18n()
const insightsStore = useInsightsStore()
const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

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

const categories = [
  { id: null, label: 'insights.all_categories' },
  { id: 0, label: 'insights.cat_frontend' },
  { id: 1, label: 'insights.cat_dotnet' },
  { id: 2, label: 'insights.cat_database' },
  { id: 3, label: 'insights.cat_system' }
]

onMounted(async () => {
  await insightsStore.fetchFeed()
  window.addEventListener('keydown', handleKeyDown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyDown)
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

async function handleCategorySelect(catId: number | null) {
  await insightsStore.fetchFeed(catId)
}

async function handleGenerateSubmit() {
  if (insightsStore.isGenerating) return
  try {
    await insightsStore.generateWithAi(customTopicInput.value, locale.value)
    isGenerateModalOpen.value = false
    customTopicInput.value = ''
  } catch {
    // Handled in store
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
    <div class="p-4 sm:p-7 rounded-2xl sm:rounded-3xl bg-gradient-to-br from-indigo-50/80 via-white to-brand-50/50 dark:from-slate-900 dark:via-slate-900 dark:to-indigo-950/40 border border-slate-200/90 dark:border-slate-800 text-slate-900 dark:text-white shadow-md dark:shadow-xl relative overflow-hidden transition-all">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 sm:gap-6 relative z-10">
        <div class="space-y-1.5 sm:space-y-2">
          <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-indigo-50 dark:bg-indigo-500/20 border border-indigo-200 dark:border-indigo-500/30 text-indigo-700 dark:text-indigo-300 text-xs font-bold uppercase tracking-wider">
            <Lightbulb class="w-3.5 h-3.5" />
            <span>{{ $t('insights.badge') }}</span>
          </div>
          <h1 class="text-xl sm:text-3xl font-black tracking-tight text-slate-900 dark:text-white">
            {{ $t('insights.title') }}
          </h1>
          <p class="text-slate-600 dark:text-slate-300 text-xs sm:text-sm leading-relaxed max-w-xl">
            {{ $t('insights.subtitle') }}
          </p>
        </div>

        <div class="flex flex-wrap sm:flex-nowrap items-center gap-2 sm:gap-3 shrink-0">
          <button
            @click="insightsStore.shuffle()"
            class="flex items-center gap-2 px-3.5 sm:px-4 py-2 sm:py-2.5 rounded-2xl bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-200 text-xs font-bold hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors shadow-sm active:scale-95"
            :title="$t('insights.shuffle')"
          >
            <Shuffle class="w-3.5 h-3.5 text-indigo-500" />
            <span>{{ $t('insights.shuffle') }}</span>
          </button>

          <button
            @click="isGenerateModalOpen = true"
            class="flex items-center gap-2 px-3.5 sm:px-4 py-2 sm:py-2.5 rounded-2xl bg-gradient-to-r from-indigo-600 to-brand-600 hover:from-indigo-500 hover:to-brand-500 text-white text-xs font-bold transition-all shadow-md shadow-indigo-500/20 active:scale-95"
          >
            <Plus class="w-3.5 h-3.5 sm:w-4 sm:h-4" />
            <span>{{ $t('insights.generate_ai') }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Category Filter Bar -->
    <div class="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none -mx-4 px-4 sm:mx-0 sm:px-0">
      <button
        v-for="cat in categories"
        :key="String(cat.id)"
        @click="handleCategorySelect(cat.id)"
        :class="[
          'px-3.5 sm:px-4 py-2 rounded-xl text-xs font-bold transition-all shrink-0 border',
          insightsStore.selectedCategory === cat.id
            ? 'bg-slate-900 text-white dark:bg-white dark:text-slate-950 border-transparent shadow-sm'
            : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/60'
        ]"
      >
        {{ $t(cat.label) }}
      </button>
    </div>

    <!-- Main Card Container -->
    <div v-if="insightsStore.isLoading" class="flex flex-col items-center justify-center py-20 space-y-4">
      <div class="w-10 h-10 border-4 border-indigo-500/20 border-t-indigo-600 rounded-full animate-spin"></div>
      <p class="text-sm font-semibold text-slate-500 dark:text-slate-400">Loading senior technical insights...</p>
    </div>

    <!-- Empty State -->
    <div
      v-else-if="!insightsStore.currentInsight"
      class="p-8 sm:p-12 text-center rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 space-y-4"
    >
      <div class="w-12 h-12 rounded-2xl bg-indigo-50 dark:bg-indigo-950/50 flex items-center justify-center mx-auto text-indigo-600 dark:text-indigo-400">
        <Sparkles class="w-6 h-6" />
      </div>
      <h3 class="text-lg font-bold text-slate-900 dark:text-white">{{ $t('insights.empty_title') }}</h3>
      <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 max-w-md mx-auto">{{ $t('insights.empty_desc') }}</p>
      <button
        @click="isGenerateModalOpen = true"
        class="px-5 py-2.5 rounded-xl bg-indigo-600 text-white text-xs font-bold hover:bg-indigo-500 shadow-md shadow-indigo-500/20 transition-all"
      >
        {{ $t('insights.generate_ai') }}
      </button>
    </div>

    <!-- Active Insight Card -->
    <div
      v-else
      class="rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl overflow-hidden transition-all duration-300"
    >
      <!-- Card Top Header -->
      <div class="p-4 sm:p-7 md:p-8 border-b border-slate-100 dark:border-slate-800/80 space-y-3.5 sm:space-y-4">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 sm:gap-3">
          <div class="flex flex-wrap items-center gap-1.5 sm:gap-2">
            <span :class="['px-2.5 sm:px-3 py-1 rounded-full text-xs font-bold border shrink-0', getCategoryBadge(insightsStore.currentInsight.category).color]">
              {{ getCategoryBadge(insightsStore.currentInsight.category).text }}
            </span>

            <span
              v-for="tag in insightsStore.currentInsight.tags"
              :key="tag"
              class="px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 text-[11px] font-mono font-medium"
            >
              #{{ tag }}
            </span>
          </div>

          <div class="flex items-center justify-between sm:justify-end gap-2 w-full sm:w-auto">
            <!-- Benchmark Badge -->
            <div class="inline-flex items-center gap-1.5 px-2.5 sm:px-3 py-1 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 border border-emerald-200 dark:border-emerald-800/80 text-emerald-700 dark:text-emerald-400 text-xs font-bold break-words min-w-0 max-w-full">
              <Zap class="w-3.5 h-3.5 fill-emerald-500 text-emerald-500 shrink-0" />
              <span class="truncate sm:whitespace-normal">{{ insightsStore.currentInsight.benchmarkStats }}</span>
            </div>

            <!-- Bookmark Button -->
            <button
              @click="insightsStore.toggleBookmark(insightsStore.currentInsight.id)"
              class="p-1.5 sm:p-2 rounded-xl text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors shrink-0"
              :title="$t('insights.save_bookmark')"
            >
              <BookmarkCheck v-if="insightsStore.currentInsight.isBookmarkedByUser" class="w-5 h-5 text-indigo-600 dark:text-indigo-400 fill-indigo-600/20" />
              <Bookmark v-else class="w-5 h-5" />
            </button>
          </div>
        </div>

        <h2 class="text-lg sm:text-2xl font-black text-slate-900 dark:text-white tracking-tight leading-snug">
          {{ insightsStore.currentInsight.title }}
        </h2>

        <div
          class="prose prose-sm dark:prose-invert max-w-none text-xs sm:text-base text-slate-600 dark:text-slate-300 leading-relaxed font-normal"
          v-html="renderedSummaryHtml"
        ></div>
      </div>

      <!-- Code Snippets Showcase -->
      <div class="p-4 sm:p-7 md:p-8 bg-slate-50/60 dark:bg-slate-950/40 border-b border-slate-100 dark:border-slate-800/80 space-y-4 sm:space-y-5">
        <div class="flex items-center justify-between border-b border-slate-200 dark:border-slate-800 pb-3">
          <div class="flex items-center gap-2">
            <button
              @click="activeCodeTab = 'solution'"
              :class="[
                'flex items-center gap-1.5 sm:gap-2 px-3 sm:px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all',
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
                'flex items-center gap-1.5 sm:gap-2 px-3 sm:px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all',
                activeCodeTab === 'problem'
                  ? 'bg-rose-500/15 text-rose-700 dark:text-rose-300 border border-rose-500/30'
                  : 'text-slate-500 hover:text-slate-900 dark:hover:text-slate-200'
              ]"
            >
              <XCircle class="w-3.5 h-3.5 sm:w-4 sm:h-4 text-rose-500" />
              <span>{{ $t('insights.problem_tab') }}</span>
            </button>
          </div>

          <span class="text-[11px] font-semibold text-slate-400 uppercase tracking-wider hidden sm:inline">
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
          <Cpu class="w-4.5 h-4.5 sm:w-5 sm:h-5 text-indigo-600 dark:text-indigo-400" />
          <span>{{ $t('insights.underthehood_title') }}</span>
        </div>

        <div
          class="prose prose-sm dark:prose-invert max-w-none text-xs sm:text-sm text-slate-700 dark:text-slate-300 leading-relaxed"
          v-html="renderedUnderTheHoodHtml"
        ></div>

        <div v-if="insightsStore.currentInsight.sourceUrl" class="pt-3 border-t border-slate-100 dark:border-slate-800 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-1 text-xs text-slate-400">
          <span>Official Documentation & Architecture Benchmark:</span>
          <a
            :href="insightsStore.currentInsight.sourceUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-1 text-indigo-600 dark:text-indigo-400 hover:underline font-semibold"
          >
            <span>Learn More</span>
            <ExternalLink class="w-3.5 h-3.5" />
          </a>
        </div>
      </div>

      <!-- Navigation & Action Footer -->
      <div class="p-4 sm:p-6 bg-slate-50 dark:bg-slate-950/60 border-t border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-4">
        <!-- Counter and Keyboard Hint -->
        <div class="flex items-center gap-3 text-xs font-semibold text-slate-500 dark:text-slate-400">
          <span>
            {{ $t('insights.card_counter', { current: insightsStore.currentIndex + 1, total: insightsStore.insights.length }) }}
          </span>
          <span class="hidden sm:inline-block w-1 h-1 rounded-full bg-slate-300 dark:bg-slate-700"></span>
          <span class="hidden sm:inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-slate-200 dark:bg-slate-800 text-[10px] font-mono text-slate-600 dark:text-slate-300">
            {{ $t('insights.keyboard_hint') }}
          </span>
        </div>

        <!-- Prev / Next Controls -->
        <div class="flex items-center gap-2 sm:gap-3 w-full sm:w-auto justify-between sm:justify-end">
          <button
            @click="insightsStore.prevInsight()"
            class="flex-1 sm:flex-none flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 text-xs font-bold hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors shadow-sm active:scale-95"
          >
            <ChevronLeft class="w-4 h-4" />
            <span>{{ $t('insights.prev') }}</span>
          </button>

          <button
            @click="insightsStore.nextInsight()"
            class="flex-1 sm:flex-none flex items-center justify-center gap-1.5 px-5 py-2.5 rounded-xl bg-slate-900 dark:bg-white text-white dark:text-slate-900 text-xs font-bold hover:bg-slate-800 dark:hover:bg-slate-100 transition-all shadow-md active:scale-95"
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
        <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 max-w-lg w-full p-5 sm:p-7 space-y-5 shadow-2xl animate-in zoom-in-95">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2 font-black text-base sm:text-lg text-slate-900 dark:text-white">
              <Sparkles class="w-5 h-5 text-indigo-500" />
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

          <div class="space-y-2">
            <input
              v-model="customTopicInput"
              @keyup.enter="handleGenerateSubmit"
              type="text"
              :placeholder="$t('insights.generate_topic_placeholder')"
              class="w-full px-4 py-3 rounded-2xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/80 text-sm text-slate-900 dark:text-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              autofocus
            />
          </div>

          <div class="flex items-center justify-end gap-3 pt-2">
            <button
              @click="isGenerateModalOpen = false"
              class="px-4 py-2.5 rounded-xl text-xs font-bold text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
            >
              Cancel
            </button>

            <button
              @click="handleGenerateSubmit"
              :disabled="insightsStore.isGenerating"
              class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold shadow-md shadow-indigo-500/20 disabled:opacity-50 transition-all"
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
