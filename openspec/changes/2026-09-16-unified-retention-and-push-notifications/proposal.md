# Proposal: Unified Knowledge Retention Loop & Customizable Web Push Notifications

## Title
Unified Knowledge Retention Loop & Customizable Web Push Notifications

## Context & Problem Statement

TechDaily has established itself as an intensive daily learning platform for senior software engineers, combining curated book slices, AI scenario challenges, interactive topic quizzes, and an SM-2 spaced repetition engine. However, the platform currently suffers from three critical architectural and experiential bottlenecks: accumulating technical debt from abandoned legacy components, disjointed learning retention loops, and high-friction, inflexible notification mechanisms.

```
┌───────────────────────────────────────────────────────────────────────────┐
│                          CURRENT DISJOINTED STATE                         │
│                                                                           │
│   [Reader: Highlights]      [Quiz: Mistakes]      [Review: SM-2 Deck]     │
│             │                      │                       │              │
│       Passive quotes         Trapped inside          Isolated topics      │
│     (No personal notes)    /quiz/review-queue        only (No quotes/     │
│             │                      │                  quiz failures)      │
│             ▼                      ▼                       │              │
│       Trapped in DB         Never scheduled                │              │
│     (No Obsidian export)    for spaced recall              ▼              │
│                                                   Rigid fixed 08:00 UTC   │
│                                                    Telegram notifications │
│                                                   (High-friction chat ID) │
└───────────────────────────────────────────────────────────────────────────┘
```

### 1. Technical Debt & Redundant Token Overhead
- **Orphaned Component:** When the reading interface transitioned to the Senior Trade-off Challenge architecture, `MicroQuizCard.vue` was removed from `DocReaderPane.vue` but remained orphaned inside `frontend/components/today/MicroQuizCard.vue`.
- **Empty Directory:** `frontend/components/reader/` exists as an empty vestigial directory in the frontend file tree.
- **Dead Schema & Domain Artifacts:** The `DocumentChunk` entity retains the `MicroQuiz` property typed as `MicroQuizVo`, which is mapped to four distinct database columns on the `DocumentChunks` table (`MicroQuiz_Question`, `MicroQuiz_Options`, `MicroQuiz_AnswerIndex`, `MicroQuiz_Explanation`).
- **Wasted AI Compute & Payload Bloat:** The backend curation handlers (`GetTodayFocusHandler`, `CurateSliceHandler`, `LookAheadBufferService`) and DTOs (`DailyFocusChunkDto.MicroQuiz`, `BookSliceDto.MicroQuiz`) continue populating and serializing this dead data. More critically, the slice curation prompt in `GeminiAiService.cs` asks Gemini to generate drill fields that are duplicated into `MicroQuizVo`, consuming unnecessary prompt/completion tokens on every slice ingestion.

### 2. Disconnected Learning Loops & Second-Brain Isolation
- **Passive Highlights Without Reflection:** When readers highlight text in `read/[bookId].vue`, they can only store verbatim quotes. The backend `CreateHighlightApiRequest` already accepts an optional `Note` parameter, but the frontend floating toolbar offers no UI to capture the engineer's personal reflections, mental models, or architectural trade-offs at the moment of reading.
- **Trapped Quiz Mistakes:** When an engineer answers a technical quiz question incorrectly on `/quiz`, the failed attempt is recorded in `UserQuizProgress` (`IsMastered = false`). However, these mistakes are siloed inside `/quiz/review-queue`. They never enter the platform's core Spaced Repetition (SM-2) engine on `/review`. Spaced repetition is the gold standard for long-term algorithmic and architectural retention, yet our SM-2 deck only supports predefined curriculum `Topic` cards.
- **Absence of PKM (Obsidian / Notion) Portability:** Senior engineers manage their personal knowledge in tools like Obsidian, Logseq, or Notion. Currently, notes and highlights saved in TechDaily are locked inside PostgreSQL. There is no automated mechanism to export a book's highlights, personal notes, and chapter summaries into a clean, markdown file formatted with structured YAML frontmatter.

