# Core Platform Specification

## Purpose
Provides the foundational user authentication, user profile management, daily curriculum doc reading slices, scenario challenges, SM-2 spaced repetition engine, document library, and background notification dispatches for the TechDaily platform.

## Requirements

### Requirement: Standard Email & Password Authentication
The system SHALL allow users to register an account with email, password (min 6 characters), full name, and preferred locale (`POST /api/v1/auth/register`), securely hash passwords using PBKDF2 with SHA-256 (16-byte random salt, 100,000 iterations), and authenticate users via email and password (`POST /api/v1/auth/login`), issuing a 256-bit JWT bearer token upon successful verification.

#### Scenario: User registers with valid email and password
- **WHEN** visitor sends `POST /api/v1/auth/register` with valid email, name, and password >= 6 characters
- **THEN** system provisions user entity with PBKDF2 password hash, creates user learning stats, and returns `201 Created` with JWT token.

#### Scenario: User authenticates with registered credentials
- **WHEN** user sends `POST /api/v1/auth/login` with registered email and correct password
- **THEN** system verifies hash and returns `200 OK` with JWT bearer token and user profile.

---

### Requirement: Google OAuth 2.0 Authentication
The system SHALL support signing in with Google via Google Identity Services (GIS) on the frontend (`POST /api/v1/auth/google`), verify Google ID token cryptographic signatures using `GoogleJsonWebSignature`, automatically provision new user accounts, and issue application JWT tokens.

#### Scenario: User logs in via Google Identity Services
- **WHEN** client sends `POST /api/v1/auth/google` with valid Google ID token credential
- **THEN** system verifies token signature with Google, provisions new user if not exists or links existing account, and returns application JWT.

---

### Requirement: User Profile Management, Route Guards & Security
The system SHALL provide dedicated User Profile endpoints (`GET /api/v1/user/profile`, `PUT /api/v1/user/profile`, `PUT /api/v1/user/change-password`) protected with strict JWT Bearer authentication, reject unauthenticated requests with `HTTP 401 Unauthorized`, and enforce route middleware guards on protected frontend pages.

#### Scenario: Unauthenticated request to user profile
- **WHEN** unauthenticated client calls `GET /api/v1/user/profile`
- **THEN** system returns `401 Unauthorized`.

#### Scenario: User updates profile settings
- **WHEN** authenticated user sends `PUT /api/v1/user/profile` with target level and learning goals
- **THEN** system updates user profile and returns updated profile DTO.

---

### Requirement: Daily Doc Reading Slice
The system SHALL serve one curated 3–5 minute reading slice per active document series per day (`GET /api/v1/daily/today`) preserving source documentation excerpt language, structured summary, key takeaways, and quick-check questions.

#### Scenario: Authenticated user retrieves today's reading slice
- **WHEN** user calls `GET /api/v1/daily/today`
- **THEN** system returns today's document chunk with excerpt, summary, takeaways, and quiz status.

---

### Requirement: Senior Scenario Interview Challenge
The system SHALL present a daily scenario interview challenge aligned with the day's curriculum topic with instant grading, architectural feedback, and score evaluation.

#### Scenario: User completes daily interview scenario
- **WHEN** user submits answer to `POST /api/v1/daily/drill/submit`
- **THEN** system records drill submission, evaluates answer, and returns score with architectural explanation.

---

### Requirement: Spaced Repetition (SM-2) Engine
The system SHALL track user performance on technical concepts and schedule review dates using the SuperMemo SM-2 spaced repetition algorithm ($EF \in [1.30, 2.50]$).

#### Scenario: User reviews flashcard deck
- **WHEN** user calls `GET /api/v1/review/deck`
- **THEN** system returns cards where `NextReviewDate <= DateTime.UtcNow`.

#### Scenario: User grades card review
- **WHEN** user sends `POST /api/v1/review/cards/{id}/grade` with grade between 0 and 5
- **THEN** system recalculates interval and ease factor according to SM-2 formula and updates card schedule.

---

### Requirement: Document Library & AI Slicing
The system SHALL allow browsing library books and importing technical articles (`GET /api/v1/library/books`, `POST /api/v1/library/import`), parsing documents into sequential daily reading slices.

#### Scenario: User imports markdown document
- **WHEN** user sends `POST /api/v1/library/import` with markdown text
- **THEN** system slices document into sequential chunks and creates a new book record.

---

### Requirement: Highlight Notes System
The system SHALL allow users to create, view, and delete highlighted quotes with optional tags and book references (`GET /api/v1/notes/highlights`, `POST /api/v1/notes/highlights`, `DELETE /api/v1/notes/highlights/{id}`).

#### Scenario: User saves a highlight note
- **WHEN** user calls `POST /api/v1/notes/highlights` with selected text excerpt and chunk ID
- **THEN** system persists the highlight note and returns `201 Created`.

---

### Requirement: Streak Tracking & Freeze Protection
The system SHALL increment active streak upon daily completion and provide monthly Streak Freezes to prevent streak reset on missed days.

#### Scenario: User completes activity on consecutive day
- **WHEN** user completes daily drill or slice on next calendar day
- **THEN** system increments `CurrentStreak` by 1 and updates `LongestStreak` if current exceeds longest.

---

### Requirement: Telegram Push Notifications
The system SHALL provide notification dispatches for morning curriculum reminders (08:00 AM) and evening streak preservation alerts (20:00 PM).

#### Scenario: Morning dispatch worker runs
- **WHEN** background scheduler triggers 08:00 AM dispatch
- **THEN** worker sends telegram message with today's reading title and link to users with configured `TelegramChatId`.

---

### Requirement: Internationalization (i18n) & Dark Mode
The web frontend SHALL support seamless switching between English (`en-US`) and Vietnamese (`vi-VN`) via `@nuxtjs/i18n` and provide persistent dark/light theme switching without visual flashing.

#### Scenario: User toggles language to Vietnamese
- **WHEN** user selects Vietnamese language option
- **THEN** interface updates labels and messages to Vietnamese locale with full layout parity.

---

### Requirement: English-Only Development Standards
All backend C# code, frontend Vue/TypeScript code, variable names, database entities/columns, unit tests, and code comments SHALL be written in 100% English.

#### Scenario: Developer inspects codebase
- **WHEN** examining classes, database migrations, and unit tests
- **THEN** all code symbols, class names, variable identifiers, and comments are strictly in English.
