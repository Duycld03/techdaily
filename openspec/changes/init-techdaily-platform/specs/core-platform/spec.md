# Delta Spec: Core Platform Capability

## Requirements

### REQ-1: Standard Email & Password Authentication
- The system SHALL allow users to register an account with email, password (min 6 characters), full name, and preferred locale (`POST /api/v1/auth/register`).
- The system SHALL securely hash passwords using PBKDF2 with SHA-256 (16-byte random salt, 100,000 iterations).
- The system SHALL authenticate users via email and password (`POST /api/v1/auth/login`), issuing a 256-bit JWT bearer token upon successful verification.
- The web frontend SHALL provide dedicated Sign In and Register tabs with instant client-side validation and clear error feedback.

### REQ-2: Google OAuth 2.0 Authentication
- The system SHALL support signing in with Google via Google Identity Services (GIS) on the frontend (`POST /api/v1/auth/google`).
- The backend SHALL verify Google ID token cryptographic signatures using `GoogleJsonWebSignature`, automatically provision new user accounts, and issue application JWT tokens.

### REQ-3: Daily Doc Reading Slice
- The system SHALL serve one curated 3–5 minute reading slice per active document series per day (`GET /api/v1/daily/today`).
- Source documentation excerpts SHALL strictly preserve their original language (English docs in English, Vietnamese docs in Vietnamese).
- Each slice SHALL contain the raw official documentation excerpt, a structured summary, bullet-point key takeaways, and a 1-question micro-quiz.
- The web reader SHALL support selecting text with a Floating UI popover triggering an instant inline AI terminology explanation tooltip backed by a semantic term cache (`TermExplanationCaches`) and `gemini-3.5-flash-lite`.

### REQ-4: Senior Scenario Interview & AI Evaluation
- The system SHALL present a daily scenario interview challenge aligned with the day's topic in a dual-pane layout on Desktop and tabbed view on Mobile.
- The user SHALL be able to submit their answer via CodeMirror 6 Markdown editor (with Shiki live preview) or HTML5 `MediaRecorder` voice audio (WebM/WAV).
- Submitted voice audio SHALL be saved to a local Docker storage volume (`/uploads/audios/{drillId}.webm`) for playback review.
- The backend SHALL evaluate the submitted answer synchronously using Gemini 3.5 Flash with 1-pass Multimodal analysis and strict JSON schema returning:
  - `score` (Integer 1 to 10)
  - `summaryFeedback` (String)
  - `strengths` (Array of Strings)
  - `missingPoints` (Array of Strings)
  - `improvedAnswerMarkdown` (Markdown String)

### REQ-5: Spaced Repetition (SM-2) Engine
- The system SHALL track user performance on topics and schedule review dates using the SuperMemo SM-2 spaced repetition algorithm.
- Flashcards for unmastered concepts SHALL appear in the `/review` deck when their `nextReviewDate` is reached (`GET /api/v1/review/deck`, `POST /api/v1/review/cards/{id}/grade`).

### REQ-6: Document Library & AI Slicing
- The user SHALL be able to browse library books and import external tech articles via Markdown (`GET /api/v1/library/books`, `POST /api/v1/library/import`).
- The AI Chunking Service SHALL parse documents into semantic slices and queue them as sequential daily readings.

### REQ-7: Highlight Notes System
- The user SHALL be able to create, view, and delete highlighted quotes with optional tags and book references (`GET /api/v1/notes/highlights`, `POST /api/v1/notes/highlights`, `DELETE /api/v1/notes/highlights/{id}`).

### REQ-8: Streak Tracking & Freeze Protection
- The system SHALL increment the user's active streak upon completing either the daily reading or the interview challenge.
- The system SHALL provide 2 monthly Streak Freezes to automatically prevent streak reset on missed days.

### REQ-9: Telegram Push Notifications
- The background worker SHALL send a morning notification at 08:00 AM containing the topic title and direct deep link to `/today`.
- The worker SHALL send an evening streak warning at 20:00 PM only if the day's drill remains incomplete.

### REQ-10: Internationalization (i18n) & Dark Mode
- The web frontend SHALL support switching between English (`en-US`) and Vietnamese (`vi-VN`) via `@nuxtjs/i18n`.
- The web frontend SHALL provide a dark mode toggle (`dark`, `light`, `system`) using `@nuxtjs/color-mode` with synchronized CodeMirror 6 and Shiki highlighting.

### REQ-11: English-Only Development Standards
- All backend C# code, frontend Vue/TypeScript code, variable names, database entities/columns, unit tests, and code comments SHALL be written in 100% English.
