# Quiz Capability Delta Specification

## Purpose
Bridges failed interview quiz questions directly into TechDaily's SM-2 spaced repetition engine, establishing an active recall loop with bidirectional mastery tracking between the quiz arena and flashcard deck.

---

## MODIFIED Requirements

### Requirement: Mistake Review Queue & Mastery Analytics
The quiz summary interface and Mistake Review Queue (`/quiz`) SHALL feature a 1-click action allowing users to promote any failed question directly into their daily SM-2 spaced repetition deck (`POST /api/v1/review/cards/from-quiz-mistake`), rather than confining mistake remediation strictly to manual re-quizzing.

#### Scenario: User promotes an incorrect question from quiz results
- **WHEN** user finishes a quiz batch with one or more incorrect answers
- **THEN** the results card displays a `🔄 Push to SM-2 Deck` button alongside each mistake explanation.

#### Scenario: User clicks Push to SM-2 Deck button
- **WHEN** user clicks `🔄 Push to SM-2 Deck` for a question
- **THEN** the client sends `POST /api/v1/review/cards/from-quiz-mistake` with `questionId`, the button updates to a disabled checkmark state ("Added to SM-2 Deck"), and a localized toast confirms scheduling for today's review session.

---

## NEW Requirements

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
