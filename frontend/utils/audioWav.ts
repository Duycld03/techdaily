// Assemble per-sentence PCM waveforms into a single seekable WAV Blob.
// The on-device model emits mono Float32 samples at a fixed sample rate; we
// concatenate the streamed sentences and encode 16-bit PCM WAV so the reader's
// <audio> element can seek freely over the complete slice.

/** Concatenate streamed per-sentence Float32 sample buffers into one buffer. */
export function concatFloat32(chunks: Float32Array[]): Float32Array {
  const total = chunks.reduce((n, c) => n + c.length, 0)
  const out = new Float32Array(total)
  let offset = 0
  for (const c of chunks) {
    out.set(c, offset)
    offset += c.length
  }
  return out
}

/** Encode mono Float32 samples as a 16-bit PCM WAV Blob (`audio/wav`). */
export function encodeWav(samples: Float32Array, sampleRate: number): Blob {
  const bytesPerSample = 2
  const numChannels = 1
  const blockAlign = numChannels * bytesPerSample
  const byteRate = sampleRate * blockAlign
  const dataSize = samples.length * bytesPerSample
  const buffer = new ArrayBuffer(44 + dataSize)
  const view = new DataView(buffer)

  const writeString = (offset: number, s: string) => {
    for (let i = 0; i < s.length; i++) view.setUint8(offset + i, s.charCodeAt(i))
  }

  writeString(0, 'RIFF')
  view.setUint32(4, 36 + dataSize, true)
  writeString(8, 'WAVE')
  writeString(12, 'fmt ')
  view.setUint32(16, 16, true) // PCM chunk size
  view.setUint16(20, 1, true) // audio format = PCM
  view.setUint16(22, numChannels, true)
  view.setUint32(24, sampleRate, true)
  view.setUint32(28, byteRate, true)
  view.setUint16(32, blockAlign, true)
  view.setUint16(34, 8 * bytesPerSample, true)
  writeString(36, 'data')
  view.setUint32(40, dataSize, true)

  let offset = 44
  for (let i = 0; i < samples.length; i++, offset += 2) {
    const s = Math.max(-1, Math.min(1, samples[i] ?? 0))
    view.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7fff, true)
  }

  return new Blob([buffer], { type: 'audio/wav' })
}

/** Playback duration (seconds) of a 16-bit mono PCM WAV blob at `sampleRate`. */
export function wavDurationSeconds(blob: Blob, sampleRate: number): number {
  const sampleBytes = Math.max(0, blob.size - 44)
  return sampleBytes / 2 / sampleRate
}
