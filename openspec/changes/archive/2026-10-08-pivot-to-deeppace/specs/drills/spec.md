# Spec Delta

## MODIFIED Requirements

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
