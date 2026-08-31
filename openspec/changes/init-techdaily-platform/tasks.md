# Implementation Tasks: Initialize TechDaily Platform

## Phase 1: Database & Solution Scaffolding
- [x] 1.1 Initialize .NET 10 Clean Architecture solution (`TechDaily.sln`, `Domain`, `Application`, `Infrastructure`, `Api`, `Tests`).
- [x] 1.2 Configure PostgreSQL 17 DbContext with `pgvector` extension, JSONB mappings (`ToJson()`), `TermExplanationCaches`, and initial EF Core migration.
- [x] 1.3 Seed 30-day curriculum topics, questions, and sample document chunks.

## Phase 2: Core Domain & Application Handlers
- [x] 2.1 Implement Rich Domain entities with encapsulated logic: `SpacedRepetitionCard` (SM-2 algorithm), `StreakRecord` (Streak/Freeze invariants), `DailyDrill`, `AiReview`, `DocumentBook`, `DocumentChunk`, `Topic`, `InterviewQuestion`, `UserHighlight`, `TermExplanationCache`.
- [ ] 2.2 Implement Plain Use-Case Handlers (Pure DI): `GetTodayFocusHandler`, `SubmitDailyDrillHandler`, `ExplainTermHandler`, `GradeReviewCardHandler`, `ImportDocumentHandler`.
- [ ] 2.3 Implement FluentValidation validators and Result Pattern error responses.

## Phase 3: Infrastructure & Integrations
- [ ] 3.1 Implement Gemini Flash AI Service with 1-pass Multimodal audio/text evaluation and strict JSON Schema output.
- [ ] 3.2 Implement Term Explainer Service with DB caching (`TermExplanationCaches`).
- [ ] 3.3 Implement Local Audio Storage Service for saving voice submissions (`/storage/audios/`).
- [ ] 3.4 Implement AI Document Chunking Service with pgvector embeddings for URL / Markdown imports.
- [ ] 3.5 Implement BackgroundService / Quartz.NET dispatcher for 08:00 AM & 20:00 PM Telegram alerts.

## Phase 4: Web API Endpoints & Middleware
- [ ] 4.1 Implement `DailyFocusController` / Endpoints (`GET /today`, `POST /submit`, `POST /explain-term`).
- [ ] 4.2 Implement `LibraryController` / Endpoints (`GET /books`, `POST /import`).
- [ ] 4.3 Implement `ReviewController` / Endpoints (`GET /deck`, `POST /grade`).
- [ ] 4.4 Implement `NotesController` / Endpoints (`GET /highlights`, `POST /highlights`).
- [ ] 4.5 Configure `IExceptionHandler` for RFC 7807 ProblemDetails and CORS.

## Phase 5: Nuxt 4 Frontend Portals
- [ ] 5.1 Initialize Nuxt 4 project with Tailwind CSS, Pinia, Shiki, Lucide icons, `@nuxtjs/i18n` (en/vi locale resources), and `@nuxtjs/color-mode` (Dark/Light).
- [ ] 5.2 Build `/today` Dual-Pane Focus Hub (Desktop: Doc Reader & CodeMirror 6 Markdown Editor / Audio Recorder; Mobile: Tab view) with dynamic dark/light Shiki theme.
- [ ] 5.3 Build Floating UI Inline AI Explainer tooltip and Highlight popover with language locale selector.
- [ ] 5.4 Build `/library` Book Cards and Import Document modal (preserving source language).
- [ ] 5.5 Build `/review` Spaced Repetition Flashcard deck and `/notes` repository.
- [ ] 5.6 Build Streak Heatmap & Score Analytics dashboard with Dark Mode support.

## Phase 6: Verification & Integration Testing
- [ ] 6.1 Domain unit tests for SM-2 calculation and Streak freeze invariants.
- [ ] 6.2 Application integration tests for synchronous AI submission handler and idempotent daily drill creation.
- [ ] 6.3 End-to-end smoke test for daily flow on web frontend.
