import { concatFloat32, encodeWav } from './audioWav'
import type { AudioPlaybackCursor } from './audioPlaybackProgress'
import type { AudioMediaSource } from './audioPlaybackMedia'
import type { PartialSliceAudio, SliceAudioCache } from './sliceAudioCache'
import type { ComputeDevice, TtsEngine } from '~/composables/useSliceAudio'

interface LocalPlaybackOptions {
  engine: () => TtsEngine | null
  cache: SliceAudioCache
  key: string
  model: string
  sentences: string[]
  checkpoint: AudioPlaybackCursor | null
  valid: () => boolean
  intendsPlay: () => boolean
  mount: (blob: Blob, source: AudioMediaSource) => void
  time: () => number
  setCursor: (read: () => AudioPlaybackCursor) => void
  setResume: (resume: (() => boolean) | null) => void
  setSeek: (seek: ((seconds: number) => boolean) | null) => void
  onProgress: (generated: number, total: number, buffered: number) => void
  onDownload: (progress: number) => void
  onDevice: (device: ComputeDevice) => void
  onReady: (restored: boolean) => void
  onBuffering: () => void
  onCompleted: () => void
  onError: (error: Error) => void
}

function usablePartial(value: PartialSliceAudio | undefined, total: number): value is PartialSliceAudio {
  return !!value && value.total === total && Number.isFinite(value.sampleRate) && value.sampleRate > 0
    && Array.isArray(value.chunks) && value.chunks.length > 0 && value.chunks.length <= total
    && value.chunks.every(chunk => chunk instanceof Float32Array && chunk.length > 0 && chunk.every(Number.isFinite))
}

