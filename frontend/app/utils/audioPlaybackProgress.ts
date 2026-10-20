import { CLIENT_STORAGE, browserStorage, type StorageAdapter } from './clientStorage'

export const AUDIO_PROGRESS_INTERVAL_MS = 5000
export const AUDIO_PROGRESS_MAX_RECORDS = 100
export const AUDIO_PROGRESS_MAX_BYTES = 65_536

export interface AudioPlaybackIdentity {
  accountId: string
  chunkId: string
  engine: 'system' | 'cloud' | 'device'
  voiceId: string
  contentHash: string
}

export type AudioPlaybackCursor =
  | { kind: 'system'; sentenceIndex: number }
  | { kind: 'file'; seconds: number }
  | { kind: 'local'; sentenceIndex: number; sentenceSeconds: number; globalSeconds: number }

export interface AudioPlaybackCheckpoint {
  version: 1
  identity: AudioPlaybackIdentity
  cursor: AudioPlaybackCursor
  completed: boolean
  updatedAt: number
}

interface ProgressCollection {
  version: 1
  checkpoints: AudioPlaybackCheckpoint[]
}

function isNonnegativeNumber(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0
}

function isSentenceIndex(value: unknown): value is number {
  return isNonnegativeNumber(value) && Number.isSafeInteger(value)
}

function parseIdentity(value: unknown): AudioPlaybackIdentity | null {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null
  const { accountId, chunkId, engine, voiceId, contentHash } = value as Partial<AudioPlaybackIdentity>
  if (typeof accountId !== 'string' || !accountId.trim()
    || typeof chunkId !== 'string' || !chunkId.trim()
    || typeof voiceId !== 'string' || !voiceId.trim()
    || typeof contentHash !== 'string' || !contentHash.trim()
    || (engine !== 'system' && engine !== 'cloud' && engine !== 'device')) return null
  return { accountId, chunkId, engine, voiceId, contentHash }
}

function parseCursor(value: unknown, engine: AudioPlaybackIdentity['engine']): AudioPlaybackCursor | null {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null
  const cursor = value as AudioPlaybackCursor
  if (engine === 'system') {
    return cursor.kind === 'system' && isSentenceIndex(cursor.sentenceIndex)
      ? { kind: 'system', sentenceIndex: cursor.sentenceIndex }
      : null
  }
  if (cursor.kind === 'file' && isNonnegativeNumber(cursor.seconds)) return { kind: 'file', seconds: cursor.seconds }
  if (engine === 'device' && cursor.kind === 'local' && isSentenceIndex(cursor.sentenceIndex)
    && isNonnegativeNumber(cursor.sentenceSeconds) && isNonnegativeNumber(cursor.globalSeconds)) {
    return { kind: 'local', sentenceIndex: cursor.sentenceIndex, sentenceSeconds: cursor.sentenceSeconds, globalSeconds: cursor.globalSeconds }
  }
  return null
}

function parseCheckpoint(value: unknown): AudioPlaybackCheckpoint | null {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null
  const record = value as Partial<AudioPlaybackCheckpoint>
  if (record.version !== 1 || typeof record.completed !== 'boolean' || !isNonnegativeNumber(record.updatedAt)) return null
  const identity = parseIdentity(record.identity)
  if (!identity) return null
  const cursor = parseCursor(record.cursor, identity.engine)
  return cursor ? { version: 1, identity, cursor, completed: record.completed, updatedAt: record.updatedAt } : null
}

function sameIdentity(first: AudioPlaybackIdentity, second: AudioPlaybackIdentity): boolean {
  return first.accountId === second.accountId && first.chunkId === second.chunkId
    && first.engine === second.engine && first.voiceId === second.voiceId && first.contentHash === second.contentHash
}

/** Read before every operation; never overwrite a collection that could not be safely validated. */
function readCollection(storage: StorageAdapter | null): ProgressCollection | null {
  if (!storage) return null
  try {
    const raw = storage.getItem(CLIENT_STORAGE.audioProgress)
    if (raw === null) return { version: 1, checkpoints: [] }
    if (raw.length > AUDIO_PROGRESS_MAX_BYTES || new TextEncoder().encode(raw).byteLength > AUDIO_PROGRESS_MAX_BYTES) return null
    const value: unknown = JSON.parse(raw)
    if (!value || typeof value !== 'object' || Array.isArray(value)) return null
    const collection = value as Partial<ProgressCollection>
    if (collection.version !== 1 || !Array.isArray(collection.checkpoints)
      || collection.checkpoints.length > AUDIO_PROGRESS_MAX_RECORDS) return null
    const checkpoints = collection.checkpoints.map(parseCheckpoint)
    if (checkpoints.some(item => item === null)) return null
    return { version: 1, checkpoints: checkpoints as AudioPlaybackCheckpoint[] }
  } catch { return null }
}

/**
 * Same-browser, account-scoped version-1 listening records only: no tokens, document text,
 * audio, or model payloads. At most 100 records and 64 KiB UTF-8 per origin; oldest updates
 * are evicted first. A successful commit replaces position even after a backward seek.
 * Session admission/fencing, five-second scheduling, and source duration/sentence bounds
 * belong to the narration owner. A completed checkpoint remains a replay-from-start marker.
 */
export function createAudioPlaybackProgress(storage: StorageAdapter | null = browserStorage('localStorage')) {
  return {
    read(identity: AudioPlaybackIdentity): { checkpoint: AudioPlaybackCheckpoint | null; available: boolean } {
      if (!parseIdentity(identity)) return { checkpoint: null, available: true }
      const collection = readCollection(storage)
      if (!collection) return { checkpoint: null, available: false }
      return { checkpoint: collection.checkpoints.find(item => sameIdentity(item.identity, identity)) ?? null, available: true }
    },

    save(identity: AudioPlaybackIdentity, cursor: AudioPlaybackCursor, completed = false, now = Date.now()): boolean {
      try {
        const next = parseCheckpoint({ version: 1, identity, cursor, completed, updatedAt: now })
        if (!next || !storage) return false
        const collection = readCollection(storage)
        if (!collection) return false
        const single = JSON.stringify({ version: 1, checkpoints: [next] })
        if (new TextEncoder().encode(single).byteLength > AUDIO_PROGRESS_MAX_BYTES) return false
        // Sort a fresh array, leaving the validated snapshot and caller objects untouched.
        const previous = collection.checkpoints.filter(item => !sameIdentity(item.identity, next.identity))
          .sort((first, second) => first.updatedAt - second.updatedAt)
        let checkpoints = [...previous.slice(-(AUDIO_PROGRESS_MAX_RECORDS - 1)), next]
        let raw = JSON.stringify({ version: 1, checkpoints })
        while (new TextEncoder().encode(raw).byteLength > AUDIO_PROGRESS_MAX_BYTES) {
          checkpoints = checkpoints.slice(1)
          raw = JSON.stringify({ version: 1, checkpoints })
        }
        storage.setItem(CLIENT_STORAGE.audioProgress, raw)
        return storage.getItem(CLIENT_STORAGE.audioProgress) === raw
      } catch { return false }
    },
  }
}
