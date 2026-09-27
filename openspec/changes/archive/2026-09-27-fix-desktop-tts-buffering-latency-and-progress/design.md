# Design

## Context

See `proposal.md` for background and problem motivation.

In the reader interface, on-device speech narration is orchestrated by `useSliceAudio.ts` which divides the markdown text into sentences using `splitSentences()`, coordinates a dedicated Web Worker running Transformers.js (`ttsSynth.worker.ts`), accumulates Float32Array PCM audio chunks, and feeds an HTML `<audio>` element. Presentation is handled by `ReaderAudioPlayer.vue`.

Recent changes introduced two regressions for desktop/laptop users:
1. `ReaderAudioPlayer.vue` checked `target > 0 && synthIndex < target` first in `statusLabel`, shadowing `downloadProgress` and displaying `0/12` while the 100 MB model was actively downloading. Additionally, `device` was accidentally omitted from the destructuring of `useSliceAudio`, producing Vue component instance template warnings and failing to show the CPU/GPU badge.
2. `useSliceAudio.ts` set `targetBufferCount = Math.max(2, Math.ceil(sentences.length / 3))`, which required 12 sentences to buffer before playing on a 36-sentence slice.
3. `ttsSynth.worker.ts` capped desktop WASM threads at 4 and mobile touch point detection risked treating touchscreen desktop laptops as mobile devices.

## Goals / Non-Goals

**Goals:**
- Provide clear visual feedback when downloading voice models (`Đang tải giọng đọc… X%`).
- Ensure immediate speech playback within 2 sentences (~10-15s audio headroom), dropping initial start latency from minutes to seconds.
- Restore multi-core desktop CPU WASM thread allocation (`Math.min(8, hardwareConcurrency)`).
- Eliminate Vue console template warnings and restore the `[GPU]` / `[CPU]` badge in `ReaderAudioPlayer.vue`.
- Ensure desktop laptops with touchscreens are not misclassified as mobile devices.

**Non-Goals:**
- Removing mobile safeguards (mobile single-threading and `q8` quantization on iOS/Android remain intact).
- Modifying Google Cloud TTS API contracts or backend audio proxy logic.

## Decisions

### 1. Status Label Precedence in `ReaderAudioPlayer.vue`

**Decision:**
Reorder the evaluation of conditions in `statusLabel`:
1. If `downloadProgress.value > 0 && downloadProgress.value < 100`: return `reader.audio_downloading` with percentage.
2. If `target > 0 && synthIndex.value < target && synthTotal.value > 0`: return `reader.audio_buffering` with `synthIndex` / `target`.
3. If `synthTotal.value > 0 && synthIndex.value >= target`: return `reader.audio_synthesizing` with `synthIndex` / `synthTotal`.
4. Otherwise, return `reader.audio_preparing`.

**Rationale:**
Model downloading is a network I/O phase that occurs before audio synthesis begins. Showing "Đang tải giọng đọc… 45%" immediately informs the user that network traffic is progressing normally, rather than presenting a static "Đang đệm audio... 0/12" that appears frozen.

### 2. Low-Latency Pre-Roll Buffer Target

**Decision:**
In `useSliceAudio.ts`, simplify and bound the pre-roll buffer target:
```typescript
const target = Math.min(2, sentences.length)
```
For single-sentence slices: target = 1.
For all multi-sentence slices: target = 2.

**Rationale:**
Two sentences of speech provide 10 to 20 seconds of audio playback time. While the user listens to sentences 1 and 2, the background Web Worker synthesizes sentence 3, sentence 4, and beyond in real-time. Buffering 33% of an entire reading slice (up to 12-25 sentences) was an excessive requirement that defeated streaming narration on CPU inference.

### 3. Compute Device Destructuring in `ReaderAudioPlayer.vue`

**Decision:**
Add `device` to the destructured bindings from `useSliceAudio()`:
```typescript
const {
  status,
  playing,
  currentTime,
  duration,
  downloadProgress,
  synthIndex,
  synthTotal,
  targetBufferCount,
  device,
  errorMessage,
  errorInfo,
  ...
} = useSliceAudio(...)
```

**Rationale:**
Resolves the runtime Vue warning `Property "device" was accessed during render but is not defined on instance` and allows the `<component :is="device === 'webgpu' ? Laptop : Cpu" />` badge and tooltip to render smoothly.

### 4. Desktop WASM Concurrency and Touchscreen Laptop Safety

**Decision:**
In `ttsSynth.worker.ts`:
- Refine `isMobile`: Only check `/android|iphone|ipad|ipod|mobile/i` on userAgent, plus `navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1` (specifically for iPadOS Safari requesting desktop site). Do NOT use generic `maxTouchPoints > 1` without platform check on Windows or Linux, as touchscreen Windows/Linux laptops are desktop CPUs.
- When `!isMobile && isIsolated`: set `onnxWasm.numThreads = Math.min(8, Math.max(1, navigator.hardwareConcurrency))`.

## Risks / Trade-offs

- **Risk:** Starting playback after 2 sentences might cause a brief pause between sentences if sentence 2 ends before sentence 3 finishes synthesizing on very slow single-core machines.
  - *Mitigation:* Audio playback automatically chains onto chunk 3 as soon as it arrives without crashing. Users overwhelmingly prefer hearing audio in 3-5 seconds with possible minor pauses over waiting 3 minutes for playback to start.
