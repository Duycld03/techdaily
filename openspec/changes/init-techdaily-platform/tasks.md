# Implementation Tasks: Initialize TechDaily Platform

## Phase 1: Database & Solution Scaffolding
- [x] 1.1 Initialize .NET 10 Clean Architecture solution (`TechDaily.sln`, `Domain`, `Application`, `Infrastructure`, `Api`, `Tests`).
- [x] 1.2 Configure PostgreSQL 17 DbContext with `pgvector` extension, JSONB mappings (`ToJson()`), `TermExplanationCaches`, and initial EF Core migration.
- [x] 1.3 Seed 30-day curriculum topics, questions, and sample document chunks.

## Phase 2: Core Domain & Application Handlers
- [x] 2.1 Implement Rich Domain entities with encapsulated logic: `SpacedRepetitionCard` (SM-2 algorithm), `StreakRecord` (Streak/Freeze invariants), `DailyDrill`, `AiReview`, `DocumentBook`, `DocumentChunk`, `Topic`, `InterviewQuestion`, `UserHighlight`, `TermExplanationCache`.
- [x] 2.2 Implement Plain Use-Case Handlers (Pure DI): `GetTodayFocusHandler`, `SubmitDailyDrillHandler`, `ExplainTermHandler`, `GradeReviewCardHandler`.
- [x] 2.3 Implement FluentValidation validators and Result Pattern error responses.

## Phase 3: Infrastructure & Integrations
- [x] 3.1 Implement Gemini Flash AI Service with 1-pass Multimodal audio/text evaluation and strict JSON Schema output.
- [x] 3.2 Implement Term Explainer Service with DB caching (`TermExplanationCaches`).
- [x] 3.3 Implement Local Audio Storage Service for saving voice submissions (`/storage/audios/`).
- [ ] 3.4 Implement AI Document Chunking Service with pgvector embeddings for URL / Markdown imports.
- [x] 3.5 Implement Telegram alert notifier with Markdown formatting and deep links.

## Phase 4: Web API Endpoints & Middleware
- [x] 4.1 Implement `DailyFocusEndpoints` (`GET /today`, `POST /submit`, `POST /explain-term`).
- [ ] 4.2 Implement `LibraryEndpoints` (`GET /books`, `POST /import`).
- [x] 4.3 Implement `ReviewEndpoints` (`GET /deck`, `POST /grade`).
- [ ] 4.4 Implement `NotesEndpoints` (`GET /highlights`, `POST /highlights`).
- [x] 4.5 Configure `IExceptionHandler` for RFC 7807 ProblemDetails, CORS, static audio serving, and Dev Mock Auth.

## Phase 5: Nuxt 4 Frontend Portals
- [x] 5.1 Initialize Nuxt 4 project with Tailwind CSS, Pinia, Shiki, Lucide icons, `@nuxtjs/i18n` (en/vi locale resources), and `@nuxtjs/color-mode` (Dark/Light).
- [x] 5.2 Build `/today` Dual-Pane Focus Hub (Desktop: Doc Reader & CodeMirror 6 Markdown Editor / Audio Recorder; Mobile: Tab view) with dynamic dark/light theme.
- [x] 5.3 Build Floating UI Inline AI Explainer tooltip and Highlight popover with language locale selector.
- [ ] 5.4 Build `/library` Book Cards and Import Document modal (preserving source language).
- [x] 5.5 Build `/review` Spaced Repetition Flashcard deck and `/settings` repository.
- [x] 5.6 Build Streak Badge & Score Analytics display with Dark Mode support.

## Phase 6: Verification & Integration Testing
- [x] 6.1 Domain unit tests for SM-2 calculation and Streak freeze invariants.
- [x] 6.2 Application integration tests for synchronous AI submission handler and idempotent daily drill creation.
- [x] 6.3 End-to-end smoke test for daily flow on web frontend and live REST API.
