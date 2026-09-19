# Design: Fail-Fast AI Highlight Flashcard Synthesis without Synthetic Fallbacks

## Context

In `CreateCardFromHighlightHandler.cs`, converting a saved highlight into a SuperMemo SM-2 spaced repetition card (`SpacedRepetitionCard`) relies on `_geminiAiService.SynthesizeActiveRecallCardAsync` to formulate technical question-and-answer pairs.

Previously, an automatic "deterministic active recall fallback" mechanism was considered to synthesize template cards when the AI model timed out, failed, or lacked configuration. In practice, this pattern swallows errors, hides AI operational failures, and pollutes the engineer's review deck with synthetic template boilerplate cards that were never reviewed or validated.

This design enforces clean fail-fast error handling: when AI synthesis fails, the operation immediately returns a failure result, rejects any database writes, and surfaces the error toast to the user.

## Goals / Non-Goals

**Goals:**
- Eliminate `GenerateFallbackRecallCard` and all fallback generation blocks from `CreateCardFromHighlightHandler.cs`.
- Return `Result<CreateCardFromHighlightResponse>.Failure(...)` immediately when `_geminiAiService.SynthesizeActiveRecallCardAsync` returns failure.
- Strictly guarantee that no `SpacedRepetitionCard` is inserted or saved in the database when AI synthesis fails.
- Verify that `frontend/pages/notes.vue` catches the failure, displays `notes.toast_flashcard_error`, and retains the active `⚡ Flashcard SM-2` button state for easy user retry.
- Update `SpacedRepetitionBridgeTests.cs` to verify that failure results do not persist records to the database.

**Non-Goals:**
- Altering the REST endpoint signature or DTOs of `POST /api/v1/review/cards/from-highlight`.
- Modifying the underlying SM-2 spaced repetition domain algorithm or entity structure.
- Changing the Gemini model configuration or prompt design.

## Decisions

### Decision 1: Strict Fail-Fast in `CreateCardFromHighlightHandler`
In `CreateCardFromHighlightHandler.cs`:
1. Call `_geminiAiService.SynthesizeActiveRecallCardAsync`.
2. If `!cardResult.IsSuccess` or if `front`/`back` are null/whitespace:
   - Log a warning with the failure reason: `_logger?.LogWarning("AI recall synthesis failed: {Error} for highlight {HighlightId}.", cardResult.Error?.Message ?? "Unknown", highlight.Id);`.
   - Return `Result<CreateCardFromHighlightResponse>.Failure(cardResult.Error ?? Error.Custom("AiService.RecallFailed", "Active recall synthesis failed."));`.
3. Do NOT instantiate `SpacedRepetitionCard.CreateFromHighlight` and do NOT invoke `_dbContext.SaveChangesAsync`.
*Rationale*: Guarantees zero junk records in the database and ensures full transparency when external AI services experience transient errors.

### Decision 2: Frontend Error Feedback and Retry Readiness
In `frontend/pages/notes.vue`:
1. The `handleCreateFlashcard` method wraps the API call in `try / catch / finally`.
2. When the backend returns HTTP 400 Bad Request, the catch block catches the error, maps it through `formatError(err, 'notes.toast_flashcard_error')`, and presents the red error toast.
3. `createdCardHighlightIds` is not modified, so the button remains in the active `⚡ Flashcard SM-2` state with full user-retry capability.
*Rationale*: Gives clear feedback to the user while keeping the action button accessible once the service is available.

## Risks / Trade-offs

- **User experience during outages**: Users cannot create flashcards during third-party AI downtime or quota exhaustion. This tradeoff is explicitly desired: users prefer a clear error notification and retrying later over having synthetic boilerplate cards in their review queue.
