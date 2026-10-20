import type { ComputeDevice, SynthHandlers, TtsEngine } from '~/composables/useSliceAudio'

interface WorkerMessage {
  type: 'progress' | 'chunk' | 'done' | 'error' | 'device'
  reqId: number
  stage?: 'download' | 'synth'
  progress?: number
  index?: number
  count?: number
  sampleRate?: number
  samples?: Float32Array
  message?: string
  device?: ComputeDevice
}

/** Keep the existing worker protocol and inference serialization unchanged. */
export function createWorkerEngine(): TtsEngine {
  let worker: Worker | null = null
  let reqCounter = 0
  const pending = new Map<number, { handlers: SynthHandlers; resolve: () => void; reject: (error: Error) => void }>()
  function ensureWorker(): Worker {
    if (worker) return worker
    worker = new Worker(new URL('../workers/ttsSynth.worker.ts', import.meta.url), { type: 'module' })
    worker.onerror = (event: ErrorEvent) => {
      const error = new Error(event.message || 'Worker initialization failed')
      console.error('[useSliceAudio] Local speech worker failed', event)
      for (const request of pending.values()) request.reject(error)
      pending.clear()
      worker = null
    }
    worker.onmessage = (event: MessageEvent<WorkerMessage>) => {
      const message = event.data
      const request = pending.get(message.reqId)
      if (!request) return
      try {
        if (message.type === 'progress') request.handlers.onProgress({ stage: message.stage ?? 'download', progress: message.progress, index: message.index, count: message.count })
        else if (message.type === 'chunk' && message.samples && message.sampleRate) request.handlers.onChunk(message.samples, message.sampleRate)
        else if (message.type === 'device' && message.device) request.handlers.onDevice?.(message.device)
        else if (message.type === 'done') { pending.delete(message.reqId); request.resolve() }
        else if (message.type === 'error') { pending.delete(message.reqId); request.reject(new Error(message.message ?? 'Synthesis failed')) }
      } catch (error) {
        pending.delete(message.reqId)
        request.reject(error instanceof Error ? error : new Error(String(error)))
      }
    }
    return worker
  }
  return {
    synthesize(model, sentences, handlers, offsetIndex = 0) {
      const request = Promise.withResolvers<void>()
      const currentWorker = ensureWorker()
      const reqId = ++reqCounter
      pending.set(reqId, { handlers, resolve: request.resolve, reject: request.reject })
      currentWorker.postMessage({ type: 'synth', reqId, model, sentences, offsetIndex })
      return request.promise
    },
    cancel(reqId) {
      worker?.postMessage({ type: 'cancel', reqId })
      const error = new Error('Synthesis cancelled')
      if (reqId != null) {
        const request = pending.get(reqId)
        pending.delete(reqId)
        request?.reject(error)
      } else {
        for (const request of pending.values()) request.reject(error)
        pending.clear()
      }
    },
    dispose() {
      worker?.terminate()
      worker = null
      for (const request of pending.values()) request.reject(new Error('Synthesis cancelled'))
      pending.clear()
    },
  }
}
