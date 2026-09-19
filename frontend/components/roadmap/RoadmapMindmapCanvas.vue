<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useEventListener, useDebounceFn } from '@vueuse/core'
import { useRouter } from 'vue-router'
import {
  ZoomIn,
  ZoomOut,
  Maximize2,
  ChevronsDownUp,
  ChevronsUpDown,
  BookOpen,
  Compass,
  ChevronDown,
  ChevronRight,
  Flame,
  CheckCircle2,
  Clock,
  Target,
  Search,
  X
} from 'lucide-vue-next'
import {
  computeRoadmapTreeLayout,
  convertBookMilestonesToTree,
  convertCurriculumToTree,
  type TreeSliceLeaf,
  type TreeChapterBranch,
  type TreeRoot
} from '~/utils/roadmapTreeLayout'
import type { CurriculumRoadmapData } from '~/stores/useRoadmapStore'
import type { BookDetail } from '~/stores/useLibraryStore'

interface ChapterSliceItem {
  id: string
  chunkOrder: number
  sliceTitle: string
  chapterTitle?: string
  summaryMarkdown?: string
  estimatedReadMinutes?: number
  isCompleted: boolean
  isActiveToday: boolean
  isUpcoming: boolean
}

interface ChapterMilestoneItem {
  chapterTitle: string
  chapterIndex: number
  isCompleted: boolean
  isActive: boolean
  completedSlicesCount: number
  totalSlicesCount: number
  slices: ChapterSliceItem[]
}

const props = defineProps<{
  selectedBook?: BookDetail | null
  chapterMilestones?: ChapterMilestoneItem[]
  roadmapData?: CurriculumRoadmapData | null
  isCurriculumSelected: boolean
  activeBookId?: string | null
  currentChunkOrder?: number
  activeDayOrder?: number
}>()

const router = useRouter()

const containerRef = ref<HTMLElement | null>(null)
const scale = ref(1.0)
const pan = ref({ x: 40, y: 40 })
const isPanning = ref(false)
const dragStart = ref({ x: 0, y: 0 })

const expandedChapterIds = ref<Set<string>>(new Set())

// Convert input dataset to unified tree structure
const treeData = computed<{ root: TreeRoot; chapters: TreeChapterBranch[] }>(() => {
  if (props.isCurriculumSelected && props.roadmapData) {
    return convertCurriculumToTree(props.roadmapData)
  }
  if (props.chapterMilestones && props.chapterMilestones.length > 0) {
    const bookTitle = props.selectedBook?.title || 'Active Document'
    const pacer = props.selectedBook
      ? {
          currentChunkOrder: props.currentChunkOrder || 1,
          totalChunks: props.selectedBook.totalChunks,
          progressPercentage: props.selectedBook.progressPercentage || 0
        }
      : null
    return convertBookMilestonesToTree(bookTitle, props.chapterMilestones, pacer)
  }
  return {
    root: {
      id: 'empty-root',
      title: props.isCurriculumSelected ? '30-Day Senior Curriculum' : 'Active Document',
      totalCount: 0,
      completedCount: 0,
      progressPercentage: 0,
      trackType: props.isCurriculumSelected ? 'curriculum' : 'book'
    },
    chapters: []
  }
})

// Initialize expanded chapters on mount / track switch
watch(
  () => treeData.value,
  (newTree) => {
    if (newTree.chapters.length === 0) return
    if (expandedChapterIds.value.size === 0) {
      const active = newTree.chapters.find((c) => c.isActive)
      if (active) {
        expandedChapterIds.value.add(active.id)
      } else if (newTree.chapters[0]) {
        expandedChapterIds.value.add(newTree.chapters[0].id)
      }
    }
  },
  { immediate: true }
)

function toggleChapter(chapterId: string) {
  const isLargeDocument = treeData.value.chapters.length > 12

  if (expandedChapterIds.value.has(chapterId)) {
    expandedChapterIds.value.delete(chapterId)
  } else {
    if (isLargeDocument) {
      expandedChapterIds.value.clear()
    }
    expandedChapterIds.value.add(chapterId)
  }
}

function expandAll() {
  for (const c of treeData.value.chapters) {
    expandedChapterIds.value.add(c.id)
  }
}

function collapseAll() {
  expandedChapterIds.value.clear()
}

// In-canvas search state & filtering
const searchQuery = ref('')
const debouncedSearch = ref('')
const applyDebouncedSearch = useDebounceFn((newVal: string) => {
  debouncedSearch.value = newVal.trim().toLowerCase()
}, 150)

