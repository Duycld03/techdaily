# Vector Embeddings Capability Delta Specification

## ADDED Requirements

### Requirement: Cache Hygiene for Term Explanation Service
The `TermExplanationService` SHALL guarantee database cache hygiene by strictly reserving persistence into `TermExplanationCaches` for verified, non-empty responses successfully returned by the Google Gemini API. Local fallback explanations generated on API failure or missing credentials MUST NEVER be persisted to the database. Additionally, legacy database entries containing mock or fallback placeholder text SHALL be purged.

#### Scenario: Successful Gemini LLM generation persists to cache
- **WHEN** a term explanation request results in a cache miss and Gemini returns a valid, non-empty explanation string
- **THEN** the service generates the term vector embedding via `IEmbeddingService`, stores the record in `TermExplanationCaches` with `HitCount = 1`, and returns the explanation with `isFromCache = false`.

#### Scenario: Gemini API failure or missing key bypasses database cache
- **WHEN** Gemini API returns an HTTP error, throws a network exception, or when `Gemini:ApiKey` is unconfigured
- **THEN** the service returns a localized fallback explanation from `GetFallbackExplanation(term, category, locale)` with `isFromCache = false`, and MUST NOT create or persist any record in `TermExplanationCaches`.

#### Scenario: Poisoned legacy cache entries purged from database
- **WHEN** the cache hygiene cleanup routine or database migration executes
- **THEN** all rows in `TermExplanationCaches` whose `ExplanationText` matches fallback placeholder patterns (e.g., matching `"represents a core runtime or architectural mechanism"` or `"Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại"`) are permanently deleted.

#### Scenario: Subsequent requests for previously failed terms attempt fresh generation
- **WHEN** a user requests an explanation for a term that previously triggered a fallback after Gemini connectivity is restored
- **THEN** because no poisoned cache record was persisted, the service attempts a fresh call to Gemini API rather than returning a cached fallback.
