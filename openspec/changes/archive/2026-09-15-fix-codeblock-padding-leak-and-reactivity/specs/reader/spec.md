## ADDED Requirements

### Requirement: Code Block Horizontal Alignment and Indentation Bounds

Code blocks rendered within reading slices SHALL preserve visual alignment between the code header controls and code body text, preventing flush-left alignment across initial cold navigations, subsequent client-side slice transitions, and hard reloads.

#### Scenario: Cold navigation to reading slice displays indented code block

- **WHEN** a user navigates directly or via client router to `/read/[bookId]?slice={order}`
- **THEN** the code block content is indented with horizontal padding matching or exceeding the header window buttons, without touching the left card border.
