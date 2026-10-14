<script setup lang="ts">
import { Sun, Moon } from 'lucide-vue-next'

withDefaults(defineProps<{ lightLabel?: string; darkLabel?: string; toggleLabel?: string }>(), {
  lightLabel: 'Switch to Light Mode', darkLabel: 'Switch to Dark Mode', toggleLabel: 'Toggle Color Theme'
})

const colorMode = useColorMode()

function toggleTheme() {
  colorMode.preference = colorMode.value === 'dark' ? 'light' : 'dark'
}
</script>

<template>
  <button
    @click="toggleTheme"
    class="p-2 rounded-xl bg-slate-100 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:text-brand-600 dark:hover:text-amber-400 hover:border-slate-300 dark:hover:border-white/[0.16] transition-all shadow-sm active:scale-95"
    :title="colorMode.value === 'dark' ? lightLabel : darkLabel"
    :aria-label="toggleLabel"
  >
    <Sun v-if="colorMode.value === 'dark'" class="w-4 h-4 text-amber-400" :stroke-width="1.5" />
    <Moon v-else class="w-4 h-4 text-slate-700" :stroke-width="1.5" />
  </button>
</template>
