import { describe, it, expect } from 'vitest'
import { ON_DEVICE_VOICES, resolveVoiceForLanguage } from '~/utils/ttsVoices'

describe('ttsVoices', () => {
  it('resolves Vietnamese content to the vie model', () => {
    expect(resolveVoiceForLanguage('vi')).toBe(ON_DEVICE_VOICES.vi)
    expect(resolveVoiceForLanguage('vi-VN')).toBe(ON_DEVICE_VOICES.vi)
    expect(ON_DEVICE_VOICES.vi.model).toBe('Xenova/mms-tts-vie')
  })

  it('resolves English content to the eng model', () => {
    expect(resolveVoiceForLanguage('en')).toBe(ON_DEVICE_VOICES.en)
    expect(resolveVoiceForLanguage('en-US')).toBe(ON_DEVICE_VOICES.en)
    expect(ON_DEVICE_VOICES.en.model).toBe('Xenova/mms-tts-eng')
  })

  it('is case-insensitive and tolerant of surrounding whitespace', () => {
    expect(resolveVoiceForLanguage('  VI  ')).toBe(ON_DEVICE_VOICES.vi)
    expect(resolveVoiceForLanguage('EN')).toBe(ON_DEVICE_VOICES.en)
  })

  it('falls back to the English voice for unknown, empty, or nullish languages', () => {
    expect(resolveVoiceForLanguage('fr')).toBe(ON_DEVICE_VOICES.en)
    expect(resolveVoiceForLanguage('')).toBe(ON_DEVICE_VOICES.en)
    expect(resolveVoiceForLanguage(null)).toBe(ON_DEVICE_VOICES.en)
    expect(resolveVoiceForLanguage(undefined)).toBe(ON_DEVICE_VOICES.en)
  })
})
