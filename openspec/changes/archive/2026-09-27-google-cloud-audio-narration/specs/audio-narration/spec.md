# Spec Delta

## ADDED Requirements

### Requirement: Google Cloud Narration Synthesis with Database Caching

The system SHALL support server-side audio narration synthesis via Google Cloud Text-to-Speech (Cloud TTS) as the primary high-speed narration engine. All calls to Google Cloud TTS SHALL be proxied through the ASP.NET Core backend using a server-side API key (`Google__TtsApiKey`); client applications SHALL NOT be exposed to external API credentials.

The backend SHALL persist complete synthesized audio files in PostgreSQL (`DocumentChunkAudios`) keyed by `(DocumentChunkId, ContentHash, VoiceId)`. When a request for a slice's narration arrives:
1. If an audio record matching `(DocumentChunkId, ContentHash, VoiceId)` exists in the database, the backend SHALL return the cached audio immediately without calling the Google Cloud TTS API.
2. If no record exists, the backend SHALL call the Google Cloud TTS REST API, store the resulting audio and character count in PostgreSQL, and stream the audio to the client.
3. Upon receiving the audio from the server, the reader client SHALL store it in browser IndexedDB under the same `(chunkId, voice, contentHash)` cache key so subsequent seeks, pauses, and replays on that browser are served locally with zero network latency.

When a slice's content changes such that its `ContentHash` changes, the existing database audio record SHALL be invalidated and new audio synthesized on next play.

#### Scenario: Database cache hit serves audio without calling Google Cloud API
- **WHEN** a reader requests narration for a slice whose `(DocumentChunkId, ContentHash, VoiceId)` audio is already cached in PostgreSQL
- **THEN** the server returns the cached audio in less than 50ms and makes zero requests to the Google Cloud TTS API.

#### Scenario: Database cache miss synthesizes, stores in PostgreSQL, and streams audio
- **WHEN** a reader requests narration for a slice not yet cached in PostgreSQL
- **THEN** the server synthesizes audio via Google Cloud TTS, writes the MP3 audio bytes to PostgreSQL, and returns the audio to the client.

#### Scenario: Client stores server-synthesized audio in IndexedDB
- **WHEN** client receives audio synthesized by Google Cloud TTS from the server
- **THEN** client stores the audio in browser IndexedDB so repeat listens in the session do not issue repeated HTTP requests.

#### Scenario: Slice content revision invalidates server cache
- **WHEN** a slice's markdown is re-formatted or edited, altering its `ContentHash`
- **THEN** the server does not serve stale audio from the prior hash and re-synthesizes audio under the new `ContentHash`.

### Requirement: Dual-Engine Audio Toggle and Server Quota Guard

The `/read/[bookId]` reader audio player SHALL provide an in-player toggle switch allowing the user to select between **Google Cloud** (cloud-accelerated) and **On-Device** (local Web Worker) synthesis engines. The default engine SHALL be Google Cloud, and the user's preference SHALL persist in `localStorage`.

The backend SHALL track cumulative characters synthesized via Google Cloud TTS during the current calendar month. To protect the free-tier monthly allowance (hard-capped at 950,000 characters to ensure a safe buffer below Google's 1,000,000 allowance):
1. The backend SHALL expose current monthly quota utilization.
2. When monthly quota consumption reaches or exceeds the threshold (900,000 characters), the reader frontend SHALL disable the Google Cloud toggle switch with an informative tooltip and automatically select the On-Device engine.
3. If an API request to synthesize via Google Cloud is received when the monthly quota is exhausted (reaches 950,000 characters, such as via direct API call or client DOM tampering), the backend SHALL reject the request with an RFC 7807 problem details response (HTTP 429 Too Many Requests or 403 Forbidden with code `AudioQuotaExhausted`).
4. Upon receiving a quota exhaustion response, the client SHALL display a localized error toast notifying the user that the monthly Google Cloud quota has been reached, and automatically switch the player to the On-Device engine.

#### Scenario: Default engine is Google Cloud
- **WHEN** a reader opens an AI-formatted slice for the first time without a saved preference
- **THEN** the audio player selects Google Cloud as the active engine.

#### Scenario: User toggles to On-Device engine
- **WHEN** a user switches the audio toggle from Google Cloud to On-Device
- **THEN** the player switches to the local Web Worker engine, synthesizes on CPU/GPU, and caches in browser IndexedDB.

#### Scenario: Near-quota usage disables Google Cloud toggle
- **WHEN** the server reports monthly character quota at or above 900,000 characters
- **THEN** the Google Cloud toggle in the reader is disabled with an explanatory tooltip and narration defaults to On-Device mode.
#### Scenario: Quota-exhausted API call rejected and client alerted
- **WHEN** a client submits a synthesis request to the backend after the monthly quota is exhausted
- **THEN** the backend responds with an RFC 7807 `AudioQuotaExhausted` error, and the client displays a toast notification and reverts to the On-Device engine.

### Requirement: Free-Tier Voice Selection and Polished Presentation

When the Google Cloud engine is active, the reader audio player SHALL provide a voice selector restricted to Google Cloud Free-Tier voices (Neural2 and WaveNet tiers) for the slice's content language, supporting at least one female and one male voice for Vietnamese (`vi-VN`) and English (`en-US`). Voice selection SHALL NOT be shown when the On-Device engine is active.

All audio controls, buttons, tooltips, and status indicators SHALL use clean, professional, descriptive copy and SHALL NOT use hype or buzzword labels such as "AI", "AI Audio", or "Google AI". The UI SHALL use standard, subtle iconography (e.g. cloud icon or plain toggle switch) and SHALL NOT use mismatched or aggressive icons such as lightning bolts (`Zap`).

#### Scenario: Free-tier voice selection for Vietnamese slice
- **WHEN** a user views a Vietnamese slice with Google Cloud engine active
- **THEN** the player provides a choice between curated Vietnamese Neural2 female (`vi-VN-Neural2-A`) and male (`vi-VN-Neural2-D`) voices.

#### Scenario: Free-tier voice selection for English slice
- **WHEN** a user views an English slice with Google Cloud engine active
- **THEN** the player provides a choice between curated English Neural2 female (`en-US-Neural2-F`) and male (`en-US-Neural2-D`) voices.

#### Scenario: UI copy and icons avoid buzzwords
- **WHEN** viewing the audio narration player in any state
- **THEN** the controls render clean labels ("Google Cloud", "Thiết bị" / "Device") without "AI" prefixes, and no lightning bolt icons are rendered in the audio control.
