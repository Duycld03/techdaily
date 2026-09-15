## ADDED Requirements

### Requirement: Isolated Code Block Container Styling

The Markdown renderer MUST wrap code blocks in dedicated, isolated container classes that prevent style leakage or collision with standalone code highlighting components. Rendered code blocks SHALL guarantee minimum horizontal padding of at least 16px (`1rem`) on mobile viewports (<640px) and 20px (`1.25rem` to `1.5rem`) on desktop screens (≥640px) regardless of asynchronous route chunk loading order or CSS injection sequence.

#### Scenario: Unscoped external styles do not override markdown code block padding

- **WHEN** route preloading or an external component injects zero-padding styles into `<head>`
- **THEN** markdown code blocks within reading slices, today pane, and explanations retain their horizontal padding and remain visually aligned under the header window controls.

#### Scenario: Asynchronous syntax highlighter readiness updates rendered code blocks

- **WHEN** the client-side syntax highlighter completes asynchronous initialization
- **THEN** all active markdown panes (including reader slices, drill explanations, and term explainer dialogs) automatically re-render to display syntax-highlighted code.
