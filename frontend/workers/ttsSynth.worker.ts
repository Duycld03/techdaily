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
// Precision per backend, chosen for lowest synthesis latency (benchmarked in a
// cross-origin-isolated reader). WebGPU uses fp16 (native half-precision). WASM
// (CPU) uses fp32: ORT-web has no fast int8 kernels for this VITS model, so the
// quantized (q8) weights transformers.js loads by default run ~4x SLOWER than
// fp32 on WASM. fp32 is also the universal fallback if a preferred build fails.
type Dtype = 'fp32' | 'fp16'
const PREFERRED_DTYPE: Record<Backend, Dtype> = { webgpu: 'fp16', wasm: 'fp32' }

interface PipelineEntry {
  device: Backend
  synth: SynthFn
}

const ctx = self as unknown as DedicatedWorkerGlobalScope

// Utilize a many-core CPU: run the WASM (CPU) backend across as many threads as
// the machine reports when the browsing context is cross-origin isolated.
// Guarding behind self.crossOriginIsolated avoids ONNX Runtime Web warnings
// and errors when crossOriginIsolated is false, while fully exploiting multi-core
// concurrency when cross-origin isolation is enabled.
const onnxWasm = env.backends.onnx.wasm
const isIsolated = typeof self !== 'undefined' && 'crossOriginIsolated' in self && Boolean(self.crossOriginIsolated)
if (onnxWasm) {
  if (isIsolated && typeof navigator !== 'undefined' && typeof navigator.hardwareConcurrency === 'number') {
    onnxWasm.numThreads = Math.max(1, navigator.hardwareConcurrency)
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
    return await buildEntry(model, device, PREFERRED_DTYPE[device], reqId)
  }
  catch {
    return buildEntry(model, device, 'fp32', reqId)
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

ctx.onmessage = async (event: MessageEvent<WorkerIncomingMessage>) => {
  const msg = event.data
  if (!msg) return

  if (msg.type === 'cancel') {
    if (msg.reqId != null) {
      cancelledReqIds.add(msg.reqId)
    } else if (activeReqId != null) {
      cancelledReqIds.add(activeReqId)
    }
    return
  }

  if (msg.type !== 'synth') return
  const { reqId, model, sentences, offsetIndex = 0 } = msg
  activeReqId = reqId

  try {
    let entry = await getPipeline(model, reqId)
    if (cancelledReqIds.has(reqId)) {
      cancelledReqIds.delete(reqId)
      if (activeReqId === reqId) activeReqId = null
      ctx.postMessage({ type: 'done', reqId })
      return
    }

    ctx.postMessage({ type: 'device', reqId, device: entry.device })
    for (let i = 0; i < sentences.length; i++) {
      if (cancelledReqIds.has(reqId)) {
        cancelledReqIds.delete(reqId)
        if (activeReqId === reqId) activeReqId = null
        ctx.postMessage({ type: 'done', reqId })
        return
      }

      const sentence = sentences[i]
      if (!sentence) continue
      let out: { audio: Float32Array, sampling_rate: number }
      try {
        out = await entry.synth(sentence)
      }
      catch (inferError) {
        if (cancelledReqIds.has(reqId)) {
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
        out = await entry.synth(sentence)
      }

      if (cancelledReqIds.has(reqId)) {
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

    if (cancelledReqIds.has(reqId)) {
      cancelledReqIds.delete(reqId)
      if (activeReqId === reqId) activeReqId = null
      ctx.postMessage({ type: 'done', reqId })
      return
    }

    if (activeReqId === reqId) activeReqId = null
    ctx.postMessage({ type: 'done', reqId })
  }
  catch (error) {
    if (activeReqId === reqId) activeReqId = null
    if (cancelledReqIds.has(reqId)) {
      cancelledReqIds.delete(reqId)
      ctx.postMessage({ type: 'done', reqId })
      return
    }
    ctx.postMessage({ type: 'error', reqId, message: error instanceof Error ? error.message : String(error) })
  }
}
