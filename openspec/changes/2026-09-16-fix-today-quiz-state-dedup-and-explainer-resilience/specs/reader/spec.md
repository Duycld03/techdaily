# Reader Capability Delta Specification

## Purpose
Defines delta requirements for reading highlight deduplication, database term length clamping, non-blocking cache resilience in `TermExplanationService`, and dedicated error UI with retry action in `TermExplainerModal.vue`.

---

## MODIFIED Requirements

### Requirement: Two-Tier Semantic Cache for AI Term Explainer
The `TermExplanationService` SHALL evaluate term lookup requests using a two-tier caching strategy combining exact string matching and pgvector approximate nearest neighbor search against `TermExplanationCaches`. The service SHALL enforce strict database column length safety by clamping normalized term strings to a maximum of 200 characters (`varchar(200)`) prior to cache querying, embedding generation, or database persistence. Auxiliary cache write operations SHALL execute within a non-blocking fault-tolerant boundary such that database persistence failures do not abort or discard the primary AI-generated term explanation.

#### Scenario: Exact match cache hit (Tier 1)
- **WHEN** user selects a technical term whose normalized text matches an existing cache entry (`t.Term == normalizedTerm && t.Locale == locale`)
- **THEN** system immediately returns the cached explanation within <5ms, increments `HitCount`, and bypasses both the vector search and external LLM.

#### Scenario: Semantic vector cache hit (Tier 2)
- **WHEN** exact match misses, but the generated embedding of the selected term has a cosine distance `<= 0.08` (similarity `>= 92%`) with an existing cached term in the same category and locale
- **THEN** system returns the semantically equivalent cached explanation within <150ms, increments `HitCount`, and avoids calling Gemini Flash completion.

#### Scenario: Full cache miss fallback
- **WHEN** neither exact nor semantic cache match is found
- **THEN** system invokes Gemini Flash to generate a fresh explanation, generates the term's embedding vector, persists the new entry into `TermExplanationCaches` with its vector, and returns the explanation.

#### Scenario: Term length clamping for safe cache persistence
- **WHEN** a user highlights a multi-line technical sentence or phrase exceeding 200 characters (e.g. 237 characters)
- **THEN** `TermExplanationService` clamps the normalized term to 200 characters (`normalizedTerm[..200]`) before checking the cache, generating embeddings, or inserting into `TermExplanationCaches`
- **AND** the operation completes successfully without throwing a PostgreSQL `22001 (value too long for type character varying(200))` exception.

#### Scenario: Non-blocking resilience during cache persistence failure
- **WHEN** Google Gemini generates a valid explanation but secondary database persistence to `TermExplanationCaches` fails due to a transient database lock, unique constraint collision, or timeout
- **THEN** `TermExplanationService` logs a warning without throwing an unhandled exception
- **AND** returns the valid LLM explanation to the client with `IsFromCache = false`.

---

## ADDED Requirements

### Requirement: Reading Highlight Deduplication and Note Upsert
The highlight creation endpoint (`POST /api/v1/notes/highlights`) SHALL perform an idempotent query against `UserHighlights` using `(UserId, DocumentChunkId, SelectedText.Trim())` before persisting new records. If an identical text selection already exists for the user in the specified document chunk, the system SHALL update existing reflection notes and tags if provided and return the existing highlight entity rather than creating redundant database rows.

#### Scenario: User highlights previously highlighted text
- **WHEN** an authenticated user selects text in `/read/[bookId]` or `/today` that was previously saved as a highlight in the same chunk
- **THEN** `CreateHighlightHandler` detects the existing highlight record
- **AND** returns `HTTP 200 OK` with the existing `HighlightId` without inserting a new database row.

#### Scenario: User attaches note to existing highlight
- **WHEN** user opens the note popover on previously highlighted text, types a personal reflection note, and clicks `Save Note`
- **THEN** the handler updates `UserHighlight.Note` and `UserHighlight.UpdatedAt` on the existing entity and returns the updated DTO.

#### Scenario: Flashcard creation reuses deduplicated highlight
- **WHEN** user initiates "Turn into Flashcard" from a text selection that already exists as a highlight
- **THEN** the client receives the stable existing `HighlightId`
- **AND** dispatches `POST /api/v1/review/cards/from-highlight` with the existing ID, allowing card idempotency checks to prevent duplicate flashcards.

---

### Requirement: Resilient Term Explainer Error Handling and Retry Action
The `TermExplainerModal` component SHALL maintain a distinct error state (`errorMessage`) completely separated from the Markdown explanation content. When an explanation request fails due to network or server issues, the modal SHALL render a dedicated error banner with a localized message and a "Retry" button rather than rendering raw error strings inside the Markdown prose box.

#### Scenario: Server error displays dedicated error banner with retry button
- **WHEN** an error occurs during term explanation (such as HTTP 500 or network failure)
- **THEN** `TermExplainerModal` renders an error alert banner with an `AlertCircle` icon, localized error description, and a `Retry` action button
- **AND** does not render raw error strings inside the Markdown explanation container.

#### Scenario: Successful retry clears error banner and renders markdown explanation
- **WHEN** user clicks the `Retry` button in the error alert banner
- **THEN** the modal re-invokes `loadExplanation()`, shows the loading spinner, and upon success clears the error state and renders the Markdown explanation.

#### Scenario: Copy button disabled or hidden during error state
- **WHEN** the modal is displaying an error state
- **THEN** the "Copy" action button in the footer is disabled or hidden to prevent copying error messages to the clipboard.
