<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount } from 'vue'
import {
  X,
  BookOpen,
  Layers,
  Highlighter,
  HelpCircle,
  Map,
  Quote,
  Clock,
  Sparkles,
  ExternalLink,
  Tag,
  Landmark,
  Filter
} from 'lucide-vue-next'
import { useKnowledgeGraphStore, type GraphNode } from '~/stores/useKnowledgeGraphStore'

const store = useKnowledgeGraphStore()

const node = computed<GraphNode | null>(() => store.selectedNode)
const isOpen = computed<boolean>(() => !!node.value)

function close() {
  store.selectNode(null)
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && isOpen.value) {
    close()
  }
}

onMounted(() => {
  window.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKeydown)
})

// Node type detection
const nodeType = computed(() => node.value?.type?.toLowerCase() || '')

// Helper for topic slug
const topicSlug = computed(() => {
  if (!node.value?.label) return ''
  return node.value.label
    .toLowerCase()
    .trim()
    .replace(/[^\w\s-]/g, '')
    .replace(/\s+/g, '-')
})

// Navigation bridge links
const quizRoute = computed(() => `/quiz?topic=${topicSlug.value}`)
const roadmapRoute = computed(() => {
  if (node.value?.dayOrder) {
    return `/roadmap#${node.value.dayOrder}`
  }
  return '/roadmap'
})

const libraryRoute = '/library'
const readBookRoute = computed(() => {
  const bId = node.value?.bookId || node.value?.id || ''
  return `/read/${bId}`
})

const cardReviewRoute = computed(() => {
  if (node.value?.id) {
    return `/review?cardId=${node.value.id}`
  }
  return '/review'
})

const highlightReadRoute = computed(() => {
  const bId = node.value?.bookId || node.value?.id || ''
  const chunk = node.value?.documentChunkId ? `#slice-${node.value.documentChunkId}` : ''
  return `/read/${bId}${chunk}`
})

const highlightNotesRoute = computed(() => {
  if (node.value?.id) {
    return `/notes?highlightId=${node.value.id}`
  }
  return '/notes'
})

// Connected entities for architectural pillar nodes
const connectedTopicsCount = computed(() => {
  if (!node.value || nodeType.value !== 'pillar') return 0
  const nodeId = node.value.id
  const edges = store.rawData?.edges || []
  const edgeCount = edges.filter(
    (e) => (e.target === nodeId || e.source === nodeId) &&
      (e.relationType?.toLowerCase() === 'topictopillar' ||
       store.rawData?.nodes?.some(n => (n.id === e.source || n.id === e.target) && n.id !== nodeId && n.type?.toLowerCase() === 'topic'))
  ).length
  if (edgeCount > 0) return edgeCount

  if (node.value.category && store.rawData?.nodes) {
    return store.rawData.nodes.filter(
      (n) => n.id !== nodeId && n.type?.toLowerCase() === 'topic' && n.category?.toLowerCase() === node.value?.category?.toLowerCase()
    ).length
  }
  return 0
})

const connectedBooksCount = computed(() => {
  if (!node.value || nodeType.value !== 'pillar') return 0
  const nodeId = node.value.id
  const edges = store.rawData?.edges || []
  const edgeCount = edges.filter(
    (e) => (e.target === nodeId || e.source === nodeId) &&
      (e.relationType?.toLowerCase() === 'booktopillar' ||
       store.rawData?.nodes?.some(n => (n.id === e.source || n.id === e.target) && n.id !== nodeId && n.type?.toLowerCase() === 'book'))
  ).length
  if (edgeCount > 0) return edgeCount

  if (node.value.category && store.rawData?.nodes) {
    return store.rawData.nodes.filter(
      (n) => n.id !== nodeId && n.type?.toLowerCase() === 'book' && n.category?.toLowerCase() === node.value?.category?.toLowerCase()
    ).length
  }
  return 0
})

function filterToThisPillar() {
  if (node.value?.category) {
    store.setCategory(node.value.category)
  }
  close()
}

