<script setup lang="ts">
import type { Component } from 'vue'
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    icon: Component
    heading: string
    description: string
    cta: string
    accent?: 'brand' | 'red' | 'emerald'
  }>(),
  {
    accent: 'brand'
  }
)

const emit = defineEmits<{ (e: 'action'): void }>()

const ring = computed(() => {
  switch (props.accent) {
    case 'red':
      return 'text-red-500 ring-red-500/20 bg-red-500/10 dark:bg-red-500/[0.08]'
    case 'emerald':
      return 'text-emerald-500 ring-emerald-500/20 bg-emerald-500/10 dark:bg-emerald-500/[0.08]'
    default:
      return 'text-brand-500 ring-brand-500/20 bg-brand-500/10 dark:bg-brand-500/[0.08]'
  }
})

const button = computed(() => {
  switch (props.accent) {
    case 'red':
      return 'bg-red-500 hover:bg-red-600'
    case 'emerald':
      return 'bg-emerald-500 hover:bg-emerald-600'
    default:
      return 'bg-brand-500 hover:bg-brand-600'
  }
})
</script>

<template>
  <div
    class="glass-card flex flex-col items-center justify-center gap-3 px-5 py-6 text-center"
    style="max-height: 220px"
  >
    <div
      :class="['flex h-11 w-11 items-center justify-center rounded-full ring-4', ring]"
    >
      <component :is="icon" class="h-4 w-4" :stroke-width="1.5" />
    </div>
    <div class="space-y-1">
      <h3 class="text-sm font-semibold text-slate-900 dark:text-white">{{ heading }}</h3>
      <p class="max-w-xs text-xs text-slate-500 dark:text-slate-400">{{ description }}</p>
    </div>
    <button
      type="button"
      :class="[
        'h-9 rounded-lg px-3.5 text-sm font-semibold text-white shadow-sm transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-offset-transparent',
        button
      ]"
      @click="emit('action')"
    >
      {{ cta }}
    </button>
  </div>
</template>
