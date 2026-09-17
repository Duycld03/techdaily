<script setup lang="ts">
import { ref } from 'vue'
import { ListOrdered, GitFork } from 'lucide-vue-next'
import type { RoadmapViewMode } from '~/composables/useRoadmapViewMode'

const props = defineProps<{
  modelValue: RoadmapViewMode
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: RoadmapViewMode): void
}>()

const timelineBtnRef = ref<HTMLButtonElement | null>(null)
const mindmapBtnRef = ref<HTMLButtonElement | null>(null)

function selectView(mode: RoadmapViewMode) {
  if (props.modelValue !== mode) {
    emit('update:modelValue', mode)
  }
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
    event.preventDefault()
    selectView('mindmap')
    mindmapBtnRef.value?.focus()
  } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
    event.preventDefault()
    selectView('timeline')
    timelineBtnRef.value?.focus()
  }
}
</script>

<template>
  <div
    role="tablist"
    :aria-label="$t('roadmap.view_mode_label') || 'Roadmap view mode'"
    class="inline-flex items-center p-1.5 bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-inner w-fit select-none"
    @keydown="handleKeydown"
  >
    <button
      ref="timelineBtnRef"
      type="button"
      role="tab"
      id="tab-timeline"
      aria-controls="tabpanel-timeline"
      :aria-selected="modelValue === 'timeline'"
      :tabindex="modelValue === 'timeline' ? 0 : -1"
      @click="selectView('timeline')"
      :class="[
        'inline-flex items-center gap-2 px-3.5 sm:px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all whitespace-nowrap shrink-0 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500',
        modelValue === 'timeline'
          ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
          : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
      ]"
    >
      <ListOrdered class="w-4 h-4 shrink-0" />
      <span>{{ $t('roadmap.timeline_view') }}</span>
    </button>

    <button
      ref="mindmapBtnRef"
      type="button"
      role="tab"
      id="tab-mindmap"
      aria-controls="tabpanel-mindmap"
      :aria-selected="modelValue === 'mindmap'"
      :tabindex="modelValue === 'mindmap' ? 0 : -1"
      @click="selectView('mindmap')"
      :class="[
        'inline-flex items-center gap-2 px-3.5 sm:px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all whitespace-nowrap shrink-0 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500',
        modelValue === 'mindmap'
          ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
          : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
      ]"
    >
      <GitFork class="w-4 h-4 shrink-0" />
      <span>{{ $t('roadmap.mindmap_view') }}</span>
    </button>
  </div>
</template>
