<script setup lang="ts">
import { Target, Layers, Library, Highligther, BarChart3, Settings } from 'lucide-vue-next'

const route = useRoute()

const links = [
  { name: 'nav.today', path: '/today', icon: Target },
  { name: 'nav.review', path: '/review', icon: Layers },
  { name: 'nav.settings', path: '/settings', icon: Settings }
]
</script>

<template>
  <aside class="w-16 md:w-56 border-r border-slate-800/80 bg-slate-950/60 flex flex-col justify-between p-3 shrink-0">
    <nav class="space-y-1.5">
      <NuxtLink
        v-for="link in links"
        :key="link.path"
        :to="link.path"
        :class="[
          'flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all',
          route.path === link.path
            ? 'bg-brand-500/10 border border-brand-500/20 text-brand-400 font-semibold'
            : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/60'
        ]"
      >
        <component :is="link.icon" class="w-4 h-4 shrink-0" />
        <span class="hidden md:inline">{{ $t(link.name) }}</span>
      </NuxtLink>
    </nav>

    <!-- Bottom summary banner -->
    <div class="hidden md:block p-3 rounded-xl bg-slate-900/60 border border-slate-800/60 text-xs text-slate-400">
      <div class="font-semibold text-slate-200 mb-1">Senior 30-Day Sprint</div>
      <div class="text-slate-500">Multimodal AI + SM-2 spaced repetition</div>
    </div>
  </aside>
</template>
