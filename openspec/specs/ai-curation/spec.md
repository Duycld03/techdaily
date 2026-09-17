# ai-curation Specification

## Purpose
Provides AI-driven structural restoration, run-in heading segmentation, paragraph rhythm formatting, and verbatim prose preservation for imported engineering craft literature.

## Requirements

### Requirement: Verbatim Text Preservation and Structural Restoration
The AI slice curation service (`IAiMarkdownFormatter.FormatSliceAsync` implemented by `GeminiAiService`) SHALL perform structural restoration on raw extracted text for `Category.EngineeringCraft` while mandating 100% verbatim text preservation.

The formatter SHALL enforce the following invariants for `Category.EngineeringCraft`:
1. **Verbatim Text Preservation:** The formatter SHALL preserve 100% of the author's original narrative prose, examples, anecdotes, and voice without summarization, omission, condensation, or synthetic rewording.
2. **Run-in Heading Detection:** The formatter SHALL identify run-in subheadings and section titles that lack distinct vertical baselines or font sizes in publisher layouts and promote them to standard Markdown headings (`### {SectionTitle}`).
3. **Duplicate Header Elimination:** The formatter SHALL detect and eliminate duplicate header echoes, running headers, and page artifacts that immediately follow the top-level `# {ChapterTitle}` header.
4. **Paragraph Rhythm & Spacing:** The formatter SHALL segment continuous run-on narrative blocks into natural paragraphs separated by double newlines (`\n\n`), restoring visual rhythm without altering words.
5. **Dialogue & Quotation Preservation:** The formatter SHALL format spoken dialogue, character exchanges, and author quotes (`"`, `“`, `”`, `—`) on distinct lines.
6. **Auxiliary Enrichment:** The formatter SHALL populate an executive context callout (`> [!NOTE]`), exactly 3 key takeaway bullet points (`### Key Takeaways`), estimated read minutes, and a Senior Scenario Drill evaluating practical engineering habits and cognitive trade-offs without requiring synthetic code blocks.

#### Scenario: Curation of imported chapter with run-in headings
- **GIVEN** an uncurated slice of an Engineering Craft book (e.g. *Atomic Habits*) where subheadings and stories are merged into single run-in text blocks
- **WHEN** `FormatSliceAsync` is invoked for `Category.EngineeringCraft`
- **THEN** the returned `FormattedMarkdown` separates run-in subheadings into distinct `### {SectionTitle}` Markdown headings
- **AND** formats narrative stories and examples into clean paragraphs separated by double newlines (`\n\n`)
- **AND** preserves 100% of the author's original words without summarization.

#### Scenario: Elimination of duplicate header echoes
- **GIVEN** raw extracted text containing repetitive chapter title echoes or page running headers
- **WHEN** the AI formatting pipeline curates the slice
- **THEN** duplicate header echoes following `# {ChapterTitle}` are eliminated
- **AND** the slice body begins directly with the executive `> [!NOTE]` callout followed by restored narrative prose.

#### Scenario: Preservation of spoken dialogue and quotes
- **GIVEN** source text containing character dialogue or author quotations formatted with quotation marks or dashes
- **WHEN** AI structural restoration processes the text block
- **THEN** dialogue exchanges and quotations are preserved on distinct lines without collapsing into adjoining narrative paragraphs.

---

### Requirement: Synchronized Ingestion Pipeline for Clean Markdown Storage
The slice curation pipeline SHALL synchronize Direct PDF uploads (`POST /api/v1/library/upload-pdf`), Web Markdown crawls (`POST /api/v1/library/import-document`), and Embedded Web PDF streaming (`POST /api/v1/library/import-remote-pdf`) through `CurateSliceHandler`.

When `CurateSliceHandler` curates an uncurated slice and receives a successful `AiFormattedSliceResult` with non-empty `FormattedMarkdown`, the handler SHALL update `chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown` across all book categories, including `Category.EngineeringCraft`. This ensures the AI-restored clean verbatim markdown is persisted in PostgreSQL and served to all reader views.

#### Scenario: CurateSliceHandler persists clean AI-restored verbatim text
- **GIVEN** a document chunk belonging to an Engineering Craft book with raw extracted text
- **WHEN** `CurateSliceHandler.ExecuteAsync` executes on-demand or during initial curation
- **AND** `_aiFormatter.FormatSliceAsync` returns a successful result with `FormattedMarkdown`
- **THEN** `chunk.OriginalTextMarkdown` is updated with `aiResult.Value.FormattedMarkdown`
- **AND** `chunk.IsAiFormatted` is set to `true`
- **AND** changes are committed to the database.

#### Scenario: Reader surfaces render AI-restored verbatim markdown
- **GIVEN** an AI-curated slice where `chunk.OriginalTextMarkdown` contains the AI-restored verbatim markdown
- **WHEN** the user views the slice on `/read/[bookId]` or `/today`
- **THEN** the reader renders the clean markdown with restored headings and paragraph spacing
- **AND** the rendered text matches the author's complete, unsummarized prose.
