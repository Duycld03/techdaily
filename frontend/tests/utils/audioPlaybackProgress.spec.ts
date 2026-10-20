import { describe, expect, it, vi } from 'vitest'
import { CLIENT_STORAGE, type StorageAdapter } from '~/utils/clientStorage'
import {
  createAudioPlaybackProgress,
  type AudioPlaybackCheckpoint,
  type AudioPlaybackCursor,
  type AudioPlaybackIdentity,
} from '~/utils/audioPlaybackProgress'

function storage(): StorageAdapter {
  const data = new Map<string, string>()
  return {
    get length() { return data.size },
    key: index => [...data.keys()][index] ?? null,
    getItem: key => data.get(key) ?? null,
    setItem: (key, value) => { data.set(key, value) },
    removeItem: key => { data.delete(key) },
  }
}

const identity: AudioPlaybackIdentity = {
  accountId: 'account-a', chunkId: 'chunk-1', engine: 'cloud', voiceId: 'en-US-Neural2-F', contentHash: 'hash-1',
}
const fileCursor: AudioPlaybackCursor = { kind: 'file', seconds: 45 }
const checkpoint = (overrides: Partial<AudioPlaybackCheckpoint> = {}): AudioPlaybackCheckpoint => ({
  version: 1, identity, cursor: fileCursor, completed: false, updatedAt: 100, ...overrides,
})
function seed(target: StorageAdapter, records: unknown[], version = 1) {
  target.setItem(CLIENT_STORAGE.audioProgress, JSON.stringify({ version, checkpoints: records }))
}

