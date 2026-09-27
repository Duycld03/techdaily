# Spec Delta: audio-narration

## MODIFIED Requirements

### Requirement: Google Cloud Narration Synthesis with Database Caching

The system SHALL support server-side audio narration synthesis via Google Cloud Text-to-Speech (Cloud TTS) as the primary high-speed narration engine. All calls to Google Cloud TTS SHALL be proxied through the ASP.NET Core backend using a server-side API key (`Google__TtsApiKey`); client applications SHALL NOT be exposed to external API credentials.

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
