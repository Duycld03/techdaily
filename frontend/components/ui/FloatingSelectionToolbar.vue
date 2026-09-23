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
    class="glass-panel inline-flex items-center gap-1 rounded-full p-1 shadow-xl border border-slate-200/80 dark:border-white/[0.12]"
    role="toolbar"
    aria-label="Text selection actions"
  >
    <button
      type="button"
      class="flex h-7 items-center gap-1.5 rounded-full bg-brand-600 px-3 text-xs font-bold text-white transition-colors hover:bg-brand-500 cursor-pointer shadow-sm"
      @click="emit('explain')"
    >
      <Sparkles class="h-3.5 w-3.5" :stroke-width="1.5" />
      <span>{{ $t('reader.explain_term') || 'Explain Term' }}</span>
    </button>

    <span class="mx-0.5 h-4 w-px bg-slate-300/60 dark:bg-white/[0.12]" aria-hidden="true" />

    <button
      type="button"
      class="flex h-7 w-7 items-center justify-center rounded-full text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-white/[0.08] dark:hover:text-white cursor-pointer"
      :title="$t('reader.translate') || 'Translate'"
      aria-label="Translate selection"
      @click="emit('translate')"
    >
      <Languages class="h-3.5 w-3.5" :stroke-width="1.5" />
    </button>
    <button
      type="button"
      class="flex h-7 w-7 items-center justify-center rounded-full text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-white/[0.08] dark:hover:text-white cursor-pointer"
      :title="$t('common.copy') || 'Copy'"
      aria-label="Copy selection"
      @click="emit('copy')"
    >
      <Copy class="h-3.5 w-3.5" :stroke-width="1.5" />
    </button>
  </div>
</template>
