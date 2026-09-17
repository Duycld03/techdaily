## MODIFIED Requirements

### Requirement: Scoped Floating Mini-Toolbar & Active Recall Quiz
The reader floating selection toolbar SHALL render exactly 3 streamlined action buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy`. The direct flashcard creation button SHALL be removed from the reader selection tooltip to protect reading immersion and avoid premature card generation. The `Highlight/Note` action SHALL unify text highlighting and note-taking into a single continuous action: clicking the button immediately creates and persists a highlight record (`POST /api/v1/notes/highlights`), while smoothly opening an attached reflection popover where users can optionally add personal reflection notes and technical tags.

#### Scenario: User highlights text in reader pane
- **WHEN** user selects text (2 to 500 characters) inside the reader markdown container
- **THEN** floating selection toolbar appears directly above the selection displaying exactly 3 buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy` without a direct flashcard creation button.

#### Scenario: User opens floating note popover on text selection
- **WHEN** user selects text in the reader markdown pane and clicks `Highlight/Note`
- **THEN** an inline popover appears above the selection containing the quote preview, a reflection note textarea, and a tag input field with Save and Close actions.

#### Scenario: User saves a highlight with attached personal reflection
- **WHEN** user types a reflection note and tags into the reflection popover and clicks `Save Note`
- **THEN** client dispatches `PUT /api/v1/notes/highlights/{id}` with updated `note` and `tags`, persists the highlight note in PostgreSQL, displays a localized confirmation toast, and smoothly closes the popover.

#### Scenario: User saves a simple highlight without a note
- **WHEN** user clicks `Highlight/Note` and dismisses the popover without typing a note or adding tags
- **THEN** the system preserves the initially persisted highlight with `note = null` and displays a confirmation toast (`reader.toast_highlight_success`).

#### Scenario: User copies selected text or triggers Gemini explanation
- **WHEN** user clicks `Copy` on the floating toolbar
- **THEN** selected text is copied to clipboard and toolbar closes with a confirmation toast (`reader.toast_copy`)
- **WHEN** user clicks `Explain with Gemini`
- **THEN** floating toolbar closes and opens the Gemini Term Explainer modal populated with selected text and surrounding context.
