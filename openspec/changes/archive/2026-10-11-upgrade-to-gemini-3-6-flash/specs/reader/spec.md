# Spec Delta

## MODIFIED Requirements

### Requirement: In-Context "Ask AI About This Book" (RAG)
The reader SHALL provide an interactive slide-over chat drawer allowing readers to ask technical questions grounded exclusively in the active document book.

#### Scenario: User asks a question about the active book
- **WHEN** user submits a technical query in the reader's Ask Book drawer
- **THEN** backend vectorizes the question, retrieves the top 3 most relevant `DocumentChunk` records from the current book (`DocumentBookId == id`) ordered by cosine distance, and synthesizes a grounded answer using `gemini-3.6-flash`.

#### Scenario: Grounded citations and source jumping
- **WHEN** the AI response is delivered to the reader
- **THEN** response includes citation metadata (`chunkOrder`, `chapterTitle`, `relevanceScore`), rendered in the UI as clickable badges. Clicking a badge immediately navigates the reader to that specific chapter slice and highlights the excerpt.

#### Scenario: Information not present in book
- **WHEN** user asks a question whose answer is not covered in the book's slices
- **THEN** the AI explicitly states that the active document does not cover the requested topic, preventing speculative or out-of-context hallucinations.
