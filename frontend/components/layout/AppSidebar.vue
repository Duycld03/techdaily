<script setup lang="ts">
import { LayoutGrid, Target, Map, Sparkles, HelpCircle, Layers, BookOpen, Highlighter, Network, User, Settings } from 'lucide-vue-next'

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
      { name: 'nav.dashboard', path: '/', icon: LayoutGrid },
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
      { name: 'nav.notes', path: '/notes', icon: Highlighter },
      { name: 'nav.graph', path: '/graph', icon: Network }
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
  if (linkPath === '/') {
    return currentPath === '/'
  }
  if (linkPath === '/today') {
    return currentPath === '/today' || currentPath.startsWith('/today')
  }
  if (linkPath === '/library') {
    return currentPath === '/library' || currentPath.startsWith('/read')
  }
  if (linkPath === '/graph') {
    return currentPath === '/graph' || currentPath.startsWith('/graph')
  }
  return currentPath === linkPath
}
</script>

<template>
  <aside class="hidden md:flex md:w-60 border-r border-slate-200/80 dark:border-white/[0.06] bg-white/95 dark:bg-canvas-subtle/80 backdrop-blur-sm flex-col justify-between p-3.5 shrink-0 transition-colors duration-200 select-none overflow-y-auto">
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
              'flex items-center gap-3 px-3.5 py-2 rounded-xl text-sm transition-colors border-l-2',
              isLinkActive(link.path)
                ? 'bg-brand-500/10 dark:bg-white/[0.06] text-brand-600 dark:text-white font-semibold border-brand-500 shadow-sm'
                : 'border-transparent text-slate-600 dark:text-slate-400 hover:bg-slate-100/70 dark:hover:bg-white/[0.04] hover:text-slate-900 dark:hover:text-slate-100 font-medium'
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
  </aside>
</template>
