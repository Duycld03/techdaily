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
The system SHALL provide a dedicated review mode to practice unmastered questions and view overall mastery analytics. The quiz summary interface and Mistake Review Queue (`/quiz`) SHALL feature a 1-click action allowing users to promote any failed question directly into their daily SM-2 spaced repetition deck (`POST /api/v1/review/cards/from-quiz-mistake`), rather than confining mistake remediation strictly to manual re-quizzing.

#### Scenario: User opens the Mistake Review Queue
- **WHEN** user requests `GET /api/v1/quiz/review-queue`
- **THEN** the system returns all questions where `IsMastered = false` for the user, allowing targeted re-practice.

#### Scenario: User masters a previously failed question during review
- **WHEN** user re-takes a question from the review queue and answers correctly
- **THEN** `IsMastered` is updated to `true` and the question is removed from active review queues.

#### Scenario: User requests quiz mastery statistics
- **WHEN** user requests `GET /api/v1/quiz/stats`
- **THEN** the system returns total answered, total mastered, unmastered count, and mastery rate percentage by category.

#### Scenario: User promotes an incorrect question from quiz results
- **WHEN** user finishes a quiz batch with one or more incorrect answers
- **THEN** the results card displays a `🔄 Push to SM-2 Deck` button alongside each mistake explanation.

#### Scenario: User clicks Push to SM-2 Deck button
- **WHEN** user clicks `🔄 Push to SM-2 Deck` for a question
- **THEN** the client sends `POST /api/v1/review/cards/from-quiz-mistake` with `questionId`, the button updates to a disabled checkmark state ("Added to SM-2 Deck"), and a localized toast confirms scheduling for today's review session.

---

### Requirement: Book & Curriculum Grounded Quiz Generation
The `GenerateQuizHandler` SHALL support generating scenario-based technical questions grounded in specific document books or curriculum slices using pgvector similarity search.

#### Scenario: User requests quiz based on a specific book
- **WHEN** user initiates quiz generation with `BookId` specified
- **THEN** generator retrieves document slices from that book, extracts key architectural tradeoffs, and prompts Gemini to create scenario questions strictly grounded in those excerpts.

#### Scenario: User requests quiz grounded in topic curriculum
- **WHEN** user requests quiz for a broad topic (e.g., "PostgreSQL Indexing & MVCC") with grounding enabled
- **THEN** generator uses pgvector to find top matching curriculum slices, includes the slice excerpts as authoritative reference context, and synthesizes deep-dive scenario questions referencing those architectural patterns.

#### Scenario: Excerpt citation in quiz question explanations
- **WHEN** user submits an answer to a grounded quiz question
- **THEN** explanation section includes a "Source Reference" badge indicating the book title, chapter slice, and context excerpt from which the question was derived.

---

### Requirement: 1-Click Spaced Repetition Bridge for Quiz Mistakes
The backend SHALL expose `POST /api/v1/review/cards/from-quiz-mistake` allowing authenticated users to transform any `QuizQuestion` into an active `SpacedRepetitionCard`.

#### Scenario: Creating a spaced repetition card from a quiz mistake
- **WHEN** an authenticated user invokes `POST /api/v1/review/cards/from-quiz-mistake` with a valid `questionId`
- **THEN** the system constructs a new `SpacedRepetitionCard` where `SourceType = CardSourceType.QuizMistake`, `SourceQuizQuestionId = questionId`, `FrontMarkdown` contains the question text and formatted answer choices, `BackMarkdown` contains the correct answer and authoritative explanation, and `NextReviewDate` is set to the current UTC date.

#### Scenario: Idempotent card creation for previously converted questions
- **WHEN** a user invokes the endpoint for a question that was previously converted to an SM-2 card
- **THEN** the system resets the existing card's `NextReviewDate` to today, resets `RepetitionCount = 0` and `Status = CardStatus.Learning` to restart the spaced repetition cycle, and returns `HTTP 200 OK` without creating duplicate records.

---

### Requirement: Bidirectional Mastery Synchronization via Spaced Repetition Grading
When a user grades a spaced repetition card derived from a quiz mistake in `/review`, the system SHALL synchronize recall performance back to `UserQuizProgress`.

#### Scenario: Successful recall in SM-2 deck masters quiz question
- **WHEN** user grades a card whose `SourceType == CardSourceType.QuizMistake` with `qualityGrade >= 3` (3 = Pass, 4 = Good, 5 = Excellent)
- **THEN** `GradeReviewCardHandler` locates the corresponding `UserQuizProgress` record and sets `IsMastered = true`, incrementing `CorrectCount` and removing the question from the pending `/quiz/review-queue`.

#### Scenario: Failed recall in SM-2 deck maintains unmastered status
- **WHEN** user grades a quiz-derived card with `qualityGrade < 3` (0 = Blackout, 1 = Incorrect, 2 = Hard)
- **THEN** `UserQuizProgress.IsMastered` remains `false`, incrementing `IncorrectCount`, and the card is scheduled for immediate repetition in tomorrow's deck.
