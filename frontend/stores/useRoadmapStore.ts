import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

export interface RoadmapDayNode {
  dayOrder: number
  slug: string
  title: string
  summary: string
  difficulty: number
  isCompleted: boolean
  isActiveToday: boolean
  isUnlocked: boolean
  drillScore: number | null
}

export interface CurriculumModule {
  category: number
  moduleTitle: string
  description: string
  startDay: number
  endDay: number
  completedCount: number
  totalCount: number
  days: RoadmapDayNode[]
}

export interface CurriculumRoadmapData {
  totalDays: number
  completedDaysCount: number
  currentActiveDay: number
  overallProgressPercentage: number
  modules: CurriculumModule[]
}

export const useRoadmapStore = defineStore('roadmap', () => {
  const api = useApiClient()
  const roadmapData = ref<CurriculumRoadmapData | null>(null)
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)

  async function fetchRoadmap() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<CurriculumRoadmapData>('/api/v1/curriculum/roadmap')
      roadmapData.value = response
    } catch (err: any) {
      error.value = err?.data?.error || err?.message || 'Failed to load curriculum roadmap.'
    } finally {
      isLoading.value = false
    }
  }

  return {
    roadmapData,
    isLoading,
    error,
    fetchRoadmap
  }
})