// Estimated next review calculation for card
const formattedNextReview = computed(() => {
  if (!node.value) return ''
  const days = node.value.intervalDays ?? 1
  if (days <= 1) return 'Tomorrow'
  if (days < 7) return `In ${days} days`
  const weeks = Math.round(days / 7)
  return `In ~${weeks} ${weeks === 1 ? 'week' : 'weeks'}`
})

// Category badge color
function getCategoryBadgeClass(category?: string | null): string {
  const cat = category?.toLowerCase() || ''
  if (cat.includes('dotnet') || cat.includes('backend')) {
    return 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border-sky-500/20'
  }
  if (cat.includes('postgres') || cat.includes('database') || cat.includes('storage')) {
    return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20'
  }
  if (cat.includes('system') || cat.includes('distributed')) {
    return 'bg-violet-500/10 text-violet-600 dark:text-violet-400 border-violet-500/20'
  }
  if (cat.includes('frontend') || cat.includes('web')) {
    return 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20'
  }
  if (cat.includes('craft')) {
    return 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20'
  }
  return 'bg-slate-500/10 text-slate-600 dark:text-slate-400 border-slate-500/20'
}

// Mastery badge class
function getMasteryBadgeClass(status?: string | null): string {
  const s = status?.toLowerCase()
  if (s === 'mastered') {
    return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/30'
  }
  if (s === 'learning') {
    return 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/30'
  }
  return 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/30'
}
</script>

