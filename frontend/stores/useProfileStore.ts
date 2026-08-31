import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useApiClient } from '~/composables/useApiClient'
import { useAuthStore } from '~/stores/useAuthStore'

export interface UserProfile {
  id: string
  email: string
  name: string
  avatarUrl?: string
  preferredLocale: string
  targetRole: string
  dailyGoalMinutes: number
  telegramChatId?: number
  hasPassword: boolean
  isGoogleLinked: boolean
}

export interface UserLearningStats {
  currentStreak: number
  longestStreak: number
  freezeCreditsRemaining: number
  totalDrillsCompleted: number
  averageScore: number
  totalCardsInDeck: number
  totalHighlightsSaved: number
  memberSince: string
}

export const useProfileStore = defineStore('profile', () => {
  const profile = ref<UserProfile | null>(null)
  const stats = ref<UserLearningStats | null>(null)
  const isLoading = ref(false)
  const isUpdating = ref(false)
  const error = ref<string | null>(null)
  const successMessage = ref<string | null>(null)

  async function fetchProfile() {
    isLoading.value = true
    error.value = null
    try {
      const api = useApiClient()
      const res = await api.get<{ user: UserProfile; stats: UserLearningStats }>('/api/v1/user/profile')
      profile.value = res.user
      stats.value = res.stats
      return res
    } catch (err: any) {
      error.value = err.message || 'Failed to load profile.'
    } finally {
      isLoading.value = false
    }
  }

  async function updateProfile(data: {
    name?: string
    avatarUrl?: string
    preferredLocale?: string
    targetRole?: string
    dailyGoalMinutes?: number
    telegramChatId?: number
  }) {
    isUpdating.value = true
    error.value = null
    successMessage.value = null
    try {
      const api = useApiClient()
      const updated = await api.post<{
        id: string
        email: string
        name: string
        avatarUrl?: string
        preferredLocale: string
        targetRole: string
        dailyGoalMinutes: number
        telegramChatId?: number
      }>('/api/v1/user/profile', data)

      if (profile.value) {
        profile.value = {
          ...profile.value,
          ...updated
        }
      }

      // Sync with auth store
      const authStore = useAuthStore()
      if (authStore.user) {
        authStore.user.name = updated.name
        authStore.user.preferredLocale = updated.preferredLocale
        authStore.user.avatarUrl = updated.avatarUrl
      }

      successMessage.value = 'Profile updated successfully.'
      return updated
    } catch (err: any) {
      error.value = err.message || 'Failed to update profile.'
      throw err
    } finally {
      isUpdating.value = false
    }
  }

  async function changePassword(currentPassword: string, newPassword: string) {
    isUpdating.value = true
    error.value = null
    successMessage.value = null
    try {
      const api = useApiClient()
      const res = await api.post<{ message: string }>('/api/v1/user/change-password', {
        currentPassword,
        newPassword
      })

      if (profile.value) {
        profile.value.hasPassword = true
      }

      successMessage.value = res.message || 'Password changed successfully.'
      return res
    } catch (err: any) {
      error.value = err.message || 'Failed to change password.'
      throw err
    } finally {
      isUpdating.value = false
    }
  }

  return {
    profile,
    stats,
    isLoading,
    isUpdating,
    error,
    successMessage,
    fetchProfile,
    updateProfile,
    changePassword
  }
})
