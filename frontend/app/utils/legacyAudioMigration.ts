import { CLIENT_STORAGE, browserStorage } from './clientStorage'
import { completeMigrationPhase, migrationPhaseComplete, type MigrationResult } from './legacyStorageMigration'
import { AUDIO_MAX_BYTES, AUDIO_MAX_ENTRIES, AUDIO_META_KEY, AUDIO_STORAGE_LOCK, type PartialSliceAudio } from './sliceAudioCache'

/** Legacy cache access lives here only. Each cursor step opens a short transaction, never a whole-DB read. */
export const LEGACY_AUDIO_DATABASE = 'techdaily-audio'
const STORE = 'slice-audio'
const cacheKey = /^[^:]+::.+::v2::[^:]+$/

export interface AudioMigrationStore {
  get(key: string): Promise<unknown>
  set(key: string, value: unknown): Promise<void>
  del(key: string): Promise<void>
  entries(): AsyncIterable<{ key: IDBValidKey; value: unknown }>
}
type AudioExclusive = <T>(work: () => Promise<T>) => Promise<T | undefined>

function audioSize(value: unknown, partial: boolean): number | null {
  if (!partial) return value instanceof Blob && value.size > 0 ? value.size : null
  if (!value || typeof value !== 'object') return null
  const data = value as PartialSliceAudio
  if (!Array.isArray(data.chunks) || !data.chunks.length || !Number.isSafeInteger(data.total) || data.total < data.chunks.length
    || !Number.isFinite(data.sampleRate) || data.sampleRate < 8000 || data.sampleRate > 192000) return null
  let bytes = 0
  for (const samples of data.chunks) {
    if (!(samples instanceof Float32Array) || !samples.length || !samples.every(Number.isFinite)) return null
    bytes += samples.byteLength
    if (bytes > AUDIO_MAX_BYTES) return null
  }
  return bytes
}

async function cacheBudget(store: AudioMigrationStore) {
  let bytes = 0
  let count = 0
  for await (const { key, value } of store.entries()) {
    if (typeof key !== 'string' || key === AUDIO_META_KEY) continue
    const size = audioSize(value, key.startsWith('partial::'))
    if (size !== null) { bytes += size; count++ }
  }
  return { bytes, count }
}

async function registerCompleteEntry(store: AudioMigrationStore, key: string, size: number) {
  const metadata = await store.get(AUDIO_META_KEY)
  const valid: Array<{ key: string; size: number; lastAccess: number }> = []
  for (const entry of Array.isArray(metadata) ? metadata.slice(-AUDIO_MAX_ENTRIES) : []) {
    if (!entry || typeof entry.key !== 'string' || entry.key === key || !Number.isFinite(entry.lastAccess)) continue
    const value = await store.get(entry.key)
    if (value instanceof Blob && value.size > 0) valid.push({ key: entry.key, size: value.size, lastAccess: entry.lastAccess })
  }
  const previous = Array.isArray(metadata) ? metadata.find(entry => entry?.key === key) : null
  valid.push({ key, size, lastAccess: Number.isFinite(previous?.lastAccess) ? previous.lastAccess : Date.now() })
  await store.set(AUDIO_META_KEY, valid)
}

/** Canonical writers share the per-record lock; a blocked transfer leaves usable canonical playback alone. */
export async function transferLegacyAudio(source: AudioMigrationStore, destination: AudioMigrationStore, exclusive: AudioExclusive): Promise<MigrationResult> {
  for await (const { key, value } of source.entries()) {
    if (typeof key !== 'string') return 'deferred'
    if (key === AUDIO_META_KEY) continue
    const transferred = await exclusive(async () => {
      const current = await destination.get(key)
      if (current instanceof Blob && !key.startsWith('partial::') && cacheKey.test(key)) {
        await registerCompleteEntry(destination, key, current.size)
      }
      if (current === undefined) {
        const partial = key.startsWith('partial::')
        const baseKey = partial ? key.slice('partial::'.length) : key
        const size = cacheKey.test(baseKey) ? audioSize(value, partial) : null
        const budget = await cacheBudget(destination)
        if (size !== null && size + budget.bytes <= AUDIO_MAX_BYTES && budget.count < AUDIO_MAX_ENTRIES) {
          await destination.set(key, value)
          if (audioSize(await destination.get(key), partial) !== size) throw new Error('Audio migration write unavailable')
          if (!partial) {
            await registerCompleteEntry(destination, key, size)
          }
        }
      }
      // Invalid/obsolete or over-budget cache entries are disposable under the existing cache policy.
      await source.del(key)
      return true
    })
    if (!transferred) return 'deferred'
    await new Promise(resolve => setTimeout(resolve, 0))
  }
  await source.del(AUDIO_META_KEY)
  return 'complete'
}

