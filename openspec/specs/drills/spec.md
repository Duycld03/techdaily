# Drills Specification

## Purpose
Provides multiple-choice Senior scenario challenges with instant grading, architectural feedback, SM-2 rescheduling for mistakes, and interactive choice cards.

## Requirements

### Requirement: Scenario Multiple-Choice Interview Question Domain Model
The `InterviewQuestion` entity SHALL be sourced exclusively from a `DocumentChunk` via a mandatory `DocumentChunkId` foreign key, and SHALL NOT reference any `Topic` or `TopicId`. The entity represents a practical decision drill, containing: 4 distinct answer choices (A, B, C, D) modeling technical solutions, architectural decisions, or behavioral strategies; zero-based correct option index; detailed markdown trade-off analysis; and difficulty tier. Uncompleted drills mask the correct answer.

#### Scenario: User queries unattempted drill question
- **WHEN** user calls `GET /api/v1/daily/today` for an uncompleted drill
- **THEN** system returns question text and 4 choices with correct option index masked.

#### Scenario: User queries completed drill question
- **WHEN** user calls `GET /api/v1/daily/today` for a completed drill
- **THEN** system returns question text, all choices, correct option index, and full explanation markdown.

#### Scenario: Drill question is grounded in a document chunk
- **WHEN** the system materializes an `InterviewQuestion`
- **THEN** the question SHALL carry a non-null `DocumentChunkId` referencing the `DocumentChunk` it was synthesized from
- **AND** the entity SHALL expose no `TopicId` field.

---

### Requirement: Multiple-Choice Submission & Instant Evaluation
The system SHALL record the selected option index, validate boundaries, evaluate correctness against the authoritative correct option index, assign a score, advance user streaks, and schedule spaced repetition review on incorrect answers. On an incorrect daily drill, the system SHALL create or refresh an SM-2 spaced-repetition card derived from the drill question's source `DocumentChunk` (so that every chunk-sourced drill failure produces a card).

#### Scenario: User submits correct option
- **WHEN** user submits the correct option index to `POST /api/v1/daily/drills/{id}/submit`
- **THEN** system marks drill as correct with full score, updates streak record, and returns evaluation result.

#### Scenario: User submits incorrect option
- **WHEN** user submits an incorrect option index to `POST /api/v1/daily/drills/{id}/submit`
- **THEN** system marks drill as incorrect with score 0, updates the streak record, and creates or refreshes an SM-2 spaced repetition review card derived from the drill question's source `DocumentChunk`, scheduling it for tomorrow.

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

### Requirement: Architectural Trade-off Challenge Representation
The decision challenge on `/today` (`InterviewChallengePane.vue`) SHALL present contextual problem statements with explicit real-world constraints across both software craftsmanship (latency, throughput, consistency) and human craft (cognitive focus, habit formation, leadership trade-offs). Answer choices SHALL represent distinct strategies or operational designs rather than trivia facts.

#### Scenario: User views an unattempted Trade-off Challenge
- **WHEN** user views `/today` challenge pane
- **THEN** system displays the scenario title, context badge (e.g., "High-Throughput Ingestion", "Deep Focus Environment"), constraints summary, and interactive proposal cards.
- **THEN** each proposal card clearly presents the approach without revealing the optimal choice indicator prior to submission.

#### Scenario: User submits their architectural choice
- **WHEN** user selects an architecture proposal and clicks "Submit Decision"
- **THEN** system evaluates the selection, increments streak and points on optimal decision, triggers celebration feedback, and switches the pane into Principal Review mode.

#### Scenario: Principal Architect Review display
- **WHEN** challenge state transitions to reviewed
- **THEN** system reveals:
  1. The optimal choice badge (`Optimal Choice`) and the user's choice badge (`Your Choice`).
  2. The deep-dive explanation detailing why the chosen pattern satisfies constraints.
  3. The structural breakdown of failure modes for the alternative options.

### Requirement: Daily Drill Status Resiliency & State Deserialization
The daily interview challenge components (`InterviewChallengePane.vue` and `today.vue`) SHALL handle both string (`"Reviewed"`, `"reviewed"`, `"Submitted"`) and integer (`2`, `1`) representations of `DrillStatus`. Upon page refresh, when a previously completed drill is retrieved from `GET /api/v1/daily/today`, the interface SHALL preserve the reviewed state, restore the user's selected option, and display the score, architectural explanation, and header completed badge without reverting to an unsubmitted state.

