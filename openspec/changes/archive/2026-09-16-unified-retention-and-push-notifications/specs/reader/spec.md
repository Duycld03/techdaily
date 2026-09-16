# Reader Capability Delta Specification

## Purpose
Enhances the technical reading experience with personal second-brain reflection capture, 1-click active recall flashcard generation from highlights, and seamless Obsidian/Notion markdown exporting with structured YAML frontmatter.

---

## MODIFIED Requirements

### Requirement: Scoped Floating Mini-Toolbar & Active Recall Quiz
The reader floating selection toolbar SHALL allow users to highlight text inside the markdown container (`✨ Explain with Gemini` and `📋 Copy`), save a highlighted quote, or attach personal reflection notes and technical tags (`POST /api/v1/notes/highlights`) via an expandable note popover directly above the selected text in `read/[bookId].vue`.

#### Scenario: User highlights text in reader pane
- **WHEN** user selects text inside the reader markdown container
- **THEN** floating toolbar appears with Gemini Explainer and Copy actions.

#### Scenario: User opens floating note popover on text selection
- **WHEN** user selects technical text in the reader markdown pane and clicks `📝 Add Note`
- **THEN** an inline popover appears above the selection containing the quote preview, a multi-line note textarea, a tag input field, and action buttons (`Save Note`, `Cancel`).

#### Scenario: User saves a highlight with attached personal reflection
- **WHEN** user types a reflection note into the popover and clicks `Save Note`
- **THEN** the client dispatches `POST /api/v1/notes/highlights` with `selectedText`, `documentChunkId`, `note`, and `tags`, persists the highlight note in PostgreSQL, displays a localized confirmation toast, and smoothly closes the popover.

#### Scenario: User saves a simple highlight without a note
- **WHEN** user selects text and clicks `Highlight` without opening the note popover
- **THEN** the system saves the highlight with `note = null` and displays a confirmation toast.

---

## ADDED Requirements

### Requirement: 1-Click Active Recall Flashcard Generation from Highlights
The system SHALL provide an automated bridge (`POST /api/v1/review/cards/from-highlight`) converting any highlighted quote and attached reflection note into an SM-2 spaced repetition flashcard via Google Gemini.

#### Scenario: User transforms a highlight into an active recall card
- **WHEN** user clicks `⚡ Turn into Flashcard` from the reader note popover or from `/notes`
- **THEN** the backend fetches the highlight and chapter context, prompts Google Gemini to synthesize a conceptual challenge question (`Front`) and architectural explanation (`Back`), saves a new `SpacedRepetitionCard` with `SourceType = CardSourceType.Highlight`, and schedules it for immediate review in `/review`.

#### Scenario: Duplicate flashcard creation prevention
- **WHEN** user attempts to turn the same highlight into a flashcard multiple times
- **THEN** the system detects the existing card by `SourceHighlightId`, returns the existing card details without creating redundant duplicate database records, and notifies the user.

#### Scenario: Gemini synthesis failure fallback
- **WHEN** the AI service is unavailable or rate-limited during flashcard generation
- **THEN** the system generates a structured fallback flashcard using the highlighted quote as the prompt context and the attached note or chapter summary as the answer, ensuring the review card is successfully provisioned.

---

### Requirement: Obsidian and Notion Markdown Book Exporter
The system SHALL expose an automated export endpoint (`GET /api/v1/library/books/{id}/export-markdown`) compiling a book's metadata, chapter summaries, key takeaways, and user highlights/notes into a standardized Markdown file with YAML frontmatter suitable for Obsidian, Logseq, and Notion vaults.

#### Scenario: User downloads book notes as Obsidian Markdown
- **WHEN** user clicks `Export to Obsidian / Markdown` in the reader drawer or book details modal
- **THEN** the browser downloads `{book-slug}-notes.md` with `Content-Type: text/markdown; charset=utf-8`.

#### Scenario: Exported file YAML frontmatter structure
- **WHEN** the export file is generated
- **THEN** the file begins with a valid YAML frontmatter block enclosed by `---` containing `title`, `author`, `category`, `source_url`, `exported_at`, `total_chapters`, `total_highlights`, and array of `tags`.

#### Scenario: Chapter highlights formatting with personal notes
- **WHEN** the export compiles a chapter containing user highlights
- **THEN** each highlight renders as a Markdown blockquote (`> "Quote text..."`), followed immediately by `**Personal Note:** {note}` and formatted hashtag chips (`#tag1 #tag2`), correctly linking the author's words with the engineer's reflections.
