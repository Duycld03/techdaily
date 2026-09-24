<script setup lang="ts">
import { ref, computed } from 'vue'
import { Clock } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    modelValue: string | null | undefined
    id?: string
    disabled?: boolean
    ariaLabel?: string
    placeholder?: string
  }>(),
  {
    modelValue: '08:00',
    disabled: false,
    placeholder: '08:00'
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'change', value: string): void
}>()

const inputRef = ref<HTMLInputElement | null>(null)
const componentId = computed(() => props.id || `app-time-input-${Math.random().toString(36).slice(2, 9)}`)

const formattedTime = computed({
  get() {
    if (!props.modelValue) return '08:00'
    const parts = props.modelValue.split(':')
    if (parts.length >= 2) {
      return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
    }
    return '08:00'
  },
  set(val: string) {
    emit('update:modelValue', val)
    emit('change', val)
  }
})

function onInput(e: Event) {
  const target = e.target as HTMLInputElement
  if (target && target.value) {
    formattedTime.value = target.value
  }
}
</script>

<template>
  <div class="relative w-full flex items-center group">
    <!-- Left Inset Clock Icon -->
    <div
      class="absolute left-3.5 flex items-center pointer-events-none text-slate-400 group-hover:text-brand-500 transition-colors z-10"
      aria-hidden="true"
    >
      <Clock class="w-4 h-4" :stroke-width="1.75" />
    </div>

    <!-- Native Time Input Styled to TechDaily Design System -->
    <input
      ref="inputRef"
      type="time"
      :id="componentId"
      :value="formattedTime"
      :disabled="disabled"
      :aria-label="ariaLabel || placeholder"
      @input="onInput"
      @change="onInput"
      class="w-full h-11 pl-10 pr-3.5 rounded-xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-900 dark:text-slate-100 font-mono text-sm font-semibold tracking-wider transition-all shadow-xs hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50/50 dark:hover:bg-white/[0.02] focus:outline-none focus:ring-2 focus:ring-brand-500/25 focus:border-brand-500 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white dark:disabled:hover:bg-canvas-subtle cursor-pointer select-none [&::-webkit-calendar-picker-indicator]:hidden"
      data-testid="app-time-picker-input"
    />
  </div>
</template>
