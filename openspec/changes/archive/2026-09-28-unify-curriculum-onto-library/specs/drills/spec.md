## MODIFIED Requirements

### Requirement: Scenario Multiple-Choice Interview Question Domain Model
The `InterviewQuestion` entity SHALL be sourced exclusively from a `DocumentChunk` via a mandatory `DocumentChunkId` foreign key, and SHALL NOT reference any `Topic` or `TopicId`. The entity SHALL include: 4 distinct answer choices (A, B, C, D) representing technical solutions, architectural decisions, or debugging actions; zero-based correct option index; detailed markdown explanation analyzing trade-offs; and difficulty tier. When the drill is not yet completed, the correct option index and explanation SHALL be masked from client-side responses.

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

### Requirement: Multiple-Choice Submission & Instant Evaluation
The system SHALL record the selected option index, validate boundaries, evaluate correctness against the authoritative correct option index, assign a score, advance user streaks, and schedule spaced repetition review on incorrect answers. On an incorrect daily drill, the system SHALL create or refresh an SM-2 spaced-repetition card derived from the drill question's source `DocumentChunk` (so that every chunk-sourced drill failure produces a card).

#### Scenario: User submits correct option
- **WHEN** user submits the correct option index to `POST /api/v1/daily/drills/{id}/submit`
- **THEN** system marks drill as correct with full score, updates streak record, and returns evaluation result.

#### Scenario: User submits incorrect option
- **WHEN** user submits an incorrect option index to `POST /api/v1/daily/drills/{id}/submit`
- **THEN** system marks drill as incorrect with score 0, updates the streak record, and creates or refreshes an SM-2 spaced repetition review card derived from the drill question's source `DocumentChunk`, scheduling it for tomorrow.
