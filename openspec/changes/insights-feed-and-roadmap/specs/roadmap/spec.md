# Delta Spec: Curriculum Roadmap

## ADDED Requirements

### Requirement: Curriculum Roadmap Progression & Macro View
The system SHALL provide an interactive 30-day curriculum overview endpoint and interactive timeline page allowing users to visualize full curriculum progression across all 4 technical modules.

#### Scenario: User queries curriculum roadmap progression
- **WHEN** user sends `GET /api/v1/curriculum/roadmap`
- **THEN** the system returns a structured response containing all 30 days grouped by technical module (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`) with each day's completion status, drill score, and active indicator for today.

#### Scenario: User navigates roadmap visual skill tree on frontend
- **WHEN** user visits `/roadmap`
- **THEN** the application renders an interactive 30-day skill tree displaying completed nodes in green, current day highlighted in gold, and upcoming nodes in locked state with overall module completion percentages.

### Requirement: Past Day Drill Review
The system SHALL allow users to click any unlocked or completed day node on the roadmap to inspect its reading material and review scenario solutions.

#### Scenario: User clicks unlocked past day node
- **WHEN** user clicks on an unlocked day on `/roadmap`
- **THEN** the application navigates to that day's focus view in review mode, allowing the user to re-read the material and view the scenario challenge explanation.
