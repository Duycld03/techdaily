# Design

## Context

In `ReaderAudioPlayer.vue`, the user can toggle between Google Cloud TTS and On-Device Web Worker synthesis.
When running on-device synthesis, `useSliceAudio.ts` splits the extracted narration script into sentences and sends `{ type: 'synth', reqId, model, sentences }` to `ttsSynth.worker.ts`. The worker iterates through all sentences sequentially using `@huggingface/transformers` (ONNX Runtime Web), emitting a `{ type: 'chunk', samples, sampleRate }` message after each sentence.

Currently, two structural flaws exist:
1. **No Worker Cancellation:** When the user switches from Device to Cloud mode or plays Cloud audio, the worker continues running in the background for all remaining sentences. It continues posting `chunk` messages to `useSliceAudio.ts`, which increments `synthIndex.value`. The UI renders `(Đang tạo audio... current/total)` because the template only checks `synthTotal > 0 && synthIndex > 0 && synthIndex < synthTotal` without checking `engineMode === 'device'`.
2. **Volatile Partial Progress:** In-progress chunks are stored strictly in an in-memory `buffers: Float32Array[]` array. If synthesis is interrupted at sentence 5 of 15 (e.g. by switching engines, pausing, or changing slices), those 5 chunks are discarded when the component state resets. Resuming on-device narration requires re-synthesizing all 15 sentences from index 0.

## Goals / Non-Goals

**Goals:**
- Provide a cancellation mechanism in `ttsSynth.worker.ts` via `{ type: 'cancel', reqId }` so the worker stops inference immediately between sentence iterations.
- Cancel any ongoing on-device worker synthesis when switching to Cloud mode or starting Cloud audio playback.
- Isolate the synthesis progress indicator in `ReaderAudioPlayer.vue` so it renders **only** when `engineMode === 'device'`.
- Persist intermediate generated sentence chunks in IndexedDB keyed by `partial:${chunkId}:${voice}:${contentHash}`.
- On on-device synthesis initialization, inspect IndexedDB for matching partial chunks:
  - If matching partial chunks exist ($K$ of $N$ sentences) under the same `contentHash`, restore chunks $0 \dots K-1$, update `synthIndex` to $K$, start pre-roll if $K \ge \text{targetBufferCount}$, and dispatch the worker to synthesize only the remaining sentences ($K \dots N-1$).
  - If slice content changed (`contentHash` mismatch), delete stale partial records and synthesize all sentences from 0.
- Clean up temporary partial chunks once the full-slice audio is assembled and stored in the permanent cache.

**Non-Goals:**
- Partial caching for Google Cloud TTS (Cloud TTS is server-synthesized and already caches complete MP3s in PostgreSQL and client IndexedDB).

## Decisions

### Decision 1: Worker Cancellation Protocol
`ttsSynth.worker.ts` will maintain a set of cancelled request IDs (`cancelledReqIds = new Set<number>()`).
When a `{ type: 'cancel', reqId }` message is received, `cancelledReqIds.add(reqId)`.
Inside the sentence loop:
```typescript
for (let i = startIndex; i < sentences.length; i++) {
  if (cancelledReqIds.has(reqId)) {
    cancelledReqIds.delete(reqId)
    ctx.postMessage({ type: 'cancelled', reqId })
    return
  }
  // synthesize sentence...
}
```
In `useSliceAudio.ts`, `TtsEngine` interface will expose `cancel(reqId?: number): void`.

### Decision 2: Partial Chunk Storage in `SliceAudioCache`
The client audio cache (`createSliceAudioCache`) will be extended with:
- `getPartial(key: string): Promise<{ chunks: Float32Array[], sampleRate: number, total: number } | null>`
- `savePartial(key: string, data: { chunks: Float32Array[], sampleRate: number, total: number }): Promise<void>`
- `deletePartial(key: string): Promise<void>`

Stored in IndexedDB (using a dedicated store or key prefix `partial:${chunkId}:${voice}:${contentHash}`). IndexedDB's structured clone natively supports `Float32Array`.

### Decision 3: Resumable Synthesis Flow
When `synthesizeOnDevice` starts:
1. Compute `key = buildAudioKey(source.chunkId, voice.id, contentHash)`.
2. Check permanent cache (`await cache.get(key)`). If hit, play complete audio.
3. Check partial cache (`await cache.getPartial(key)`).
4. If partial cache exists with $K$ chunks ($0 < K < N$):
   - Pre-populate `buffers = [...cachedPartial.chunks]`.
   - Set `synthIndex.value = K`.
   - Set `sampleRate = cachedPartial.sampleRate`.
   - If $K \ge \text{targetBufferCount}$, trigger `playPreRoll()`.
   - Invoke `engine.synthesize(model, sentences.slice(K), { startIndex: K, ... })`.
5. On each chunk received from the worker:
   - Append to `buffers`.
   - Asynchronously persist updated partial chunks to `cache.savePartial(key, ...)`.
6. When all $N$ chunks are received:
   - Assemble full WAV and save to permanent cache (`await cache.set(key, fullWavBlob)`).
   - Delete partial cache entry (`await cache.deletePartial(key)`).

### Decision 4: Content Integrity via `contentHash`
Because the partial cache key is `partial:${chunkId}:${voice}:${contentHash}`, where `contentHash` is the SHA-256 of the normalized markdown narration script:
- If the user or author edits the slice text, the new `contentHash` will not match any existing partial key.
- The player will see a partial cache miss and synthesize fresh from sentence 0.
- Stale partial entries can be lazily evicted when creating a new partial entry for the same `chunkId`.

### Decision 5: UI Engine State Isolation
In `ReaderAudioPlayer.vue`:
```html
<span
  v-if="engineMode === 'device' && synthTotal > 0 && synthIndex > 0 && synthIndex < synthTotal"
  class="text-xs text-brand-600 dark:text-brand-400 shrink-0 whitespace-nowrap font-medium"
>
  ({{ t('reader.audio_synthesizing', { current: synthIndex, total: synthTotal }) }})
</span>
```
In `useSliceAudio.ts`, `setEngineMode(mode)`:
When transitioning to `'cloud'`, cancel any running worker task immediately and reset `synthIndex.value = 0`, `synthTotal.value = 0`.

## Risks / Trade-offs

- **IndexedDB I/O overhead:** Writing intermediate chunks to IndexedDB after each sentence adds minimal I/O (~0.5ms per sentence in a Web Worker or async event loop), which is completely negligible compared to the ~500ms–2000ms taken by neural inference per sentence.
- **Worker termination vs cooperative cancellation:** Cooperative cancellation allows already-generated sentences to be safely recorded in IndexedDB before halting, whereas abrupt `worker.terminate()` would discard the in-flight chunk.
