# audio-narration Spec Delta

## MODIFIED Requirements

### Requirement: Free-Tier Voice Selection and Polished Presentation

When the Google Cloud engine is active, the reader audio player SHALL provide a voice selector restricted to Google Cloud Free-Tier voices (Neural2 and WaveNet tiers) for the slice's content language, supporting at least one female and one male voice for Vietnamese (`vi-VN`) and English (`en-US`). Voice selection SHALL NOT be shown when the On-Device engine is active.

The system (both frontend client and backend synthesis handler) SHALL strictly enforce language compatibility between the document slice's content language and the selected voice model:
1. Slices in English (`en`) SHALL only be synthesized with English voice models (`en-US-*`).
2. Slices in Vietnamese (`vi`) SHALL only be synthesized with Vietnamese voice models (`vi-VN-*`).
3. The system SHALL NOT synthesize English documentation with a Vietnamese voice model or Vietnamese documentation with an English voice model, even if a user previously selected a voice in another language.
4. Voice preference persistence in client storage SHALL be partitioned by language (e.g. separate storage keys for `en` and `vi`), ensuring that selecting a preferred voice in one language does not overwrite or corrupt the voice selection when reading documents in another language.
5. If the client submits a voice ID that does not match the slice's content language, the backend SHALL reject the request with an RFC 7807 validation error (`VOICE_LANGUAGE_MISMATCH`).

#### Scenario: Switching to Cloud engine on English slice selects English voice
- **WHEN** a user who previously selected a Vietnamese voice (`vi-VN-Neural2-A`) on a Vietnamese book switches to Cloud mode on an English book slice
- **THEN** the player resolves and uses an English voice (`en-US-Neural2-F`), never the persisted Vietnamese voice.

#### Scenario: Voice preferences persist independently per language
- **WHEN** a user selects a male voice (`en-US-Neural2-D`) for English documents and later selects a female voice (`vi-VN-Neural2-A`) for Vietnamese documents
- **THEN** returning to an English document restores the male English voice (`en-US-Neural2-D`), while opening a Vietnamese document uses the female Vietnamese voice (`vi-VN-Neural2-A`).

#### Scenario: Backend rejects voice ID conflicting with chunk language
- **WHEN** an API request arrives to synthesize an English document chunk with a `vi-VN-*` voice ID
- **THEN** the backend responds with HTTP 400 Bad Request and error code `VOICE_LANGUAGE_MISMATCH`.

#### Scenario: Free-tier voice selection for Vietnamese slice
- **WHEN** a user views a Vietnamese slice with Google Cloud engine active
- **THEN** the player provides a choice between curated Vietnamese Neural2 female (`vi-VN-Neural2-A`) and male (`vi-VN-Neural2-D`) voices.

#### Scenario: Free-tier voice selection for English slice
- **WHEN** a user views an English slice with Google Cloud engine active
- **THEN** the player provides a choice between curated English Neural2 female (`en-US-Neural2-F`) and male (`en-US-Neural2-D`) voices.

#### Scenario: UI copy and icons avoid buzzwords
- **WHEN** viewing the audio narration player in any state
- **THEN** the controls render clean labels ("Google Cloud", "Thiết bị" / "Device") without "AI" prefixes, and no lightning bolt icons are rendered in the audio control.
