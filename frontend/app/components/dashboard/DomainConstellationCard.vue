<script setup lang="ts">
import { computed } from 'vue'
import { Network, Compass, Layers, Bookmark, FileText } from 'lucide-vue-next'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

const props = withDefaults(
  defineProps<{
    nodeCount?: number
    edgeCount?: number
    cardCount?: number
    highlightCount?: number
    chunkCount?: number
    activePillar?: string
  }>(),
  {
    nodeCount: undefined,
    edgeCount: undefined,
    cardCount: undefined,
    highlightCount: undefined,
    chunkCount: undefined,
    activePillar: 'Distributed Systems'
  }
)

const graphStore = useKnowledgeGraphStore()

const displayNodeCount = computed(() => {
  return props.nodeCount ?? graphStore.rawData?.stats?.totalNodes ?? graphStore.rawData?.nodes?.length ?? 148
})

const displayEdgeCount = computed(() => {
  return props.edgeCount ?? graphStore.rawData?.stats?.totalEdges ?? graphStore.rawData?.edges?.length ?? 210
})

const flashcardCount = computed(() => {
  if (props.cardCount !== undefined) return props.cardCount
  return graphStore.rawData?.stats?.nodeTypeCounts?.['card'] ?? 0
})

const highlightCount = computed(() => {
  if (props.highlightCount !== undefined) return props.highlightCount
  return graphStore.rawData?.stats?.nodeTypeCounts?.['highlight'] ?? 0
})

const chunkCount = computed(() => {
  if (props.chunkCount !== undefined) return props.chunkCount
  return graphStore.rawData?.stats?.nodeTypeCounts?.['chunk'] ?? 0
})
</script>

<template>
  <div class="glass-card p-3 sm:p-3.5 flex flex-col justify-between group hover:border-white/[0.12] transition-all shrink-0">
    <!-- Header -->
    <div class="flex items-center justify-between mb-2">
      <div class="flex items-center gap-2">
        <div class="w-7 h-7 rounded-lg bg-brand-500/10 text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
          <Network class="w-4 h-4" :stroke-width="1.5" />
        </div>
        <span class="text-xs font-bold text-slate-800 dark:text-slate-200">
          {{ $t('dashboard.domain_constellation') }}
        </span>
      </div>

      <NuxtLink
        to="/graph"
        class="text-xs font-semibold text-brand-400 hover:text-brand-300 flex items-center gap-1 transition-colors"
      >
        <span>{{ $t('dashboard.open_cosmos') }}</span>
        <Compass class="w-3.5 h-3.5 shrink-0" :stroke-width="1.5" />
      </NuxtLink>
    </div>

    <!-- Personal Knowledge Breakdown Table -->
    <div class="space-y-1.5 w-full my-1">
      <!-- SM-2 Flashcards -->
      <div class="flex items-center justify-between p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle/80 border border-slate-200/60 dark:border-white/[0.04]">
        <div class="flex items-center gap-2 min-w-0">
          <div class="w-5 h-5 rounded-md bg-purple-500/10 text-purple-500 dark:text-purple-400 flex items-center justify-center shrink-0">
            <Layers class="w-3 h-3" :stroke-width="1.5" />
          </div>
          <span class="text-xs font-medium text-slate-700 dark:text-slate-300 truncate">
            {{ $t('dashboard.stat_flashcards') }}
          </span>
        </div>
        <span class="text-xs font-mono font-bold text-slate-900 dark:text-white tabular-nums px-2 py-0.5 rounded-md bg-slate-200/50 dark:bg-white/[0.06]">
          {{ flashcardCount }}
        </span>
      </div>

      <!-- Notes & Highlights -->
      <div class="flex items-center justify-between p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle/80 border border-slate-200/60 dark:border-white/[0.04]">
        <div class="flex items-center gap-2 min-w-0">
          <div class="w-5 h-5 rounded-md bg-amber-500/10 text-amber-500 dark:text-amber-400 flex items-center justify-center shrink-0">
            <Bookmark class="w-3 h-3" :stroke-width="1.5" />
          </div>
          <span class="text-xs font-medium text-slate-700 dark:text-slate-300 truncate">
            {{ $t('dashboard.stat_highlights') }}
          </span>
        </div>
        <span class="text-xs font-mono font-bold text-slate-900 dark:text-white tabular-nums px-2 py-0.5 rounded-md bg-slate-200/50 dark:bg-white/[0.06]">
          {{ highlightCount }}
        </span>
      </div>

      <!-- Learned Slices -->
      <div class="flex items-center justify-between p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle/80 border border-slate-200/60 dark:border-white/[0.04]">
        <div class="flex items-center gap-2 min-w-0">
          <div class="w-5 h-5 rounded-md bg-cyan-500/10 text-cyan-500 dark:text-cyan-400 flex items-center justify-center shrink-0">
            <FileText class="w-3 h-3" :stroke-width="1.5" />
          </div>
          <span class="text-xs font-medium text-slate-700 dark:text-slate-300 truncate">
            {{ $t('dashboard.stat_learned_chunks') }}
          </span>
        </div>
        <span class="text-xs font-mono font-bold text-slate-900 dark:text-white tabular-nums px-2 py-0.5 rounded-md bg-slate-200/50 dark:bg-white/[0.06]">
          {{ chunkCount }}
        </span>
      </div>
    </div>

    <!-- Telemetry Counters Footer -->
    <div class="grid grid-cols-2 gap-2 w-full mt-1.5 text-center font-mono">
      <div class="p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
        <div class="text-sm sm:text-base font-black text-slate-800 dark:text-white tabular-nums">
          {{ displayNodeCount }}
        </div>
        <div class="text-[9px] text-slate-500 dark:text-slate-400 uppercase tracking-wider truncate">
          {{ $t('dashboard.connected_nodes') || 'Nodes' }}
        </div>
      </div>

      <div class="p-2 rounded-xl bg-slate-100/60 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
        <div class="text-sm sm:text-base font-black text-slate-800 dark:text-white tabular-nums">
          {{ displayEdgeCount }}
        </div>
        <div class="text-[9px] text-slate-500 dark:text-slate-400 uppercase tracking-wider truncate">
          {{ $t('dashboard.active_relations') || 'Relations' }}
        </div>
      </div>
    </div>
  </div>
</template>
