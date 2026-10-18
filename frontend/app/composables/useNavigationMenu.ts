import type { Component } from 'vue'
import {
  LayoutGrid,
  Target,
  Map,
  HelpCircle,
  Layers,
  Compass,
  BookOpen,
  Highlighter,
  Network,
  Settings,
  Palette
} from 'lucide-vue-next'
import { computed } from 'vue'

export interface NavLink {
  name: string
  path: string
  icon: Component
}

export interface NavGroup {
  titleKey: string
  links: NavLink[]
}

export function getNavGroups(isDev: boolean = Boolean(import.meta.dev)): NavGroup[] {
  const accountLinks: NavLink[] = [
    { name: 'nav.settings_profile', path: '/settings', icon: Settings },
    ...(isDev ? [{ name: 'nav.showcase', path: '/showcase', icon: Palette }] : [])
  ]

  const knowledgeLinks: NavLink[] = [
    { name: 'nav.insights', path: '/insights', icon: Compass },
    { name: 'nav.library', path: '/library', icon: BookOpen },
    { name: 'nav.notes', path: '/notes', icon: Highlighter },
    { name: 'nav.graph', path: '/graph', icon: Network }
  ]

  return [
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
      links: knowledgeLinks
    },
    {
      titleKey: 'nav.group_account',
      links: accountLinks
    }
  ]
}

export const navGroups: NavGroup[] = getNavGroups()

export function isLinkActive(linkPath: string, currentPath: string): boolean {
  if (linkPath === '/') {
    return currentPath === '/'
  }
  if (linkPath === '/today') {
    return currentPath === '/today' || currentPath.startsWith('/today/')
  }
  if (linkPath === '/library') {
    return (
      currentPath === '/library' ||
      currentPath.startsWith('/library/') ||
      currentPath === '/read' ||
      currentPath.startsWith('/read/')
    )
  }
  if (linkPath === '/graph') {
    return currentPath === '/graph' || currentPath.startsWith('/graph/')
  }
  if (linkPath === '/settings') {
    return currentPath === '/settings' || currentPath === '/profile'
  }
  return currentPath === linkPath
}

export function useNavigationMenu() {
  const route = useRoute()

  const navGroups = computed(() => getNavGroups(Boolean(import.meta.dev)))

  return {
    navGroups,
    isLinkActive: (linkPath: string): boolean => isLinkActive(linkPath, route.path)
  }
}
