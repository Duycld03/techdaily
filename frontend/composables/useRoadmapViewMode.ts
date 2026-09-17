import { ref, onMounted } from 'vue'

export type RoadmapViewMode = 'timeline' | 'mindmap'

export const ROADMAP_VIEW_MODE_STORAGE_KEY = 'techdaily_roadmap_view_mode'

export function useRoadmapViewMode() {
  const viewMode = ref<RoadmapViewMode>('timeline')

  function init() {
    if (typeof window !== 'undefined' && window.localStorage) {
      const stored = window.localStorage.getItem(ROADMAP_VIEW_MODE_STORAGE_KEY)
      if (stored === 'timeline' || stored === 'mindmap') {
        viewMode.value = stored
      }
    }
  }

  function setViewMode(mode: RoadmapViewMode) {
    if (mode !== 'timeline' && mode !== 'mindmap') return
    viewMode.value = mode
    if (typeof window !== 'undefined' && window.localStorage) {
      window.localStorage.setItem(ROADMAP_VIEW_MODE_STORAGE_KEY, mode)
    }
  }

  onMounted(() => {
    init()
  })

  // Synchronous initialization for non-SSR / test environments
  if (typeof window !== 'undefined' && window.localStorage) {
    init()
  }

  return {
    viewMode,
    setViewMode,
    STORAGE_KEY: ROADMAP_VIEW_MODE_STORAGE_KEY
  }
}
