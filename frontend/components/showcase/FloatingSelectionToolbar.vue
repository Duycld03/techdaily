<script setup lang="ts">
import { Sparkles, Languages, Copy } from 'lucide-vue-next'

const emit = defineEmits<{
  (e: 'explain'): void
  (e: 'translate'): void
  (e: 'copy'): void
}>()
</script>

<template>
  <div
    class="glass-panel inline-flex items-center gap-1 rounded-full p-1 shadow-xl"
    role="toolbar"
    aria-label="Text selection actions"
  >
    <button
      type="button"
      class="flex h-[26px] items-center gap-1.5 rounded-full bg-brand-500 px-2.5 text-xs font-semibold text-white transition-colors hover:bg-brand-600 focus:outline-none focus-visible:ring-1 focus-visible:ring-brand-300"
      @click="emit('explain')"
    >
      <Sparkles class="h-3.5 w-3.5" :stroke-width="1.5" />
      Explain Term
    </button>

    <span class="mx-0.5 h-4 w-px bg-slate-300/60 dark:bg-white/[0.12]" aria-hidden="true" />

    <button
      type="button"
      class="flex h-[26px] w-[26px] items-center justify-center rounded-full text-slate-600 transition-colors hover:bg-slate-200/70 dark:text-slate-300 dark:hover:bg-white/[0.08] focus:outline-none focus-visible:ring-1 focus-visible:ring-brand-300"
      aria-label="Translate selection"
      @click="emit('translate')"
    >
      <Languages class="h-3.5 w-3.5" :stroke-width="1.5" />
    </button>
    <button
      type="button"
      class="flex h-[26px] w-[26px] items-center justify-center rounded-full text-slate-600 transition-colors hover:bg-slate-200/70 dark:text-slate-300 dark:hover:bg-white/[0.08] focus:outline-none focus-visible:ring-1 focus-visible:ring-brand-300"
      aria-label="Copy selection"
      @click="emit('copy')"
    >
      <Copy class="h-3.5 w-3.5" :stroke-width="1.5" />
    </button>
  </div>
</template>
