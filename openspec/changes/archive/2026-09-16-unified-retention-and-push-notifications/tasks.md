# Tasks: Unified Knowledge Retention Loop & Customizable Web Push Notifications

## Phase 1: Tech Debt & Legacy Pruning

- [x] 1. Frontend Component & Directory Hygiene
  - [x] 1.1 Delete orphaned component `frontend/components/today/MicroQuizCard.vue`.
  - [x] 1.2 Remove empty directory `frontend/components/reader/`.
  - [x] 1.3 Prune obsolete `MicroQuiz` type definition and state from `frontend/stores/useDailyFocusStore.ts`.

- [x] 2. Backend Domain & DTO Pruning
  - [x] 2.1 Remove `MicroQuizVo` value object and delete `backend/src/TechDaily.Domain/ValueObjects/MicroQuizVo.cs`.
  - [x] 2.2 Remove `MicroQuiz` property from `TechDaily.Domain.Entities.DocumentChunk`.
  - [x] 2.3 Remove `MicroQuiz` property from DTOs:
    - `DailyFocusChunkDto` in `backend/src/TechDaily.Application/Features/DailyFocus/DTOs/DailyFocusDtos.cs`.
    - `BookSliceDto` in `backend/src/TechDaily.Application/Features/Library/DTOs/LibraryDtos.cs`.
  - [x] 2.4 Update backend handlers to remove `MicroQuiz` mapping:
    - `GetTodayFocusHandler.cs`
    - `CurateSliceHandler.cs`
    - `GetBookSliceHandler.cs`
    - `ImportDocumentHandler.cs`
    - `LookAheadBufferService.cs`

- [x] 3. Gemini Prompt Optimization & Database Migration
  - [x] 3.1 Update curation prompt in `backend/src/TechDaily.Infrastructure/Services/GeminiAiService.cs` to eliminate micro-quiz instructions, saving input/output prompt tokens on every document curation pass.
  - [x] 3.2 Update `backend/src/TechDaily.Infrastructure/Persistence/Configurations/EntityConfigurations.cs` to remove `builder.OwnsOne(c => c.MicroQuiz, ...)`.
  - [x] 3.3 Create EF Core migration `PruneMicroQuizFromDocumentChunks` to drop columns: `MicroQuiz_Question`, `MicroQuiz_Options`, `MicroQuiz_AnswerIndex`, `MicroQuiz_Explanation` from `DocumentChunks`.

---

## Phase 2: Layered Notes & Second Brain (Obsidian Exporter)

- [x] 4. Reader In-Context Note Popover
  - [x] 4.1 Enhance floating text selection toolbar in `frontend/pages/read/[bookId].vue`:
    - Add "Add Note" button (`📝 Add Note`) alongside "Explain with Gemini" and "Copy".
    - Implement expandable popover containing a multi-line reflection textarea, tag input, "Save Note" button, and "Cancel" button.
  - [x] 4.2 Connect note submission to `notesStore.createHighlight({ documentChunkId, selectedText, note, tags })`.
  - [x] 4.3 Add i18n translation keys in `en.json` and `vi.json` for note creation, placeholders, and confirmation toasts.

- [x] 5. Obsidian / Markdown Exporter
  - [x] 5.1 Implement `ExportBookMarkdownHandler` in `backend/src/TechDaily.Application/Features/Library/ExportBookMarkdown/`:
    - Fetch `DocumentBook` including all `DocumentChunks` and the authenticated user's `UserHighlights`.
    - Construct YAML frontmatter (title, author, source URL, export timestamp, chapter count, highlight count, tags).
    - Format chapter sections with summaries, key takeaways, and blockquoted highlights with attached personal notes.
  - [x] 5.2 Expose endpoint `GET /api/v1/library/books/{id}/export-markdown` in `LibraryEndpoints.cs` returning `Content-Disposition: attachment; filename="{slug}-notes.md"`.
  - [x] 5.3 Add "Export to Obsidian / Markdown" action button in `frontend/pages/read/[bookId].vue` header drawer and `frontend/pages/library.vue` book detail modal.
  - [x] 5.4 Update `frontend/pages/notes.vue` to display attached personal notes prominently with tag chips and search filtering.

---

## Phase 3: Spaced Repetition Bridge

- [x] 6. Generalize Spaced Repetition Domain Model
  - [x] 6.1 Add `CardSourceType` enum (`Topic`, `Highlight`, `QuizMistake`) in `TechDaily.Domain.Enums`.
  - [x] 6.2 Update `TechDaily.Domain.Entities.SpacedRepetitionCard`:
    - Make `TopicId` nullable (`Guid? TopicId`).
    - Add `SourceType`, `FrontMarkdown`, `BackMarkdown`, `SourceHighlightId`, and `SourceQuizQuestionId`.
    - Add factory methods `CreateFromHighlight` and `CreateFromQuizMistake`.
  - [x] 6.3 Update `SpacedRepetitionCardConfiguration` in EF Core and create database migration `GeneralizeSpacedRepetitionCards`.

