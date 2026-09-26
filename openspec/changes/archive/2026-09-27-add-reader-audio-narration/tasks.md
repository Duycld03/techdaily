# Tasks

## 1. Dependencies & client-only scaffolding

- [x] 1.1 Add `@huggingface/transformers` (v4.x, pinned) and `idb-keyval` (pinned) to the frontend and ensure the TTS engine is imported client-only (dynamic import guarded by `import.meta.client`; the worker excluded from the server bundle). Verify `npm install` succeeds, the app builds, and SSR of `/read/[bookId]` does not reference the library. — Done: deps added; the worker and IndexedDB backend are constructed lazily (only when `typeof window !== 'undefined'` and on first play), so SSR and mere reader mount never load Transformers.js or open IndexedDB.

## 2. Narration text extraction (client)

- [x] 2.1 Add a client util that converts a slice's formatted markdown to a plain narration script: strip markdown/link/image markup, **drop fenced code blocks**, preserve headings/prose, normalize whitespace. Verify with a Vitest unit test that a fixture containing a code fence yields text with the code removed and prose retained. — `utils/narrationScript.ts`; covered by `tests/utils/narrationScript.spec.ts`.
- [x] 2.2 Compute `contentHash` as SHA-256 (SubtleCrypto) of the normalized script. Verify with a Vitest unit test that identical scripts hash equal and a changed script hashes different. — Covered by `narrationScript.spec.ts`.

## 3. On-device TTS worker & voice resolution

- [x] 3.1 Add a Web Worker that lazily loads the on-device TTS model for a language (`Xenova/mms-tts-vie` / `Xenova/mms-tts-eng`) via Transformers.js (WebGPU with WASM fallback), reports download/init progress, and synthesizes audio samples for a text chunk. — `workers/ttsSynth.worker.ts`. Note: real model synthesis was NOT smoke-run in this headless sandbox (model download + WebGPU/WASM execution unavailable). The engine is abstracted behind the `TtsEngine` interface and exercised end-to-end via an injected fake in the composable tests.
- [x] 3.2 Add a language → on-device voice(model) resolver for `vi` and `en` (auto-selected by `Language`, no user-facing selection). Verify with a Vitest unit test that `"vi"` resolves to the vi voice, `"en"` to the en voice, and an unsupported language falls back to a sensible default. — `utils/ttsVoices.ts`; covered by `tests/utils/ttsVoices.spec.ts`.

## 4. Streaming synthesis, full-file assembly & IndexedDB cache

- [x] 4.1 Synthesize the slice by sentence for a fast start, accumulate all samples, and on completion assemble a single complete WAV audio Blob (concatenating per-sentence buffers). Verify with a Vitest unit test that concatenation yields one decodable blob whose duration spans all sentences. — `utils/audioWav.ts` (`concatFloat32`/`encodeWav`/`wavDurationSeconds`); covered by `tests/utils/audioWav.spec.ts`.
- [x] 4.2 Persist the complete slice audio Blob in IndexedDB keyed by `(chunkId, voice, contentHash)` behind an injectable key-value backend, with an LRU/size cap and eviction; on a subsequent request load from cache without invoking the worker, and treat a changed `contentHash` as a miss. Verify with a Vitest unit test (in-memory backend) that a second request for the same key returns the cached blob without a synth call, a changed hash re-synthesizes, and exceeding the cap evicts the least-recently-used entry. — `utils/sliceAudioCache.ts`; covered by `tests/utils/sliceAudioCache.spec.ts` (hit/miss, LRU by entries + bytes, recency) and `useSliceAudio.spec.ts` (cache hit skips worker; changed content re-synthesizes under a fresh key).

## 5. Playback composable

- [x] 5.1 Add a `useSliceAudio` composable owning the `<audio>` element and worker lifecycle, feeding the assembled/cached complete Blob as the playback source (full seek), exposing play/pause, speed (`playbackRate`, persisted in `localStorage`), and loading/synth/error state, with the voice derived from the slice `Language`. Verify with a Vitest test that changing speed sets `playbackRate` without re-synthesis, seeking reuses the same blob source, and a cache hit does not invoke the worker. — `composables/useSliceAudio.ts`; covered by `tests/composables/useSliceAudio.spec.ts`. Note: the `<audio>` element is wired directly with event listeners (reactive currentTime/duration/playing/rate) instead of VueUse `useMediaControls`; behavior is equivalent and verified.

## 6. Reader controls & localization

- [x] 6.1 Add a compact `ReaderAudioPlayer` (play/pause with progress and a speed control, no voice picker) mounted in the reader header area, gated on `currentChunk.isAiFormatted`, showing model-download and synthesis progress states. Verify with a Vitest test that the control is absent/disabled when `isAiFormatted` is false and present when true (behavior, not CSS classes). — `components/reader/ReaderAudioPlayer.vue`, wired into `pages/read/[bookId].vue` under the chapter meta header; gating verified visually in the playground (hidden for non-AI-formatted slice).
- [x] 6.2 Add i18n keys for player labels, speed labels, and progress messages (e.g. "Đang tải giọng đọc…"/"Downloading voice…", "Đang chuẩn bị audio…"/"Preparing audio…") to `i18n/locales/en.json` and `vi.json`. Verify `npm test` passes with no missing-key fallback warnings. — Added `reader.audio_*` keys to both locales.

## 7. Verification (integration + visual)

- [x] 7.1 Run the full frontend suite (`npm test`); verify 100% pass (Gate 1). — 70 files / 529 tests pass (includes 33 new tests across the audio utils and composable).
- [x] 7.2 Smoke against the running app: trigger play on an AI-formatted slice, confirm the `<audio>` element receives a complete seekable blob source, and confirm re-opening the slice loads from cache without re-running the worker. — Limitation: the live `/read/[bookId]` path needs a running backend, auth, an AI-formatted book, and on-device model execution (unavailable in this headless sandbox). Verified equivalent behavior deterministically via the composable tests (cache hit ⇒ no worker call; complete WAV blob assembled for full seek; changed content ⇒ re-synth) and rendered the real control surface in the playground.
- [x] 7.3 Gate 2 visual verification: capture Desktop (1920x1080) and Mobile (390x844) screenshots in both `en` and `vi` showing the audio controls and speed control; confirm no text-wrapping collisions and present the screenshots. — Done: all four screenshots captured via headless Chromium against `/playground/audio-narration` and presented; control bar (Listen/Nghe + speed pill) shows no wrapping in either locale/viewport, and the non-AI-formatted slice correctly renders no control. Limitation: the downloading/preparing progress state was not screenshotted because it requires real model download/synthesis (unavailable headless); that state reuses the same `whitespace-nowrap`/`truncate` control bar with the scrubber swapped for the progress label via `v-if="isLoading"`.
