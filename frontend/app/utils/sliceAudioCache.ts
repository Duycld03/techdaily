// On-device audio cache for synthesized slice narration.
//
// The complete slice WAV is stored keyed by (chunkId, voice, contentHash) so
// playback is seekable and replays never re-synthesize. A least-recently-used
// cap bounds device storage. The store is abstracted behind `KvBackend` so the
// eviction/invalidation logic is unit-testable with an in-memory backend, while
// production uses IndexedDB via `idb-keyval`.

import { createStore, del as idbDel, get as idbGet, set as idbSet } from 'idb-keyval'
import { CLIENT_STORAGE } from './clientStorage'

export interface KvBackend {
  get<T = unknown>(key: string): Promise<T | undefined>
  set(key: string, value: unknown): Promise<void>
  del(key: string): Promise<void>
  exclusive?<T>(work: () => Promise<T>): Promise<T>
}

export interface SliceAudioCacheOptions {
  /** Evict LRU entries once total cached bytes exceed this. */
  maxBytes?: number
  /** Evict LRU entries once the entry count exceeds this. */
  maxEntries?: number
}

export interface PartialSliceAudio {
  chunks: Float32Array[]
  sampleRate: number
  total: number
}

export interface SliceAudioCache {
  get: (key: string) => Promise<Blob | undefined>
  set: (key: string, blob: Blob) => Promise<void>
  getPartial?: (key: string) => Promise<PartialSliceAudio | undefined>
  savePartial?: (key: string, data: PartialSliceAudio) => Promise<void>
  deletePartial?: (key: string) => Promise<void>
}

interface LruEntry {
  key: string
  size: number
  lastAccess: number
}

export const AUDIO_META_KEY = '__slice_audio_lru__'
export const AUDIO_MAX_BYTES = 300 * 1024 * 1024
export const AUDIO_MAX_ENTRIES = 60
export const AUDIO_STORAGE_LOCK = 'deeppace_audio_storage'
const META_KEY = AUDIO_META_KEY
const DEFAULT_MAX_BYTES = AUDIO_MAX_BYTES
const DEFAULT_MAX_ENTRIES = AUDIO_MAX_ENTRIES

/** Compose the cache key from the slice, its voice, and the content hash (v2 avoids stale partial caches). */
export function buildAudioKey(chunkId: string, voice: string, contentHash: string): string {
  return `${chunkId}::${voice}::v2::${contentHash}`
}

export function createSliceAudioCache(backend: KvBackend, opts: SliceAudioCacheOptions = {}): SliceAudioCache {
  const maxBytes = opts.maxBytes ?? DEFAULT_MAX_BYTES
  const maxEntries = opts.maxEntries ?? DEFAULT_MAX_ENTRIES

  async function readMeta(): Promise<LruEntry[]> {
    return (await backend.get<LruEntry[]>(META_KEY)) ?? []
  }
  async function writeMeta(meta: LruEntry[]): Promise<void> {
    await backend.set(META_KEY, meta)
  }

  async function get(key: string): Promise<Blob | undefined> {
    const blob = await backend.get<Blob>(key)
    if (!blob) return undefined
    const meta = await readMeta()
    const entry = meta.find(e => e.key === key)
    if (entry) {
      entry.lastAccess = Date.now()
      await writeMeta(meta)
    }
    return blob
  }

  async function set(key: string, blob: Blob): Promise<void> {
    await backend.set(key, blob)

    // Rebuild meta with the new entry as most-recent.
    const meta = (await readMeta()).filter(e => e.key !== key)
    meta.push({ key, size: blob.size ?? 0, lastAccess: Date.now() })

    // Evict least-recently-used entries until within caps, never the entry we
    // just stored.
    meta.sort((a, b) => a.lastAccess - b.lastAccess)
    let totalBytes = meta.reduce((n, e) => n + e.size, 0)
    while (
      (meta.length > maxEntries || totalBytes > maxBytes)
      && meta.length > 1
      && meta[0]!.key !== key
    ) {
      const victim = meta.shift()!
      totalBytes -= victim.size
      await backend.del(victim.key)
    }

    await writeMeta(meta)
  }

  async function getPartial(key: string): Promise<PartialSliceAudio | undefined> {
    return await backend.get<PartialSliceAudio>(`partial::${key}`)
  }

  async function savePartial(key: string, data: PartialSliceAudio): Promise<void> {
    await backend.set(`partial::${key}`, data)
  }

  async function deletePartial(key: string): Promise<void> {
    await backend.del(`partial::${key}`)
  }

  const exclusive = <T>(work: () => Promise<T>) => backend.exclusive ? backend.exclusive(work) : work()
  return {
    get: key => exclusive(() => get(key)),
    set: (key, blob) => exclusive(() => set(key, blob)),
    getPartial: key => exclusive(() => getPartial(key)),
    savePartial: (key, data) => exclusive(() => savePartial(key, data)),
    deletePartial: key => exclusive(() => deletePartial(key)),
  }
}

/** In-memory backend for tests. */
export function createMemoryBackend(): KvBackend {
  const map = new Map<string, unknown>()
  return {
    async get<T>(key: string) {
      return map.get(key) as T | undefined
    },
    async set(key: string, value: unknown) {
      map.set(key, value)
    },
    async del(key: string) {
      map.delete(key)
    },
  }
}

/** IndexedDB-backed store for production (client only). */
export function createIdbBackend(storeName = 'slice-audio'): KvBackend {
  const store = createStore(CLIENT_STORAGE.audioDatabase, storeName)
  return {
    get: <T>(key: string) => idbGet<T>(key, store),
    set: (key: string, value: unknown) => idbSet(key, value, store),
    del: (key: string) => idbDel(key, store),
    exclusive: work => typeof navigator !== 'undefined' && navigator.locks?.request
      ? navigator.locks.request(AUDIO_STORAGE_LOCK, work) : work(),
  }
}