describe('Account-scoped audio listening checkpoints', () => {
  it('stores only versioned listening data in its dedicated collection', () => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    expect(progress.read(identity)).toEqual({ checkpoint: null, available: true })
    expect(progress.save(identity, fileCursor, false, 123)).toBe(true)
    expect(progress.read(identity)).toEqual({ checkpoint: checkpoint({ updatedAt: 123 }), available: true })
    expect(JSON.parse(target.getItem(CLIENT_STORAGE.audioProgress)!)).toEqual({
      version: 1, checkpoints: [checkpoint({ updatedAt: 123 })],
    })
    expect(target.length).toBe(1)
  })

  it.each(['accountId', 'chunkId', 'engine', 'voiceId', 'contentHash'] as const)('requires an exact %s match without overwriting another identity', field => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    progress.save(identity, fileCursor, false, 100)
    const other = { ...identity, [field]: field === 'engine' ? 'device' : 'different' } as AudioPlaybackIdentity
    expect(progress.read(other)).toEqual({ checkpoint: null, available: true })
    expect(progress.save(other, { kind: 'file', seconds: 9 }, false, 200)).toBe(true)
    expect(progress.read(identity).checkpoint?.cursor).toEqual(fileCursor)
    expect(progress.read(other).checkpoint?.cursor).toEqual({ kind: 'file', seconds: 9 })
  })

  it.each([
    [{ ...identity, engine: 'system' }, { kind: 'system', sentenceIndex: 2 }],
    [{ ...identity, engine: 'device' }, { kind: 'local', sentenceIndex: 1, sentenceSeconds: 2.5, globalSeconds: 7.5 }],
    [{ ...identity, engine: 'device' }, { kind: 'file', seconds: 7.5 }],
    [identity, { kind: 'file', seconds: 0 }],
  ] as [AudioPlaybackIdentity, AudioPlaybackCursor][])('round-trips the compatible cursor for %j', (current, cursor) => {
    const progress = createAudioPlaybackProgress(storage())
    expect(progress.save(current, cursor, false, 0)).toBe(true)
    expect(progress.read(current).checkpoint).toEqual(checkpoint({ identity: current, cursor, updatedAt: 0 }))
  })

  it('preserves completed as a replay marker rather than hiding or advancing it', () => {
    const progress = createAudioPlaybackProgress(storage())
    expect(progress.save(identity, fileCursor, true, 10)).toBe(true)
    expect(progress.read(identity).checkpoint).toEqual(checkpoint({ completed: true, updatedAt: 10 }))
    expect(progress.save(identity, { kind: 'file', seconds: 0 }, false, 20)).toBe(true)
    expect(progress.read(identity).checkpoint?.completed).toBe(false)
  })

  it('restores the latest intentional backward seek, not the furthest position or maximum timestamp', () => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    expect(progress.save(identity, fileCursor, false, 200)).toBe(true)
    expect(progress.save(identity, { kind: 'file', seconds: 5 }, false, 100)).toBe(true)
    expect(createAudioPlaybackProgress(target).read(identity).checkpoint).toEqual(checkpoint({
      cursor: { kind: 'file', seconds: 5 }, updatedAt: 100,
    }))
  })

  it('reads fresh collection data before each save and each restore', () => {
    const target = storage()
    const first = createAudioPlaybackProgress(target)
    const second = createAudioPlaybackProgress(target)
    const accountB = { ...identity, accountId: 'account-b' }
    expect(first.save(identity, fileCursor, false, 1)).toBe(true)
    expect(second.save(accountB, { kind: 'file', seconds: 9 }, false, 2)).toBe(true)
    expect(first.read(accountB).checkpoint?.cursor).toEqual({ kind: 'file', seconds: 9 })
    expect(first.save(identity, { kind: 'file', seconds: 3 }, false, 3)).toBe(true)
    expect(second.read(accountB).checkpoint?.cursor).toEqual({ kind: 'file', seconds: 9 })
    target.removeItem(CLIENT_STORAGE.audioProgress)
    expect(first.read(identity)).toEqual({ checkpoint: null, available: true })
  })

  it('does not retain caller-owned identity/cursor objects or allow read results to mutate storage', () => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    const current = Object.freeze({ ...identity })
    const cursor = Object.freeze({ kind: 'file' as const, seconds: 4 })
    expect(progress.save(current, cursor, false, 10)).toBe(true)
    const restored = progress.read(current).checkpoint!
    restored.identity.accountId = 'other-account'
    restored.cursor = { kind: 'file', seconds: 99 }
    expect(progress.read(current).checkpoint).toEqual(checkpoint({ cursor, updatedAt: 10 }))
  })

  it('supports Unicode and delimiter-like identity text without conflating distinct identities', () => {
    const progress = createAudioPlaybackProgress(storage())
    const first = { ...identity, accountId: 'a|b', chunkId: 'c', voiceId: 'Tiếng Việt 🔈' }
    const second = { ...first, accountId: 'a', chunkId: 'b|c' }
    expect(progress.save(first, fileCursor, false, 1)).toBe(true)
    expect(progress.save(second, { kind: 'file', seconds: 2 }, false, 2)).toBe(true)
    expect(progress.read(first).checkpoint?.cursor).toEqual(fileCursor)
    expect(progress.read(second).checkpoint?.cursor).toEqual({ kind: 'file', seconds: 2 })
  })
})

