# Spec Delta: audio-narration

## MODIFIED Requirements

### Requirement: Google Cloud Narration Synthesis with Database Caching

The system SHALL support server-side audio narration synthesis via Google Cloud Text-to-Speech (Cloud TTS) as the primary high-speed narration engine. All calls to Google Cloud TTS SHALL be proxied through the ASP.NET Core backend using a server-side API key (`Google__TtsApiKey`); client applications SHALL NOT be exposed to external API credentials.

Client applications requesting audio synthesis or quota status SHALL resolve backend API routes using canonical relative URLs in production environments (matching the reverse proxy configuration) and MUST NOT fall back to hardcoded localhost addresses when environment configuration strings evaluate to empty values.

The backend SHALL persist complete synthesized audio files in PostgreSQL (`DocumentChunkAudios`) keyed by `(DocumentChunkId, ContentHash, VoiceId)`. When a request for a slice's narration arrives:
1. If an audio record matching `(DocumentChunkId, ContentHash, VoiceId)` exists in the database, the backend SHALL return the cached audio immediately without calling the Google Cloud TTS API.
2. If no record exists, the backend SHALL synthesize the audio via Google Cloud TTS:
   - If the narration text exceeds 4,500 bytes UTF-8 (to ensure safe compliance with Google Cloud TTS's single-request limit of 5,000 bytes), the backend SHALL automatically partition the text into sentence-bounded sub-chunks under 4,500 bytes.
   - The backend SHALL synthesize each sub-chunk in order and concatenate the resulting MP3 audio byte streams into a single seamless MP3 audio stream.
3. The backend SHALL store the complete synthesized MP3 audio and total character count in PostgreSQL (`DocumentChunkAudios`) and return the audio stream to the client.
4. Upon receiving the audio from the server, the reader client SHALL store it in browser IndexedDB under the same `(chunkId, voice, contentHash)` cache key so subsequent seeks, pauses, and replays on that browser are served locally with zero network latency.

When a slice's content changes such that its `ContentHash` changes, the existing database audio record SHALL be invalidated and new audio synthesized on next play.

#### Scenario: Database cache hit serves audio without calling Google Cloud API
- **WHEN** a reader requests narration for a slice whose `(DocumentChunkId, ContentHash, VoiceId)` audio is already cached in PostgreSQL
- **THEN** the server returns the cached audio in less than 50ms and makes zero requests to the Google Cloud TTS API.

#### Scenario: Long text exceeding 5,000 bytes is automatically partitioned and concatenated
- **WHEN** a reader requests narration for a slice whose plain-text script exceeds 4,500 UTF-8 bytes (such as a lengthy Vietnamese or English chapter)
- **THEN** the server partitions the text along sentence boundaries into sub-chunks strictly under 4,500 bytes, synthesizes each sub-chunk without encountering Google Cloud 400 Bad Request errors, concatenates the resulting MP3 streams, and returns a single unified audio payload.

#### Scenario: Database cache miss synthesizes, stores in PostgreSQL, and streams audio
- **WHEN** a reader requests narration for a slice not yet cached in PostgreSQL
- **THEN** the server synthesizes audio via Google Cloud TTS, writes the MP3 audio bytes to PostgreSQL, and returns the audio to the client.

#### Scenario: Client stores server-synthesized audio in IndexedDB
- **WHEN** client receives audio synthesized by Google Cloud TTS from the server
- **THEN** client stores the audio in browser IndexedDB so repeat listens in the session do not issue repeated HTTP requests.

#### Scenario: Slice content revision invalidates server cache
- **WHEN** a slice's markdown is re-formatted or edited, altering its `ContentHash`
- **THEN** the server does not serve stale audio from the prior hash and re-synthesizes audio under the new `ContentHash`.

#### Scenario: Production synthesis requests resolve to relative API endpoint without localhost fallback
- **WHEN** a user initiates Google Cloud narration on a production deployment where `NUXT_PUBLIC_API_BASE_URL` is configured as an empty string
- **THEN** the audio composable resolves the endpoint against relative origin (`/api/v1/library/chunks/.../audio`)
- **AND** zero connection refused errors or mixed-content protocol violations occur.

### Requirement: Multi-Threaded Synthesis Enabled on Reader Routes Only

On reader routes, the application SHALL be cross-origin isolated so on-device synthesis can use multi-threaded execution. This isolation SHALL be established across all deployment tiers, including Nginx proxy headers and Nuxt Nitro route response headers. This isolation SHALL be confined to reader routes and SHALL NOT be applied to the authentication route or other routes, so cross-origin sign-in — which depends on cross-window communication — continues to function. Cross-origin resources the reader legitimately needs (web fonts, document images) SHALL continue to load under the isolation policy.

When cross-origin isolation is active (`self.crossOriginIsolated === true`), the synthesis worker SHALL configure the CPU (WASM) execution backend to run across multiple threads, scaling the thread count to the machine's available logical cores (`navigator.hardwareConcurrency`), so that a many-core CPU without a usable GPU is fully utilized for synthesis rather than running on a single thread. The configured thread count SHALL be bounded by the reported hardware concurrency to avoid oversubscription. When cross-origin isolation or multi-threaded execution is unavailable, the worker SHALL clamp thread allocation to 1 without emitting console errors; thread configuration SHALL NOT change the produced audio.

#### Scenario: Reader route is cross-origin isolated
- **WHEN** a reader route (`/read/...`) is loaded
- **THEN** the page reports cross-origin isolation, enabling multi-threaded synthesis.

#### Scenario: Sign-in route is not isolated and OAuth still works
- **WHEN** the sign-in route is loaded and the user completes cross-origin sign-in
- **THEN** the page is not cross-origin isolated and sign-in completes successfully.

#### Scenario: Reader cross-origin assets still load
- **WHEN** a reader slice references cross-origin fonts or images
- **THEN** those resources still load and render under the reader's isolation policy.

#### Scenario: Many-core CPU uses multiple synthesis threads
- **WHEN** a reader route is cross-origin isolated on a machine with no usable GPU and multiple logical cores, and a user plays narration
- **THEN** the CPU synthesis backend runs across multiple threads scaled to the available cores rather than on a single thread.

#### Scenario: Single-threaded fallback still completes
- **WHEN** cross-origin isolation or multi-threaded execution is unavailable
- **THEN** synthesis still completes correctly on a single thread and produces the same audio.
