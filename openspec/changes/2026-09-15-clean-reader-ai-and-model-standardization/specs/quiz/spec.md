# Quiz Capability Delta Specification

## MODIFIED Requirements

### Requirement: Structured AI Question Generation with Gemini
The system SHALL generate batches of 5 to 10 multiple-choice questions tailored by topic and seniority level (Fresher, Junior, Middle, Senior) via Google Gemini Flash Lite.

#### Scenario: User requests a new quiz batch
- **WHEN** an authenticated user submits `POST /api/v1/quiz/generate` with a valid topic (2-100 characters), category, level, and count (5 or 10)
- **THEN** the system generates fresh questions using Gemini with 4 options and 1 correct answer, saves them into the `QuizQuestions` table, and returns the questions (excluding previously mastered questions).

#### Scenario: User requests additional questions for the same topic ("Generate More")
- **WHEN** user clicks "Generate More Questions" after completing a quiz
- **THEN** the system passes the titles of existing questions to Gemini's prompt to prevent duplication, saves newly generated questions to `QuizQuestions`, and returns the new batch.

#### Scenario: Gemini API rate limit or network failure
- **WHEN** the Gemini API returns HTTP 429, timeout, or invalid JSON
- **THEN** the system falls back gracefully to existing unmastered questions in the database or structured fallback templates without crashing.