describe('Checkpoint validation and safe failure', () => {
  it.each([
    null, {}, { ...identity, accountId: '' }, { ...identity, accountId: '   ' },
    { ...identity, chunkId: '' }, { ...identity, voiceId: '' }, { ...identity, contentHash: '' },
    { ...identity, engine: 'auto' }, { ...identity, accountId: 12 },
  ])('does not persist invalid or anonymous identity %j', invalid => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    expect(progress.save(invalid as AudioPlaybackIdentity, fileCursor)).toBe(false)
    expect(progress.read(invalid as AudioPlaybackIdentity).checkpoint).toBeNull()
    expect(target.getItem(CLIENT_STORAGE.audioProgress)).toBeNull()
  })

  it.each([
    null, {}, { kind: 'file', seconds: -1 }, { kind: 'file', seconds: NaN },
    { kind: 'file', seconds: Infinity }, { kind: 'file', seconds: '5' },
    { kind: 'system', sentenceIndex: 1 },
    { kind: 'local', sentenceIndex: 1, sentenceSeconds: 2, globalSeconds: 4 },
  ])('rejects invalid or incompatible Cloud cursor %j without changing a good checkpoint', invalid => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    progress.save(identity, fileCursor, false, 1)
    expect(progress.save(identity, invalid as AudioPlaybackCursor, false, 2)).toBe(false)
    expect(progress.read(identity).checkpoint?.cursor).toEqual(fileCursor)
  })

  it.each([
    { kind: 'system', sentenceIndex: -1 }, { kind: 'system', sentenceIndex: 0.5 },
    { kind: 'system', sentenceIndex: Number.MAX_SAFE_INTEGER + 1 }, { kind: 'system', sentenceIndex: NaN },
    { kind: 'file', seconds: 2 },
  ])('rejects invalid System cursor %j', cursor => {
    const progress = createAudioPlaybackProgress(storage())
    expect(progress.save({ ...identity, engine: 'system' }, cursor as AudioPlaybackCursor)).toBe(false)
  })

  it.each([
    { kind: 'local', sentenceIndex: 0.5, sentenceSeconds: 0, globalSeconds: 0 },
    { kind: 'local', sentenceIndex: 1, sentenceSeconds: -1, globalSeconds: 3 },
    { kind: 'local', sentenceIndex: 1, sentenceSeconds: 2, globalSeconds: Infinity },
    { kind: 'local', sentenceIndex: 1, sentenceSeconds: NaN, globalSeconds: 3 },
    { kind: 'system', sentenceIndex: 1 },
  ])('rejects invalid Local cursor %j', cursor => {
    const progress = createAudioPlaybackProgress(storage())
    expect(progress.save({ ...identity, engine: 'device' }, cursor as AudioPlaybackCursor)).toBe(false)
  })

  it.each([-1, NaN, Infinity])('rejects invalid checkpoint update time %s', now => {
    const target = storage()
    expect(createAudioPlaybackProgress(target).save(identity, fileCursor, false, now)).toBe(false)
    expect(target.getItem(CLIENT_STORAGE.audioProgress)).toBeNull()
  })

  it.each([
    '{broken', 'null', '[]', '{}', '{"version":2,"checkpoints":[]}',
    '{"version":1,"checkpoints":{}}',
  ])('reports corrupt or unsupported collection unavailable without clearing it: %s', raw => {
    const target = storage()
    target.setItem(CLIENT_STORAGE.audioProgress, raw)
    const progress = createAudioPlaybackProgress(target)
    expect(progress.read(identity)).toEqual({ checkpoint: null, available: false })
    expect(progress.save(identity, fileCursor)).toBe(false)
    expect(target.getItem(CLIENT_STORAGE.audioProgress)).toBe(raw)
  })

  it.each([
    { version: 2 }, { identity: { ...identity, accountId: '' } },
    { cursor: { kind: 'file', seconds: -1 } }, { cursor: { kind: 'system', sentenceIndex: 2 } },
    { completed: 'false' }, { updatedAt: -1 }, { updatedAt: null },
  ])('never restores a malformed checkpoint %j', invalid => {
    const target = storage()
    seed(target, [{ ...checkpoint(), ...invalid }])
    expect(createAudioPlaybackProgress(target).read(identity).checkpoint).toBeNull()
  })

  it('reports explicit unavailable storage without throwing', () => {
    const progress = createAudioPlaybackProgress(null)
    expect(progress.read(identity)).toEqual({ checkpoint: null, available: false })
    expect(progress.save(identity, fileCursor)).toBe(false)
  })

  it('guards denied reads without attempting a destructive recovery write', () => {
    const target = storage()
    vi.spyOn(target, 'getItem').mockImplementation(() => { throw new Error('denied') })
    const write = vi.spyOn(target, 'setItem')
    const progress = createAudioPlaybackProgress(target)
    expect(progress.read(identity)).toEqual({ checkpoint: null, available: false })
    expect(progress.save(identity, fileCursor)).toBe(false)
    expect(write).not.toHaveBeenCalled()
  })

  it.each(['throw', 'ignored'] as const)('reports %s writes unsuccessful and preserves the last successful checkpoint', failure => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    progress.save(identity, fileCursor, false, 1)
    vi.spyOn(target, 'setItem').mockImplementation(() => {
      if (failure === 'throw') throw new Error('quota')
    })
    expect(progress.save(identity, { kind: 'file', seconds: 2 }, false, 2)).toBe(false)
    expect(createAudioPlaybackProgress(target).read(identity).checkpoint?.cursor).toEqual(fileCursor)
  })
})

