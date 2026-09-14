# Delta Spec: Library Ingestion & UI

## ADDED REQUIREMENTS

### Requirement: Lean 3-Slice Initial Ingestion
The system SHALL only format the first 3 slices of an uploaded document during initial background ingestion. Slices 4..N SHALL remain stored with raw markdown until accessed by a reader.

#### Scenario: Initial document upload completes in under 10 seconds
- **GIVEN** a user uploads a PDF book with 20 chapters
- **WHEN** `PdfIngestionWorker` processes the ingestion job
- **THEN** it formats Slices 1, 2, and 3 using the all-in-one AI prompt
- **AND** it marks `DocumentBook.Status = Ready`, `ProgressPercentage = 100`, and `StatusMessage = "Ready for reading"`
- **AND** it stops processing without running an eager background loop on Slices 4..20.

### Requirement: Clean Library Card Presentation
The `/library` page SHALL display book status as `Ready` without rendering an ongoing background curation progress bar.

#### Scenario: User views library card
- **GIVEN** a book that has completed Tier 1 initial ingestion
- **WHEN** the user views the book card on `/library`
- **THEN** the card displays a `Ready` badge and the user's reading milestone (e.g. `Resumes at Slice X` or slice count)
- **AND** no pulsing background progress bar is shown.
