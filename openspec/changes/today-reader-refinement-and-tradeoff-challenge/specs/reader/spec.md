# Delta Spec: Reader — Distraction-Free Daily Reading Experience

## MODIFIED Requirements

### Requirement: Distraction-Free Daily Reader Pane
The daily reader pane on `/today` (`DocReaderPane.vue`) SHALL present authoritative technical content, summary, key takeaways, and source context without inline micro-quizzes or superficial interruption components. Reading flow ends cleanly after the content or source context, leaving the right pane as the sole evaluation venue.

#### Scenario: User views the daily reader pane on /today
- **WHEN** user navigates to `/today` or selects a curriculum day
- **THEN** `DocReaderPane` renders the document header (title, summary, estimated read time, key takeaway pills), the deep-dive architectural markdown, optional authoritative source context, and optional benchmark snippets.
- **THEN** no inline micro-quiz card or redundant quick-check questions appear at the bottom of the reading column.

#### Scenario: Text selection floating toolbar remains fully functional
- **WHEN** user selects text (2 to 500 characters) inside `.doc-reader-content`
- **THEN** the floating selection toolbar appears above the selection offering "Explain with Gemini", "Highlight", and "Copy".
- **THEN** mouse selection is not blocked or corrupted by deleted quiz selectors.
