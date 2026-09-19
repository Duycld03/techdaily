# Spec Delta: Insights

## MODIFIED Requirements

### Requirement: Tech Insights Feed Data Model & Query API
The system SHALL maintain a standalone `TechInsight` catalog decoupled from library documents and expose paginated/random browsing APIs alongside a dynamic metadata query endpoint `GET /api/v1/insights/meta`, adhering to the **Dev-Learning Studio** visual theme. Technical insight cards SHALL render as `.glass-card` containers with hairline borders (`border-white/[0.08]`). Benchmark telemetry metrics SHALL be parsed and rendered as individual, compact metric chips with leading emoji deduplication and responsive alignment, eliminating crowded multi-line text wrapping in the header.

#### Scenario: Structured benchmark telemetry metric chips
- **WHEN** an insight card renders with benchmark telemetry statistics (`benchmarkStats` containing single or pipe-delimited multiple metrics)
- **THEN** the frontend parses and renders each metric as an individual, self-contained compact chip with a single Lucide `<Zap>` icon
- **AND** any leading raw emojis (`⚡`, `🔥`, `🚀`) in the data string are stripped to eliminate duplicate side-by-side icon rendering
- **AND** each metric chip renders with Dev-Learning Studio brand violet tokens (`bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-300`).

#### Scenario: Responsive card header layout and telemetry placement
- **WHEN** user views an insight card on any screen width (mobile, tablet, or desktop)
- **THEN** the top header row cleanly separates topic taxonomy badges on the left from the bookmark interaction button on the right
- **AND** benchmark telemetry chips wrap gracefully without pushing the bookmark button out of view or compressing the card title.
