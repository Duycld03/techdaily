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
The web frontend SHALL render the Senior Scenario challenge with 4 interactive option cards (A, B, C, D), hover effects, selection highlights, confetti celebrations on correct answers, and rich markdown explanation breakdown.

#### Scenario: User selects option card
- **WHEN** user clicks on choice card B
- **THEN** choice card highlights with active selection border and enables submit button.