watch(searchQuery, (newVal) => {
  applyDebouncedSearch(newVal)
})

const matchingNodeIds = computed<Set<string>>(() => {
  const query = debouncedSearch.value
  if (!query) return new Set()

  const matches = new Set<string>()
  for (const ch of treeData.value.chapters) {
    let chapterMatches = ch.title.toLowerCase().includes(query)
    for (const sl of ch.slices) {
      if (
        sl.title.toLowerCase().includes(query) ||
        (sl.subtitle && sl.subtitle.toLowerCase().includes(query))
      ) {
        matches.add(sl.id)
        chapterMatches = true
      }
    }
    if (chapterMatches) {
      matches.add(ch.id)
    }
  }
  return matches
})

watch(debouncedSearch, (query) => {
  if (!query) return
  for (const ch of treeData.value.chapters) {
    const hasMatchingSlice = ch.slices.some(
      (s) => s.title.toLowerCase().includes(query) || (s.subtitle && s.subtitle.toLowerCase().includes(query))
    )
    if (hasMatchingSlice || ch.title.toLowerCase().includes(query)) {
      expandedChapterIds.value.add(ch.id)
    }
  }
})

function isNodeDimmed(nodeId: string): boolean {
  if (!debouncedSearch.value) return false
  return !matchingNodeIds.value.has(nodeId)
}

function isNodeHighlighted(nodeId: string): boolean {
  if (!debouncedSearch.value) return false
  return matchingNodeIds.value.has(nodeId)
}
// Compute tree layout coordinates
const layout = computed(() => {
  return computeRoadmapTreeLayout(
    treeData.value.root,
    treeData.value.chapters,
    expandedChapterIds.value
  )
})

// Canvas zoom controls
function zoomIn() {
  scale.value = Number(Math.min(2.0, scale.value + 0.15).toFixed(2))
}

function zoomOut() {
  scale.value = Number(Math.max(0.15, scale.value - 0.15).toFixed(2))
}

function handleWheel(e: WheelEvent) {
  e.preventDefault()
  const delta = e.deltaY < 0 ? 0.08 : -0.08
  scale.value = Number(Math.min(2.0, Math.max(0.15, scale.value + delta)).toFixed(2))
}

// Canvas drag-to-pan controls
function startPan(e: MouseEvent) {
  const target = e.target as HTMLElement | null
  if (target?.closest('.interactive-node') || target?.closest('button') || target?.closest('input')) return
  e.preventDefault()
  isPanning.value = true
  dragStart.value = {
    x: e.clientX - pan.value.x,
    y: e.clientY - pan.value.y
  }
}

function onMouseMove(e: MouseEvent) {
  if (!isPanning.value) return
  pan.value = {
    x: e.clientX - dragStart.value.x,
    y: e.clientY - dragStart.value.y
  }
}

function endPan() {
  isPanning.value = false
}

// Touch gesture pan support
function onTouchStart(e: TouchEvent) {
  if (e.touches.length === 1) {
    const target = e.target as HTMLElement | null
    if (target?.closest('.interactive-node') || target?.closest('button') || target?.closest('input')) return
    isPanning.value = true
    dragStart.value = {
      x: e.touches[0].clientX - pan.value.x,
      y: e.touches[0].clientY - pan.value.y
    }
  }
}

function onTouchMove(e: TouchEvent) {
  if (!isPanning.value || e.touches.length !== 1) return
  pan.value = {
    x: e.touches[0].clientX - dragStart.value.x,
    y: e.touches[0].clientY - dragStart.value.y
  }
}

function onTouchEnd() {
  isPanning.value = false
}

const windowTarget = typeof window !== 'undefined' ? window : null
useEventListener(windowTarget, 'mousemove', onMouseMove)
useEventListener(windowTarget, 'mouseup', endPan)
useEventListener(windowTarget, 'touchmove', onTouchMove, { passive: false })
useEventListener(windowTarget, 'touchend', onTouchEnd)
useEventListener(windowTarget, 'touchcancel', onTouchEnd)

