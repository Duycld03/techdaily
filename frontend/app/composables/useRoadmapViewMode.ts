import { ref, onMounted } from 'vue'
import { browserStorage, readStored } from '~/utils/clientStorage'

export type RoadmapViewMode = 'timeline' | 'mindmap'

export const ROADMAP_VIEW_MODE_STORAGE_KEY = 'deeppace_roadmap_view_mode'

export function useRoadmapViewMode() {
  const viewMode = ref<RoadmapViewMode>('timeline')

  function init() {
    if (typeof window !== 'undefined') {
      const stored = readStored(browserStorage('localStorage'), ROADMAP_VIEW_MODE_STORAGE_KEY)
      if (stored === 'timeline' || stored === 'mindmap') {
        viewMode.value = stored
      }
    }
  }

  function setViewMode(mode: RoadmapViewMode) {
    if (mode !== 'timeline' && mode !== 'mindmap') return
    viewMode.value = mode
    try { browserStorage('localStorage')?.setItem(ROADMAP_VIEW_MODE_STORAGE_KEY, mode) } catch { /* Keep the in-memory choice usable. */ }
  }

  onMounted(() => {
    init()
  })

  // Synchronous initialization for non-SSR / test environments
  if (typeof window !== 'undefined') {
    init()
  }

  return {
    viewMode,
    setViewMode,
    STORAGE_KEY: ROADMAP_VIEW_MODE_STORAGE_KEY
  }
}
