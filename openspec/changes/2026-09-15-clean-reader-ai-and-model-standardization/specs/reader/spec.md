# Reader Capability Delta Specification

## MODIFIED Requirements

### Requirement: Two-Tier Semantic Cache for Floating Term Explainer
The floating AI term explainer SHALL query a two-tier cache before invoking Google Gemini API: first exact string match (`Term == normalizedTerm`), then pgvector cosine distance on `TermExplanationCaches.Embedding` (distance `<= 0.08` representing `>= 92%` similarity within the same category and locale). In the UI, the explainer modal SHALL accurately reflect "Powered by Google Gemini" and display a discreet `⚡ Instant Cache` badge when served from the semantic cache.

#### Scenario: Exact cache hit (Tier 1)
- **WHEN** user requests explanation for a term that exists verbatim in `TermExplanationCaches` for the current locale
- **THEN** system immediately returns the cached explanation within <5ms, increments `HitCount`, and bypasses both the vector search and external LLM.

#### Scenario: Semantic vector cache hit (Tier 2)
- **WHEN** exact match misses, but the generated embedding of the selected term has a cosine distance `<= 0.08` (similarity `>= 92%`) with an existing cached term in the same category and locale
- **THEN** system returns the semantically equivalent cached explanation within <150ms, increments `HitCount`, and avoids calling Google Gemini completion.

#### Scenario: Full cache miss fallback
- **WHEN** neither exact nor semantic cache match is found
- **THEN** system invokes Google Gemini to generate a fresh explanation, generates the term's embedding vector, persists the new entry into `TermExplanationCaches` with its vector, and returns the explanation.

#### Scenario: Accurate AI branding displayed
- **WHEN** user opens the term explainer modal
- **THEN** the modal footer displays "Powered by Google Gemini" and the loading state indicates analysis with Google Gemini, with zero references to non-existent models.

### Requirement: Multi-Layer Anti-Spam & Rate Limiting Defense
All AI generation endpoints (`/explain-term`, `/quiz/generate`) SHALL enforce multi-layer anti-spam protection across reverse proxy, API rate limiting, and client-side UI guarding.

#### Scenario: User exceeds rate limit threshold
- **WHEN** a client submits more than 10 AI requests within a rolling 60-second window
- **THEN** the system rejects subsequent requests with `HTTP 429 Too Many Requests` problem details containing a `Retry-After` header without invoking external Gemini services.

#### Scenario: Rapid multi-click prevention in reader UI
- **WHEN** user triggers generation while an existing request is pending
- **THEN** the frontend locks the action with an active mutex, disables buttons with loading spinners, and ignores subsequent clicks or submissions until completed.

#### Scenario: Request input bounds validation
- **WHEN** a user submits an explanation or quiz generation request exceeding character bounds (e.g. term > 500 chars or topic > 100 chars)
- **THEN** the request fails validation immediately with `HTTP 400 Bad Request`, preventing prompt injection and excessive token consumption.

## REMOVED Requirements

### Requirement: In-Context "Ask AI About This Book" (RAG)
**Reason**: The slide-over conversational chatbot drawer introduced unwanted UI clutter and distraction into the reading view, deviating from TechDaily's focused, minimalist reading philosophy.
**Migration**: Removed `AskBookDrawer.vue` and `POST /api/v1/library/books/{id}/ask`. Users consume technical literature via immersive chapter slices, floating term explanations, and grounded quizzes.