<template>
  <div v-if="isOpen">
    <!-- Backdrop overlay -->
    <div
      class="fixed inset-0 bg-slate-950/40 dark:bg-black/60 backdrop-blur-sm z-40 transition-opacity duration-200"
      @click="close"
    />

    <!-- Drawer container: slide-over on desktop, bottom sheet on mobile -->
    <aside
      class="fixed z-50 bg-white dark:bg-canvas-subtle border-slate-200 dark:border-white/[0.08] shadow-2xl flex flex-col overflow-hidden transition-transform duration-300 ease-out
        inset-x-0 bottom-0 max-h-[85vh] rounded-t-3xl border-t
        md:inset-x-auto md:right-0 md:top-16 md:bottom-0 md:w-96 md:max-w-md md:max-h-full md:rounded-none md:border-t-0 md:border-l"
      role="dialog"
      aria-modal="true"
      aria-labelledby="drawer-node-title"
    >
      <!-- Mobile drag handle indicator -->
      <div class="md:hidden flex justify-center pt-2.5 pb-1">
        <div class="w-10 h-1 rounded-full bg-slate-300 dark:bg-white/20" />
      </div>

      <!-- Drawer Header -->
      <div class="p-4 sm:p-5 border-b border-slate-100 dark:border-white/[0.06] flex items-start justify-between gap-3">
        <div class="flex flex-wrap items-center gap-1.5 min-w-0">
          <!-- Type Badge -->
          <span
            class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-slate-100 dark:bg-canvas-elevated text-slate-700 dark:text-slate-200 border border-slate-200/60 dark:border-white/[0.08] whitespace-nowrap shrink-0"
          >
            <Map v-if="nodeType === 'topic'" class="w-3.5 h-3.5 text-sky-500 shrink-0" />
            <BookOpen v-else-if="nodeType === 'book'" class="w-3.5 h-3.5 text-slate-500 shrink-0" />
            <Layers v-else-if="nodeType === 'card'" class="w-3.5 h-3.5 text-amber-500 shrink-0" />
            <Highlighter v-else-if="nodeType === 'highlight'" class="w-3.5 h-3.5 text-violet-500 shrink-0" />
            <Landmark v-else-if="nodeType === 'pillar'" class="w-3.5 h-3.5 text-brand-500 shrink-0" />
            <Sparkles v-else class="w-3.5 h-3.5 text-brand-500 shrink-0" />
            <span class="capitalize whitespace-nowrap shrink-0">{{ node?.type }}</span>
          </span>

          <!-- Category Badge -->
          <span
            v-if="node?.category"
            :class="[
              'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border whitespace-nowrap shrink-0',
              getCategoryBadgeClass(node.category)
            ]"
          >
            {{ node.category }}
          </span>

          <!-- Difficulty Badge (Topic) -->
          <span
            v-if="node?.difficulty"
            class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 whitespace-nowrap shrink-0"
          >
            {{ node.difficulty }}
          </span>

          <!-- Status Badge (Card) -->
          <span
            v-if="node?.status && nodeType === 'card'"
            :class="[
              'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold border whitespace-nowrap shrink-0',
              getMasteryBadgeClass(node.status)
            ]"
          >
            {{ node.status }}
          </span>
        </div>

        <!-- Close Button -->
        <button
          type="button"
          class="p-1.5 rounded-xl text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-white/[0.08] transition-colors whitespace-nowrap shrink-0"
          aria-label="Close detail drawer"
          @click="close"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Drawer Body (Scrollable) -->
      <div class="flex-1 overflow-y-auto p-4 sm:p-5 space-y-4">
        <!-- Title & Subtitle -->
        <div>
          <h3
            id="drawer-node-title"
            class="text-base sm:text-lg font-bold text-slate-900 dark:text-white leading-snug break-words"
          >
            {{ node?.label }}
          </h3>
          <p
            v-if="node?.subtitle"
            class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-1"
          >
            {{ node.subtitle }}
          </p>
          <p
            v-if="node?.dayOrder && nodeType === 'topic'"
            class="text-xs text-brand-600 dark:text-brand-400 font-medium mt-1 flex items-center gap-1.5"
          >
            <span>Curriculum Day {{ node.dayOrder }}</span>
          </p>
        </div>

        <!-- Pillar Section: Domain Summary & Metrics -->
        <div v-if="nodeType === 'pillar'" class="space-y-3">
          <div v-if="node?.summary" class="space-y-1.5">
            <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {{ $t('graph.drawer.takeaways') }}
            </h4>
            <p class="text-xs sm:text-sm text-slate-700 dark:text-slate-300 leading-relaxed bg-slate-50 dark:bg-canvas-elevated p-3 rounded-xl border border-slate-200/60 dark:border-white/[0.08]">
              {{ node.summary }}
            </p>
          </div>

          <!-- Pillar Metrics: Connected Topics & Books -->
          <div class="space-y-1.5">
            <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {{ $t('graph.drawer.metrics') }}
            </h4>
            <div class="grid grid-cols-2 gap-2">
              <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-canvas-elevated border border-slate-200/60 dark:border-white/[0.08]">
                <span class="text-[11px] font-medium text-slate-400 dark:text-slate-500 block">
                  {{ $t('graph.drawer.connectedTopics') }}
                </span>
                <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block" data-test="pillar-topics-count">
                  {{ connectedTopicsCount }}
                </span>
              </div>
              <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-canvas-elevated border border-slate-200/60 dark:border-white/[0.08]">
                <span class="text-[11px] font-medium text-slate-400 dark:text-slate-500 block">
                  {{ $t('graph.drawer.connectedBooks') }}
                </span>
                <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block" data-test="pillar-books-count">
                  {{ connectedBooksCount }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Topic Section: Key Takeaways -->
        <div v-if="nodeType === 'topic' && node?.summary" class="space-y-1.5">
          <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('graph.drawer.takeaways') }}
          </h4>
          <p class="text-xs sm:text-sm text-slate-700 dark:text-slate-300 leading-relaxed bg-slate-50 dark:bg-canvas-elevated p-3 rounded-xl border border-slate-200/60 dark:border-white/[0.08]">
            {{ node.summary }}
          </p>
        </div>

        <!-- Book Section: Metadata & Summary -->
        <div v-if="nodeType === 'book'" class="space-y-3">
          <div v-if="node?.summary" class="space-y-1.5">
            <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              Overview
            </h4>
            <p class="text-xs sm:text-sm text-slate-700 dark:text-slate-300 leading-relaxed bg-slate-50 dark:bg-canvas-elevated p-3 rounded-xl border border-slate-200/60 dark:border-white/[0.08]">
              {{ node.summary }}
            </p>
          </div>
        </div>

        <!-- Card Section: Spaced Repetition SM-2 Metrics -->
        <div v-if="nodeType === 'card'" class="space-y-2">
          <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('graph.drawer.metrics') }}
          </h4>
          <div class="grid grid-cols-2 gap-2">
            <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-canvas-elevated border border-slate-200/60 dark:border-white/[0.08]">
              <span class="text-[11px] font-medium text-slate-400 dark:text-slate-500 block">
                {{ $t('graph.drawer.intervalDays') }}
              </span>
              <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block">
                {{ node.intervalDays ?? 0 }} days
              </span>
            </div>
            <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-canvas-elevated border border-slate-200/60 dark:border-white/[0.08]">
              <span class="text-[11px] font-medium text-slate-400 dark:text-slate-500 block">
                {{ $t('graph.drawer.easeFactor') }}
              </span>
              <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block">
                {{ typeof node.easeFactor === 'number' ? node.easeFactor.toFixed(2) : (node.easeFactor ?? '2.50') }}
              </span>
            </div>
            <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-canvas-elevated border border-slate-200/60 dark:border-white/[0.08]">
              <span class="text-[11px] font-medium text-slate-400 dark:text-slate-500 block">
                {{ $t('graph.drawer.repetitions') }}
              </span>
              <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block">
                {{ node.repetitionCount ?? 0 }} reviews
              </span>
            </div>
            <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-canvas-elevated border border-slate-200/60 dark:border-white/[0.08]">
              <span class="text-[11px] font-medium text-slate-400 dark:text-slate-500 block">
                {{ $t('graph.drawer.nextReview') }}
              </span>
              <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block">
                {{ formattedNextReview }}
              </span>
            </div>
          </div>
        </div>

        <!-- Highlight Section: Blockquote Excerpt & Personal Reflection -->
        <div v-if="nodeType === 'highlight'" class="space-y-3">
          <div class="space-y-1.5">
            <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 flex items-center gap-1.5">
              <Quote class="w-3.5 h-3.5 text-violet-500" />
              {{ $t('graph.drawer.quote') }}
            </h4>
            <blockquote
              class="relative pl-3.5 pr-3 py-2.5 border-l-4 border-violet-500 bg-violet-500/5 dark:bg-violet-500/10 rounded-r-xl text-xs sm:text-sm italic text-slate-800 dark:text-slate-200 leading-relaxed"
            >
              "{{ node.label }}"
            </blockquote>
          </div>

          <div v-if="node?.summary" class="space-y-1.5">
            <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {{ $t('graph.drawer.note') }}
            </h4>
            <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-canvas-elevated p-3 rounded-xl border border-slate-200/60 dark:border-white/[0.08]">
              {{ node.summary }}
            </p>
          </div>
        </div>

        <!-- Associated Tags -->
        <div v-if="node?.tags && node.tags.length" class="space-y-1.5">
          <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 flex items-center gap-1.5">
            <Tag class="w-3.5 h-3.5" />
            Tags
          </h4>
          <div class="flex flex-wrap gap-1.5">
            <span
              v-for="tag in node.tags"
              :key="tag"
              class="px-2 py-0.5 rounded-md text-[11px] font-medium bg-slate-100 dark:bg-canvas-elevated text-slate-600 dark:text-slate-300 border border-transparent dark:border-white/[0.06] whitespace-nowrap shrink-0"
            >
              #{{ tag }}
            </span>
          </div>
        </div>
      </div>

      <!-- Drawer Action Bridges (Footer) -->
      <div
        class="p-4 border-t border-slate-200/80 dark:border-white/[0.06] bg-slate-50/70 dark:bg-canvas-subtle/90 flex flex-wrap gap-2 items-center justify-end"
      >
        <!-- Topic Actions -->
        <template v-if="nodeType === 'topic'">
          <NuxtLink
            :to="quizRoute"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-brand-600 hover:bg-brand-500 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0"
          >
            <HelpCircle class="w-4 h-4 shrink-0" />
            <span class="whitespace-nowrap shrink-0">{{ $t('graph.drawer.practiceQuiz') }}</span>
          </NuxtLink>
          <NuxtLink
            :to="roadmapRoute"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-slate-200/80 hover:bg-slate-300 dark:bg-white/[0.06] dark:hover:bg-white/[0.12] border border-transparent dark:border-white/[0.08] text-slate-800 dark:text-slate-200 transition-all active:scale-95 whitespace-nowrap shrink-0"
          >
            <Map class="w-4 h-4 shrink-0" />
            <span class="whitespace-nowrap shrink-0">{{ $t('graph.drawer.viewRoadmap') }}</span>
          </NuxtLink>
        </template>

        <!-- Book Actions -->
        <template v-else-if="nodeType === 'book'">
          <NuxtLink
            :to="libraryRoute"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-slate-200/80 hover:bg-slate-300 dark:bg-white/[0.06] dark:hover:bg-white/[0.12] border border-transparent dark:border-white/[0.08] text-slate-800 dark:text-slate-200 transition-all active:scale-95 whitespace-nowrap shrink-0"
          >
            <BookOpen class="w-4 h-4 shrink-0" />
            <span class="whitespace-nowrap shrink-0">{{ $t('graph.drawer.browseLibrary') }}</span>
          </NuxtLink>
          <NuxtLink
            :to="readBookRoute"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-brand-600 hover:bg-brand-500 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0"
          >
            <ExternalLink class="w-4 h-4 shrink-0" />
            <span class="whitespace-nowrap shrink-0">{{ $t('graph.drawer.readSlices') }}</span>
          </NuxtLink>
        </template>

        <!-- Card Actions -->
        <template v-else-if="nodeType === 'card'">
          <NuxtLink
            :to="cardReviewRoute"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-brand-600 hover:bg-brand-500 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0"
          >
            <Layers class="w-4 h-4 shrink-0" />
            <span class="whitespace-nowrap shrink-0">{{ $t('graph.drawer.reviewFlashcard') }}</span>
          </NuxtLink>
        </template>

        <!-- Highlight Actions -->
        <template v-else-if="nodeType === 'highlight'">
          <NuxtLink
            :to="highlightNotesRoute"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-slate-200/80 hover:bg-slate-300 dark:bg-white/[0.06] dark:hover:bg-white/[0.12] border border-transparent dark:border-white/[0.08] text-slate-800 dark:text-slate-200 transition-all active:scale-95 whitespace-nowrap shrink-0"
          >
            <Highlighter class="w-4 h-4 shrink-0" />
            <span class="whitespace-nowrap shrink-0">{{ $t('graph.drawer.viewInNotes') }}</span>
          </NuxtLink>
          <NuxtLink
            :to="highlightReadRoute"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-brand-600 hover:bg-brand-500 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0"
          >
            <BookOpen class="w-4 h-4 shrink-0" />
            <span class="whitespace-nowrap shrink-0">{{ $t('graph.drawer.readChapter') }}</span>
          </NuxtLink>
        </template>

        <!-- Pillar Actions -->
        <template v-else-if="nodeType === 'pillar'">
          <button
            type="button"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-brand-600 hover:bg-brand-500 text-white shadow-sm transition-all active:scale-95 whitespace-nowrap shrink-0"
            data-test="filter-to-pillar"
            @click="filterToThisPillar"
          >
            <Filter class="w-4 h-4 shrink-0" />
            <span class="whitespace-nowrap shrink-0">{{ $t('graph.drawer.filterToPillar') }}</span>
          </button>
        </template>
      </div>
    </aside>
  </div>
</template>