### 3. Notification Inflexibility & Friction
- **Telegram Setup Barrier:** Daily study reminders and streak alerts are currently coupled exclusively to Telegram via `TelegramNotifier.cs`. To configure reminders, users must manually search for a bot, execute `/start`, retrieve their numerical Telegram Chat ID through third-party utility bots, and paste it into `/profile`. Over 85% of active users abandon this flow.
- **Ignorance of Personal Routines & Timezones:** Existing notification dispatches are hardcoded to fixed server times (08:00 and 20:00 UTC/server time). A developer in Hanoi (UTC+7) receives a "morning reminder" at 15:00 in the afternoon, while a developer in San Francisco (UTC-7) receives it at 01:00 AM.
- **Lack of Modern Web Push (VAPID):** The platform lacks native Web Push notifications (RFC 8291 / RFC 8292). Browsers across macOS, Windows, Linux, Android, and iOS (PWA) support 1-click native system push notifications that do not require third-party messaging apps.

---

## Proposed Solution

We propose a unified, three-part architecture to prune technical debt, unify the knowledge retention loop into a cohesive Second-Brain workflow, and implement customizable timezone-aware Web Push notifications.

```
┌───────────────────────────────────────────────────────────────────────────┐
│                           UNIFIED RETENTION LOOP                          │
│                                                                           │
│   [Reader: Selection] ──► Floating Popover ──► Personal Note + Quote      │
│            │                                           │                  │
│            ├─► 1-Click "Turn into SM-2 Card" ──────────┤                  │
│            │   (Gemini synthesizes recall Q/A)         ▼                  │
│            │                                   [SM-2 Spaced Deck]         │
│            └─► Obsidian Exporter (.md + YAML)          ▲                  │
│                                                        │                  │
│   [Quiz: Mistakes] ──► 1-Click "Push to SM-2 Deck" ────┘                  │
│                        (Failed Question + Explanation)                    │
│                                                                           │
│   [Web Push Notifications] ──► Timezone-Aware Background Worker           │
│                                (Personalized 15-min Evaluation Window)    │
└───────────────────────────────────────────────────────────────────────────┘
```

### Part 1: Housekeeping & Tech Debt Cleanup
1. **Frontend Hygiene:** Permanently delete `frontend/components/today/MicroQuizCard.vue` and prune the empty `frontend/components/reader/` directory.
2. **Domain & DTO Pruning:** Remove `MicroQuizVo` and the `MicroQuiz` navigation property from `DocumentChunk.cs`, `DailyFocusDtos.cs`, `LibraryDtos.cs`, and handler projections (`GetTodayFocusHandler`, `CurateSliceHandler`, `GetBookSliceHandler`, `ImportDocumentHandler`, `LookAheadBufferService`).
3. **Prompt & AI Optimization:** Update `GeminiAiService.cs` curation prompts to eliminate redundant micro-quiz instructions, saving Gemini Flash tokens on every document curation pass.
4. **Database Migration:** Create an EF Core migration (`RemoveMicroQuizFromDocumentChunks`) dropping `MicroQuiz_Question`, `MicroQuiz_Options`, `MicroQuiz_AnswerIndex`, and `MicroQuiz_Explanation` from the `DocumentChunks` table.

### Part 2: Layered Knowledge Retention ("Second Brain" Loop)
1. **In-Reader Note Popover:** Replace the single-click highlight button in `read/[bookId].vue` with a floating note popover. Readers can quickly save a quote or write an attached markdown reflection (`POST /api/v1/notes/highlights`).
2. **1-Click "Highlight to SM-2 Flashcard":** Add an endpoint `POST /api/v1/review/cards/from-highlight`. When invoked, Gemini analyzes the quote and surrounding chapter context to generate an active recall question and answer card, automatically scheduled into the user's SM-2 deck (`SpacedRepetitionCards`).
3. **1-Click "Quiz Mistake to SM-2 Deck":** Add an endpoint `POST /api/v1/review/cards/from-quiz-mistake`. On the quiz summary screen or mistake queue (`/quiz`), engineers can promote any failed question into their SM-2 deck with one click. When subsequently reviewed and passed in `/review`, the card updates `UserQuizProgress.IsMastered = true`, creating a bidirectional feedback loop.
4. **Obsidian / Markdown Vault Exporter:** Expose `GET /api/v1/library/books/{id}/export-markdown`. This compiles book metadata, chapter summaries, key takeaways, and user highlights/notes into a downloadable Markdown file featuring standard YAML frontmatter compatible with Obsidian, Logseq, and Notion.

