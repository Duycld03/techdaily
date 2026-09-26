# Design

## Context

See `proposal.md` — Why. The behavior contract is in `specs/audio-narration/spec.md` (delta).

Relevant current state (verified):
- Reader slices are `DocumentChunk` rows; the frontend already has the slice's formatted text and metadata on the client (`currentChunk.originalTextMarkdown`, `isAiFormatted`, `Language` `"en"`/`"vi"`) in `frontend/pages/read/[bookId].vue`, rendered via `useMarkdownRenderer`. Reader header controls live in `frontend/components/reader/ReaderHeaderBar.vue`.
- Frontend is Nuxt 3 (`future.compatibilityVersion: 4`, SSR/PWA). Reader preferences already persist in `localStorage` (`useReaderTypography.ts`, `techdaily_bookmark_{bookId}`). Custom selects use `AppSelect.vue`; toasts use `useToast()`. `@vueuse/core` (v15) is available (`useMediaControls`, `useObjectUrl`, `useStorage`).
- **Blocker that forced this approach:** the design's earlier cloud provider (Google Cloud Text-to-Speech) rejects API-key auth (`HTTP 401 "API keys are not supported by this API"`) and requires a service-account credential that is not provisioned in this project's `.env`. On-device synthesis needs no credential and is genuinely free, which matches the product's "free" requirement and removes the quota machinery entirely.

## Goals / Non-Goals

**Goals:**
- Read an AI-formatted slice aloud with a free, offline, on-device neural voice — no backend, no API key, no per-use limit.
- Make playback fully seekable without re-synthesizing on seek, and free on replay, by assembling and caching the complete slice audio on the user's device.
- Provide in-reader play/pause and client-side speed control that never triggers re-synthesis.

**Non-Goals:**
- Any server-side TTS, cloud provider, credential, or monthly quota guardrail — all removed by moving synthesis on-device.
- A user-facing voice picker or multiple voices per language — the on-device model is single-speaker per language and the voice is auto-selected by slice language; rich voice variety is a cloud-only feature and out of scope.
- A browser Web Speech API path — inconsistent/absent voices (empty on Linux Chromium); the downloaded model runs uniformly across environments instead.
- Reading fenced code blocks aloud; pre-caching audio for slices the user never opens.

## Decisions

- **Engine: Transformers.js (`@huggingface/transformers`, v4.x) on ONNX Runtime Web, in a Web Worker.** Runs the model off the main thread (no UI freeze), using WebGPU when available and falling back to WASM. All TTS code is **client-only** (dynamic import guarded by `import.meta.client`, plus the worker itself) so Nuxt SSR never bundles or executes it on the server.
- **Voices: MMS-TTS (`Xenova/mms-tts-vie`, `Xenova/mms-tts-eng`), auto-selected by `Language`.** Same VITS architecture for both languages → one code path, two model files, resolved from `DocumentChunk.Language`. MMS is single-speaker per language, so there is exactly one voice per language and **no user voice picker** (a language→voice resolver, not a selectable list). Additional on-device voices could be added later without a contract change. Alternatives: Kokoro (higher quality but **no Vietnamese** → rejected); Web Speech (rejected, see Non-Goals).
- **First-use model download, cached for offline reuse.** A language's model (tens–low-hundreds of MB `[INFERENCE]`) downloads lazily on first listen with a visible progress state, then is cached (the library's Cache API / IndexedDB store) and reused offline. Only the first listen in a language pays the download.
- **Fast start via sentence streaming; full-file assembly for seeking.** The worker splits the narration script into sentences, synthesizes sequentially, and posts the first sentence's audio so playback can begin within ~1–2s while later sentences render in the background. When the whole slice is synthesized, the samples are concatenated into one complete audio Blob that becomes the authoritative, fully seekable source.
- **On-device audio cache in IndexedDB, keyed by `(chunkId, voice, contentHash)`.** The complete slice Blob is stored so seeking is free (a normal complete file) and re-opening the slice (even in a later session) loads instantly with zero re-synthesis. `voice` is the language's model id; `contentHash` = SHA-256 (SubtleCrypto) of the normalized narration script, so a re-formatted slice is a cache miss and re-synthesizes once. Model weights are cached separately (the library's own cache).
- **Storage bounded by an LRU/size cap.** Raw WAV is large (a "~5 min" slice ≈ ~14 MB `[INFERENCE]`), so caching an entire book is heavy. The audio cache enforces a configurable cap (max entries or total MB) and evicts least-recently-used slices; evicted slices re-synthesize once on next open. The cache logic is written against an injectable key-value backend (production: IndexedDB via `idb-keyval`; tests: in-memory) so eviction/hash-miss behavior is unit-testable without a real IndexedDB.
- **Client-side playback speed via `HTMLAudioElement.playbackRate` (0.5x–2.0x), not a synthesis parameter.** One cached file serves every speed with zero re-synthesis. The speed preference persists in `localStorage` (mirroring `useReaderTypography`).
- **Frontend integration.** A `useSliceAudio` composable owns the `<audio>` element (via `useMediaControls`), the worker lifecycle, the IndexedDB cache, the persisted speed, and loading/synth/error state; the voice is derived from `currentChunk.Language`. A compact `ReaderAudioPlayer` (play/pause, progress, speed control) mounts in the reader header area, gated on `currentChunk.isAiFormatted`, and shows model-download/synthesis progress. There is no voice picker. New i18n keys (player labels, speed, "downloading voice", "preparing audio") go in `i18n/locales/en.json` and `vi.json`.

## Risks / Trade-offs

- [Large one-time model download (tens–hundreds of MB) `[INFERENCE]`] → Download lazily on first listen, cache weights so it happens once and works offline after; show a clear "downloading voice" progress state.
- [Synthesis latency on weak/mobile CPUs] → Sentence streaming starts playback before the full slice is ready; WebGPU accelerates when present; the complete file is cached so it is a one-time cost per (slice, language).
- [Device storage growth from cached audio] → LRU/size cap with eviction; evicted slices re-synthesize once.
- [Robotic single-speaker voice quality] → Accepted trade-off for "free + offline + no credentials"; richer voices remain a future cloud option behind the same reader controls.
- [SSR/bundle pitfalls with a heavy client lib] → Dynamic client-only import and a Web Worker keep it out of the server bundle and off the main thread.

## Migration Plan

- Frontend-only. Add `@huggingface/transformers` (v4.x, pinned) and `idb-keyval` (pinned) plus the worker; no backend changes, no database migrations, no new secrets.
- No data migration; the on-device audio cache is per-browser and rebuilds on demand.
- Rollback = remove the reader audio player, composable, worker, and utils, and drop the dependencies; no persisted server state is affected.