function idbStore(database: IDBDatabase): AudioMigrationStore {
  const request = <T>(mode: IDBTransactionMode, run: (store: IDBObjectStore) => IDBRequest<T>): Promise<T> => new Promise((resolve, reject) => {
    const transaction = database.transaction(STORE, mode)
    const pending = run(transaction.objectStore(STORE))
    let value: T
    pending.onsuccess = () => { value = pending.result }
    transaction.oncomplete = () => resolve(value)
    transaction.onerror = transaction.onabort = () => reject(new Error('Audio storage transaction unavailable'))
  })
  return {
    get: key => request('readonly', store => store.get(key)),
    set: async (key, value) => { await request('readwrite', store => store.put(value, key)) },
    del: async key => { await request('readwrite', store => store.delete(key)) },
    async *entries() {
      let previous: IDBValidKey | undefined
      while (true) {
        const next = await new Promise<{ key: IDBValidKey; value: unknown } | null>((resolve, reject) => {
          const transaction = database.transaction(STORE, 'readonly')
          const cursor = transaction.objectStore(STORE).openCursor(previous === undefined ? undefined : IDBKeyRange.lowerBound(previous, true))
          cursor.onsuccess = () => resolve(cursor.result ? { key: cursor.result.key, value: cursor.result.value } : null)
          transaction.onerror = transaction.onabort = () => reject(new Error('Audio cursor unavailable'))
        })
        if (!next) return
        previous = next.key
        yield next
      }
    },
  }
}

/** Aborting an absent legacy DB probe prevents an empty retired database from being created. */
export function openAudioDatabase(factory: IDBFactory, name: string, create: boolean): Promise<IDBDatabase | null> {
  return new Promise((resolve, reject) => {
    let absent = false
    let expired = false
    const timer = setTimeout(() => { expired = true; reject(new Error('Audio database unavailable')) }, 1500)
    const request = factory.open(name)
    request.onupgradeneeded = () => {
      if (!create) { absent = true; request.transaction!.abort(); return }
      if (!request.result.objectStoreNames.contains(STORE)) request.result.createObjectStore(STORE)
    }
    request.onsuccess = () => {
      clearTimeout(timer)
      if (expired) { request.result.close(); return }
      request.result.onversionchange = () => request.result.close()
      resolve(request.result)
    }
    request.onerror = event => {
      event.preventDefault()
      clearTimeout(timer)
      if (absent) resolve(null)
      else reject(new Error('Audio database unavailable'))
    }
    request.onblocked = () => { clearTimeout(timer); expired = true; reject(new Error('Audio database blocked')) }
  })
}

export function deleteLegacyAudioDatabase(factory: IDBFactory): Promise<boolean> {
  return new Promise(resolve => {
    const timer = setTimeout(() => resolve(false), 1500)
    const pending = factory.deleteDatabase(LEGACY_AUDIO_DATABASE)
    pending.onsuccess = () => { clearTimeout(timer); resolve(true) }
    pending.onerror = pending.onblocked = () => { clearTimeout(timer); resolve(false) }
  })
}

let pendingMigration: Promise<MigrationResult> | null = null

/** Incomplete cleanup resumes on a later startup. Never reverse-copy on rollback or clear unrelated stores. */
export function migrateLegacyAudio(): Promise<MigrationResult> {
  if (pendingMigration) return pendingMigration
  pendingMigration = migrateBrowserAudio().finally(() => { pendingMigration = null })
  return pendingMigration
}

async function migrateBrowserAudio(): Promise<MigrationResult> {
  const local = browserStorage('localStorage')
  if (migrationPhaseComplete(local, 'audio')) return 'complete'
  if (!local || typeof indexedDB === 'undefined' || typeof navigator === 'undefined' || !navigator.locks?.request) return 'deferred'
  let source: IDBDatabase | null = null
  let destination: IDBDatabase | null = null
  try {
    source = await openAudioDatabase(indexedDB, LEGACY_AUDIO_DATABASE, false)
    if (source) {
      if (!source.objectStoreNames.contains(STORE)) return 'deferred'
      destination = await openAudioDatabase(indexedDB, CLIENT_STORAGE.audioDatabase, true)
      if (!destination) return 'deferred'
      const result = await transferLegacyAudio(idbStore(source), idbStore(destination), work => navigator.locks.request(AUDIO_STORAGE_LOCK, { ifAvailable: true }, lock => lock ? work() : undefined))
      if (result !== 'complete' || source.objectStoreNames.length !== 1) return 'deferred'
      source.close()
      if (!await deleteLegacyAudioDatabase(indexedDB)) return 'deferred'
    }
    completeMigrationPhase(local, 'audio')
    return 'complete'
  } catch { return 'deferred' }
  finally { source?.close(); destination?.close() }
}
