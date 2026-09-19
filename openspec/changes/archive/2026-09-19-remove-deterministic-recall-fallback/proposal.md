# Proposal: Remove Deterministic Active Recall Fallback and Enforce Explicit Error Reporting

## Why

In `CreateCardFromHighlightHandler.cs`, synthesizing active recall flashcards via a deterministic template when the AI model fails or times out masks underlying service failures and inserts synthetic "junk" cards into the user's database. Software engineers curate highlights to generate meaningful, high-quality spaced repetition cards; generating fallback cards without AI synthesis clutters their review decks with low-value template text that was never verified.

Engineers understand and accept that external AI services occasionally experience rate limits, latency spikes, or temporary provider outages. In these scenarios, the system must fail fast, reject database writes, and clearly surface the error to the user via toast notifications so they can retry when the service recovers, rather than silently persisting low-quality fallback cards.

## What Changes

- **Remove Deterministic Active Recall Fallback**:
  - Remove `GenerateFallbackRecallCard` and all fallback generation blocks from `CreateCardFromHighlightHandler.cs`.
  - When `_geminiAiService.SynthesizeActiveRecallCardAsync` returns a failure result or throws an exception, fail fast and return `Result<CreateCardFromHighlightResponse>.Failure(...)` with HTTP 400 Bad Request.
  - Strictly prevent creating or persisting `SpacedRepetitionCard` records in the database on AI synthesis failure, ensuring zero junk data in the database.
- **Explicit Frontend Error Handling**:
  - In `frontend/pages/notes.vue`, ensure the failure response from `POST /api/v1/review/cards/from-highlight` triggers the standard localized error toast (`notes.toast_flashcard_error`), explaining that flashcard generation failed.
  - Keep the highlight card's button in the active `⚡ Flashcard SM-2` state so the user can easily re-trigger generation when ready.
- **Automated Test Coverage**:
  - Update `SpacedRepetitionBridgeTests.cs` to assert that when AI synthesis fails, `CreateCardFromHighlightHandler` returns failure (`IsSuccess == false`, `Error.Code == "AiService.RecallFailed"`) and does not insert any card into `SpacedRepetitionCards`.
- **Zero Breaking Contract Changes**:
  - No changes to API endpoints, request/response DTOs, or database schema.

## Capabilities

### New Capabilities
<!-- None: uses existing capabilities -->

### Modified Capabilities
- `notes`: Update specification requirements and scenarios to specify that flashcard generation from highlights fails fast when AI service fails, reports errors explicitly to the user, and never persists synthetic fallback cards to the database.