#### Scenario: User refreshes page after completing daily interview challenge
- **WHEN** an authenticated user completes the daily scenario challenge and later refreshes `/today`
- **THEN** the API returns the persisted drill entity with `status = "Reviewed"`
- **AND** the scenario challenge pane recognizes the drill as reviewed (`isReviewed = true`)
- **AND** the header completed badge remains visible
- **AND** the submit button remains hidden, rendering the architectural explanation card and feedback banner.

#### Scenario: Preserving selected option index and feedback across sessions
- **WHEN** user reloads the today view for an already reviewed drill with `selectedOptionIndex = 2`
- **THEN** option 2 is visually selected and styled with appropriate correctness highlighting (emerald for correct, rose for incorrect)
- **AND** the user cannot re-submit or alter the completed drill.

### Requirement: Natural Vertical Layout for Scenario Challenge Interface
The scenario multiple-choice challenge container in `InterviewChallengePane.vue` SHALL utilize natural vertical content stacking (`flex flex-col justify-start gap-5`) rather than artificial edge stretching (`justify-between`). Multiple-choice options, contextual alert banners, submit controls, and post-submission explanations SHALL follow in continuous vertical sequence without arbitrary 300–400px empty gaps on widescreen viewports.

#### Scenario: Natural vertical spacing without layout gaps
- **WHEN** viewing the scenario challenge on a wide desktop screen (>1280px) with extended vertical height
- **THEN** the question header, options list, submit button, and explanation card maintain consistent vertical spacing (`space-y-5 sm:space-y-6`) without pushing controls to the extreme bottom of the viewport.

#### Scenario: Responsive layout during post-submission feedback
- **WHEN** the drill transitions to reviewed state
- **THEN** the result banner and deep-dive explanation card render directly beneath the options list with natural spacing.

---

### Requirement: Read-Time Streak Decay Evaluation
When querying user focus statistics or user profile metrics, the system SHALL evaluate the learner's effective streak status based on elapsed calendar days since `LastActiveDate`. If the gap exceeds the consecutive day window and remaining freeze credits cannot cover the absence, the system SHALL report an effective active streak of `0` in read-time responses without mutating database persistence prior to drill submission.

#### Scenario: User queries focus stats after multi-day absence exceeding freeze credits
- **GIVEN** a user with stored `CurrentStreak = 2`, `FreezeCreditsRemaining = 1`, and `LastActiveDate = 2026-09-30`
- **WHEN** the user calls `GET /api/v1/daily/today` or `GET /api/v1/user/profile` on 2026-10-06 (6 days later)
- **THEN** the response reports `CurrentStreak = 0`
- **AND** the best record `LongestStreak = 4` is preserved.

#### Scenario: User queries focus stats on same day as completed activity
- **GIVEN** a user who completed a drill on 2026-10-06 with resulting `CurrentStreak = 1`
- **WHEN** the user queries focus stats on 2026-10-06
- **THEN** the response reports `CurrentStreak = 1`.

#### Scenario: User queries focus stats next day with consecutive continuity
- **GIVEN** a user with `LastActiveDate` equal to yesterday and `CurrentStreak = 3`
- **WHEN** the user queries focus stats today prior to completing today's drill
- **THEN** the response reports `CurrentStreak = 3` as the streak remains active within the daily grace window.

---

### Requirement: Daily Drill Materialization and Lifecycle Isolation
When an active pacer advances or a daily focus slice is served for a specific calendar date, the system SHALL isolate new daily sessions from historical submissions. Newly advanced slices with unattempted challenges SHALL be served in `Pending` state with correct answers masked. When a user explicitly navigates to review a previously completed slice, the system SHALL display that slice's historical evaluation without resetting today's active drill.

#### Scenario: New daily slice receives fresh pending drill
- **GIVEN** an active pacer advances from Slice 1 to Slice 2 on 2026-10-06
- **WHEN** the system resolves the scenario challenge for Slice 2
- **THEN** the drill is served with `Status = "Pending"`
- **AND** `SelectedOptionIndex` is null
- **AND** correct answer index and architectural explanation are masked.

#### Scenario: Learner reviews previously completed slice without resetting today's drill
- **GIVEN** today's active pacer is on Slice 2 with a pending drill
- **WHEN** the user explicitly navigates to review completed Slice 1 (`?chunkOrder=1`)
- **THEN** Slice 1 displays its reviewed state and explanation
- **AND** navigating back to Slice 2 restores today's pending challenge.
