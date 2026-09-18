<script setup lang="ts">
import { ref, watch, computed, onMounted, onUnmounted } from 'vue'
import {
  Search,
  X,
  Maximize2,
  RotateCcw,
  SlidersHorizontal,
  ChevronDown,
  ChevronUp,
  Network,
  Globe
} from 'lucide-vue-next'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

const emit = defineEmits<{
  (e: 'fit-screen'): void
}>()

const store = useKnowledgeGraphStore()
const isExpanded = ref(false)

const isMobileScreen = ref(false)

function checkMobile() {
  if (typeof window !== 'undefined') {
    isMobileScreen.value = window.innerWidth < 640
  }
}

onMounted(() => {
  checkMobile()
  window.addEventListener('resize', checkMobile)
})

onUnmounted(() => {
  if (typeof window !== 'undefined') {
    window.removeEventListener('resize', checkMobile)
  }
})
const searchLocal = ref(store.searchQuery)

watch(
  () => store.searchQuery,
  (newVal) => {
    if (newVal !== searchLocal.value) {
      searchLocal.value = newVal
    }
  }
)

function onSearchInput() {
  store.setSearchQuery(searchLocal.value)
}

function clearSearch() {
  searchLocal.value = ''
  store.setSearchQuery('')
}

function onReset() {
  searchLocal.value = ''
  store.resetFilters()
}

// Category filter pills
const categoryPills = [
  { id: 'all', key: 'graph.filters.allPillars', defaultLabel: 'All Pillars' },
  { id: 'BackendRuntime', key: 'graph.filters.backendRuntime', defaultLabel: 'Backend & Runtime' },
  { id: 'DatabaseStorage', key: 'graph.filters.databaseStorage', defaultLabel: 'Database & Storage' },
  { id: 'SystemDesign', key: 'graph.filters.systemDesign', defaultLabel: 'Distributed Systems' },
  { id: 'FrontendWeb', key: 'graph.filters.frontendWeb', defaultLabel: 'Frontend & Web' },
  { id: 'EngineeringCraft', key: 'graph.filters.engineeringCraft', defaultLabel: 'Engineering Craft' }
]

// Node type toggles
const nodeTypes = [
  { id: 'all', key: 'graph.filters.allTypes', defaultLabel: 'All Types' },
  { id: 'topic', key: 'graph.filters.topics', defaultLabel: 'Topics' },
  { id: 'book', key: 'graph.filters.books', defaultLabel: 'Books' },
  { id: 'card', key: 'graph.filters.cards', defaultLabel: 'Flashcards' },
  { id: 'highlight', key: 'graph.filters.highlights', defaultLabel: 'Highlights' }
]

// Flashcard mastery selector
const masteryOptions = [
  { id: 'all', key: 'graph.filters.allMastery', defaultLabel: 'All Statuses' },
  { id: 'learning', key: 'graph.filters.learning', defaultLabel: 'Learning' },
  { id: 'reviewing', key: 'graph.filters.reviewing', defaultLabel: 'Reviewing' },
  { id: 'mastered', key: 'graph.filters.mastered', defaultLabel: 'Mastered' }
]

const hasActiveFilters = computed(() => {
  return (
    store.selectedCategory.toLowerCase() !== 'all' ||
    store.selectedNodeType.toLowerCase() !== 'all' ||
    store.selectedMastery.toLowerCase() !== 'all' ||
    store.searchQuery.trim() !== ''
  )
})
</script>

