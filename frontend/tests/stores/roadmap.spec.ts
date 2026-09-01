import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useRoadmapStore } from '~/stores/useRoadmapStore'

const mockRoadmapData = {
  totalDays: 30,
  completedDaysCount: 5,
  currentActiveDay: 6,
  overallProgressPercentage: 16.7,
  modules: [
    {
      category: 0,
      moduleTitle: 'Frontend & Browser Internals',
      description: 'Vue 3 Reactivity and Browser Pipelines',
      startDay: 1,
      endDay: 7,
      completedCount: 5,
      totalCount: 7,
      days: [
        {
          dayOrder: 1,
          slug: 'vue3-reactivity',
          title: 'Vue 3 Reactivity Engine',
          summary: 'Proxy and track/trigger',
          difficulty: 1,
          isCompleted: true,
          isActiveToday: false,
          isUnlocked: true,
          drillScore: 10
        },
        {
          dayOrder: 6,
          slug: 'realtime-frontend',
          title: 'Real-time Frontend',
          summary: 'WebSockets and SSE',
          difficulty: 1,
          isCompleted: false,
          isActiveToday: true,
          isUnlocked: true,
          drillScore: null
        }
      ]
    }
  ]
}

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: vi.fn(async (url: string) => {
      if (url.includes('/roadmap')) return mockRoadmapData
      throw new Error('Not found')
    })
  })
}))

describe('useRoadmapStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('fetches curriculum roadmap correctly', async () => {
    const store = useRoadmapStore()
    expect(store.roadmapData).toBeNull()

    await store.fetchRoadmap()

    expect(store.roadmapData).not.toBeNull()
    expect(store.roadmapData?.totalDays).toBe(30)
    expect(store.roadmapData?.completedDaysCount).toBe(5)
    expect(store.roadmapData?.currentActiveDay).toBe(6)
    expect(store.roadmapData?.modules).toHaveLength(1)
    expect(store.roadmapData?.modules[0].days[0].isCompleted).toBe(true)
    expect(store.roadmapData?.modules[0].days[1].isActiveToday).toBe(true)
  })
})
