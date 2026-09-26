# Spec Delta

## Purpose

Synthesizes a reader slice's AI-formatted text into speech entirely on the user's device (in-browser neural TTS), with the voice chosen automatically by slice language, and plays it back inside `/read/[bookId]` — caching the complete slice audio locally so playback is fully seekable, free, offline, and never re-synthesized on seek or replay.

## ADDED Requirements

### Requirement: On-Device Slice Narration Synthesis

The reader SHALL synthesize narration audio for a slice **entirely on the client**, using an on-device neural text-to-speech model run in a Web Worker, without any server request for synthesis, external API credential, or usage quota. All synthesis code SHALL be client-only and MUST NOT execute during server-side rendering.

Narration SHALL be available only for slices that are AI-formatted (`isAiFormatted == true`); for a non-formatted slice the reader SHALL NOT synthesize audio. Before synthesis, the reader SHALL derive a plain-text narration script from the slice's formatted markdown by removing markup and **excluding fenced code blocks** (so source code is not read aloud), preserving headings and prose. The synthesis voice SHALL be determined automatically by the slice's content language (`Language`, `en` or `vi`); there is one on-device voice per supported language and no user-facing voice selection.

The narration model for a language SHALL be downloaded lazily on first use and cached in the browser so it is fetched once and reused offline on subsequent uses. While a model is downloading or audio is being synthesized, the reader SHALL surface a localized progress/loading state.

#### Scenario: Synthesis runs on-device without a server request
- **WHEN** a user plays narration for an AI-formatted slice
- **THEN** the reader synthesizes the audio locally via the on-device model worker and issues no server request for text-to-speech.

#### Scenario: Narration gated on AI formatting
- **WHEN** the current slice is not AI-formatted (`isAiFormatted == false`)
- **THEN** the reader does not synthesize audio and the playback control is hidden or disabled.

#### Scenario: Code fences excluded from narration
- **WHEN** the slice's formatted content contains fenced code blocks
- **THEN** the derived narration script omits the code block contents while retaining the surrounding prose and headings.

#### Scenario: Voice selected automatically by content language
- **WHEN** a user plays a Vietnamese slice (`Language == "vi"`)
- **THEN** the reader synthesizes with the on-device vi voice without prompting for a voice choice.

#### Scenario: Voice model downloaded once and cached for offline reuse
- **WHEN** a user plays narration for a language whose model is not yet cached, and later plays narration again in that language
- **THEN** the first play downloads and caches the model with a visible progress state, and the later play reuses the cached model without re-downloading, including offline.

### Requirement: Seekable Full-Slice Audio Cache

To begin playback quickly, the reader SHALL stream synthesis by sentence and start playing before the whole slice is rendered. Upon completing synthesis of a slice, the reader SHALL assemble the sentences into a **single complete audio object** and SHALL use that complete audio as the playback source so the user can seek to any position within the slice **without re-synthesizing**.

The reader SHALL persist the complete slice audio in the browser's on-device storage (IndexedDB), keyed by `(chunkId, voice, contentHash)`, where `voice` is the language's on-device voice and `contentHash` is derived from the normalized narration script. A subsequent request to narrate the same `(chunkId, voice, contentHash)` — including in a later session — SHALL load the cached audio and SHALL NOT re-synthesize. When a slice's formatted content changes so its `contentHash` differs, the stale cache entry SHALL NOT be used and the audio SHALL be re-synthesized once. The audio cache SHALL enforce a bounded size (an entry or total-size cap) and evict least-recently-used entries; an evicted slice re-synthesizes on next play.

#### Scenario: Seeking within a slice does not re-synthesize
- **WHEN** a slice's audio has finished synthesizing and the user seeks backward or forward within it
- **THEN** playback jumps to the new position using the already-assembled complete audio without invoking the synthesis worker.

#### Scenario: Replaying a slice loads cached audio without re-synthesis
- **WHEN** a user re-opens a slice whose `(chunkId, voice, contentHash)` audio is present in IndexedDB
- **THEN** the reader loads the cached audio and begins playback without running the synthesis worker.

#### Scenario: Re-formatted slice invalidates cached audio
- **WHEN** a slice's formatted content changes so its `contentHash` differs from the cached entry, and the user plays narration
- **THEN** the stale entry is not used and the reader re-synthesizes and caches audio under the new `contentHash`.

#### Scenario: Cache eviction bounds device storage
- **WHEN** cached audio exceeds the configured cap
- **THEN** the reader evicts least-recently-used slice audio, and a subsequently re-opened evicted slice re-synthesizes once and is re-cached.

### Requirement: Reader Audio Playback Controls

The `/read/[bookId]` reader SHALL provide an audio playback control that lets the user listen to the current slice's narration, available only when the current slice is AI-formatted (hidden or disabled otherwise). Playback SHALL use an HTML5 `<audio>` element supporting play, pause, and position seeking over the assembled complete slice audio.

The reader SHALL offer a **playback speed** control spanning 0.5x to 2.0x applied client-side to the `<audio>` element's `playbackRate` **without re-synthesizing audio**; the selected speed SHALL persist across sessions and slices. The reader SHALL NOT present a voice picker, since the voice is chosen automatically by slice language.

All audio control labels, speed labels, and download/synthesis progress messages SHALL render localized text (en/vi) from the i18n catalog, and the controls SHALL follow the reader's responsive typography and `whitespace-nowrap shrink-0` layout invariants across both locales. The reader SHALL NOT use native browser dialogs for audio state; status is conveyed via in-page controls and `useToast()`.

#### Scenario: User plays the current slice narration
- **WHEN** a user viewing an AI-formatted slice activates the play control
- **THEN** the reader synthesizes (or loads cached) audio for the slice's language voice, loads it into the `<audio>` element, and begins playback with seek support.

#### Scenario: User changes playback speed during playback
- **WHEN** a user sets the playback speed to 1.5x while audio is playing
- **THEN** the `<audio>` element's `playbackRate` updates to 1.5 immediately without re-synthesizing, and the speed preference is persisted.

#### Scenario: Speed preference persists across sessions
- **WHEN** a user who previously selected 1.25x returns to the reader in a later session
- **THEN** the reader restores 1.25x playback speed as the active preference.