<template>
  <div
    class="bg-white/85 dark:bg-slate-900/85 backdrop-blur-md border border-slate-200/80 dark:border-slate-800/80 rounded-2xl shadow-xl p-3 sm:p-4 space-y-3 transition-all"
  >
    <!-- Top Row: Live Search & Action Buttons -->
    <div class="flex items-center gap-2 sm:gap-3">
      <!-- Search Input -->
      <div class="relative flex-1 min-w-0">
        <Search class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 pointer-events-none" />
        <input
          v-model="searchLocal"
          type="text"
          :placeholder="isMobileScreen ? $t('graph.searchPlaceholderShort') : $t('graph.searchPlaceholder')"
          class="w-full pl-9 pr-9 py-2 bg-slate-100/80 dark:bg-slate-800/60 border border-slate-200/60 dark:border-slate-700/60 rounded-xl text-xs sm:text-sm text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500/40 focus:border-brand-500 transition-all"
          @input="onSearchInput"
        />
        <button
          v-if="searchLocal"
          type="button"
          class="absolute right-2.5 top-1/2 -translate-y-1/2 p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors"
          title="Clear search"
          aria-label="Clear search"
          @click="clearSearch"
        >
          <X class="w-3.5 h-3.5" />
        </button>
      </div>
      <!-- 2D / 3D Engine Mode Switcher -->
      <div class="inline-flex p-0.5 rounded-xl bg-slate-100/90 dark:bg-slate-800/90 border border-slate-200/60 dark:border-slate-700/60 shrink-0">
        <button
          type="button"
          :class="[
            'px-2 sm:px-2.5 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap flex items-center gap-1.5',
            store.viewMode === '2d'
              ? 'bg-white dark:bg-slate-700 text-brand-600 dark:text-brand-400 shadow-sm'
              : 'text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
          ]"
          :title="$t('graph.mode2d')"
          :aria-label="$t('graph.mode2d')"
          @click="store.setViewMode('2d')"
        >
          <Network class="w-3.5 h-3.5" />
          <span class="hidden sm:inline">{{ $t('graph.mode2d') }}</span>
          <span class="sm:hidden">2D</span>
        </button>
        <button
          type="button"
          :class="[
            'px-2 sm:px-2.5 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap flex items-center gap-1.5',
            store.viewMode === '3d'
              ? 'bg-white dark:bg-slate-700 text-brand-600 dark:text-brand-400 shadow-sm'
              : 'text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
          ]"
          :title="$t('graph.mode3d')"
          :aria-label="$t('graph.mode3d')"
          @click="store.setViewMode('3d')"
        >
          <Globe class="w-3.5 h-3.5" />
          <span class="hidden sm:inline">{{ $t('graph.mode3d') }}</span>
          <span class="sm:hidden">3D</span>
        </button>
      </div>

      <!-- Action Button: Fit Screen -->
      <button
        type="button"
        class="hidden sm:inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-slate-100 dark:bg-slate-800/80 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 border border-slate-200/60 dark:border-slate-700/60 transition-all active:scale-95 whitespace-nowrap shrink-0 shadow-sm"
        :title="$t('graph.fitScreen')"
        @click="$emit('fit-screen')"
      >
        <Maximize2 class="w-3.5 h-3.5 sm:w-4 sm:h-4 text-brand-500 shrink-0" />
        <span class="hidden sm:inline whitespace-nowrap shrink-0">{{ $t('graph.fitScreen') }}</span>
      </button>

      <!-- Action Button: Reset Filters -->
      <button
        v-if="hasActiveFilters"
        type="button"
        class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-rose-50 dark:bg-rose-950/30 hover:bg-rose-100 dark:hover:bg-rose-900/40 text-rose-600 dark:text-rose-400 border border-rose-200/60 dark:border-rose-800/40 transition-all active:scale-95 whitespace-nowrap shrink-0 shadow-sm"
        :title="$t('graph.resetFilters')"
        @click="onReset"
      >
        <RotateCcw class="w-3.5 h-3.5 sm:w-4 sm:h-4 shrink-0" />
        <span class="hidden md:inline whitespace-nowrap shrink-0">{{ $t('graph.resetFilters') }}</span>
      </button>

      <!-- Mobile Expand/Collapse Toggle -->
      <button
        type="button"
        class="sm:hidden inline-flex items-center justify-center p-2 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-slate-700 shrink-0"
        :aria-expanded="isExpanded"
        aria-label="Toggle filters"
        @click="isExpanded = !isExpanded"
      >
        <SlidersHorizontal class="w-4 h-4" />
      </button>
    </div>

    <!-- Filter Pills Container (Desktop visible, mobile collapsible) -->
    <div :class="['space-y-2.5 transition-all max-h-[50vh] sm:max-h-none overflow-y-auto sm:overflow-visible pr-0.5', isExpanded ? 'block' : 'hidden sm:block']">
      <!-- Category Pillars Row -->
      <div class="flex flex-wrap items-center gap-1.5">
        <span class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mr-1 whitespace-nowrap shrink-0">
          {{ $t('graph.filters.pillars') }}:
        </span>
        <button
          v-for="pill in categoryPills"
          :key="pill.id"
          type="button"
          :class="[
            'px-2.5 py-1 rounded-xl text-xs font-medium transition-all whitespace-nowrap shrink-0',
            store.selectedCategory.toLowerCase() === pill.id.toLowerCase()
              ? 'bg-slate-900 text-white dark:bg-white dark:text-slate-950 shadow-sm font-semibold'
              : 'bg-slate-100/80 dark:bg-slate-800/60 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800'
          ]"
          @click="store.setCategory(pill.id)"
        >
          {{ pill.key ? $t(pill.key) : pill.defaultLabel }}
        </button>
      </div>

      <!-- Secondary Row: Node Types & Flashcard Mastery -->
      <div class="flex flex-wrap items-center gap-x-4 gap-y-2 pt-1 border-t border-slate-100 dark:border-slate-800/60">
        <!-- Node Type Toggles -->
        <div class="flex flex-wrap items-center gap-1.5">
          <span class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mr-1 whitespace-nowrap shrink-0">
            {{ $t('graph.filters.nodeTypes') }}:
          </span>
          <button
            v-for="nt in nodeTypes"
            :key="nt.id"
            type="button"
            :class="[
              'px-2.5 py-1 rounded-xl text-xs font-medium transition-all whitespace-nowrap shrink-0',
              store.selectedNodeType.toLowerCase() === nt.id.toLowerCase()
                ? 'bg-brand-600 text-white shadow-sm font-semibold'
                : 'bg-slate-100/80 dark:bg-slate-800/60 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800'
            ]"
            @click="store.setNodeType(nt.id)"
          >
            {{ nt.key ? $t(nt.key) : nt.defaultLabel }}
          </button>
        </div>

        <!-- Flashcard Mastery Selector (visible if card is not filtered out) -->
        <div
          v-if="store.selectedNodeType.toLowerCase() === 'all' || store.selectedNodeType.toLowerCase() === 'card'"
          class="flex flex-wrap items-center gap-1.5"
        >
          <span class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mr-1 whitespace-nowrap shrink-0">
            {{ $t('graph.filters.mastery') }}:
          </span>
          <button
            v-for="m in masteryOptions"
            :key="m.id"
            type="button"
            :class="[
              'px-2.5 py-1 rounded-xl text-xs font-medium transition-all whitespace-nowrap shrink-0',
              store.selectedMastery.toLowerCase() === m.id.toLowerCase()
                ? 'bg-emerald-600 text-white shadow-sm font-semibold'
                : 'bg-slate-100/80 dark:bg-slate-800/60 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800'
            ]"
            @click="store.setMastery(m.id)"
          >
            {{ m.key ? $t(m.key) : m.defaultLabel }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.no-scrollbar::-webkit-scrollbar {
  display: none;
}
.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
