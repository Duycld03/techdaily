# Proposal

## Why

The `/read/[bookId]` reader is text-only. Users who want to consume a slice hands-free — commuting, resting their eyes, or reinforcing reading by listening — have no option on the web. Since slices are already AI-formatted into clean prose and carry a per-chunk `Language` (`en`/`vi`), the platform can read them aloud with a voice that runs **on the user's own device**: free, offline after first use, and private, with no API keys, no server, and no usage limits.

## What Changes

- Add an **AI Audio Narration** capability that turns a slice's AI-formatted text into speech **entirely in the browser**, using an on-device neural TTS model (Transformers.js / ONNX Runtime Web) in a Web Worker. Nothing is sent to a server: no TTS endpoint, no cloud provider, no credentials, and no per-use limit.
- **First use downloads the voice model once.** A language's model (tens to low-hundreds of MB) is fetched on the first listen for that language, shown with a clear "downloading voice" progress state, then cached (Cache API / IndexedDB) and reused **offline** on every later use — no repeat download.
- **Voice is chosen automatically by the slice language** (`en`/`vi`). The on-device model provides a single voice per language, so there is **no in-app voice picker** — picking among multiple natural voices is a cloud-only capability and is out of scope here.
- Narration text is derived in the browser from the slice's formatted markdown with markup removed and **fenced code blocks excluded** (source code is not read aloud).
- **Seekable, no re-generation on seek.** The worker streams synthesis sentence-by-sentence for a fast start, then assembles the whole slice into one audio Blob and caches it in the browser's **IndexedDB** on the user's device, keyed by `(chunkId, voice, contentHash)`. Playback uses the complete cached file, so the user can seek anywhere; re-opening the slice (even in a later session) loads from cache with zero recomputation. If the slice's formatted content changes (`contentHash` differs), the entry is invalidated and re-synthesized once. Cached audio is bounded by an LRU/size cap to protect device storage.
- Add an in-reader **audio player** — play/pause plus **playback speed** (0.5x–2.0x) applied client-side via `HTMLAudioElement.playbackRate` (never re-synthesizes). The speed preference persists across sessions.
- Narration is **gated on `IsAiFormatted == true`** so only clean, curated slices are read aloud; download and synthesis progress states are localized (en/vi) and honor the reader's responsive typography and layout invariants.

## Capabilities

### New Capabilities

- `audio-narration`: On-device (in-browser) synthesis of AI-formatted slice text into speech with the voice auto-selected by slice language, a seekable full-slice audio cache in IndexedDB, and in-reader playback controls (play/pause and client-side playback speed).

### Modified Capabilities

_None._ Audio behavior is owned end-to-end by the new `audio-narration` capability and runs entirely on the client; the reader page consumes it without a change to existing `reader` requirements.
