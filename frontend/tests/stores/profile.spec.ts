import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useProfileStore } from '~/stores/useProfileStore'

const mockProfile = {
  user: {
    id: 'u-1',
    email: 'architect@techdaily.local',
    name: 'Senior Architect',
    avatarUrl: undefined,
    preferredLocale: 'en',
    targetRole: 'Principal Architect',
    dailyGoalMinutes: 15,
    telegramChatId: 987654321,
    hasPassword: true,
    isGoogleLinked: false
  },
  stats: {
    currentStreak: 10,
    longestStreak: 25,
    freezeCreditsRemaining: 2,
    totalDrillsCompleted: 10,
    averageScore: 9.2,
    totalCardsInDeck: 18,
    totalHighlightsSaved: 7,
    memberSince: '2026-08-01T00:00:00Z'
  }
}

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/profile')) return mockProfile
      throw new Error('Not found')
    }),
    put: vi.fn(async (url: string, body: any) => {
      if (url.includes('/change-password')) {
        return { message: 'Password updated successfully.' }
      }
      if (url.includes('/profile')) {
        return {
          id: 'u-1',
          email: 'architect@techdaily.local',
          name: body.name || 'Updated Name',
          targetRole: body.targetRole || 'Principal Architect',
          dailyGoalMinutes: body.dailyGoalMinutes || 15,
          preferredLocale: body.preferredLocale || 'vi',
          telegramChatId: body.telegramChatId
        }
      }
      throw new Error('Not found')
    })
  })
}))

describe('useProfileStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('fetches user profile and learning statistics', async () => {
    const store = useProfileStore()
    expect(store.profile).toBeNull()
    expect(store.stats).toBeNull()

    await store.fetchProfile()
    expect(store.profile?.name).toBe('Senior Architect')
    expect(store.profile?.targetRole).toBe('Principal Architect')
    expect(store.stats?.currentStreak).toBe(10)
    expect(store.stats?.averageScore).toBe(9.2)
  })

  it('updates profile metadata', async () => {
    const store = useProfileStore()
    await store.fetchProfile()

    await store.updateProfile({
      name: 'Tech Lead Manager',
      targetRole: 'Tech Lead',
      dailyGoalMinutes: 30
    })

    expect(store.profile?.name).toBe('Tech Lead Manager')
    expect(store.profile?.targetRole).toBe('Tech Lead')
    expect(store.profile?.dailyGoalMinutes).toBe(30)
    expect(store.successMessage).toBe('Profile updated successfully.')
  })

  it('changes password successfully', async () => {
    const store = useProfileStore()
    await store.fetchProfile()

    const res = await store.changePassword('oldpassword', 'newSecurePass123')
    expect(res.message).toBe('Password updated successfully.')
    expect(store.profile?.hasPassword).toBe(true)
  })
})