- [x] 7. Highlight to SM-2 Active Recall Generator
  - [x] 7.1 Add `SynthesizeActiveRecallCardAsync` to `IGeminiAiService` and implement in `GeminiAiService.cs`:
    - Prompt Gemini to generate a conceptual recall question (`Front`) and authoritative architectural explanation (`Back`) from quote and personal note.
  - [x] 7.2 Implement `CreateCardFromHighlightHandler` in `backend/src/TechDaily.Application/Features/Review/CreateCardFromHighlight/`.
  - [x] 7.3 Expose endpoint `POST /api/v1/review/cards/from-highlight` in `ReviewEndpoints.cs`.
  - [x] 7.4 Add "Turn into Flashcard" 1-click action in reader note popover and notes view cards.

- [x] 8. Quiz Mistake to SM-2 Conversion & Mastery Synchronization
  - [x] 8.1 Implement `CreateCardFromQuizMistakeHandler` in `backend/src/TechDaily.Application/Features/Review/CreateCardFromQuizMistake/`:
    - Convert failed `QuizQuestion` into a spaced repetition card with question prompt and correct option/explanation.
  - [x] 8.2 Expose endpoint `POST /api/v1/review/cards/from-quiz-mistake` in `ReviewEndpoints.cs`.
  - [x] 8.3 Update `GradeReviewCardHandler.cs`: when a user grades a card with `qualityGrade >= 3`, inspect `SourceQuizQuestionId` and mark `UserQuizProgress.IsMastered = true`.
  - [x] 8.4 Update `/quiz` summary view and mistake review queue in `frontend/pages/quiz.vue` to display a 1-click "Push to SM-2 Deck" button on failed questions.
---

## Phase 4: Web Push & Timezone Scheduling

- [x] 9. User Profile Timezone & Preferences
  - [x] 9.1 Update `TechDaily.Domain.Entities.User` with `PreferredStudyTime`, `StreakAlertTime`, `TimeZone`, and `IsPushEnabled`.
  - [x] 9.2 Update `UserConfiguration` in EF Core and create migration `AddUserPushAndSchedulePreferences`.
  - [x] 9.3 Update `UserProfileDto` and `UpdateUserProfileHandler` in `UserEndpoints.cs` to handle the new fields.

- [x] 10. Web Push Subscription Entity & Service
  - [x] 10.1 Create `UserPushSubscription` entity in `TechDaily.Domain.Entities`.
  - [x] 10.2 Configure entity in `EntityConfigurations.cs` and create migration `AddUserPushSubscriptionsTable`.
  - [x] 10.3 Implement `IWebPushService` and `WebPushService.cs` in `TechDaily.Infrastructure/Services/`:
    - Configure VAPID subject, public key, and private key from `appsettings.json`.
    - Implement payload encryption and HTTP dispatch to browser push services (FCM, Mozilla, Apple).
  - [x] 10.4 Expose push endpoints in `NotificationEndpoints.cs`:
    - `GET /api/v1/notifications/push/vapid-public-key`
    - `POST /api/v1/notifications/push/subscribe`
    - `POST /api/v1/notifications/push/unsubscribe`

- [x] 11. Frontend Service Worker & Settings UI
  - [x] 11.1 Create `frontend/public/sw.js` handling `push` and `notificationclick` events with graceful fallback and deep linking to `/today`.
  - [x] 11.2 Create `frontend/composables/useWebPush.ts` handling permission requests, service worker registration, and VAPID subscription exchange.
  - [x] 11.3 Update `frontend/pages/settings.vue` and `frontend/pages/profile.vue`:
    - Add Web Push toggle switch.
    - Add timezone selector pre-populated with browser's auto-detected timezone (`Intl.DateTimeFormat().resolvedOptions().timeZone`).
    - Add time pickers for Preferred Study Time and Streak Alert Time.
    - Wire save action to update profile and push subscription.

- [x] 12. Timezone-Aware Background Dispatch Worker
  - [x] 12.1 Implement `DailyPushNotificationWorker.cs` in `backend/src/TechDaily.Infrastructure/Workers/` running every 15 minutes using `PeriodicTimer`.
  - [x] 12.2 For each active push subscriber, convert `UtcNow` to local `TimeZoneInfo` and verify study reminder / streak alert time matches.
  - [x] 12.3 Implement deduplication logic preventing multiple dispatches within the same day/window.
  - [x] 12.4 Add automatic cleanup of revoked/stale endpoints receiving HTTP 404/410 from push services.
  - [x] 12.5 Register `DailyPushNotificationWorker` as a hosted service in `TechDaily.Infrastructure.DependencyInjection`.

---

## Phase 5: Verification & Automated Tests

- [x] 13. Unit & Integration Testing
  - [x] 13.1 Backend Tests:
    - Test `CreateHighlightHandler` verifies note and tags persistence.
    - Test `CreateCardFromHighlightHandler` verifies Gemini active recall synthesis and card creation.
    - Test `CreateCardFromQuizMistakeHandler` verifies card conversion and `GradeReviewCardHandler` mastery sync.
    - Test `ExportBookMarkdownHandler` verifies YAML frontmatter formatting and markdown structure.
    - Test `DailyPushNotificationWorker` with simulated timezones (UTC+7, UTC-5, UTC+0) and verify window matching.
  - [x] 13.2 Frontend Tests:
    - Test text selection note popover in reader component.
    - Test 1-click "Push to SM-2" button interaction on quiz mistake card.
    - Test `useWebPush` subscription flow and permission state handling.
  - [x] 13.3 End-to-End Verification:
    - Run full test suite (`dotnet test` and `npm test`).
    - Verify clean compilation with zero warnings on both backend and frontend.
