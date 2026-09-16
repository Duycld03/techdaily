# Cleanup Capability Delta Specification

## Purpose
Specifies the permanent decommissioning and removal of obsolete `MicroQuiz` artifacts, dead frontend components, unused domain value objects, redundant database columns, and wasteful AI curation prompt instructions across the TechDaily platform.

---

## REMOVED Requirements

### Requirement: Legacy Reader MicroQuiz Card UI
**Reason**: When TechDaily upgraded the reading view from elementary multiple-choice checks to Senior Architectural Trade-off Challenges in `DocReaderPane.vue`, the `MicroQuizCard.vue` component was orphaned and rendered dead code. Additionally, `frontend/components/reader/` remains as an empty vestigial directory.
**Migration**: Permanently delete `frontend/components/today/MicroQuizCard.vue` and remove the empty `frontend/components/reader/` directory.

#### Scenario: Frontend bundle builds without dead quiz component
- **WHEN** building the frontend application via `npm run build`
- **THEN** `MicroQuizCard.vue` is excluded from the compilation output and bundle manifest.

#### Scenario: Clean component directory hierarchy
- **WHEN** inspecting `frontend/components/`
- **THEN** the empty directory `frontend/components/reader/` is completely absent from the file tree.

---

### Requirement: MicroQuiz Domain Value Object and Database Schema
**Reason**: `DocumentChunk` previously stored an embedded `MicroQuizVo` value object mapped to four database columns (`MicroQuiz_Question`, `MicroQuiz_Options`, `MicroQuiz_AnswerIndex`, `MicroQuiz_Explanation`). This data is never consumed by the client reader or quiz modules.
**Migration**: Remove `MicroQuizVo.cs` from `TechDaily.Domain.ValueObjects`, drop the `MicroQuiz` property on `DocumentChunk.cs`, remove `builder.OwnsOne(c => c.MicroQuiz)` from EF Core configurations, and execute a database migration dropping the four legacy columns.

#### Scenario: Database schema drops legacy MicroQuiz columns
- **WHEN** the `PruneMicroQuizFromDocumentChunks` migration applies
- **THEN** PostgreSQL table `DocumentChunks` no longer contains columns `MicroQuiz_Question`, `MicroQuiz_Options`, `MicroQuiz_AnswerIndex`, or `MicroQuiz_Explanation`.

#### Scenario: Domain entity instantiate without MicroQuiz Vo
- **WHEN** instantiating or querying a `DocumentChunk` entity through `ITechDailyDbContext`
- **THEN** the entity populates cleanly without tracking or loading `MicroQuiz` properties.

---

### Requirement: AI Curation MicroQuiz Generation Instructions
**Reason**: In `GeminiAiService.cs`, the curation prompt explicitly instructed Gemini Flash to generate micro-quiz questions alongside key takeaways and chapter summaries. This wasted input and completion tokens on every book slice ingestion.
**Migration**: Remove micro-quiz prompt rules from `GeminiAiService.cs`, directing Gemini's token budget strictly toward executive summaries, key architectural takeaways, and senior scenario drills.

#### Scenario: Document slice ingestion generates without micro-quiz fields
- **WHEN** `GeminiAiService.CurateSliceAsync` is invoked for a new chapter slice
- **THEN** the Gemini prompt excludes micro-quiz instructions, reducing output tokens by 15–20% while maintaining architectural summary and takeaway fidelity.

---

## MODIFIED Requirements

### Requirement: Document Chunk and Library Slice Projections
The system SHALL serve document chunk and library slice DTOs (`DailyFocusChunkDto`, `BookSliceDto`) without serializing or allocating `MicroQuiz` objects.

#### Scenario: User queries today's reading slice
- **WHEN** an authenticated user sends `GET /api/v1/daily/today`
- **THEN** the returned `DailyFocusChunkDto` contains `id`, `chapterTitle`, `summaryMarkdown`, `originalTextMarkdown`, `keyTakeaways`, and `estimatedReadMinutes`, omitting any `microQuiz` key.

#### Scenario: User queries book slice from library
- **WHEN** a client sends `GET /api/v1/library/books/{id}/slices/{sliceOrder}`
- **THEN** the returned `BookSliceDto` contains chapter metadata, original markdown, and key takeaways without `microQuiz` fields.
