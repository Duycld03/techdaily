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
  User,
  Settings
} from 'lucide-vue-next'

export interface NavLink {
  name: string
  path: string
  icon: Component
}

export interface NavGroup {
  titleKey: string
  links: NavLink[]
}

export const navGroups: NavGroup[] = [
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
      { name: 'nav.insights', path: '/insights', icon: Compass },
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
  return currentPath === linkPath
}

export function useNavigationMenu() {
  const route = useRoute()

  return {
    navGroups,
    isLinkActive: (linkPath: string): boolean => isLinkActive(linkPath, route.path)
  }
}
