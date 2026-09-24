# Spec Delta: today-reader

## ADDED Requirements

### Requirement: Unified Reading Studio Workspace Density
The daily reading studio workspace at `/today` SHALL enforce consistent padding (`p-3 sm:p-4 md:p-6`) and split container spacing across desktop and mobile layouts:
1. **Dock Margin Alignment**: Spacing between the Left Rail Outline, Center Reading Canvas (`DocReaderPane.vue`), and Right Dock (`InterviewChallengePane.vue`) SHALL maintain unified gap density (`gap-4 md:gap-5 lg:gap-6`), eliminating dead whitespace margins on Windows 11 and high-DPI desktop viewports.
2. **Responsive Mobile Tab Sizing**: On mobile viewports (< 1024px), tab switchers between reading and evaluation panels SHALL use uniform touch heights (>= 44px) with `whitespace-nowrap shrink-0` badges preventing text overflow across English and Vietnamese locales.

#### Scenario: User resizes browser window between desktop and tablet
- **WHEN** viewport transitions across the 1024px breakpoint on `/today`
- **THEN** the layout cleanly switches between the 3-column studio layout and tabbed mobile panes without layout jitter, clipped borders, or unbalanced horizontal margins.
