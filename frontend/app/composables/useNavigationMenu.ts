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
  Palette,
  ShieldCheck
} from 'lucide-vue-next'
import { computed } from 'vue'
import { useCatalogStore } from '~/stores/useCatalogStore'

export interface NavLink {
  name: string
  path: string
  icon: Component
}

export interface NavGroup {
  titleKey: string
  links: NavLink[]
}

export function getNavGroups(isDev: boolean = Boolean(import.meta.dev), canManageCatalog: boolean = false): NavGroup[] {
  const accountLinks: NavLink[] = [
    { name: 'nav.settings_profile', path: '/settings', icon: Settings }
  ]

  if (isDev) {
    accountLinks.push({ name: 'nav.showcase', path: '/showcase', icon: Palette })
  }

  const knowledgeLinks: NavLink[] = [
    { name: 'nav.insights', path: '/insights', icon: Compass },
    { name: 'nav.library', path: '/library', icon: BookOpen },
    { name: 'nav.notes', path: '/notes', icon: Highlighter },
    { name: 'nav.graph', path: '/graph', icon: Network }
  ]

  if (canManageCatalog) {
    knowledgeLinks.push({ name: 'nav.admin_catalog', path: '/admin/catalog', icon: ShieldCheck })
  }

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
  if (linkPath === '/admin/catalog') {
    return currentPath === '/admin/catalog' || currentPath.startsWith('/admin/catalog')
  }
  return currentPath === linkPath
}

export function useNavigationMenu() {
  const route = useRoute()

  const navGroups = computed(() => {
    let canManage = false
    try {
      const catalogStore = useCatalogStore()
      canManage = catalogStore.canManageCatalog
    } catch {
      // Pinia might not be initialized
    }
    return getNavGroups(Boolean(import.meta.dev), canManage)
  })

  return {
    navGroups,
    isLinkActive: (linkPath: string): boolean => isLinkActive(linkPath, route.path)
  }
}
