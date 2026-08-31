<script setup lang="ts">
import { Target, Layers, BookOpen, Highlighter, User, Settings } from 'lucide-vue-next'

const route = useRoute()

const links = [
  { name: 'nav.today', path: '/today', icon: Target },
  { name: 'nav.review', path: '/review', icon: Layers },
  { name: 'nav.library', path: '/library', icon: BookOpen },
  { name: 'nav.notes', path: '/notes', icon: Highlighter },
  { name: 'nav.profile', path: '/profile', icon: User },
  { name: 'nav.settings', path: '/settings', icon: Settings }
]

function isLinkActive(linkPath: string): boolean {
  if (linkPath === '/today') {
    return route.path === '/today' || route.path === '/'
  }
  if (linkPath === '/library') {
    return route.path.startsWith('/library') || route.path.startsWith('/read')
  }
  return route.path === linkPath || route.path.startsWith(linkPath + '/')
}
</script>

<template>
  <aside class="w-16 md:w-60 border-r border-slate-200 dark:border-slate-800/80 bg-white/95 dark:bg-slate-950/60 flex flex-col justify-between p-3.5 shrink-0 transition-colors duration-200">
    <nav class="space-y-2">
      <NuxtLink
        v-for="link in links"
        :key="link.path"
        :to="link.path"
        active-class=""
        exact-active-class=""
        :class="[
          'flex items-center gap-3.5 px-3.5 py-3 rounded-2xl text-sm font-semibold border transition-all outline-none focus:outline-none',
          isLinkActive(link.path)
            ? 'bg-brand-50 dark:bg-brand-500/10 border-brand-200 dark:border-brand-500/20 text-brand-700 dark:text-brand-400 font-bold shadow-sm'
            : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-100 dark:hover:bg-slate-900/60'
        ]"
      >
        <component :is="link.icon" class="w-4 h-4 shrink-0" />
        <span class="hidden md:inline">{{ $t(link.name) }}</span>
      </NuxtLink>
    </nav>

    <!-- Bottom summary banner -->
    <div class="hidden md:block p-3.5 rounded-2xl bg-slate-100/80 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800/60 text-xs text-slate-600 dark:text-slate-400">
      <div class="font-bold text-slate-800 dark:text-slate-200 mb-1">Senior 30-Day Sprint</div>
      <div class="text-slate-500 text-[11px] leading-relaxed">Multimodal AI + SM-2 spaced repetition</div>
    </div>
  </aside>
</template>
