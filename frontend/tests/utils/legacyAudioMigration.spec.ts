import { describe, it, expect, vi } from 'vitest'
import { transferLegacyAudio, type AudioMigrationStore } from '~/utils/legacyAudioMigration'
import { AUDIO_MAX_BYTES, AUDIO_MAX_ENTRIES, AUDIO_META_KEY } from '~/utils/sliceAudioCache'

function store(): AudioMigrationStore {
  const values = new Map<string, unknown>()
  return {
    get: async key => values.get(key), set: async (key, value) => { values.set(key, value) }, del: async key => { values.delete(key) },
    async *entries() { for (const [key, value] of [...values]) yield { key, value } },
  }
}
const exclusive = async <T>(work: () => Promise<T>) => work()
const key = 'chunk-1::voice::v2::hash'

describe('Bounded resumable legacy narration transfer', () => {
  it('transfers complete audio and compatible partials without changing identity', async () => {
    const source = store(); const destination = store()
    const blob = new Blob(['audio'], { type: 'audio/wav' })
    const partial = { chunks: [new Float32Array([0.1, 0.2])], total: 3, sampleRate: 22050 }
    await source.set(key, blob)
    await source.set(`partial::${key}`, partial)
    expect(await transferLegacyAudio(source, destination, exclusive)).toBe('complete')
    expect(await destination.get(key)).toBe(blob)
    expect(await destination.get(`partial::${key}`)).toBe(partial)
    expect(await destination.get(AUDIO_META_KEY)).toEqual([expect.objectContaining({ key, size: blob.size })])
    expect(await source.get(key)).toBeUndefined()
  })
  it('keeps a canonical entry and repairs metadata after an interrupted copy', async () => {
    const source = store(); const destination = store()
    const current = new Blob(['canonical'])
    await source.set(key, new Blob(['legacy']))
    await destination.set(key, current)
    await destination.set(AUDIO_META_KEY, { corrupt: true })
    await transferLegacyAudio(source, destination, exclusive)
    expect(await destination.get(key)).toBe(current)
    expect(await destination.get(AUDIO_META_KEY)).toEqual([expect.objectContaining({ key, size: current.size })])
  })
  it('retains a source on write failure and resumes a verified copy without replacement', async () => {
    const source = store(); const destination = store()
    const blob = new Blob(['audio'])
    await source.set(key, blob)
    const write = vi.spyOn(destination, 'set').mockRejectedValueOnce(new Error('quota'))
    await expect(transferLegacyAudio(source, destination, exclusive)).rejects.toThrow('quota')
    expect(await source.get(key)).toBe(blob)
    write.mockRestore()
    const cleanup = vi.spyOn(source, 'del').mockRejectedValueOnce(new Error('interrupted'))
    await expect(transferLegacyAudio(source, destination, exclusive)).rejects.toThrow('interrupted')
    cleanup.mockRestore()
    await transferLegacyAudio(source, destination, exclusive)
    expect(await destination.get(key)).toBe(blob)
    expect(await source.get(key)).toBeUndefined()
  })
  it('retains source data when an updated tab owns the lock', async () => {
    const source = store(); const destination = store()
    await source.set(key, new Blob(['audio']))
    expect(await transferLegacyAudio(source, destination, async () => undefined)).toBe('deferred')
    expect(await source.get(key)).toBeInstanceOf(Blob)
    expect(await destination.get(key)).toBeUndefined()
  })
  it('discards corrupt partials/obsolete cache values while retaining unrelated keys in other stores', async () => {
    const source = store(); const destination = store()
    await source.set(`partial::${key}`, { chunks: ['invalid'], total: 1, sampleRate: 22050 })
    await source.set('chunk::voice::v1::old', new Blob(['obsolete']))
    await source.set(AUDIO_META_KEY, 'invalid')
    await transferLegacyAudio(source, destination, exclusive)
    expect(await destination.get(`partial::${key}`)).toBeUndefined()
    expect(await destination.get('chunk::voice::v1::old')).toBeUndefined()
    expect(await source.get(AUDIO_META_KEY)).toBeUndefined()
  })
  it('bounds copied complete and partial data by current byte and entry limits', async () => {
    const source = store(); const destination = store()
    class OversizedBlob extends Blob { get size() { return AUDIO_MAX_BYTES + 1 } }
    const oversizedKey = 'oversized::voice::v2::hash'
    await source.set(oversizedKey, new OversizedBlob(['small backing data']))
    for (let index = 0; index <= AUDIO_MAX_ENTRIES; index++) await source.set(`chunk-${index}::voice::v2::hash`, new Blob(['x']))
    await transferLegacyAudio(source, destination, exclusive)
    expect(await destination.get(oversizedKey)).toBeUndefined()
    const metadata = await destination.get(AUDIO_META_KEY) as unknown[]
    expect(metadata).toHaveLength(AUDIO_MAX_ENTRIES)
  })
})