### Part 3: Customizable Web Push Notifications & Timezone Scheduling
1. **User Profile Preferences:** Extend the `Users` entity with:
   - `PreferredStudyTime` (`TimeOnly?`, e.g. `07:30`)
   - `StreakAlertTime` (`TimeOnly?`, e.g. `21:00`)
   - `TimeZone` (`string`, e.g. `"Asia/Ho_Chi_Minh"`, default `"UTC"`)
   - `IsPushEnabled` (`bool`, default `false`)
2. **Push Subscription Entity & Endpoints:** Create the `UserPushSubscriptions` table storing endpoint URLs, VAPID `P256dh` public keys, and `Auth` secrets. Expose:
   - `GET /api/v1/notifications/push/vapid-public-key`
   - `POST /api/v1/notifications/push/subscribe`
   - `POST /api/v1/notifications/push/unsubscribe`
3. **Service Worker & Frontend Controls:**
   - Implement `frontend/public/sw.js` to handle `push` and `notificationclick` events, routing users directly to `/today` or `/review`.
   - Update `settings.vue` and `profile.vue` with 1-click browser push enablement, browser timezone auto-detection (`Intl.DateTimeFormat().resolvedOptions().timeZone`), and intuitive time pickers for morning study reminders and evening streak preservation alerts.
4. **Timezone-Aware Background Worker:** Implement `DailyPushNotificationWorker` executing every 15 minutes. It converts `DateTimeOffset.UtcNow` into each subscriber's local `TimeZone` and dispatches notifications when the local time matches the user's configured `PreferredStudyTime` or `StreakAlertTime`.

---

## Value & Impact

| Layer | Changes & Enhancements | Concrete Impact |
|---|---|---|
| **Frontend** | - Delete orphaned `MicroQuizCard.vue`<br/>- Delete empty `components/reader/`<br/>- Reader floating note popover<br/>- 1-click "Turn to SM-2" buttons<br/>- Obsidian export button on book details<br/>- Web Push toggle & time pickers in Settings/Profile<br/>- Service Worker (`sw.js`) | - Zero dead components in bundle<br/>- Active reflection replaces passive highlighting<br/>- Frictionless 1-click Web Push subscription replaces manual Telegram bot ID setup<br/>- High-value PKM data portability |
| **Backend** | - Prune `MicroQuiz` from domain, DTOs, and handlers<br/>- Optimize Gemini curation prompt<br/>- New review card factory endpoints (Highlight & Quiz Mistake)<br/>- Markdown exporter with YAML builder<br/>- Web Push VAPID cryptographic dispatch service<br/>- Timezone-aware 15-minute background worker | - Cleaner domain model and EF Core configuration<br/>- 15–20% token savings on Gemini document curation<br/>- Unified SM-2 deck supporting diverse learning inputs<br/>- Personalized, time-accurate notification delivery |
| **Database** | - Drop 4 legacy `MicroQuiz_*` columns on `DocumentChunks`<br/>- Add timezone & push preference columns on `Users`<br/>- Create `UserPushSubscriptions` table<br/>- Extend `SpacedRepetitionCards` with `SourceType`, `FrontMarkdown`, `BackMarkdown`, `SourceHighlightId`, `SourceQuizQuestionId` | - Schema normalization and database cleanup<br/>- Multi-device browser push support<br/>- Bidirectional synchronization between Spaced Repetition and Quiz mastery |
