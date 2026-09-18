## MODIFIED Requirements

### Requirement: Curriculum Roadmap Progression & Macro View
The system SHALL provide an interactive curriculum overview endpoint and interactive timeline page at `/roadmap` allowing users to visualize curriculum progression across core technical modules (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`). The `/roadmap` page SHALL render this progression within a single, unified timeline container without competing view tabs, presenting module progress bars, completed node highlights, and drill score badges.

The 30-day curriculum track SHALL be explicitly designated in the user interface as the **"Starter Pack (Senior Fullstack Demo Track)"**, serving as seed onboarding material while clearly communicating that users can ingest arbitrary technical books and documentation via `/library` to generate custom personalized learning roadmaps.

#### Scenario: User queries curriculum roadmap progression
- **WHEN** user sends `GET /api/v1/curriculum/roadmap`
- **THEN** the system returns a structured response containing days grouped by technical module (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`) with each day's completion status, drill score, and active indicator for today.

#### Scenario: User navigates roadmap visual skill tree on frontend
- **WHEN** user visits `/roadmap`
- **THEN** the application renders an interactive skill tree displaying completed nodes in green, current day highlighted in gold, and upcoming nodes in locked state with overall module completion percentages.

#### Scenario: 30-day curriculum fallback
- **WHEN** user has no active document book pacer or explicitly selects the 30-day senior curriculum track in the track switcher dropdown
- **THEN** the application renders the 4 core curriculum technical modules labeled as the Starter Pack demo track (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`), with clear call-to-action prompts to upload custom materials in `/library`.