// Fit to screen calculation
function focusActiveNode() {
  const clientWidth = containerRef.value?.clientWidth || 1000
  const clientHeight = containerRef.value?.clientHeight || 600

  // 1. Locate active-today slice
  const activeSliceNode = layout.value.slices.find((s) => s.data.isActiveToday)
  if (activeSliceNode) {
    scale.value = 1.0
    pan.value = {
      x: Math.round(clientWidth / 2 - (activeSliceNode.x + activeSliceNode.width / 2) * scale.value),
      y: Math.round(clientHeight / 2 - (activeSliceNode.y + activeSliceNode.height / 2) * scale.value)
    }
    return
  }

  // 2. Locate active chapter
  const activeChapterNode = layout.value.chapters.find((c) => c.data.isActive)
  if (activeChapterNode) {
    scale.value = 1.0
    pan.value = {
      x: Math.round(clientWidth / 2 - (activeChapterNode.x + activeChapterNode.width / 2) * scale.value),
      y: Math.round(clientHeight / 2 - (activeChapterNode.y + activeChapterNode.height / 2) * scale.value)
    }
    return
  }

  // 3. Fallback: first chapter
  if (layout.value.chapters.length > 0) {
    const firstChapter = layout.value.chapters[0]
    scale.value = 1.0
    pan.value = {
      x: Math.round(clientWidth / 2 - (firstChapter.x + firstChapter.width / 2) * scale.value),
      y: Math.round(clientHeight / 2 - (firstChapter.y + firstChapter.height / 2) * scale.value)
    }
    return
  }

  fitToScreen()
}

// Fit to screen calculation
function fitToScreen() {
  const clientWidth = containerRef.value?.clientWidth || 1000
  const clientHeight = containerRef.value?.clientHeight || 600
  const bb = layout.value.boundingBox
  const padding = 50
  const availableW = Math.max(100, clientWidth - padding * 2)
  const availableH = Math.max(100, clientHeight - padding * 2)
  const scaleX = availableW / bb.width
  const scaleY = availableH / bb.height
  const targetScale = Math.min(Math.max(Math.min(scaleX, scaleY), 0.15), 1.15)
  scale.value = Number(targetScale.toFixed(2))
  pan.value = {
    x: Math.round((clientWidth - bb.width * scale.value) / 2 - bb.minX * scale.value),
    y: Math.round((clientHeight - bb.height * scale.value) / 2 - bb.minY * scale.value)
  }
}

onMounted(() => {
  focusActiveNode()
})
// 1-Click action bridges
function handleSliceClick(slice: TreeSliceLeaf) {
  if (props.isCurriculumSelected) {
    router.push(`/today?day=${slice.order}`)
    return
  }

  const bookId = props.activeBookId || props.selectedBook?.id
  if (!bookId) return

  if (slice.isActiveToday) {
    router.push(`/today?bookId=${bookId}&chunkOrder=${slice.order}`)
  } else {
    router.push(`/read/${bookId}?slice=${slice.order}`)
  }
}
</script>

