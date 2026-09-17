import { ref, computed, watch, onMounted, getCurrentInstance } from 'vue'

export interface ReaderTypography {
  fontSize: 'sm' | 'base' | 'lg' | 'xl' | '2xl'
  fontFamily: 'sans' | 'serif' | 'mono'
  lineSpacing: 'normal' | 'relaxed' | 'loose'
  readingWidth: 'standard' | 'wide' | 'full'
}

export const TYPOGRAPHY_STORAGE_KEY = 'techdaily_reader_typography'

export const DEFAULT_TYPOGRAPHY: ReaderTypography = {
  fontSize: 'base',
  fontFamily: 'sans',
  lineSpacing: 'relaxed',
  readingWidth: 'standard'
}

export const fontSizes = ['sm', 'base', 'lg', 'xl', '2xl'] as const

export const fontSizePxMap: Record<ReaderTypography['fontSize'], string> = {
  sm: '14px',
  base: '16px',
  lg: '18px',
  xl: '20px',
  '2xl': '22px'
}

export const fontScalePercentages: Record<ReaderTypography['fontSize'], string> = {
  sm: '85%',
  base: '100%',
  lg: '115%',
  xl: '130%',
  '2xl': '145%'
}

export const lineHeightMap: Record<ReaderTypography['lineSpacing'], string> = {
  normal: '1.5',
  relaxed: '1.75',
  loose: '2.05'
}

// Module-level shared reactive state
const typography = ref<ReaderTypography>({ ...DEFAULT_TYPOGRAPHY })
let isInitialized = false
let isWatching = false

const isClient = typeof window !== 'undefined'

export function initTypography(force = false) {
  if (!isClient) return
  if (isInitialized && !force) return

  try {
    const saved = localStorage.getItem(TYPOGRAPHY_STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (parsed && typeof parsed === 'object') {
        typography.value = {
          fontSize: (fontSizes as readonly string[]).includes(parsed.fontSize)
            ? parsed.fontSize
            : DEFAULT_TYPOGRAPHY.fontSize,
          fontFamily: ['sans', 'serif', 'mono'].includes(parsed.fontFamily)
            ? parsed.fontFamily
            : DEFAULT_TYPOGRAPHY.fontFamily,
          lineSpacing: ['normal', 'relaxed', 'loose'].includes(parsed.lineSpacing)
            ? parsed.lineSpacing
            : DEFAULT_TYPOGRAPHY.lineSpacing,
          readingWidth: ['standard', 'wide', 'full'].includes(parsed.readingWidth)
            ? parsed.readingWidth
            : DEFAULT_TYPOGRAPHY.readingWidth
        }
      }
    }
  } catch {
    // Fallback gracefully to defaults
  }
  isInitialized = true
}

export function resetTypography() {
  isInitialized = false
  typography.value = { ...DEFAULT_TYPOGRAPHY }
  if (isClient) {
    try {
      localStorage.removeItem(TYPOGRAPHY_STORAGE_KEY)
    } catch {
      // ignore
    }
  }
}

function setupPersistenceWatcher() {
  if (!isClient || isWatching) return
  isWatching = true

  watch(
    typography,
    (newVal) => {
      if (!isInitialized) return
      try {
        localStorage.setItem(TYPOGRAPHY_STORAGE_KEY, JSON.stringify(newVal))
      } catch {
        // ignore quota errors
      }
    },
    { deep: true }
  )

  if (typeof window !== 'undefined') {
    window.addEventListener('storage', (event) => {
      if (event.key === TYPOGRAPHY_STORAGE_KEY && event.newValue) {
        initTypography(true)
      }
    })
  }
}

export function useReaderTypography() {
  if (isClient) {
    setupPersistenceWatcher()
    if (!isInitialized) {
      if (getCurrentInstance()) {
        onMounted(() => {
          initTypography()
        })
      } else {
        initTypography()
      }
    }
  }

  const currentFontSizeIndex = computed(() => fontSizes.indexOf(typography.value.fontSize))
  const canDecreaseFontSize = computed(() => currentFontSizeIndex.value > 0)
  const canIncreaseFontSize = computed(() => currentFontSizeIndex.value < fontSizes.length - 1)

  function decreaseFontSize() {
    if (canDecreaseFontSize.value) {
      const nextSize = fontSizes[currentFontSizeIndex.value - 1]
      if (nextSize) {
        typography.value.fontSize = nextSize
      }
    }
  }

  function increaseFontSize() {
    if (canIncreaseFontSize.value) {
      const nextSize = fontSizes[currentFontSizeIndex.value + 1]
      if (nextSize) {
        typography.value.fontSize = nextSize
      }
    }
  }

  const fontSizePx = computed(() => fontSizePxMap[typography.value.fontSize])
  const lineHeightValue = computed(() => lineHeightMap[typography.value.lineSpacing])
  const fontFamilyClass = computed(() => {
    switch (typography.value.fontFamily) {
      case 'serif':
        return 'font-serif'
      case 'mono':
        return 'font-mono'
      default:
        return 'font-sans'
    }
  })
  const readingWidthClass = computed(() => {
    switch (typography.value.readingWidth) {
      case 'wide':
        return 'max-w-4xl'
      case 'full':
        return 'max-w-full'
      default:
        return 'max-w-3xl'
    }
  })

  return {
    typography,
    fontSizes,
    fontSizePxMap,
    fontScalePercentages,
    lineHeightMap,
    currentFontSizeIndex,
    canDecreaseFontSize,
    canIncreaseFontSize,
    decreaseFontSize,
    increaseFontSize,
    fontSizePx,
    lineHeightValue,
    fontFamilyClass,
    readingWidthClass,
    initTypography,
    resetTypography
  }
}
