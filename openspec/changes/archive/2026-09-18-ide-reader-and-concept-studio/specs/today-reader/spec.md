# Spec Delta: today-reader

## ADDED Requirements

### Requirement: Focus Studio 3-Column IDE Layout & Responsive Panels
The Focus Studio reading workspace (`frontend/pages/today.vue`) SHALL implement a 3-column IDE Concept Studio layout uniting document navigation, distraction-free reading, and architectural problem-solving in a single cohesive workspace.

1. **Workspace Architecture:**
   - **Left Rail (Outline & Slice Navigator):** Collapsible sidebar displaying active curriculum chapters/slices, progress percentages, completion status checkmarks, and duration badges with Electric Violet active accents.
   - **Center Canvas:** High-contrast reading surface (`DocReaderPane.vue`), studio control bar with breadcrumb trail (`Book Title > Chapter Title > Slice N/M`), time-to-read badge, and floating selection toolbar (Highlight, Note, Explain Term).
   - **Right Dock (Scenario Studio Copilot):** Docked architectural scenario challenge pane (`InterviewChallengePane.vue`) with collapsible drawer toggle button, allowing engineers to evaluate trade-offs and solve problems side-by-side with authoritative source documentation.

2. **Responsive Panel Invariants:**
   - On wide desktop viewports ($\ge 1280\text{px}$), the workspace SHALL support simultaneous 3-column display with independent panel collapse toggles.
   - On standard desktop viewports ($1024\text{px} - 1279\text{px}$), the outline navigator SHALL collapse into a slide-over drawer by default while the reader and scenario dock share the viewport (50/50 split).
   - On mobile viewports ($< 768\text{px}$), the workspace SHALL provide a sleek segmented tab switcher (`[ 📖 Reading Slice | ⚡ Scenario Challenge ]`) with touch-optimized targets.

3. **Visual Invariants:**
   - All panels SHALL adhere to the Dev-Learning Studio theme (`canvas` `#09080e`, hairline borders `border-white/[0.08]`, and subtle ambient glows).
   - Reading typography SHALL strictly enforce the platform standard ($\ge 16\text{px}$ body text on desktop, line height $1.75$).

#### Scenario: Wide desktop 3-column studio layout
- **WHEN** an authenticated user opens `/today` on a wide screen ($\ge 1280\text{px}$)
- **THEN** the system renders the 3-column studio layout
- **AND** the outline navigator, reading markdown pane, and scenario challenge pane are simultaneously accessible.

#### Scenario: User toggles outline rail
- **WHEN** the user clicks the outline toggle button in the studio control bar
- **THEN** the left outline rail collapses smoothly with a sliding transition
- **AND** the reading canvas expands to fill the remaining horizontal space.

#### Scenario: User toggles right scenario copilot dock
- **WHEN** the user clicks the copilot drawer toggle button
- **THEN** the right scenario challenge dock collapses into a compact edge tab
- **AND** the reading canvas enters full-width distraction-free immersion mode.
