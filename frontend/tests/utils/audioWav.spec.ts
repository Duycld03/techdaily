import { describe, it, expect } from 'vitest'
import { concatFloat32, encodeWav } from '~/utils/audioWav'

function readAscii(view: DataView, offset: number, length: number): string {
  let s = ''
  for (let i = 0; i < length; i++) s += String.fromCharCode(view.getUint8(offset + i))
  return s
}

describe('audioWav', () => {
  describe('concatFloat32', () => {
    it('concatenates buffers in order into a single buffer', () => {
      const out = concatFloat32([new Float32Array([1, 2]), new Float32Array([3]), new Float32Array([4, 5])])
      expect(Array.from(out)).toEqual([1, 2, 3, 4, 5])
    })

    it('returns an empty buffer for no chunks', () => {
      expect(concatFloat32([]).length).toBe(0)
    })
  })

  describe('encodeWav', () => {
    it('emits an audio/wav blob sized 44-byte header plus 16-bit samples', async () => {
      const samples = new Float32Array([0, 0.5, -0.5, 1])
      const blob = encodeWav(samples, 16000)
      expect(blob.type).toBe('audio/wav')
      expect(blob.size).toBe(44 + samples.length * 2)

      const view = new DataView(await blob.arrayBuffer())
      expect(readAscii(view, 0, 4)).toBe('RIFF')
      expect(readAscii(view, 8, 4)).toBe('WAVE')
      expect(readAscii(view, 36, 4)).toBe('data')
      expect(view.getUint16(20, true)).toBe(1) // PCM
      expect(view.getUint16(22, true)).toBe(1) // mono
      expect(view.getUint32(24, true)).toBe(16000) // sample rate
      expect(view.getUint16(34, true)).toBe(16) // bits per sample
      expect(view.getUint32(40, true)).toBe(samples.length * 2) // data size
    })

    it('clamps out-of-range samples to the 16-bit PCM limits', async () => {
      const blob = encodeWav(new Float32Array([2, -2]), 8000)
      const view = new DataView(await blob.arrayBuffer())
      expect(view.getInt16(44, true)).toBe(0x7fff)
      expect(view.getInt16(46, true)).toBe(-0x8000)
    })
  })

})
