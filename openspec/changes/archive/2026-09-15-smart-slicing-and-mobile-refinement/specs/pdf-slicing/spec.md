# Delta Spec: Smart PDF Slicing & Fallback Invariant

## ADDED REQUIREMENTS

### Requirement: Hard Word Limit Invariant on PDF Slices
The PDF extraction pipeline SHALL guarantee that no extracted slice exceeds 2,500 words (approximately 8 minutes reading time).

#### Scenario: Subdividing monolithic bookmark sections
- **GIVEN** a PDF bookmark section that contains 15,000 words across 23 pages
- **WHEN** the document is ingested by `PdfPigExtractor`
- **THEN** the section is subdivided into coherent slices of no more than 2,500 words each
- **AND** each sub-slice is titled sequentially (e.g. `Chapter Title (Section 1)`, `Chapter Title (Section 2)`).

### Requirement: AI Fallback Unformatted Invariant
When AI formatting fails or produces fallback output, the system SHALL NOT mark `DocumentChunk.IsAiFormatted` as `true` and SHALL NOT persist template fallback interview questions.

#### Scenario: Graceful degradation on AI failure
- **GIVEN** an AI curation request fails or encounters an unrecoverable exception
- **WHEN** `CurateSliceHandler` or `GetTodayFocusHandler` processes the response
- **THEN** `DocumentChunk.IsAiFormatted` remains `false`
- **AND** no generic template interview question is persisted to `InterviewQuestions`
- **AND** the reader UI displays the retry card or allows viewing raw text temporarily.
