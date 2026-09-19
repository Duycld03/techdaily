import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import ProfilePage from '~/pages/profile.vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useProfileStore } from '~/stores/useProfileStore'
import { useInterviewQuizStore } from '~/stores/useInterviewQuizStore'

const mockUser = {
  id: 'usr-1',
  email: 'staff.dev@techdaily.dev',
  name: 'Taylor TechLead',
  targetRole: 'Tech Lead',
  dailyGoalMinutes: 15,
  preferredLocale: 'en',
  hasPassword: true,
  isGoogleLinked: false
}

const mockStats = {
  currentStreak: 9,
  longestStreak: 21,
  freezeCreditsRemaining: 2,
  totalDrillsCompleted: 35,
  averageScore: 8.8,
  totalCardsInDeck: 80,
  totalHighlightsSaved: 20,
  memberSince: '2026-01-15'
}

const mockQuizStats = {
  totalAnswered: 40,
  masteredCount: 30,
  reviewQueueCount: 5,
  accuracyRate: 75,
  levelBreakdown: [],
  topicBreakdown: [
    { topic: 'C# 13 Runtime', answeredCount: 10, masteredCount: 8, accuracyRate: 80 },
    { topic: 'PostgreSQL 17 Storage Engine', answeredCount: 10, masteredCount: 7, accuracyRate: 70 },
    { topic: 'System Design Patterns', answeredCount: 10, masteredCount: 9, accuracyRate: 90 },
    { topic: 'Browser Performance', answeredCount: 10, masteredCount: 6, accuracyRate: 60 }
  ]
}

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/api/v1/user/profile')) {
        return { user: { ...mockUser }, stats: { ...mockStats } }
      }
      if (url.includes('/api/v1/quiz/stats')) {
        return { ...mockQuizStats }
      }
      return {}
    }),
    put: vi.fn(async (url: string, body: Record<string, unknown>) => {
      if (url.includes('/api/v1/user/profile')) {
        return { ...mockUser, ...body }
      }
      if (url.includes('/api/v1/user/change-password')) {
        return { message: 'Password changed successfully.' }
      }
      return {}
    })
  })
}))

describe('profile.vue (Asymmetric 2-Column Bento Dashboard)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    const authStore = useAuthStore()
    authStore.token = 'mock-jwt-token'
    authStore.user = {
      id: 'usr-1',
      email: 'staff.dev@techdaily.dev',
      name: 'Taylor TechLead',
      preferredLocale: 'en'
    }
  })

  it('mounts the asymmetric 2-column layout and loads profile & quiz stats', async () => {
    const wrapper = mount(ProfilePage, {
      global: {
        stubs: {
          NuxtLink: true,
          Teleport: true
        }
      }
    })

    await flushPromises()

    // Title
    expect(wrapper.text()).toContain('profile.title')

    // Identity Passport & Milestones
    expect(wrapper.text()).toContain('Taylor TechLead')
    expect(wrapper.text()).toContain('staff.dev@techdaily.dev')
    expect(wrapper.text()).toContain('Tech Lead')
    expect(wrapper.text()).toContain('profile.longest_streak_record')
    expect(wrapper.text()).toContain('35') // Drills
    expect(wrapper.text()).toContain('80') // Memory Vault
    expect(wrapper.text()).toContain('20') // Highlights Saved
    // Left Column: Domain Goal Tracker
    expect(wrapper.text()).toContain('profile.domain_mastery')
    expect(wrapper.text()).toContain('profile.domain_backend_runtime')
    expect(wrapper.text()).toContain('profile.domain_data_storage')
    expect(wrapper.text()).toContain('profile.domain_system_design')
    expect(wrapper.text()).toContain('profile.domain_frontend')
  })

  it('switches between Personal Info and Security tabs', async () => {
    const wrapper = mount(ProfilePage)
    await flushPromises()

    // Default tab: personal
    expect(wrapper.find('input[type="text"]').exists()).toBe(true)

    // Switch to security tab
    const tabButtons = wrapper.findAll('button[type="button"]')
    const securityTabBtn = tabButtons.find(b => b.text().includes('profile.tab_security'))
    expect(securityTabBtn).toBeDefined()
    await securityTabBtn!.trigger('click')

    // Security form is visible
    expect(wrapper.text()).toContain('profile.current_password')
    expect(wrapper.text()).toContain('profile.new_password')
    expect(wrapper.text()).toContain('profile.confirm_password')
  })

  it('updates daily goal pace chips when clicked', async () => {
    const wrapper = mount(ProfilePage)
    await flushPromises()

    // Find 30m chip
    const buttons = wrapper.findAll('button[type="button"]')
    const thirtyMinChip = buttons.find(b => b.text() === '30m')
    expect(thirtyMinChip).toBeDefined()
    await thirtyMinChip!.trigger('click')

    // Active chip styling check
    expect(thirtyMinChip!.classes()).toContain('border-brand-500')
  })

  it('saves profile updates via handleProfileSave', async () => {
    const wrapper = mount(ProfilePage)
    await flushPromises()

    const nameInput = wrapper.find('input[type="text"]')
    await nameInput.setValue('Taylor Senior Lead')

    const form = wrapper.find('form')
    await form.trigger('submit.prevent')
    await flushPromises()

    const profileStore = useProfileStore()
    expect(profileStore.profile?.name).toBe('Taylor Senior Lead')
  })
})
