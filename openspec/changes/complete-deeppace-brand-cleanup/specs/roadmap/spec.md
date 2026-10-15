# Roadmap Spec Delta

## MODIFIED Requirements

### Requirement: Roadmap Dual-View Switcher
The system SHALL provide a dual-view switcher on the `/roadmap` page allowing users to toggle between a linear milestone timeline view (`timeline`) and an interactive hierarchical tree mindmap view (`mindmap`). The active view mode SHALL be stored in `localStorage` under key `deeppace_roadmap_view_mode` and restored upon subsequent page visits, defaulting to `timeline` when no prior preference exists. All switcher buttons and badges SHALL enforce `whitespace-nowrap shrink-0` to prevent text wrapping across both English and Vietnamese locales.

The dual-view switcher container SHALL adhere to the Dev-Learning Studio design system, rendering as a sleek glassmorphic segmented container (`.glass-panel`) with neutral obsidian tokens (`bg-slate-100 dark:bg-canvas-subtle/80`, `border-slate-200/80 dark:border-white/[0.08]`), and active tab buttons elevated with `dark:bg-canvas-elevated` and subtle hairline border treatments, completely eliminating legacy slate-800 and slate-900 surfaces.

#### Scenario: User toggles between timeline and mindmap views
- **GIVEN** a user is on `/roadmap` in the default `timeline` view
- **WHEN** user clicks the `Mindmap View` button on the dual-view switcher
- **THEN** the timeline milestone container is replaced by the interactive hierarchical mindmap canvas without reloading the page or altering the roadmap header banner.

#### Scenario: View mode preference preserved in localStorage
- **GIVEN** a user switches the roadmap view mode to `mindmap`
- **WHEN** the user reloads the page or navigates away and returns to `/roadmap`
- **THEN** the application reads `deeppace_roadmap_view_mode` from `localStorage` and automatically renders the `mindmap` view as the active view.

#### Scenario: Responsive switch buttons layout
- **WHEN** user views the roadmap dual-view switcher on any viewport size
- **THEN** both switch buttons enforce `whitespace-nowrap shrink-0` and responsive gap layout, preventing text truncation or wrapping across English and Vietnamese locales.

#### Scenario: Glassmorphic studio segmented styling across themes
- **WHEN** user views the dual-view switcher on `/roadmap` in dark mode
- **THEN** the switcher container background renders with `dark:bg-canvas-subtle/80` and border `dark:border-white/[0.08]`
- **AND** the active view tab renders with `dark:bg-canvas-elevated` and electric violet text `dark:text-brand-400`
- **AND** the inactive tab displays `dark:text-slate-400 hover:dark:text-white` without visual clipping.

