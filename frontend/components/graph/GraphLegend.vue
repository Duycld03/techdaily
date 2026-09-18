<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
  HelpCircle,
  ChevronDown,
  ChevronUp,
  Layers,
  Sparkles
} from 'lucide-vue-next'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

const store = useKnowledgeGraphStore()
const STORAGE_KEY = 'techdaily_graph_legend_collapsed'

function getInitialCollapsedState(): boolean {
  if (typeof window === 'undefined') return false
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved !== null) {
      return saved === 'true'
    }
    return window.innerWidth < 640
  } catch {
    return false
  }
}

const isCollapsed = ref(getInitialCollapsedState())

function toggleCollapse() {
  isCollapsed.value = !isCollapsed.value
  if (typeof window !== 'undefined') {
    try {
      localStorage.setItem(STORAGE_KEY, String(isCollapsed.value))
    } catch {
      // ignore localStorage error
    }
  }
}

function onHover(type: string | null) {
  store.setHoveredLegendType(type)
}

const entityItems = [
  {
    type: 'pillar',
    labelKey: 'graph.legend.pillar',
    color: 'bg-sky-500',
    shape: 'rounded-full w-3.5 h-3.5 ring-2 ring-sky-400/40 shadow-sm shadow-sky-500/40'
  },
  {
    type: 'topic',
    labelKey: 'graph.legend.topic',
    color: 'bg-amber-500',
    shape: 'rounded-full w-2.5 h-2.5 shadow-sm shadow-amber-500/30'
  },
  {
    type: 'book',
    labelKey: 'graph.legend.book',
    color: 'bg-indigo-500',
    shape: 'rounded-sm w-3 h-2.5 shadow-sm shadow-indigo-500/30'
  },
  {
    type: 'highlight',
    labelKey: 'graph.legend.highlight',
    color: 'bg-cyan-500',
    shape: 'w-2.5 h-2.5 rotate-45 rounded-[2px] shadow-sm shadow-cyan-500/30'
  }
]

const masteryItems = [
  {
    type: 'learning',
    labelKey: 'graph.legend.learning',
    color: 'bg-amber-500',
    shape: 'rounded-full w-2 h-2'
  },
  {
    type: 'reviewing',
    labelKey: 'graph.legend.reviewing',
    color: 'bg-blue-500',
    shape: 'rounded-full w-2 h-2'
  },
  {
    type: 'mastered',
    labelKey: 'graph.legend.mastered',
    color: 'bg-emerald-500',
    shape: 'rounded-full w-2 h-2'
  }
]
</script>

<template>
  <div class="pointer-events-auto select-none transition-all duration-300">
    <!-- Collapsed Trigger Pill Button -->
    <button
      v-if="isCollapsed"
      type="button"
      data-testid="legend-expand-btn"
      class="inline-flex items-center gap-2 px-3 py-2 rounded-2xl bg-white/85 dark:bg-slate-900/85 backdrop-blur-md border border-slate-200/80 dark:border-slate-800/80 shadow-lg text-slate-700 dark:text-slate-300 hover:text-brand-600 dark:hover:text-brand-400 text-xs font-semibold transition-all active:scale-95"
      :title="$t('graph.legend.title')"
      @click="toggleCollapse"
    >
      <HelpCircle class="w-4 h-4 text-brand-500" />
      <span class="whitespace-nowrap shrink-0">{{ $t('graph.legend.title') }}</span>
      <ChevronUp class="w-3.5 h-3.5 text-slate-400" />
    </button>

    <!-- Expanded Legend Card -->
    <div
      v-else
      data-testid="legend-card"
      class="w-56 sm:w-60 rounded-2xl bg-white/90 dark:bg-slate-900/90 backdrop-blur-md border border-slate-200/80 dark:border-slate-800/80 shadow-xl p-3 space-y-2.5 text-xs text-slate-700 dark:text-slate-300"
    >
      <!-- Header with title & collapse button -->
      <div class="flex items-center justify-between pb-1.5 border-b border-slate-100 dark:border-slate-800/60">
        <div class="flex items-center gap-1.5 font-bold text-slate-900 dark:text-white tracking-wide">
          <Layers class="w-3.5 h-3.5 text-brand-500" />
          <span>{{ $t('graph.legend.title') }}</span>
        </div>
        <button
          type="button"
          data-testid="legend-collapse-btn"
          class="p-1 rounded-lg text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
          :title="$t('graph.legend.collapse')"
          @click="toggleCollapse"
        >
          <ChevronDown class="w-3.5 h-3.5" />
        </button>
      </div>

      <!-- Entity Types Key -->
      <div class="space-y-1.5">
        <div class="text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
          {{ $t('graph.legend.entities') }}
        </div>
        <div class="grid grid-cols-2 gap-1">
          <div
            v-for="item in entityItems"
            :key="item.type"
            :data-testid="`legend-item-${item.type}`"
            :class="[
              'flex items-center gap-2 px-2 py-1 rounded-lg cursor-pointer transition-all',
              store.hoveredLegendType === item.type
                ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-bold'
                : 'hover:bg-slate-100/80 dark:hover:bg-slate-800/60'
            ]"
            @mouseenter="onHover(item.type)"
            @mouseleave="onHover(null)"
          >
            <div :class="[item.color, item.shape, 'shrink-0']" />
            <span class="truncate text-[11px]">{{ $t(item.labelKey) }}</span>
          </div>
        </div>
      </div>

      <!-- Flashcard Mastery Status Key -->
      <div class="space-y-1.5 pt-1.5 border-t border-slate-100 dark:border-slate-800/60">
        <div class="flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
          <Sparkles class="w-3 h-3 text-amber-500" />
          <span>{{ $t('graph.legend.sm2Status') }}</span>
        </div>
        <div class="flex items-center justify-between gap-1 px-1">
          <div
            v-for="item in masteryItems"
            :key="item.type"
            :data-testid="`legend-item-${item.type}`"
            :class="[
              'flex items-center gap-1.5 px-1.5 py-1 rounded-lg cursor-pointer transition-all',
              store.hoveredLegendType === item.type
                ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-bold'
                : 'hover:bg-slate-100/80 dark:hover:bg-slate-800/60'
            ]"
            @mouseenter="onHover(item.type)"
            @mouseleave="onHover(null)"
          >
            <div :class="[item.color, item.shape, 'shrink-0']" />
            <span class="text-[10px] whitespace-nowrap">{{ $t(item.labelKey) }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
