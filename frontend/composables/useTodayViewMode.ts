import { ref, onMounted } from 'vue'

export type TodayViewMode = 'bento' | 'split'

export const TODAY_VIEW_MODE_STORAGE_KEY = 'techdaily_today_view_mode'

export function useTodayViewMode() {
  const viewMode = ref<TodayViewMode>('bento')

  function init() {
    if (typeof window !== 'undefined' && window.localStorage) {
      const stored = window.localStorage.getItem(TODAY_VIEW_MODE_STORAGE_KEY)
      if (stored === 'bento' || stored === 'split') {
        viewMode.value = stored
      }
    }
  }

  function setViewMode(mode: TodayViewMode) {
    if (mode !== 'bento' && mode !== 'split') return
    viewMode.value = mode
    if (typeof window !== 'undefined' && window.localStorage) {
      window.localStorage.setItem(TODAY_VIEW_MODE_STORAGE_KEY, mode)
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
    STORAGE_KEY: TODAY_VIEW_MODE_STORAGE_KEY
  }
}
