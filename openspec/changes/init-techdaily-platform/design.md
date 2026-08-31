# Technical Design: TechDaily Core Platform

## 1. Architectural Layers & Dependencies

Following inward-pointing Clean Architecture:
- `TechDaily.Domain`: Rich Domain Entities, Enums, Value Objects, Domain Exceptions, Business Invariants (No EF, No ASP.NET, No I/O). Encapsulates SM-2 algorithm inside `SpacedRepetitionCard` and Streak/Freeze state inside `StreakRecord`. All code and comments strictly in 100% English.
- `TechDaily.Application`: Plain Use-Case Handlers with Pure DI (no MediatR reflection overhead), DTOs, FluentValidation, Result Pattern, Service Interfaces (`IAiReviewService`, `ITelegramNotifier`, `IDocumentChunker`, `ITermExplanationService`).
- `TechDaily.Infrastructure`: PostgreSQL DbContext with `pgvector` extension support, EF Core 10 `ToJson()` JSONB mappings, Local Volume Audio Storage (`/storage/audios/`), PBKDF2 Password Security (`PasswordHasher`), Gemini AI 3.5 Flash Client with Structured Outputs (JSON Schema), Quartz.NET / BackgroundService Dispatcher, Telegram HTTP Client.
- `TechDaily.Api`: REST Endpoints (Minimal APIs), Synchronous AI Submission Handler (2-4s response), `IExceptionHandler` with RFC 7807 ProblemDetails, JWT Bearer Token Security, Dependency Injection wiring.
- `frontend`: Nuxt 4 (Vue 3, SSR/PWA), Tailwind CSS with Dark Mode (`@nuxtjs/color-mode`), i18n localization (`@nuxtjs/i18n` with English & Vietnamese), Pinia, Google Identity Services SDK, CodeMirror 6 + Shiki Live Preview (dual theme switching), Floating UI Inline Explainer Popover, HTML5 MediaRecorder (audio/webm).

## 2. Invariants & Data Integrity
1. **100% English Codebase & Dev Standards:** All classes, methods, variables, unit tests, code comments, and database column names MUST be in English.
2. **Source Language Preservation:** Authoritative documentation excerpts preserve their original language (English docs remain in English, Vietnamese docs remain in Vietnamese).
3. **i18n & Localization:** Frontend supports switching between English (`en-US`) and Vietnamese (`vi-VN`). AI explanations adapt to the user's requested locale.
4. **Dark Mode First:** Full UI dark mode support with Tailwind `class` mode and Shiki dual themes (`github-dark` / `github-light`) for comfortable nighttime study.
5. **Secure Authentication & Passwords:** Passwords must be at least 6 characters and are salted and hashed using PBKDF2 with SHA-256 (100,000 iterations). JWT tokens are signed with HMAC-SHA256 (256-bit secret).
6. **Idempotent Daily Drills:** A user has at most one `DailyDrill` record per scheduled calendar day per question.
7. **Rich Domain SM-2 Boundaries:** `EaseFactor` is constrained to $[1.30, 2.50]$. Calculation and interval progression ($I_1 = 1$, $I_2 = 6$, $I_n = I_{n-1} \times \text{EF}$) are encapsulated in `SpacedRepetitionCard.ApplyReview(grade)`.
8. **Streak & Freeze Protection:** Invariants for consecutive days, freeze credit deductions (max 2/month), and streak resets are encapsulated in `StreakRecord.RecordCompletion(today)`.
9. **Structured AI Contract (1-Pass Multimodal):** Gemini API calls (`gemini-3.5-flash`) enforce JSON Schema validation (`response_mime_type: "application/json"`) with 1-pass multimodal evaluation (audio blob or markdown text direct to review).
10. **Terminology Semantic Cache:** Common terms (e.g. *LOH*, *MVCC*, *Zero Copy*) are looked up in `TermExplanationCaches` before invoking `gemini-3.5-flash-lite`.
11. **Soft Delete with Partial Indexes:** Soft-deleted entities use `IsDeleted` with EF Core query filters and partial unique indexes (`WHERE "IsDeleted" = false`).

## 3. API Contract Highlights
- `POST /api/v1/auth/register`: Creates new account with email, password, and name, returning JWT and User profile.
- `POST /api/v1/auth/login`: Authenticates with email and password, returning JWT and User profile.
- `POST /api/v1/auth/google`: Validates Google ID token via Google APIs and returns JWT.
- `GET /api/v1/daily/today`: Returns today's active doc excerpt, quiz, and interview challenge.
- `POST /api/v1/daily/drills/{id}/submit`: Accepts `{ answerText, audioFile }` and performs synchronous 1-pass AI evaluation, saving review and audio locally and returning `AiReviewDto`.
- `POST /api/v1/daily/explain-term`: Accepts `{ term, category, context, locale }` and returns concise 2-sentence explanation in the requested locale with DB caching.
- `GET /api/v1/library/books`: Returns active books and user reading progress.
- `POST /api/v1/library/import`: Accepts `{ sourceUrl, markdownContent, title, language }` to trigger AI chunking.
- `GET /api/v1/review/deck`: Returns pending spaced repetition cards due for today.
- `POST /api/v1/review/cards/{id}/grade`: Accepts quality rating (0–5) to calculate next interval.
- `GET /api/v1/notes/highlights`: Returns user highlights and notes.
- `POST /api/v1/notes/highlights`: Creates a new highlight note.
- `DELETE /api/v1/notes/highlights/{id}`: Soft-deletes a highlight note.
