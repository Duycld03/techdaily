# Delta Spec: Reader Mode, Prefetch & Ephemeral Fallback

## ADDED REQUIREMENTS

### Requirement: 1-Step Lookahead Prefetching
When a user views Slice $N$ in `/read/[bookId]`, the client SHALL automatically initiate a background curation request for Slice $N+1$ if it is not yet curated.

#### Scenario: Background prefetch during reading
- **GIVEN** a user is reading Slice 3
- **AND** Slice 4 has `IsAiFormatted == false`
- **WHEN** the reader view is active on Slice 3
- **THEN** a background request curates Slice 4
- **AND** when the user clicks "Next Slice", Slice 4 renders immediately.

### Requirement: Ephemeral Raw Text Fallback
When AI curation fails in the reader view, the system SHALL display a retry interface with an option to view raw text temporarily without altering persistent database state.

#### Scenario: Temporary view of raw text
- **GIVEN** AI curation fails for an uncurated slice
- **WHEN** the user clicks "View raw text temporarily"
- **THEN** the raw markdown text renders with an amber notice banner
- **AND** `DocumentChunk.IsAiFormatted` remains `false` in the database
- **AND** reloading the page (F5) re-initiates the AI curation workflow.
