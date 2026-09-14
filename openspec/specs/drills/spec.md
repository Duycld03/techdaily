# Drills Specification

## Purpose
Provides multiple-choice Senior scenario challenges with instant grading, architectural feedback, SM-2 rescheduling for mistakes, and interactive choice cards.

## Requirements

### Requirement: Scenario Multiple-Choice Interview Question Domain Model
The `InterviewQuestion` entity SHALL include: 4 distinct answer choices (A, B, C, D) representing technical solutions, architectural decisions, or debugging actions; zero-based correct option index; detailed markdown explanation analyzing trade-offs; and difficulty tier. When the drill is not yet completed, the correct option index and explanation SHALL be masked from client-side responses.

#### Scenario: User queries unattempted drill question
- **WHEN** user calls `GET /api/v1/daily/today` for an uncompleted drill
- **THEN** system returns question text and 4 choices with correct option index masked.

#### Scenario: User queries completed drill question
- **WHEN** user calls `GET /api/v1/daily/today` for a completed drill
- **THEN** system returns question text, all choices, correct option index, and full explanation markdown.

---

### Requirement: Multiple-Choice Submission & Instant Evaluation
The system SHALL record the selected option index, validate boundaries, evaluate correctness against the authoritative correct option index, assign a score, advance user streaks, and schedule spaced repetition review on incorrect answers.

#### Scenario: User submits correct option
- **WHEN** user submits the correct option index to `POST /api/v1/daily/drills/{id}/submit`
- **THEN** system marks drill as correct with full score, updates streak record, and returns evaluation result.

#### Scenario: User submits incorrect option
- **WHEN** user submits an incorrect option index to `POST /api/v1/daily/drills/{id}/submit`
- **THEN** system marks drill as incorrect with score 0 and schedules a spaced repetition review card for tomorrow.

---

### Requirement: Frontend Interactive Scenario Drill UI
The web frontend SHALL render the Senior Scenario challenge with 4 interactive option cards (A, B, C, D), hover effects, selection highlights, confetti celebrations on correct answers, and rich markdown explanation breakdown. When a submission fails due to network error or authorization failure, the UI SHALL preserve the reading pane and question choices intact, presenting feedback via notifications rather than unmounting the entire view.

#### Scenario: User selects option card
- **WHEN** user clicks on choice card B
- **THEN** choice card highlights with active selection border and enables submit button.

#### Scenario: Drill submission encounters error
- **WHEN** user clicks "Submit Answer" and the backend returns an error (such as 401 or network failure)
- **THEN** the application DOES NOT replace the dual-pane content with a full-screen `HTTP Error` container.
- **THEN** an error notification appears alerting the user to the failure.
- **THEN** the question, selected option, and document reader remain visible and intact.

### Requirement: Look-Ahead JIT Pre-Generation Buffer
The system SHALL maintain a sliding look-ahead buffer of 3 pre-generated Senior Trade-off Challenges ahead of the user's active reading position (`CurrentChunkOrder + 1`, `+ 2`, `+ 3`). Upon initial book ingestion, the background worker SHALL generate challenges strictly for the first 3 chunks, achieving instant book readiness without upfront batch saturation.

#### Scenario: User advances to next reading slice
- **WHEN** user completes their daily reading slice and advances to slice $N$
- **THEN** system serves the pre-generated challenge for slice $N$ with 0ms latency.
- **THEN** background service enqueues look-ahead challenge generation for slice $N + 3$ to maintain buffer depth.

---

### Requirement: Non-Blocking AI Synthesis State & Priority Jump
When a user rapidly skips or navigates to a slice whose challenge is not yet generated, the system SHALL render the document reader pane immediately without delay, while presenting an interactive AI Synthesis state in the challenge pane. The system SHALL immediately promote the active slice to the head of the generation queue.

#### Scenario: User skips forward beyond pre-generated buffer
- **WHEN** user navigates to a slice whose Trade-off Challenge is pending generation
- **THEN** document reader pane displays the slice text immediately with zero waiting.
- **THEN** challenge pane displays an AI Synthesis Card with pulsating animation, chapter context, and skeleton option placeholders.
- **THEN** server promotes the target slice to Priority 1 in the generation channel.

#### Scenario: Challenge synthesis completes while user reads
- **WHEN** Gemini completes generation of the promoted challenge
- **THEN** challenge pane smoothly transitions from the synthesis skeleton to the interactive scenario options.

---

### Requirement: Resilient Timeout & Fallback Scenario
If AI scenario synthesis fails or exceeds a 6-second threshold, the system SHALL provide an immediate recovery mechanism without unmounting the view or displaying uncaught errors.

#### Scenario: AI generation timeout or rate limit
- **WHEN** AI generation encounters a rate limit (429), timeout, or parsing error
- **THEN** challenge pane presents an elegant fallback card with a "Retry Scenario Generation" action button and standard architectural discussion prompts for that chapter.
