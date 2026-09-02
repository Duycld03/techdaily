<script setup lang="ts">
import { Target, Map, Sparkles, HelpCircle, Layers, BookOpen, Highlighter, User, Settings } from 'lucide-vue-next'

const route = useRoute()

interface NavGroup {
  titleKey: string
  links: Array<{
    name: string
    path: string
    icon: any
  }>
}

const navGroups: NavGroup[] = [
  {
    titleKey: 'nav.group_practice',
    links: [
      { name: 'nav.today', path: '/today', icon: Target },
      { name: 'nav.roadmap', path: '/roadmap', icon: Map },
      { name: 'nav.quiz', path: '/quiz', icon: HelpCircle },
      { name: 'nav.review', path: '/review', icon: Layers }
    ]
  },
  {
    titleKey: 'nav.group_knowledge',
    links: [
      { name: 'nav.insights', path: '/insights', icon: Sparkles },
      { name: 'nav.library', path: '/library', icon: BookOpen },
      { name: 'nav.notes', path: '/notes', icon: Highlighter }
    ]
  },
  {
    titleKey: 'nav.group_account',
    links: [
      { name: 'nav.profile', path: '/profile', icon: User },
      { name: 'nav.settings', path: '/settings', icon: Settings }
    ]
  }
]

function isLinkActive(linkPath: string): boolean {
  const currentPath = route.path
  if (linkPath === '/today') {
    return currentPath === '/today' || currentPath === '/'
  }
  if (linkPath === '/library') {
    return currentPath === '/library' || currentPath.startsWith('/read')
  }
  return currentPath === linkPath
}
</script>

<template>
  <aside class="hidden md:flex md:w-60 border-r border-slate-200 dark:border-slate-800/80 bg-white/95 dark:bg-slate-950/60 flex-col justify-between p-3.5 shrink-0 transition-colors duration-200 select-none overflow-y-auto">
    <nav class="space-y-4">
      <div v-for="group in navGroups" :key="group.titleKey" class="space-y-0.5">
        <!-- Category Section Header -->
        <div class="px-3.5 py-1 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
          {{ $t(group.titleKey) }}
        </div>

        <NuxtLink
          v-for="link in group.links"
          :key="link.path"
          :to="link.path"
          custom
          v-slot="{ navigate, href }"
        >
          <a
            :href="href"
            @click="navigate"
            :class="[
              'flex items-center gap-3 px-3.5 py-2 rounded-xl text-sm transition-all',
              isLinkActive(link.path)
                ? 'bg-slate-100 dark:bg-slate-800/80 text-brand-600 dark:text-brand-400 font-bold'
                : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100/70 dark:hover:bg-slate-800/50 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
            ]"
          >
            <component
              :is="link.icon"
              :class="[
                'w-4 h-4 shrink-0 transition-colors',
                isLinkActive(link.path)
                  ? 'text-brand-600 dark:text-brand-400'
                  : 'text-slate-400 dark:text-slate-500'
              ]"
            />
            <span class="whitespace-nowrap">{{ $t(link.name) }}</span>
          </a>
        </NuxtLink>
      </div>
    </nav>

    <!-- Bottom summary banner -->
    <div class="hidden md:block p-3.5 rounded-2xl bg-slate-100/80 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-800/60 text-xs text-slate-600 dark:text-slate-400 mt-4">
      <div class="font-bold text-slate-800 dark:text-slate-200 mb-0.5">Senior 30-Day Sprint</div>
      <div class="text-slate-500 text-[11px] leading-relaxed">Multimodal AI + SM-2 spaced repetition</div>
    </div>
  </aside>
</template>
