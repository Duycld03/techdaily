<script setup lang="ts">
import { computed } from 'vue'
import { Check, X } from 'lucide-vue-next'

type OptionState = 'default' | 'selected' | 'correct' | 'incorrect'

const props = withDefaults(
  defineProps<{
    letter: string
    text: string
    state?: OptionState
    disabled?: boolean
  }>(),
  {
    state: 'default',
    disabled: false
  }
)

const emit = defineEmits<{ (e: 'select'): void }>()

const containerClass = computed(() => {
  switch (props.state) {
    case 'selected':
      return 'border-brand-500 bg-brand-500/10 dark:bg-brand-500/[0.12] ring-1 ring-brand-500/40'
    case 'correct':
      return 'border-emerald-500 bg-emerald-500/10 dark:bg-emerald-500/[0.12] ring-1 ring-emerald-500/40'
    case 'incorrect':
      return 'border-red-500 bg-red-500/10 dark:bg-red-500/[0.12] ring-1 ring-red-500/40'
    default:
      return 'border-slate-200/80 dark:border-white/[0.08] bg-white/60 dark:bg-canvas-elevated/50 hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50 dark:hover:bg-white/[0.04]'
  }
})

const badgeClass = computed(() => {
  switch (props.state) {
    case 'selected':
      return 'bg-brand-500 text-white border-brand-500'
    case 'correct':
      return 'bg-emerald-500 text-white border-emerald-500'
    case 'incorrect':
      return 'bg-red-500 text-white border-red-500'
    default:
      return 'bg-slate-100 dark:bg-white/[0.06] text-slate-500 dark:text-slate-400 border-slate-200 dark:border-white/[0.08]'
  }
})

const textClass = computed(() => {
  switch (props.state) {
    case 'correct':
      return 'text-emerald-700 dark:text-emerald-300'
    case 'incorrect':
      return 'text-red-700 dark:text-red-300'
    case 'selected':
      return 'text-slate-900 dark:text-white'
    default:
      return 'text-slate-700 dark:text-slate-300'
  }
})
</script>

<template>
  <button
    type="button"
    :disabled="disabled"
    :aria-pressed="state === 'selected'"
    @click="emit('select')"
    :class="[
      'group flex w-full items-center gap-3 rounded-lg border px-3 py-2.5 text-left transition-colors duration-150 focus:outline-none disabled:cursor-not-allowed disabled:opacity-60',
      containerClass
    ]"
  >
    <span
      :class="[
        'flex h-7 w-7 shrink-0 items-center justify-center rounded-md border text-xs font-bold tabular-nums transition-colors',
        badgeClass
      ]"
    >
      <Check v-if="state === 'correct'" class="h-4 w-4" :stroke-width="2.5" />
      <X v-else-if="state === 'incorrect'" class="h-4 w-4" :stroke-width="2.5" />
      <template v-else>{{ letter }}</template>
    </span>
    <span :class="['text-sm font-medium leading-snug', textClass]">{{ text }}</span>
  </button>
</template>