<template>
  <div
    ref="containerRef"
    class="relative w-full h-[620px] sm:h-[720px] rounded-3xl bg-slate-50/80 dark:bg-canvas border border-slate-200/90 dark:border-white/[0.08] overflow-hidden select-none transition-colors"
    @wheel="handleWheel"
    @mousedown="startPan"
    @mousemove="onMouseMove"
    @mouseup="endPan"
    @mouseleave="endPan"
    @touchstart="onTouchStart"
    @touchmove="onTouchMove"
    @touchend="onTouchEnd"
    :class="isPanning ? 'cursor-grabbing' : 'cursor-grab'"
  >
    <!-- Search Bar in Top Left -->
    <div
      class="absolute top-3 sm:top-4 left-3 sm:left-4 z-10 flex items-center gap-1.5 px-3 py-1.5 glass-panel dark:bg-canvas-subtle/90 backdrop-blur-md rounded-2xl border border-slate-200/90 dark:border-white/[0.08] shadow-md max-w-[180px] sm:max-w-[240px] md:max-w-xs transition-all"
      @mousedown.stop
      @touchstart.stop
      @wheel.stop
    >
      <Search class="w-3.5 h-3.5 text-slate-400 shrink-0" />
      <input
        v-model="searchQuery"
        type="text"
        :placeholder="$t('roadmap.mindmap.search_placeholder')"
        class="w-full bg-transparent text-xs text-slate-800 dark:text-slate-200 placeholder-slate-400 focus:outline-none"
        data-testid="mindmap-search-input"
      />
      <button
        v-if="searchQuery"
        type="button"
        @click="searchQuery = ''"
        class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 shrink-0"
        data-testid="btn-clear-search"
      >
        <X class="w-3 h-3" />
      </button>
    </div>

    <!-- Floating Toolbar -->
    <div
      class="absolute top-3 sm:top-4 right-3 sm:right-4 z-10 flex items-center gap-1 sm:gap-1.5 p-1.5 glass-panel dark:bg-canvas-subtle/90 backdrop-blur-md rounded-2xl border border-slate-200/90 dark:border-white/[0.08] shadow-md"
      @mousedown.stop
      @touchstart.stop
      @wheel.stop
    >
      <button
        @click="focusActiveNode"
        :title="$t('roadmap.mindmap.focus_active')"
        class="p-1.5 sm:p-2 rounded-xl text-amber-600 dark:text-amber-400 hover:bg-amber-50 dark:hover:bg-amber-950/40 transition-colors whitespace-nowrap shrink-0 active:scale-95 flex items-center gap-1"
        data-testid="btn-focus-active"
      >
        <Target class="w-4 h-4" />
        <span class="hidden lg:inline text-xs font-bold">{{ $t('roadmap.mindmap.focus_active') }}</span>
      </button>
      <div class="w-px h-4 bg-slate-200 dark:bg-white/10 mx-0.5"></div>
      <button
        type="button"
        @click="zoomIn"
        :title="$t('roadmap.mindmap.zoom_in')"
        class="p-1.5 sm:p-2 rounded-xl text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.08] transition-colors whitespace-nowrap shrink-0 active:scale-95"
        data-testid="btn-zoom-in"
      >
        <ZoomIn class="w-4 h-4" />
      </button>
      <button
        type="button"
        @click="zoomOut"
        :title="$t('roadmap.mindmap.zoom_out')"
        class="p-1.5 sm:p-2 rounded-xl text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.08] transition-colors whitespace-nowrap shrink-0 active:scale-95"
        data-testid="btn-zoom-out"
      >
        <ZoomOut class="w-4 h-4" />
      </button>
      <span class="text-xs font-mono font-bold text-slate-500 dark:text-slate-400 px-1 whitespace-nowrap shrink-0">
        {{ Math.round(scale * 100) }}%
      </span>
      <div class="w-px h-4 bg-slate-200 dark:bg-white/10 mx-0.5"></div>
      <button
        type="button"
        @click="fitToScreen"
        :title="$t('roadmap.mindmap.fit_screen')"
        class="p-1.5 sm:p-2 rounded-xl text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.08] transition-colors whitespace-nowrap shrink-0 active:scale-95"
        data-testid="btn-fit-screen"
      >
        <Maximize2 class="w-4 h-4" />
      </button>
      <div class="w-px h-4 bg-slate-200 dark:bg-white/10 mx-0.5"></div>
      <button
        type="button"
        @click="expandAll"
        :title="$t('roadmap.mindmap.expand_all')"
        class="px-2 sm:px-2.5 py-1.5 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.08] transition-colors whitespace-nowrap shrink-0 flex items-center gap-1 active:scale-95"
        data-testid="btn-expand-all"
      >
        <ChevronsDownUp class="w-3.5 h-3.5 shrink-0" />
        <span class="hidden md:inline">{{ $t('roadmap.mindmap.expand_all') }}</span>
      </button>
      <button
        type="button"
        @click="collapseAll"
        :title="$t('roadmap.mindmap.collapse_all')"
        class="px-2 sm:px-2.5 py-1.5 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.08] transition-colors whitespace-nowrap shrink-0 flex items-center gap-1 active:scale-95"
        data-testid="btn-collapse-all"
      >
        <ChevronsUpDown class="w-3.5 h-3.5 shrink-0" />
        <span class="hidden md:inline">{{ $t('roadmap.mindmap.collapse_all') }}</span>
      </button>
    </div>

    <!-- SVG Canvas Viewport -->
    <svg
      class="w-full h-full block"
      xmlns="http://www.w3.org/2000/svg"
    >
      <g :style="{ transform: `translate(${pan.x}px, ${pan.y}px) scale(${scale})`, transformOrigin: '0 0' }">
        <!-- SVG Bezier Curves (Edges) -->
        <g class="edges-layer pointer-events-none">
          <path
            v-for="edge in layout.edges"
            :key="edge.id"
            :d="edge.path"
            fill="none"
            :class="[
              isPanning ? 'transition-none' : 'transition-all duration-300',
              isNodeDimmed(edge.toId) ? 'opacity-20' : 'opacity-100',
              edge.status === 'active_today'
                ? 'stroke-amber-400 dark:stroke-amber-500 stroke-2'
                : edge.status === 'completed'
                  ? 'stroke-brand-500 dark:stroke-brand-400 stroke-2'
                  : 'stroke-slate-300 dark:stroke-zinc-700 stroke-1.5'
            ]"
          />
        </g>

        <!-- SVG Nodes via foreignObject -->
        <!-- 1. ROOT NODE -->
        <foreignObject
          :x="layout.root.x"
          :y="layout.root.y"
          :width="layout.root.width"
          :height="layout.root.height"
          class="overflow-visible"
        >
          <div
            data-testid="root-node"
            class="interactive-node w-full h-full p-3.5 rounded-2xl bg-gradient-to-br from-indigo-50/90 via-white to-brand-50/70 dark:from-canvas-subtle dark:via-canvas-subtle dark:to-brand-950/40 border-2 border-brand-500/40 dark:border-brand-500/50 shadow-md flex flex-col justify-between"
          >
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-2 min-w-0">
                <component
                  :is="isCurriculumSelected ? Compass : BookOpen"
                  class="w-4 h-4 text-brand-600 dark:text-brand-400 shrink-0"
                />
                <span class="text-xs font-extrabold uppercase tracking-wider text-brand-700 dark:text-brand-300 truncate">
                  {{ isCurriculumSelected ? $t('roadmap.curriculum_track') : $t('roadmap.active_book') }}
                </span>
              </div>
              <span class="px-2 py-0.5 rounded-full text-xs font-mono font-bold bg-brand-600 text-white whitespace-nowrap shrink-0">
                {{ layout.root.data.progressPercentage }}%
              </span>
            </div>
            <div class="font-bold text-sm text-slate-900 dark:text-white truncate">
              {{ layout.root.data.title }}
            </div>
            <div class="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
              <span class="truncate">{{ layout.root.data.subtitle }}</span>
              <span class="font-mono font-semibold shrink-0">
                {{ layout.root.data.completedCount }}/{{ layout.root.data.totalCount }}
              </span>
            </div>
          </div>
        </foreignObject>

        <!-- 2. CHAPTER BRANCH NODES -->
        <foreignObject
          v-for="ch in layout.chapters"
          :key="ch.data.id"
          :x="ch.x"
          :y="ch.y"
          :width="ch.width"
          :height="ch.height"
          class="overflow-visible"
        >
          <div
            :data-testid="`chapter-node-${ch.data.id}`"
            :data-chapter-id="ch.data.id"
            @click="toggleChapter(ch.data.id)"
            :class="[
              'interactive-node w-full h-full p-3 rounded-2xl border transition-all duration-200 cursor-pointer shadow-sm flex items-center justify-between gap-2.5 select-none hover:scale-[1.01] active:scale-98',
              isNodeDimmed(ch.data.id) ? 'opacity-25' : 'opacity-100',
              isNodeHighlighted(ch.data.id) ? 'ring-2 ring-brand-500 shadow-md' : '',
              ch.data.isActive
                ? 'bg-amber-50/90 dark:bg-amber-950/40 border-amber-400 dark:border-amber-500/80 text-amber-950 dark:text-amber-100 ring-2 ring-amber-500/20'
                : ch.data.isCompleted
                  ? 'bg-brand-50/80 dark:bg-brand-950/40 border-brand-400/60 dark:border-brand-600/60 text-brand-950 dark:text-brand-100'
                  : 'bg-white dark:bg-canvas-subtle border-slate-200 dark:border-white/[0.08] text-slate-800 dark:text-slate-200 hover:border-brand-400 dark:hover:border-brand-500/40'
            ]"
          >
            <div class="flex items-center gap-2.5 min-w-0">
              <div
                :class="[
                  'w-8 h-8 rounded-xl flex items-center justify-center font-bold text-xs shrink-0',
                  ch.data.isActive
                    ? 'bg-amber-500 text-slate-950'
                    : ch.data.isCompleted
                      ? 'bg-brand-600 text-white'
                      : 'bg-slate-100 dark:bg-canvas-elevated text-slate-600 dark:text-slate-300 border border-transparent dark:border-white/[0.06]'
                ]"
              >
                <span>{{ ch.data.index }}</span>
              </div>
              <div class="min-w-0">
                <div class="font-bold text-xs sm:text-sm truncate">
                  {{ ch.data.title }}
                </div>
                <div class="text-xs text-slate-400 dark:text-slate-500 truncate flex items-center gap-1.5">
                  <span>{{ ch.data.completedCount }}/{{ ch.data.totalCount }} {{ $t('roadmap.slices') }}</span>
                  <span v-if="ch.data.isActive" class="text-amber-600 dark:text-amber-400 font-bold flex items-center gap-0.5">
                    <Flame class="w-3 h-3 shrink-0" />
                    <span>{{ $t('roadmap.today') }}</span>
                  </span>
                </div>
              </div>
            </div>

            <div class="flex items-center gap-1.5 shrink-0">
              <span class="text-xs font-mono font-bold text-slate-400 dark:text-slate-500">
                {{ ch.data.slices.length }}
              </span>
              <component
                :is="expandedChapterIds.has(ch.data.id) ? ChevronDown : ChevronRight"
                class="w-4 h-4 text-slate-400 transition-transform"
              />
            </div>
          </div>
        </foreignObject>

        <!-- 3. SLICE LEAF NODES -->
        <foreignObject
          v-for="sl in layout.slices"
          :key="sl.data.id"
          :x="sl.x"
          :y="sl.y"
          :width="sl.width"
          :height="sl.height"
          class="overflow-visible"
        >
          <div
            :data-testid="`slice-node-${sl.data.id}`"
            :data-slice-id="sl.data.id"
            @click="handleSliceClick(sl.data)"
            role="button"
            tabindex="0"
            @keydown.enter="handleSliceClick(sl.data)"
            @keydown.space.prevent="handleSliceClick(sl.data)"
            :class="[
              'interactive-node w-full h-full px-3 py-2 rounded-xl border transition-all duration-200 cursor-pointer shadow-sm flex items-center justify-between gap-2 select-none hover:scale-[1.02] active:scale-95 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500',
              isNodeDimmed(sl.data.id) ? 'opacity-25' : 'opacity-100',
              isNodeHighlighted(sl.data.id) ? 'ring-2 ring-brand-500 shadow-md' : '',
              sl.data.isActiveToday
                ? 'bg-amber-50 dark:bg-amber-950/50 border-amber-500 dark:border-amber-400 text-amber-950 dark:text-amber-100 ring-2 ring-amber-500/30'
                : sl.data.isCompleted
                  ? 'bg-brand-50/90 dark:bg-brand-950/40 border-brand-400/50 dark:border-brand-600/60 text-brand-950 dark:text-brand-100'
                  : 'bg-white dark:bg-canvas-subtle border-slate-200 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:border-brand-400 dark:hover:border-brand-500/40'
            ]"
          >
            <div class="flex items-center gap-2 min-w-0">
              <component
                :is="sl.data.isActiveToday ? Flame : sl.data.isCompleted ? CheckCircle2 : Clock"
                :class="[
                  'w-3.5 h-3.5 shrink-0',
                  sl.data.isActiveToday
                    ? 'text-amber-500 animate-pulse'
                    : sl.data.isCompleted
                      ? 'text-brand-500'
                      : 'text-slate-400'
                ]"
              />
              <span class="text-xs font-bold truncate">
                {{ sl.data.title }}
              </span>
            </div>

            <div class="flex items-center gap-1.5 shrink-0">
              <span
                v-if="sl.data.estimatedMinutes"
                class="text-xs text-slate-400 dark:text-slate-500 font-mono whitespace-nowrap shrink-0"
              >
                {{ sl.data.estimatedMinutes }}m
              </span>
              <span
                :class="[
                  'px-2 py-0.5 rounded-md text-xs font-bold whitespace-nowrap shrink-0',
                  sl.data.isActiveToday
                    ? 'bg-amber-500 text-slate-950'
                    : sl.data.isCompleted
                      ? 'bg-brand-500/20 text-brand-700 dark:text-brand-300'
                      : 'bg-slate-100 dark:bg-canvas-elevated text-slate-500 dark:text-slate-400 border border-transparent dark:border-white/[0.06]'
                ]"
              >
                {{ sl.data.isActiveToday ? $t('roadmap.mindmap.start_drill') : (sl.data.isCompleted ? $t('roadmap.completed') : $t('roadmap.ready')) }}
              </span>
            </div>
          </div>
        </foreignObject>
      </g>
    </svg>
  </div>
</template>
