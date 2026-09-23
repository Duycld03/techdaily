import { describe, it, expect } from 'vitest'
import { navGroups, getNavGroups, isLinkActive } from '~/composables/useNavigationMenu'

describe('useNavigationMenu', () => {
  it('defines structured navigation groups with complete route entries', () => {
    expect(navGroups).toHaveLength(3)

    const practice = navGroups[0]
    const knowledge = navGroups[1]
    const account = navGroups[2]
    expect(practice).toBeDefined()
    expect(knowledge).toBeDefined()
    expect(account).toBeDefined()
    if (!practice || !knowledge || !account) return

    expect(practice.titleKey).toBe('nav.group_practice')
    expect(practice.links.map((l) => l.path)).toEqual([
      '/',
      '/today',
      '/roadmap',
      '/quiz',
      '/review'
    ])
    expect(practice.links[0]?.name).toBe('nav.dashboard')

    expect(knowledge.titleKey).toBe('nav.group_knowledge')
    expect(knowledge.links.map((l) => l.path)).toEqual([
      '/insights',
      '/library',
      '/notes',
      '/graph'
    ])

    expect(account.titleKey).toBe('nav.group_account')
    expect(account.links.map((l) => l.path)).toContain('/settings')
    expect(account.links[0]?.name).toBe('nav.settings_profile')
  })

  it('includes showcase route in development mode (isDev = true)', () => {
    const devGroups = getNavGroups(true)
    const account = devGroups[2]
    expect(account).toBeDefined()
    expect(account?.links.map((l) => l.path)).toEqual([
      '/settings',
      '/showcase'
    ])
    expect(account?.links[1]?.name).toBe('nav.showcase')
  })

  it('excludes showcase route when running in production mode (import.meta.dev = false)', () => {
    const prodGroups = getNavGroups(false)
    expect(prodGroups).toHaveLength(3)

    const account = prodGroups[2]
    expect(account).toBeDefined()
    expect(account?.links.map((l) => l.path)).toEqual(['/settings'])
    expect(account?.links.some((l) => l.path === '/showcase')).toBe(false)
  })

  it('correctly matches active links without false positives on root path', () => {
    // When on root dashboard
    expect(isLinkActive('/', '/')).toBe(true)
    expect(isLinkActive('/today', '/')).toBe(false)
    expect(isLinkActive('/quiz', '/')).toBe(false)

    // When on /today
    expect(isLinkActive('/today', '/today')).toBe(true)
    expect(isLinkActive('/today', '/today/practice')).toBe(true)
    expect(isLinkActive('/', '/today')).toBe(false)

    // When on /library and /read
    expect(isLinkActive('/library', '/library')).toBe(true)
    expect(isLinkActive('/library', '/library/docs/senior-aspnet')).toBe(true)
    expect(isLinkActive('/library', '/read')).toBe(true)
    expect(isLinkActive('/library', '/read/slice-123')).toBe(true)
    expect(isLinkActive('/today', '/read')).toBe(false)

    // When on /graph
    expect(isLinkActive('/graph', '/graph')).toBe(true)
    expect(isLinkActive('/graph', '/graph/pillar-backend')).toBe(true)
    // When on /settings or redirected from /profile
    expect(isLinkActive('/settings', '/settings')).toBe(true)
    expect(isLinkActive('/settings', '/profile')).toBe(true)

    // When on exact routes
    expect(isLinkActive('/settings', '/settings')).toBe(true)
    expect(isLinkActive('/profile', '/settings')).toBe(false)
  })
})