describe('Bounded checkpoint retention', () => {
  it('evicts the least recently updated identity at 100 records and preserves unrelated state', () => {
    const target = storage()
    target.setItem('deeppace_bookmark_book', '7')
    target.setItem('deeppace_reader_audio_speed', '1.5')
    const progress = createAudioPlaybackProgress(target)
    for (let index = 0; index < 100; index++) {
      expect(progress.save({ ...identity, chunkId: `chunk-${index}` }, fileCursor, false, index)).toBe(true)
    }
    progress.save({ ...identity, chunkId: 'chunk-0' }, { kind: 'file', seconds: 1 }, false, 200)
    progress.save({ ...identity, chunkId: 'chunk-100' }, fileCursor, false, 201)
    expect(progress.read({ ...identity, chunkId: 'chunk-1' }).checkpoint).toBeNull()
    expect(progress.read({ ...identity, chunkId: 'chunk-0' }).checkpoint?.cursor).toEqual({ kind: 'file', seconds: 1 })
    expect(progress.read({ ...identity, chunkId: 'chunk-100' }).checkpoint).not.toBeNull()
    expect(JSON.parse(target.getItem(CLIENT_STORAGE.audioProgress)!).checkpoints).toHaveLength(100)
    expect(target.getItem('deeppace_bookmark_book')).toBe('7')
    expect(target.getItem('deeppace_reader_audio_speed')).toBe('1.5')
  })

  it('evicts old records to remain within 65536 UTF-8 bytes rather than JavaScript character count', () => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    const largeVoice = '語'.repeat(1000)
    for (let index = 0; index < 40; index++) {
      expect(progress.save({ ...identity, chunkId: `large-${index}`, voiceId: largeVoice }, fileCursor, false, index)).toBe(true)
    }
    const raw = target.getItem(CLIENT_STORAGE.audioProgress)!
    expect(new TextEncoder().encode(raw).byteLength).toBeLessThanOrEqual(65536)
    expect(progress.read({ ...identity, chunkId: 'large-0', voiceId: largeVoice }).checkpoint).toBeNull()
    expect(progress.read({ ...identity, chunkId: 'large-39', voiceId: largeVoice }).checkpoint).not.toBeNull()
  })

  it('rejects one oversized checkpoint without evicting existing valid progress', () => {
    const target = storage()
    const progress = createAudioPlaybackProgress(target)
    progress.save(identity, fileCursor, false, 1)
    const before = target.getItem(CLIENT_STORAGE.audioProgress)
    expect(progress.save({ ...identity, voiceId: '語'.repeat(23000) }, fileCursor, false, 2)).toBe(false)
    expect(target.getItem(CLIENT_STORAGE.audioProgress)).toBe(before)
  })

  it('rejects oversized and over-retention stored collections without restoring them', () => {
    const target = storage()
    seed(target, Array.from({ length: 101 }, (_, index) => checkpoint({ identity: { ...identity, chunkId: `chunk-${index}` } })))
    expect(createAudioPlaybackProgress(target).read(identity)).toEqual({ checkpoint: null, available: false })
    seed(target, [checkpoint({ identity: { ...identity, voiceId: '語'.repeat(23000) } })])
    const raw = target.getItem(CLIENT_STORAGE.audioProgress)!
    expect(new TextEncoder().encode(raw).byteLength).toBeGreaterThan(65536)
    expect(createAudioPlaybackProgress(target).read(identity)).toEqual({ checkpoint: null, available: false })
    expect(target.getItem(CLIENT_STORAGE.audioProgress)).toBe(raw)
  })
})
