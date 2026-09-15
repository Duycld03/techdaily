## ADDED Requirements

### Requirement: Lightweight TOC and Lazy Slice Loading
The reader SHALL decouple Table of Contents metadata from slice body content. When navigating to `/read/[bookId]`, the reader SHALL immediately load the book metadata and lightweight TOC items (`id`, `chunkOrder`, `chapterTitle`, `estimatedReadMinutes`, `isAiFormatted`), rendering the reader shell and TOC in `<100ms`. The reader SHALL load the active slice content on demand and cache retrieved slice content in memory for instant switching.

#### Scenario: User opens a multi-slice book in the reader
- **WHEN** user opens `/read/[bookId]` for a book with multiple slices
- **THEN** reader receives lightweight TOC metadata without transferring full markdown bodies for all slices
- **AND** reader loads the active slice content on demand, rendering the reading pane without multi-second delays.

#### Scenario: User navigates between previously visited slices
- **WHEN** user navigates back to a slice that was already loaded during the current reading session
- **THEN** reader renders the slice immediately from in-memory cache without an additional network request.
