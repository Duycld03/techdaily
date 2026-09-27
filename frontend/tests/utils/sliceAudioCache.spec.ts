import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  buildAudioKey,
  createMemoryBackend,
  createSliceAudioCache
} from '~/utils/sliceAudioCache'

function blobOf(bytes: number): Blob {
  return new Blob(['x'.repeat(bytes)])
}

describe('sliceAudioCache', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('buildAudioKey', () => {
    it('composes the key from chunk, voice, and content hash with v2 prefix', () => {
      expect(buildAudioKey('chunk-1', 'mms-eng', 'abc123')).toBe('chunk-1::mms-eng::v2::abc123')
    })
  })

  it('stores and retrieves a blob by key, and misses return undefined', async () => {
    const cache = createSliceAudioCache(createMemoryBackend())
    const blob = blobOf(10)
    await cache.set('k1', blob)
    expect(await cache.get('k1')).toBe(blob)
    expect(await cache.get('missing')).toBeUndefined()
  })

  it('evicts the least-recently-used entry once the entry cap is exceeded', async () => {
    let now = 1000
    vi.spyOn(Date, 'now').mockImplementation(() => now++)
    const cache = createSliceAudioCache(createMemoryBackend(), { maxEntries: 2 })

    await cache.set('a', blobOf(1))
    await cache.set('b', blobOf(1))
    await cache.set('c', blobOf(1))

    expect(await cache.get('a')).toBeUndefined() // oldest evicted
    expect(await cache.get('b')).toBeDefined()
    expect(await cache.get('c')).toBeDefined()
  })

  it('treats a recent get as fresh so it is not the next eviction victim', async () => {
    let now = 1000
    vi.spyOn(Date, 'now').mockImplementation(() => now++)
    const cache = createSliceAudioCache(createMemoryBackend(), { maxEntries: 2 })

    await cache.set('a', blobOf(1))
    await cache.set('b', blobOf(1))
    await cache.get('a') // refresh A's recency
    await cache.set('c', blobOf(1)) // must evict B, the true LRU

    expect(await cache.get('a')).toBeDefined()
    expect(await cache.get('b')).toBeUndefined()
    expect(await cache.get('c')).toBeDefined()
  })

  it('evicts by total byte budget while keeping the just-stored entry', async () => {
    let now = 1000
    vi.spyOn(Date, 'now').mockImplementation(() => now++)
    const cache = createSliceAudioCache(createMemoryBackend(), { maxBytes: 100, maxEntries: 100 })

    await cache.set('a', blobOf(60))
    await cache.set('b', blobOf(60)) // 120 > 100 -> evict A

    expect(await cache.get('a')).toBeUndefined()
    expect(await cache.get('b')).toBeDefined()
  })

  it('saves, retrieves, and deletes partial audio chunks', async () => {
    const cache = createSliceAudioCache(createMemoryBackend())
    const partialData = {
      chunks: [new Float32Array([0.1, 0.2]), new Float32Array([0.3, 0.4])],
      sampleRate: 16000,
      total: 5,
    }
    await cache.savePartial?.('chunk-1::voice::hash1', partialData)
    const retrieved = await cache.getPartial?.('chunk-1::voice::hash1')
    expect(retrieved).toEqual(partialData)

    await cache.deletePartial?.('chunk-1::voice::hash1')
    expect(await cache.getPartial?.('chunk-1::voice::hash1')).toBeUndefined()
  })
})
