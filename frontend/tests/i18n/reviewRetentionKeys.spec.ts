import { describe, it, expect } from 'vitest'
import enJson from '~/i18n/locales/en.json'
import viJson from '~/i18n/locales/vi.json'

// Keys consumed by AtRiskLeechCard.vue, SourceChannelRetentionCard.vue, and
// EaseFactorDistributionCard.vue. A missing/empty key leaks the raw i18n path
// into the UI, so both locales must define every key with a non-empty string.
const RETENTION_KEYS = [
  'atrisk_title',
  'atrisk_desc',
  'atrisk_overdue',
  'atrisk_leech',
  'atrisk_total',
  'atrisk_review_btn',
  'atrisk_empty',
  'source_title',
  'source_desc',
  'source_highlight',
  'source_quiz',
  'source_drill',
  'source_ease',
  'source_empty',
  'ease_dist_title',
  'ease_dist_desc',
  'ease_struggling',
  'ease_developing',
  'ease_comfortable',
  'ease_dist_empty',
  'tab_stats'
]
// Static build-time locale bundles; type the review namespace as an unknown
// map so it can be indexed by the dynamic keys above and narrowed per value.
interface LocaleBundle { review: Record<string, unknown> }
const en: LocaleBundle = enJson
const vi: LocaleBundle = viJson

describe('review retention analytics i18n keys', () => {
  it.each([
    ['en', en],
    ['vi', vi]
  ])('%s locale defines every retention key with a non-empty string', (locale, dict) => {
    for (const key of RETENTION_KEYS) {
      const value = dict.review[key]
      expect(typeof value, `${locale}.review.${key}`).toBe('string')
      if (typeof value === 'string') {
        expect(value.trim().length, `${locale}.review.${key}`).toBeGreaterThan(0)
      }
    }
  })

  it('disambiguates retention terminology in Vietnamese locale', () => {
    expect(vi.review.atrisk_leech).toBe('Hay quên')
    expect(vi.review.ease_struggling).toBe('Chưa vững')
    expect(vi.review.tab_stats).toBe('Thống kê')
  })

  it('defines tab_stats in English locale', () => {
    expect(en.review.tab_stats).toBe('Analytics')
  })
})
