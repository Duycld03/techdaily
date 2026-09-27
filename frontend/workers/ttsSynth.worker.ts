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
// Precision per backend, chosen for lowest synthesis latency. WebGPU uses fp16
// (native half-precision). Desktop WASM uses fp32 (~4x faster than int8 VITS kernels
// on desktop CPUs). Mobile WASM uses quantized q8 (model_quantized.onnx, 36.6 MB vs
// 109 MB) to fit mobile memory ceilings and avoid out-of-memory browser tab crashes.
type Dtype = 'fp32' | 'fp16' | 'q8'
const PREFERRED_DTYPE: Record<Backend, Dtype> = { webgpu: 'fp16', wasm: 'fp32' }

const isMobile = typeof navigator !== 'undefined' && (
  /android|iphone|ipad|ipod|mobile/i.test(navigator.userAgent) ||
  (navigator.platform === 'MacIntel' && typeof navigator.maxTouchPoints === 'number' && navigator.maxTouchPoints > 1)
)

function getPreferredDtype(device: Backend): Dtype {
  if (isMobile && device === 'wasm') {
    return 'q8'
  }
  return PREFERRED_DTYPE[device]
}
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

// Pick the execution backend once. `'gpu' in navigator` only proves the API is
// exposed (e.g. headless Chromium) — an adapter may still be unavailable, which
// would make WebGPU inference throw. Requesting the adapter is the real probe.
async function pickDevice(): Promise<Backend> {
  if (typeof navigator === 'undefined' || !('gpu' in navigator)) return 'wasm'
  // WebGPU is absent from the default worker lib types; treat navigator.gpu as
  // the standard GPU interface. Unexpressible library type → named-const cast.
  const gpu = (navigator as unknown as { gpu?: { requestAdapter: (options?: { powerPreference?: string }) => Promise<unknown> } }).gpu
  if (gpu == null) return 'wasm'
  try {
    // Prefer a discrete GPU over integrated graphics on hybrid-graphics machines.
    const adapter = await gpu.requestAdapter({ powerPreference: 'high-performance' })
    return adapter ? 'webgpu' : 'wasm'
  }
  catch {
    return 'wasm'
  }
}

// The adapter probe (and its "No available adapters." console notice on
// GPU-less browsers) runs at most once per worker, not once per model.
let devicePromise: Promise<Backend> | null = null
function resolveDevice(): Promise<Backend> {
  devicePromise ??= pickDevice()
  return devicePromise
}

function buildEntry(model: string, device: Backend, dtype: Dtype, reqId: number): Promise<PipelineEntry> {
  // Transformers.js fires progress per downloaded file (config, tokenizer,
  // model weights). The tiny JSON files each race to 100% before the large
  // `.onnx` weights start, which made the bar flash 100% then restart 0→100%.
  // Surface only the model-weights file so the percentage climbs once, 0→100.
  const built = pipeline('text-to-speech', model, {
    device,
    dtype,
    progress_callback: (info: { status?: string, file?: string, progress?: number } & Record<string, unknown>) => {
      if (info?.status === 'progress' && typeof info.file === 'string' && info.file.endsWith('.onnx')) {
        ctx.postMessage({ type: 'progress', reqId, stage: 'download', progress: info.progress ?? 0 })
      }
    },
  }) as unknown as Promise<SynthFn>
  return built.then(synth => ({ device, synth }))
}

// Build for one backend, trying its preferred quantized dtype first and falling
// back to full precision (fp32) when the quantized weights are missing or fail
// to build — at most two dtype attempts per device.
async function buildForDevice(model: string, device: Backend, reqId: number): Promise<PipelineEntry> {
  try {
    return await buildEntry(model, device, getPreferredDtype(device), reqId)
  }
  catch {
    return await buildEntry(model, device, 'fp32', reqId)
  }
}

// Prefer WebGPU when an adapter exists, but a GPU-init failure must not surface
// as a narration error while WASM can still run: fall back to CPU on build error.
async function resolveEntry(model: string, reqId: number): Promise<PipelineEntry> {
  const device = await resolveDevice()
  if (device === 'webgpu') {
    try {
      return await buildForDevice(model, 'webgpu', reqId)
    }
    catch {
      // GPU backend failed to initialize; continue on CPU WASM.
    }
  }
  return buildForDevice(model, 'wasm', reqId)
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
        // A WebGPU pipeline can initialize yet fail at inference on some
        // browsers. Rebuild once on CPU WASM and retry this sentence; later
        // sentences then reuse the fallback without re-probing.
        if (entry.device !== 'webgpu') throw inferError
        const fallback = buildForDevice(model, 'wasm', reqId)
        pipelines.set(model, fallback)
        entry = await fallback
        ctx.postMessage({ type: 'device', reqId, device: entry.device })
        out = await (inferenceQueue = inferenceQueue.catch(() => {}).then(async () => {
          if (jobId !== currentJobId || cancelledReqIds.has(reqId)) return null
          return await entry.synth(sentence)
        })) as { audio: Float32Array, sampling_rate: number } | null
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
