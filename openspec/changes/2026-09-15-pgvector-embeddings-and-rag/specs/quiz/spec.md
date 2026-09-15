# Delta Specification: Interview Quiz (Grounded Document-Anchored Generation)

## ADDED Requirements

### Requirement: Book & Curriculum Grounded Quiz Generation
The `GenerateQuizHandler` SHALL support generating scenario-based technical questions grounded in specific document books or curriculum slices using pgvector similarity search.

#### Scenario: User requests quiz based on a specific book
- **WHEN** user initiates quiz generation with `BookId` specified
- **THEN** generator retrieves document slices from that book, extracts key architectural tradeoffs, and prompts Gemini to create scenario questions strictly grounded in those excerpts.

#### Scenario: User requests quiz grounded in topic curriculum
- **WHEN** user requests quiz for a broad topic (e.g., "PostgreSQL Indexing & MVCC") with grounding enabled
- **THEN** generator uses pgvector to find top matching curriculum slices, includes the slice excerpts as authoritative reference context, and synthesizes deep-dive scenario questions referencing those architectural patterns.

#### Scenario: Excerpt citation in quiz question explanations
- **WHEN** user submits an answer to a grounded quiz question
- **THEN** explanation section includes a "Source Reference" badge indicating the book title, chapter slice, and context excerpt from which the question was derived.
