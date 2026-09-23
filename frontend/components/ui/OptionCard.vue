<script setup lang="ts">
import { computed } from 'vue'
import { Check, X } from 'lucide-vue-next'

export type OptionState = 'default' | 'selected' | 'correct' | 'incorrect'

const props = withDefaults(
  defineProps<{
    letter: string | number
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
      return 'border-brand-500 bg-brand-500/10 dark:bg-brand-500/[0.12] ring-1 ring-brand-500/30 text-brand-950 dark:text-brand-100'
    case 'correct':
      return 'border-emerald-500 bg-emerald-500/10 dark:bg-emerald-500/[0.12] ring-1 ring-emerald-500/40 text-emerald-950 dark:text-emerald-100'
    case 'incorrect':
      return 'border-rose-500 bg-rose-500/10 dark:bg-rose-500/[0.12] ring-1 ring-rose-500/40 text-rose-950 dark:text-rose-100'
    default:
      return 'border-slate-200/80 dark:border-white/[0.08] bg-white/60 dark:bg-canvas-elevated/50 text-slate-700 dark:text-slate-300 hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50 dark:hover:bg-white/[0.04]'
  }
})

const badgeClass = computed(() => {
  switch (props.state) {
    case 'selected':
      return 'bg-brand-600 text-white border-brand-500'
    case 'correct':
      return 'bg-emerald-600 text-white border-emerald-500'
    case 'incorrect':
      return 'bg-rose-600 text-white border-rose-500'
    default:
      return 'bg-slate-100 dark:bg-white/[0.06] text-slate-500 dark:text-slate-400 border-slate-200 dark:border-white/[0.08]'
  }
})

const textClass = computed(() => {
  switch (props.state) {
    case 'correct':
      return 'text-emerald-900 dark:text-emerald-200 font-semibold'
    case 'incorrect':
      return 'text-rose-900 dark:text-rose-200'
    case 'selected':
      return 'text-slate-900 dark:text-white font-semibold'
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
    data-testid="quiz-option"
    @click="emit('select')"
    :class="[
      'group flex w-full items-start gap-3 rounded-xl border px-3 py-2.5 text-left transition-colors duration-150 focus:outline-none disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer select-none',
      containerClass
    ]"
  >
    <span
      :class="[
        'flex h-7 w-7 shrink-0 items-center justify-center rounded-lg border text-xs font-bold tabular-nums transition-colors mt-0.5',
        badgeClass
      ]"
    >
      <Check v-if="state === 'correct'" class="h-3.5 w-3.5" :stroke-width="2.5" />
      <X v-else-if="state === 'incorrect'" class="h-3.5 w-3.5" :stroke-width="2.5" />
      <template v-else>{{ letter }}</template>
    </span>
    <span :class="['flex-1 min-w-0 break-words text-sm leading-relaxed', textClass]">{{ text }}</span>
    <slot name="trailing" />
  </button>
</template>
