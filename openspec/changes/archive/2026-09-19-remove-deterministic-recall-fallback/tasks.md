# Tasks

## 1. Application (Fail-Fast AI Synthesis)

- [x] 1.1 Remove `GenerateFallbackRecallCard` and fallback catch/else blocks in `CreateCardFromHighlightHandler.cs` so that AI synthesis failures return `Result.Failure` without inserting or persisting cards to the database.
- [x] 1.2 Update `SpacedRepetitionBridgeTests.cs` to assert that `CreateCardFromHighlightHandler` returns failure (`IsSuccess == false`, `Error.Code == "AiService.RecallFailed"`) and does not persist any card when AI synthesis fails.

## 2. Frontend (Error Toast Notification)

- [x] 2.1 Verify `frontend/pages/notes.vue` error toast handling when `handleCreateFlashcard` fails, ensuring the button remains unconverted (`⚡ Flashcard SM-2`) for user retry.
- [x] 2.2 Add or update unit test in `frontend/tests/pages/notes.spec.ts` asserting that an API failure displays the error toast without transitioning the button into the disabled `In SM-2` state.

## 3. Verification & Testing

- [x] 3.1 Run full backend test suite (`dotnet test`) and verify 100% passing tests.
- [x] 3.2 Run full frontend test suite (`npm test`) and verify 100% passing tests.