/** Generation keeps its existing cache format; only actual PCM durations locate the listener. */
export async function playLocalAudio(options: LocalPlaybackOptions): Promise<void> {
  const { cache, key, sentences, valid } = options
  const complete = await cache.get(key)
  if (!valid()) return
  if (complete?.size) {
    const checkpoint = options.checkpoint
    const seconds = checkpoint?.kind === 'file' ? checkpoint.seconds : checkpoint?.kind === 'local' ? checkpoint.globalSeconds : 0
    options.setCursor(() => ({ kind: 'file', seconds: options.time() }))
    options.mount(complete, { valid, offset: seconds, onReady: (_offset, restored) => options.onReady(restored), onEnded: options.onCompleted, onError: options.onError })
    return
  }
  const partial = await cache.getPartial?.(key)
  if (!valid()) return
  let buffers = usablePartial(partial, sentences.length) ? [...partial.chunks] : []
  let sampleRate = usablePartial(partial, sentences.length) ? partial.sampleRate : 16000
  let offsets = [0]
  for (const buffer of buffers) offsets = [...offsets, offsets[offsets.length - 1]! + buffer.length / sampleRate]
  const startIndex = buffers.length
  const target = Math.min(2, sentences.length)
  let endIndex = 0
  let streaming = true
  let waiting = false
  let naturallyExhausted = false
  let mounted = false
  let restored = false
  const prefix = (index: number) => offsets[index] ?? 0
  const checkpoint = options.checkpoint
  const localRestore = checkpoint?.kind === 'local' && checkpoint.sentenceIndex < buffers.length
    && checkpoint.sentenceSeconds < prefix(checkpoint.sentenceIndex + 1) - prefix(checkpoint.sentenceIndex)
    && Math.abs(prefix(checkpoint.sentenceIndex) + checkpoint.sentenceSeconds - checkpoint.globalSeconds) < 0.05
      ? checkpoint : null

  function cursor(): AudioPlaybackCursor {
    const globalSeconds = options.time()
    let sentenceIndex = 0
    while (sentenceIndex + 1 < buffers.length && prefix(sentenceIndex + 1) <= globalSeconds) sentenceIndex++
    return { kind: 'local', sentenceIndex, sentenceSeconds: Math.max(0, globalSeconds - prefix(sentenceIndex)), globalSeconds }
  }
  options.setCursor(cursor)
  options.onProgress(buffers.length, sentences.length, target)

  function next(): void {
    if (!valid() || !options.intendsPlay()) return
    if (endIndex < buffers.length) mountSegment(endIndex, endIndex + 1)
    else {
      naturallyExhausted = endIndex === sentences.length
      waiting = true
      options.onBuffering()
    }
  }

  function mountSegment(start: number, end: number, offset = 0): void {
    if (!valid()) return
    mounted = true
    waiting = false
    naturallyExhausted = false
    endIndex = end
    const seconds = prefix(end) - prefix(start)
    options.mount(encodeWav(concatFloat32(buffers.slice(start, end)), sampleRate), {
      valid, offset, globalStart: prefix(start), duration: prefix(buffers.length), segmentDuration: seconds,
      onReady: () => options.onReady(restored), onEnded: () => { if (streaming) next() }, onError: options.onError,
    })
  }

  options.setResume(() => {
    if (!waiting) return false
    next()
    return true
  })
  options.setSeek(seconds => {
    if (!valid() || !Number.isFinite(seconds) || seconds < 0 || seconds >= prefix(buffers.length)) return false
    let index = 0
    while (index + 1 < buffers.length && prefix(index + 1) <= seconds) index++
    mountSegment(index, index + 1, seconds - prefix(index))
    return true
  })

  if (localRestore) {
    restored = true
    mountSegment(localRestore.sentenceIndex, localRestore.sentenceIndex + 1, localRestore.sentenceSeconds)
  } else if (buffers.length >= target) mountSegment(0, buffers.length)

  const engine = startIndex < sentences.length ? options.engine() : null
  if (startIndex < sentences.length && !engine) throw new Error('Local speech is unavailable in this browser')
  try {
    if (engine) await engine.synthesize(options.model, sentences.slice(startIndex), {
      onProgress: progress => {
        if (valid() && progress.stage === 'download' && progress.progress != null) options.onDownload(progress.progress)
      },
      onDevice: device => { if (valid()) options.onDevice(device) },
      onChunk: (samples, rate) => {
        if (!valid()) return
        if (!Number.isFinite(rate) || rate <= 0 || !samples.length || !samples.every(Number.isFinite)
          || buffers.length >= sentences.length || (buffers.length > 0 && rate !== sampleRate)) throw new Error('Local speech returned invalid PCM audio')
        sampleRate = rate
        buffers = [...buffers, samples]
        offsets = [...offsets, prefix(buffers.length - 1) + samples.length / sampleRate]
        options.onProgress(buffers.length, sentences.length, target)
        if (cache.savePartial) {
          void cache.savePartial(key, { chunks: buffers, sampleRate, total: sentences.length })
            .catch(error => console.warn('[useSliceAudio] Could not cache local partial audio', error))
        }
        if (!mounted && buffers.length >= target) mountSegment(0, target)
        else if (waiting && options.intendsPlay()) next()
      },
    }, startIndex)
  } catch (error) {
    if (!valid()) return
    if (error instanceof Error && error.message === 'Synthesis cancelled') return
    throw error
  }
  if (!valid()) return
  if (buffers.length !== sentences.length || !buffers.length) throw new Error('Synthesis returned incomplete audio')
  const full = encodeWav(concatFloat32(buffers), sampleRate)
  await cache.set(key, full)
  if (!valid()) return
  await cache.deletePartial?.(key)
  if (!valid()) return
  if (naturallyExhausted && options.intendsPlay()) {
    options.onCompleted()
    return
  }
  const seconds = options.time()
  streaming = false
  options.setResume(null)
  options.setSeek(null)
  options.setCursor(() => ({ kind: 'file', seconds: options.time() }))
  options.mount(full, {
    valid, offset: seconds, allowEnd: true, duration: prefix(buffers.length),
    onReady: () => options.onReady(restored), onEnded: options.onCompleted, onError: options.onError,
  })
}
