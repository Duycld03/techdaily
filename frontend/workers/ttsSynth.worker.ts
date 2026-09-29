/// <reference lib="webworker" />
// On-device text-to-speech synthesis worker. Loads an MMS-TTS model via
// Transformers.js (ONNX Runtime Web) and synthesizes one audio buffer per
// sentence, streamed back to the main thread. Runs on WebGPU when a real GPU
// adapter is available and falls back to CPU WASM otherwise (or if GPU
// inference fails), so synthesis completes on any supported browser. Kept off
// the UI thread so synthesis never freezes the reader.
import { env, pipeline } from '@huggingface/transformers'

interface SynthRequest {
  type: 'synth'
  reqId: number
  model: string
  sentences: string[]
  offsetIndex?: number
}

interface CancelRequest {
  type: 'cancel'
  reqId?: number
}

type WorkerIncomingMessage = SynthRequest | CancelRequest
type SynthFn = (text: string) => Promise<{ audio: Float32Array, sampling_rate: number }>
type Backend = 'webgpu' | 'wasm'
// Precision per environment, chosen for lowest synthesis latency.
// Desktop WASM uses fp32 (~4x faster than int8 VITS kernels on desktop CPUs
// and fully supported by multi-threaded SIMD).
// Mobile WASM uses quantized q8 (model_quantized.onnx, ~36.6 MB vs 109 MB)
// to fit mobile memory ceilings and avoid out-of-memory browser tab crashes.
// Note: fp16 is explicitly prohibited for MMS-TTS due to invalid remote ONNX graph schemas.
type Dtype = 'fp32' | 'q8'

const isMobile = typeof navigator !== 'undefined' && (
  /android|iphone|ipad|ipod|mobile/i.test(navigator.userAgent) ||
  (navigator.platform === 'MacIntel' && typeof navigator.maxTouchPoints === 'number' && navigator.maxTouchPoints > 1)
)

interface PipelineEntry {
  device: Backend
  synth: SynthFn
}

const ctx = self as unknown as DedicatedWorkerGlobalScope

// Utilize a many-core CPU on desktop: run the WASM (CPU) backend across worker
// threads when the browsing context is cross-origin isolated. On mobile devices
// (iOS Safari, mobile Chromium), spawning multi-threaded WASM workers inside a
// Web Worker exhausts process thread and memory bounds; force 1 thread on mobile.
const onnxWasm = env.backends.onnx.wasm
const isIsolated = typeof self !== 'undefined' && 'crossOriginIsolated' in self && Boolean(self.crossOriginIsolated)
if (onnxWasm) {
  if (!isMobile && isIsolated && typeof navigator !== 'undefined' && typeof navigator.hardwareConcurrency === 'number') {
    onnxWasm.numThreads = Math.min(8, Math.max(1, navigator.hardwareConcurrency))
  } else {
    onnxWasm.numThreads = 1
  }
}

// One pipeline per model, cached for the worker lifetime. The resolved backend
// is recorded so it is decided once per model rather than re-probed per run.
const pipelines = new Map<string, Promise<PipelineEntry>>()

// MMS-TTS (Meta VITS) architecture models require 64-bit integer (INT64)
// indexing on GatherND operators within the stochastic duration predictor,
// which ONNX Runtime Web's WebGPU WGSL shader kernels do not support (type 7).
// Furthermore, remote model_fp16.onnx weights contain graph validation errors.
// Always route MMS-TTS models directly to CPU WASM for 100% stability across
// all operating systems and browsers (Windows, Linux, macOS, Android, iOS).
function resolveEntry(model: string, reqId: number): Promise<PipelineEntry> {
  const dtype: Dtype = isMobile ? 'q8' : 'fp32'
  const built = pipeline('text-to-speech', model, {
    device: 'wasm',
    dtype,
    progress_callback: (info: { status?: string, file?: string, progress?: number } & Record<string, unknown>) => {
      if (info?.status === 'progress' && typeof info.file === 'string' && info.file.endsWith('.onnx')) {
        ctx.postMessage({ type: 'progress', reqId, stage: 'download', progress: info.progress ?? 0 })
      }
    },
  }) as unknown as Promise<SynthFn>
  return built.then(synth => ({ device: 'wasm', synth }))
}

