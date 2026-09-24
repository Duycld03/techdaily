import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import AppHeader from '~/components/layout/AppHeader.vue'
import AppSidebar from '~/components/layout/AppSidebar.vue'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import { useAuthStore } from '~/stores/useAuthStore'

describe('components/layout/AppChrome', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setActivePinia(createPinia())
  })

  describe('AppHeader.vue', () => {
    it('renders sticky header with brand title, command palette trigger, and telemetry widgets', async () => {
      const authStore = useAuthStore()
      authStore.user = { id: 'u1', email: 'test@example.com', name: 'Alex' } as any

      const wrapper = mount(AppHeader, {
        global: {
          stubs: {
            NuxtLink: { template: '<a><slot /></a>' },
            StreakBadge: { template: '<div data-testid="streak-badge-stub">Streak</div>' },
            LocaleSelector: { template: '<div data-testid="locale-selector-stub">Locale</div>' },
            ThemeToggle: { template: '<div data-testid="theme-toggle-stub">Theme</div>' },
            Teleport: true
          }
        }
      })
      await flushPromises()

      const header = wrapper.find('header')
      expect(header.exists()).toBe(true)
      expect(header.classes()).toContain('sticky')
      expect(header.classes()).toContain('z-40')
      expect(wrapper.text()).toContain('TechDaily')
      expect(wrapper.find('[data-testid="streak-badge-stub"]').exists()).toBe(true)
    })

    it('opens mobile navigation drawer on hamburger button click', async () => {
      const wrapper = mount(AppHeader, {
        global: {
          stubs: {
            NuxtLink: { template: '<a><slot /></a>' },
            StreakBadge: true,
            LocaleSelector: true,
            ThemeToggle: true,
            Teleport: true
          }
        }
      })
      await flushPromises()

      // Click mobile hamburger
      const menuBtn = wrapper.find('button[aria-label="Open Navigation Menu"]')
      expect(menuBtn.exists()).toBe(true)
      await menuBtn.trigger('click')
      await flushPromises()

      const drawer = wrapper.find('[data-testid="mobile-nav-drawer"]')
      expect(drawer.exists()).toBe(true)
      expect(drawer.classes()).toContain('z-50')
    })
  })

  describe('AppSidebar.vue', () => {
    it('renders desktop sidebar with md:w-64 width and label nowrap', async () => {
      const wrapper = mount(AppSidebar, {
        global: {
          stubs: {
            NuxtLink: {
              props: ['to'],
              template: '<div class="nuxt-link"><slot :navigate="() => {}" :href="to" /></div>'
            }
          }
        }
      })
      await flushPromises()

      const aside = wrapper.find('aside')
      expect(aside.exists()).toBe(true)
      expect(aside.classes()).toContain('md:w-64')

      const labels = wrapper.findAll('span.whitespace-nowrap')
      expect(labels.length).toBeGreaterThan(0)
    })
  })

  describe('MasterDetailLayout.vue', () => {
    it('renders sub-navigation rail and expansive content panel slots', () => {
      const wrapper = mount(MasterDetailLayout, {
        slots: {
          header: '<div data-testid="test-header">Settings Header</div>',
          nav: '<div data-testid="test-nav">Nav Rail Content</div>',
          content: '<div data-testid="test-content">Settings Form Content</div>'
        }
      })

      expect(wrapper.find('[data-testid="test-header"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="test-nav"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="test-content"]').exists()).toBe(true)

      const navEl = wrapper.find('nav')
      expect(navEl.classes()).toContain('md:w-72')
      expect(navEl.classes()).toContain('overflow-x-auto')

      const sectionEl = wrapper.find('section')
      expect(sectionEl.classes()).toContain('flex-1')
    })
  })
})
