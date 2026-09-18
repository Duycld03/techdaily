import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AppCommandPalette from '~/components/app/AppCommandPalette.vue'
import { useCommandPalette } from '~/composables/useCommandPalette'

const mockPush = vi.fn()
vi.stubGlobal('useRouter', () => ({
  push: mockPush
}))
const translations: Record<string, string> = {
  'command_palette.action_dashboard': 'Command Center Dashboard',
  'command_palette.action_dashboard_desc': 'Overview of daily momentum, active recall pace, and learning milestones',
  'command_palette.action_today': "Today's Reading Slice",
  'command_palette.action_today_desc': 'Resume your active reading slice and daily practice',
  'command_palette.action_roadmap': 'Architecture Roadmap & Mindmap',
  'command_palette.action_roadmap_desc': 'Inspect 30-day curriculum pillars and document trees',
  'command_palette.action_quiz': 'Active Recall Quiz',
  'command_palette.action_quiz_desc': 'Challenge yourself with senior architecture scenarios',
  'command_palette.action_review': 'Spaced Repetition Flashcards',
  'command_palette.action_review_desc': 'Review cards scheduled by the SM-2 algorithm',
  'command_palette.action_graph': 'Knowledge Graph 2D / 3D',
  'command_palette.action_graph_desc': 'Explore connected concepts, pillars, and memory nodes',
  'command_palette.action_library': 'Technical Library & Books',
  'command_palette.action_library_desc': 'Browse and import engineering books and documentation',
  'command_palette.action_notes': 'Highlights & Notes',
  'command_palette.action_notes_desc': 'Search your personal bookmarks, excerpts, and tags',
  'command_palette.action_profile': 'Engineer Profile',
  'command_palette.action_profile_desc': 'View your streaks, goals, and domain progress',
  'command_palette.action_settings': 'Settings & Notifications',
  'command_palette.action_settings_desc': 'Configure study reminders, timezone, and appearance',
  'nav.insights': 'Insights',
  'command_palette.search_placeholder': 'Type a destination, topic, or command...',
  'command_palette.no_results': 'No matching destinations found.',
  'command_palette.hint_navigate': 'Navigate',
  'command_palette.hint_select': 'Select',
  'command_palette.hint_close': 'Close'
}

const tMock = (key: string) => translations[key] || key

vi.stubGlobal('useI18n', () => ({
  t: tMock
}))

const globalMountOptions = {
  stubs: {
    Teleport: true
  },
  mocks: {
    $t: tMock
  }
}

describe('AppCommandPalette.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    const { close } = useCommandPalette()
    close()
  })

  it('remains unrendered when isOpen is false', () => {
    const wrapper = mount(AppCommandPalette, {
      global: globalMountOptions
    })

    expect(wrapper.find('input').exists()).toBe(false)
  })

  it('renders modal and search input when isOpen is true', async () => {
    const { open } = useCommandPalette()
    open()

    const wrapper = mount(AppCommandPalette, {
      global: globalMountOptions
    })
    expect(wrapper.find('input').exists()).toBe(true)
    expect(wrapper.text()).toContain("Today's Reading Slice")
    expect(wrapper.text()).toContain('Knowledge Graph 2D / 3D')
  })

  it('filters destinations based on search query', async () => {
    const { open } = useCommandPalette()
    open()

    const wrapper = mount(AppCommandPalette, {
      global: globalMountOptions
    })

    const input = wrapper.find('input')
    await input.setValue('graph')

    expect(wrapper.text()).toContain('Knowledge Graph 2D / 3D')
    expect(wrapper.text()).not.toContain('Spaced Repetition Flashcards')

    await input.setValue('dashboard')
    expect(wrapper.text()).toContain('Command Center Dashboard')
  })
  it('navigates to destination and closes palette on click', async () => {
    const { isOpen, open } = useCommandPalette()
    open()

    const wrapper = mount(AppCommandPalette, {
      global: globalMountOptions
    })

    const input = wrapper.find('input')
    await input.setValue('roadmap')

    const items = wrapper.findAll('div.cursor-pointer')
    expect(items.length).toBeGreaterThan(0)
    await items[0]!.trigger('click')
    expect(mockPush).toHaveBeenCalledWith('/roadmap')
    expect(isOpen.value).toBe(false)
  })

  it('handles Escape key to close the palette', async () => {
    const { isOpen, open } = useCommandPalette()
    open()

    mount(AppCommandPalette, {
      global: globalMountOptions
    })

    expect(isOpen.value).toBe(true)

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))

    expect(isOpen.value).toBe(false)
  })

  it('handles Cmd+K shortcut to open the palette', async () => {
    const { isOpen, close } = useCommandPalette()
    close()

    mount(AppCommandPalette, {
      global: globalMountOptions
    })

    expect(isOpen.value).toBe(false)

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'k', metaKey: true }))

    expect(isOpen.value).toBe(true)
  })
})
