# Delta Spec: Core Platform Capability

## Requirements

### REQ-1: Daily Doc Reading Slice
- The system SHALL serve one curated 3–5 minute reading slice per active document series per day.
- Source documentation excerpts SHALL strictly preserve their original language (English docs in English, Vietnamese docs in Vietnamese).
- Each slice SHALL contain the raw official documentation excerpt, a structured summary, bullet-point key takeaways, and a 1-question micro-quiz.
- The web reader SHALL support selecting and highlighting text with a Floating UI popover triggering an instant inline AI terminology explanation tooltip backed by a semantic term cache (`TermExplanationCaches`).

### REQ-2: Senior Scenario Interview & AI Evaluation
- The system SHALL present a daily scenario interview challenge aligned with the day's topic in a dual-pane layout on Desktop and tabbed view on Mobile.
- The user SHALL be able to submit their answer via CodeMirror 6 Markdown editor (with Shiki live preview) or HTML5 `MediaRecorder` voice audio (WebM/WAV).
- Submitted voice audio SHALL be saved to a local Docker storage volume (`/uploads/audios/{drillId}.webm`) for playback review.
- The backend SHALL evaluate the submitted answer synchronously (2-4s) using Gemini 2.5/2.0 Flash with 1-pass Multimodal analysis and strict JSON schema returning:
  - `score` (Integer 1 to 10)
  - `summaryFeedback` (String)
  - `strengths` (Array of Strings)
  - `missingPoints` (Array of Strings)
  - `improvedAnswer` (Markdown String)

### REQ-3: Spaced Repetition (SM-2) Engine
- The system SHALL track user performance on topics and schedule review dates using the SuperMemo SM-2 spaced repetition algorithm.
- Flashcards for unmastered concepts SHALL appear in the `/review` deck when their `nextReviewDate` is reached.

### REQ-4: Document Library & AI Slicing
- The user SHALL be able to import external tech articles via URL or Markdown.
- The AI Chunking Service SHALL parse the document into semantic slices (500–800 words) and queue them as sequential daily readings.

### REQ-5: Streak Tracking & Freeze Protection
- The system SHALL increment the user's active streak upon completing either the daily reading or the interview challenge.
- The system SHALL provide 2 monthly Streak Freezes to automatically prevent streak reset on missed days.

### REQ-6: Telegram Push Notifications
- The background worker SHALL send a morning notification at 08:00 AM containing the topic title and direct deep link to `/today`.
- The worker SHALL send an evening streak warning at 20:00 PM only if the day's drill remains incomplete.

### REQ-7: Internationalization (i18n)
- The web frontend SHALL support switching between English (`en-US`) and Vietnamese (`vi-VN`) via `@nuxtjs/i18n`.
- All UI navigation, buttons, tooltips, flashcard actions, and scorecards SHALL be localized.

### REQ-8: Dark Mode Theme Support
- The web frontend SHALL provide a dark mode toggle (`dark`, `light`, `system`) using `@nuxtjs/color-mode`.
- The CodeMirror 6 editor and Shiki code highlighting SHALL synchronize themes dynamically with the application color mode.

### REQ-9: English-Only Development Standards
- All backend C# code, frontend Vue/TypeScript code, variable names, database entities/columns, unit tests, and code comments SHALL be written in 100% English.
