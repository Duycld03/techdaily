# Delta Spec: Drills — Architectural Trade-off Challenge

## MODIFIED Requirements

### Requirement: Architectural Trade-off Challenge Representation
The Senior Scenario challenge on `/today` (`InterviewChallengePane.vue`) SHALL present architectural problem statements with explicit production constraints (e.g. throughput requirements, latency SLA, consistency level, disaster recovery tolerances). Answer choices SHALL represent distinct architectural designs or engineering strategies rather than trivia facts.

#### Scenario: User views an unattempted Trade-off Challenge
- **WHEN** user views `/today` challenge pane
- **THEN** system displays the scenario title, production context badge (e.g., "High-Throughput Ingestion", "FinTech Consistency"), constraints summary, and interactive architecture proposal cards.
- **THEN** each proposal card clearly presents the architectural approach without revealing the optimal choice indicator prior to submission.

#### Scenario: User submits their architectural choice
- **WHEN** user selects an architecture proposal and clicks "Submit Decision"
- **THEN** system evaluates the selection, increments streak and points on optimal decision, triggers celebration feedback, and switches the pane into Principal Review mode.

#### Scenario: Principal Architect Review display
- **WHEN** challenge state transitions to reviewed
- **THEN** system reveals:
  1. The optimal architectural choice badge (`Optimal Choice`) and the user's choice badge (`Your Choice`).
  2. The deep-dive architectural explanation detailing why the chosen pattern satisfies the SLA/constraints.
  3. The structural breakdown of failure modes for the alternative options (e.g., why Optimistic Locking causes high retry storms at 30k RPS, or why Distributed Locking introduces single-point-of-failure bottlenecks).
