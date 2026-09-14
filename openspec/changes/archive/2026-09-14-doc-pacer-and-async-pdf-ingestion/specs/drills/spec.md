# Delta Spec: Drills — Look-Ahead JIT Buffer & AI Synthesis State

## ADDED Requirements

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
