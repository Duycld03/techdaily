import { describe, it, expect, beforeEach } from 'vitest'
import {
  useReaderTypography,
  DEFAULT_TYPOGRAPHY,
  fontSizes,
  fontSizePxMap,
  fontScalePercentages,
  lineHeightMap,
  TYPOGRAPHY_STORAGE_KEY,
  resetTypography,
  initTypography
} from '~/composables/useReaderTypography'

describe('useReaderTypography composable', () => {
  beforeEach(() => {
    localStorage.clear()
    resetTypography()
  })

  it('initializes with default typography values', () => {
    const {
      typography,
      fontSizePx,
      lineHeightValue,
      fontFamilyClass,
      readingWidthClass,
      canDecreaseFontSize,
      canIncreaseFontSize
    } = useReaderTypography()

    expect(typography.value.fontSize).toBe(DEFAULT_TYPOGRAPHY.fontSize)
    expect(typography.value.fontFamily).toBe(DEFAULT_TYPOGRAPHY.fontFamily)
    expect(typography.value.lineSpacing).toBe(DEFAULT_TYPOGRAPHY.lineSpacing)
    expect(typography.value.readingWidth).toBe(DEFAULT_TYPOGRAPHY.readingWidth)

    expect(fontSizePx.value).toBe(fontSizePxMap.base)
    expect(lineHeightValue.value).toBe(lineHeightMap.relaxed)
    expect(fontFamilyClass.value).toBe('font-sans')
    expect(readingWidthClass.value).toBe('max-w-3xl')

    expect(canDecreaseFontSize.value).toBe(true) // 'base' is index 1 > 0
    expect(canIncreaseFontSize.value).toBe(true) // 'base' is index 1 < 4
  })

  it('steps font size within bounds', () => {
    const {
      typography,
      fontSizePx,
      canDecreaseFontSize,
      canIncreaseFontSize,
      decreaseFontSize,
      increaseFontSize
    } = useReaderTypography()

    // Initially at 'base' (16px)
    expect(typography.value.fontSize).toBe('base')

    // Step down to 'sm' (14px)
    decreaseFontSize()
    expect(typography.value.fontSize).toBe('sm')
    expect(fontSizePx.value).toBe('14px')
    expect(canDecreaseFontSize.value).toBe(false)
    expect(canIncreaseFontSize.value).toBe(true)

    // Attempting to step below 'sm' is a no-op
    decreaseFontSize()
    expect(typography.value.fontSize).toBe('sm')

    // Step up to 'base' -> 'lg' -> 'xl' -> '2xl'
    increaseFontSize()
    expect(typography.value.fontSize).toBe('base')
    increaseFontSize()
    expect(typography.value.fontSize).toBe('lg')
    increaseFontSize()
    expect(typography.value.fontSize).toBe('xl')
    increaseFontSize()
    expect(typography.value.fontSize).toBe('2xl')
    expect(fontSizePx.value).toBe('22px')
    expect(canIncreaseFontSize.value).toBe(false)

    // Attempting to step above '2xl' is a no-op
    increaseFontSize()
    expect(typography.value.fontSize).toBe('2xl')
  })

  it('updates computed font family and reading width classes correctly', () => {
    const { typography, fontFamilyClass, readingWidthClass } = useReaderTypography()

    typography.value.fontFamily = 'serif'
    expect(fontFamilyClass.value).toBe('font-serif')

    typography.value.fontFamily = 'mono'
    expect(fontFamilyClass.value).toBe('font-mono')

    typography.value.fontFamily = 'sans'
    expect(fontFamilyClass.value).toBe('font-sans')

    typography.value.readingWidth = 'wide'
    expect(readingWidthClass.value).toBe('max-w-4xl')

    typography.value.readingWidth = 'full'
    expect(readingWidthClass.value).toBe('max-w-full')

    typography.value.readingWidth = 'standard'
    expect(readingWidthClass.value).toBe('max-w-3xl')
  })

  it('persists changes to localStorage and hydrates saved preferences', () => {
    const customConfig = {
      fontSize: 'xl',
      fontFamily: 'serif',
      lineSpacing: 'loose',
      readingWidth: 'wide'
    }
    localStorage.setItem(TYPOGRAPHY_STORAGE_KEY, JSON.stringify(customConfig))

    initTypography(true)
    const { typography, fontSizePx, lineHeightValue, fontFamilyClass, readingWidthClass } = useReaderTypography()

    expect(typography.value.fontSize).toBe('xl')
    expect(typography.value.fontFamily).toBe('serif')
    expect(typography.value.lineSpacing).toBe('loose')
    expect(typography.value.readingWidth).toBe('wide')

    expect(fontSizePx.value).toBe('20px')
    expect(lineHeightValue.value).toBe('2.05')
    expect(fontFamilyClass.value).toBe('font-serif')
    expect(readingWidthClass.value).toBe('max-w-4xl')
  })
})