function getPipeline(model: string, reqId: number): Promise<PipelineEntry> {
  let existing = pipelines.get(model)
  if (!existing) {
    existing = resolveEntry(model, reqId)
    pipelines.set(model, existing)
    // Do not cache a rejected build, so a later run can retry initialization.
    existing.catch(() => pipelines.delete(model))
  }
  return existing
}

const cancelledReqIds = new Set<number>()
let activeReqId: number | null = null
let currentJobId = 0
let inferenceQueue: Promise<unknown> = Promise.resolve()

ctx.onmessage = async (event: MessageEvent<WorkerIncomingMessage>) => {
  const msg = event.data
  if (!msg) return

  if (msg.type === 'cancel') {
    ++currentJobId
    const targetId = msg.reqId ?? activeReqId
    if (targetId != null) {
      cancelledReqIds.add(targetId)
    }
    return
  }

  if (msg.type !== 'synth') return
  const { reqId, model, sentences, offsetIndex = 0 } = msg

  // Automatically cancel any previous active request if a new synth starts
  if (activeReqId != null && activeReqId !== reqId) {
    cancelledReqIds.add(activeReqId)
  }
  activeReqId = reqId
  const jobId = ++currentJobId

  try {
    let entry = await getPipeline(model, reqId)
    if (jobId !== currentJobId || cancelledReqIds.has(reqId)) {
      cancelledReqIds.delete(reqId)
      if (activeReqId === reqId) activeReqId = null
      ctx.postMessage({ type: 'done', reqId })
      return
    }
    ctx.postMessage({ type: 'device', reqId, device: entry.device })
    for (let i = 0; i < sentences.length; i++) {
      if (jobId !== currentJobId || cancelledReqIds.has(reqId)) {
        cancelledReqIds.delete(reqId)
        if (activeReqId === reqId) activeReqId = null
        ctx.postMessage({ type: 'done', reqId })
        return
      }

      const sentence = sentences[i]
      if (!sentence) continue
      let out: { audio: Float32Array, sampling_rate: number } | null = null
      try {
        out = await (inferenceQueue = inferenceQueue.catch(() => {}).then(async () => {
          if (jobId !== currentJobId || cancelledReqIds.has(reqId)) return null
          return await entry.synth(sentence)
        })) as { audio: Float32Array, sampling_rate: number } | null
      }
      catch (inferError) {
        if (jobId !== currentJobId || cancelledReqIds.has(reqId)) {
          cancelledReqIds.delete(reqId)
          if (activeReqId === reqId) activeReqId = null
          ctx.postMessage({ type: 'done', reqId })
          return
        }
        throw inferError
      }

      if (!out || jobId !== currentJobId || cancelledReqIds.has(reqId)) {
        cancelledReqIds.delete(reqId)
        if (activeReqId === reqId) activeReqId = null
        ctx.postMessage({ type: 'done', reqId })
        return
      }

      const samples = out.audio
      const chunkIndex = offsetIndex + i
      ctx.postMessage(
        { type: 'chunk', reqId, index: chunkIndex, count: offsetIndex + sentences.length, sampleRate: out.sampling_rate, samples },
        [samples.buffer],
      )
    }

    if (jobId === currentJobId && !cancelledReqIds.has(reqId)) {
      if (activeReqId === reqId) activeReqId = null
      ctx.postMessage({ type: 'done', reqId })
    }
  }
  catch (error) {
    if (activeReqId === reqId) activeReqId = null
    if (jobId !== currentJobId || cancelledReqIds.has(reqId)) {
      cancelledReqIds.delete(reqId)
      ctx.postMessage({ type: 'done', reqId })
      return
    }
    ctx.postMessage({ type: 'error', reqId, message: error instanceof Error ? error.message : String(error) })
  }
}
