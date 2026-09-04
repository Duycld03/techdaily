# Quiz Specification

## Purpose
Provides high-speed AI multiple-choice interview quiz generation via Gemini, real-time timer arena, mastery tracking, and mistake review queue.

## Requirements

### Requirement: Mandatory Authentication on All Quiz Capabilities
The system SHALL require authenticated user identity for all quiz generation, submission, review queue, and analytics endpoints.

#### Scenario: Unauthenticated visitor navigates to /quiz
- **WHEN** an unauthenticated visitor navigates to `/quiz`
- **THEN** the system immediately redirects to `/login?redirect=/quiz` without exposing quiz data.

#### Scenario: Unauthenticated API request to quiz endpoints
- **WHEN** a client sends a request to any `/api/v1/quiz/*` endpoint without a valid JWT token
- **THEN** the system responds with `HTTP 401 Unauthorized`.

---

### Requirement: Structured AI Question Generation with Gemini
The system SHALL generate batches of 5 to 10 multiple-choice questions tailored by topic and seniority level (Fresher, Junior, Middle, Senior) via Gemini 3.6 Flash.

#### Scenario: User requests a new quiz batch
- **WHEN** an authenticated user submits `POST /api/v1/quiz/generate` with a valid topic (2-100 characters), category, level, and count (5 or 10)
- **THEN** the system generates fresh questions using Gemini with 4 options and 1 correct answer, saves them into the `QuizQuestions` table, and returns the questions (excluding previously mastered questions).

#### Scenario: User requests additional questions for the same topic ("Generate More")
- **WHEN** user clicks "Generate More Questions" after completing a quiz
- **THEN** the system passes the titles of existing questions to Gemini's prompt to prevent duplication, saves newly generated questions to `QuizQuestions`, and returns the new batch.

#### Scenario: Gemini API rate limit or network failure
- **WHEN** the Gemini API returns HTTP 429, timeout, or invalid JSON
- **THEN** the system falls back gracefully to existing unmastered questions in the database or structured fallback templates without crashing.

---

### Requirement: Answer Submission & Mastery Tracking
The system SHALL persist user answer attempts, mark correctly answered questions as Mastered (`IsMastered = true`), and route incorrect questions into a Mistake Review Queue.

#### Scenario: User answers a quiz question correctly
- **WHEN** user submits the correct option index via `POST /api/v1/quiz/submit`
- **THEN** the system sets `IsMastered = true`, increments `CorrectCount`, and excludes this question from future generation batches for this user.

#### Scenario: User answers a quiz question incorrectly
- **WHEN** user submits an incorrect option index via `POST /api/v1/quiz/submit`
- **THEN** the system sets `IsMastered = false`, increments `IncorrectCount`, and retains the question in the user's Review Queue.

#### Scenario: Idempotent Submission Handling
- **WHEN** user double-clicks submit or submits concurrently for the same question
- **THEN** the system executes an idempotent upsert against `UserQuizProgress` using the unique `(UserId, QuestionId)` constraint without duplicating records.

---

### Requirement: Mistake Review Queue & Mastery Analytics
The system SHALL provide a dedicated review mode to practice unmastered questions and view overall mastery analytics.

#### Scenario: User opens the Mistake Review Queue
- **WHEN** user requests `GET /api/v1/quiz/review-queue`
- **THEN** the system returns all questions where `IsMastered = false` for the user, allowing targeted re-practice.

#### Scenario: User masters a previously failed question during review
- **WHEN** user answers a review queue question correctly
- **THEN** the question's status transitions to `IsMastered = true` and it is immediately removed from the pending review queue.

#### Scenario: User requests quiz mastery statistics
- **WHEN** user requests `GET /api/v1/quiz/stats`
- **THEN** the system returns total questions answered, mastered count, review queue count, and accuracy breakdown by seniority level and topic.
