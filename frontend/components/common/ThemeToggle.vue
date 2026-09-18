<script setup lang="ts">
import { Sun, Moon } from 'lucide-vue-next'

const colorMode = useColorMode()

function toggleTheme() {
  colorMode.preference = colorMode.value === 'dark' ? 'light' : 'dark'
}
</script>

<template>
  <button
    @click="toggleTheme"
    class="p-2 rounded-xl bg-slate-100 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:text-brand-600 dark:hover:text-amber-400 hover:border-slate-300 dark:hover:border-white/[0.16] transition-all shadow-sm active:scale-95"
    :title="colorMode.value === 'dark' ? 'Switch to Light Mode' : 'Switch to Dark Mode'"
    aria-label="Toggle Color Theme"
  >
    <Sun v-if="colorMode.value === 'dark'" class="w-4 h-4 text-amber-400" />
    <Moon v-else class="w-4 h-4 text-slate-700" />
  </button>
</template>
