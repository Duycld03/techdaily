# Delta Specification: Reader (Semantic Term Cache, In-Book RAG Chat & Anti-Spam)

## ADDED Requirements

### Requirement: Two-Tier Semantic Cache for AI Term Explainer
The `TermExplanationService` SHALL evaluate term lookup requests using a two-tier caching strategy combining exact string matching and pgvector approximate nearest neighbor search against `TermExplanationCaches`.

#### Scenario: Exact match cache hit (Tier 1)
- **WHEN** user selects a technical term whose normalized text matches an existing cache entry (`t.Term == normalizedTerm && t.Locale == locale`)
- **THEN** system immediately returns the cached explanation within <5ms, increments `HitCount`, and bypasses both the vector search and external LLM.

#### Scenario: Semantic vector cache hit (Tier 2)
- **WHEN** exact match misses, but the generated embedding of the selected term has a cosine distance `<= 0.08` (similarity `>= 92%`) with an existing cached term in the same category and locale
- **THEN** system returns the semantically equivalent cached explanation within <150ms, increments `HitCount`, and avoids calling Gemini Flash completion.

#### Scenario: Full cache miss fallback
- **WHEN** neither exact nor semantic cache match is found
- **THEN** system invokes Gemini Flash to generate a fresh explanation, generates the term's embedding vector, persists the new entry into `TermExplanationCaches` with its vector, and returns the explanation.

---

### Requirement: In-Context "Ask AI About This Book" (RAG)
The reader SHALL provide an interactive slide-over chat drawer allowing readers to ask technical questions grounded exclusively in the active document book.

#### Scenario: User asks a question about the active book
- **WHEN** user submits a technical query in the reader's Ask Book drawer
- **THEN** backend vectorizes the question, retrieves the top 3 most relevant `DocumentChunk` records from the current book (`DocumentBookId == id`) ordered by cosine distance, and synthesizes a grounded answer using `gemini-3.5-flash-lite`.

#### Scenario: Grounded citations and source jumping
- **WHEN** the AI response is delivered to the reader
- **THEN** response includes citation metadata (`chunkOrder`, `chapterTitle`, `relevanceScore`), rendered in the UI as clickable badges. Clicking a badge immediately navigates the reader to that specific chapter slice and highlights the excerpt.

#### Scenario: Information not present in book
- **WHEN** user asks a question whose answer is not covered in the book's slices
- **THEN** the AI explicitly states that the active document does not cover the requested topic, preventing speculative or out-of-context hallucinations.

---

### Requirement: Multi-Layer Anti-Spam & Rate Limiting Defense
All AI generation and RAG endpoints (`/ask`, `/explain-term`, `/quiz/generate`) SHALL enforce multi-layer anti-spam protection across reverse proxy, API rate limiting, and client-side UI guarding.

#### Scenario: User exceeds rate limit threshold
- **WHEN** a client submits more than 10 AI requests within a rolling 60-second window
- **THEN** the system rejects subsequent requests with `HTTP 429 Too Many Requests` problem details containing a `Retry-After` header without invoking external Gemini services.

#### Scenario: Rapid multi-click prevention in reader UI
- **WHEN** user clicks "Ask Book" or triggers generation while an existing request is pending
- **THEN** the frontend locks the action with an active mutex (`isAsking = true`), disables buttons with loading spinners, and ignores subsequent clicks or keyboard submissions until completed.

#### Scenario: Request input bounds validation
- **WHEN** a user submits an Ask Book question exceeding 300 characters or under 3 characters
- **THEN** the request fails validation immediately with `HTTP 400 Bad Request`, preventing prompt injection and excessive token consumption.
