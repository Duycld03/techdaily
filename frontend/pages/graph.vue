<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { Core } from 'cytoscape'
import {
  Network,
  RotateCcw,
  AlertTriangle,
  Loader2
} from 'lucide-vue-next'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'
import GraphCanvas from '~/components/graph/GraphCanvas.vue'
import GraphControlBar from '~/components/graph/GraphControlBar.vue'
import GraphMinimap from '~/components/graph/GraphMinimap.vue'
import GraphDetailDrawer from '~/components/graph/GraphDetailDrawer.vue'

const store = useKnowledgeGraphStore()
const canvasRef = ref<InstanceType<typeof GraphCanvas> | null>(null)
const cyInstance = ref<Core | null>(null)

function onCyReady(cy: Core) {
  cyInstance.value = cy
}

function handleFitScreen() {
  canvasRef.value?.fitScreen()
}

onMounted(() => {
  store.fetchGraph()
})
</script>

<template>
  <div class="h-[calc(100vh-4rem)] w-full overflow-hidden relative select-none bg-slate-50 dark:bg-slate-950">
    <!-- Floating Glassmorphic Control Bar -->
    <div class="absolute top-3 left-3 right-3 sm:top-4 sm:left-4 sm:right-4 z-20 pointer-events-none">
      <div class="pointer-events-auto max-w-4xl mx-auto">
        <GraphControlBar @fit-screen="handleFitScreen" />
      </div>
    </div>

    <!-- Loading State Overlay -->
    <div
      v-if="store.isLoading"
      class="absolute inset-0 z-30 flex flex-col items-center justify-center bg-slate-50/80 dark:bg-slate-950/80 backdrop-blur-sm space-y-3"
    >
      <div class="relative flex items-center justify-center">
        <div class="w-16 h-16 rounded-2xl bg-brand-500/10 border border-brand-500/20 animate-pulse" />
        <Loader2 class="w-8 h-8 text-brand-500 animate-spin absolute" />
      </div>
      <p class="text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 tracking-wide animate-pulse">
        {{ $t('graph.loading') }}
      </p>
    </div>

    <!-- Error State Overlay -->
    <div
      v-else-if="store.error"
      class="absolute inset-0 z-30 flex flex-col items-center justify-center bg-slate-50/95 dark:bg-slate-950/95 p-6 text-center space-y-3"
    >
      <div class="w-12 h-12 rounded-2xl bg-rose-500/10 border border-rose-500/20 flex items-center justify-center text-rose-500">
        <AlertTriangle class="w-6 h-6" />
      </div>
      <h3 class="text-base font-bold text-slate-900 dark:text-white">
        {{ $t('graph.error.title') }}
      </h3>
      <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 max-w-sm">
        {{ store.error }}
      </p>
      <button
        type="button"
        class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-semibold transition-all active:scale-95 whitespace-nowrap shrink-0 shadow-md shadow-brand-500/20"
        @click="store.fetchGraph(true)"
      >
        <RotateCcw class="w-4 h-4 shrink-0" />
        <span class="whitespace-nowrap shrink-0">{{ $t('graph.error.retry') }}</span>
      </button>
    </div>

    <!-- Empty State Guidance (when filtered down to 0 visible nodes) -->
    <div
      v-else-if="!store.isLoading && !store.error && store.filteredNodes.length === 0"
      class="absolute inset-0 z-10 flex flex-col items-center justify-center p-6 text-center space-y-3 pointer-events-none"
    >
      <div
        class="pointer-events-auto bg-white/90 dark:bg-slate-900/90 backdrop-blur-md border border-slate-200 dark:border-slate-800 rounded-3xl p-6 sm:p-8 max-w-md shadow-2xl flex flex-col items-center space-y-3"
      >
        <div class="w-12 h-12 rounded-2xl bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-slate-400">
          <Network class="w-6 h-6" />
        </div>
        <h3 class="text-base font-bold text-slate-900 dark:text-white">
          {{ $t('graph.empty.title') }}
        </h3>
        <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
          {{ $t('graph.empty.description') }}
        </p>
        <button
          type="button"
          class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-xs sm:text-sm font-semibold transition-all active:scale-95 whitespace-nowrap shrink-0"
          @click="store.resetFilters()"
        >
          <RotateCcw class="w-4 h-4 shrink-0" />
          <span class="whitespace-nowrap shrink-0">{{ $t('graph.resetFilters') }}</span>
        </button>
      </div>
    </div>

    <!-- 2D Cytoscape Canvas -->
    <GraphCanvas
      ref="canvasRef"
      class="w-full h-full"
      @cy-ready="onCyReady"
    />

    <!-- Locator Minimap (Bottom Right) -->
    <GraphMinimap
      :cy="cyInstance"
      class="absolute bottom-4 right-4 z-20 hidden sm:block"
    />

    <!-- Slide-Over / Bottom-Sheet Detail Drawer -->
    <GraphDetailDrawer />
  </div>
</template>
