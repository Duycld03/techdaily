# Tasks

## 1. Api (OpenAPI Security, Redirection & Scalar Options)

- [x] 1.1 Register global `OpenApiSecurityRequirement` referencing `Bearer` scheme in `AddOpenApi` within `Program.cs`.
- [x] 1.2 Configure `MapScalarApiReference` with dark theme (`ScalarTheme.Moon`), custom title ("TechDaily API Reference"), and default HTTP client.
- [x] 1.3 Configure `/swagger` and `/swagger/index.html` redirection to `/scalar/v1` with `.ExcludeFromDescription()`.
- [x] 1.4 Update `/health` endpoint to belong to tag `System Diagnostics & Health` with summary `System Health & Database Liveness` and proper description.

## 2. Api (Endpoint Summaries, Descriptions & Tagging Taxonomy)

- [x] 2.1 Refactor `LibraryEndpoints.cs`: add `.WithTags("Technical Library")`, concise `.WithSummary(...)` (2–5 words), and rich `.WithDescription(...)` across all 11 endpoints (`GetBooks`, `GetBookById`, `DeleteBook`, `GetBookStatus`, `GetBookSlice`, `CurateSlice`, `ImportDocument`, `UploadPdf`, `ImportRemotePdf`, `CrawlUrl`, `ExportBookMarkdown`).
- [x] 2.2 Refactor `NotesEndpoints.cs`: add `.WithTags("Reading Highlights & Notes")`, concise `.WithSummary(...)`, and rich `.WithDescription(...)` across all endpoints (`GetHighlights`, `CreateHighlight`, `UpdateHighlight`, `DeleteHighlight`).
- [x] 2.3 Refactor `CurriculumEndpoints.cs`, `DailyFocusEndpoints.cs`, `InsightsEndpoints.cs`, and `QuizEndpoints.cs`: separate multi-sentence summaries into concise 2–5 word summaries and detailed descriptions.
- [x] 2.4 Refactor `ReviewEndpoints.cs`, `KnowledgeGraphEndpoints.cs`, `AuthEndpoints.cs`, `UserEndpoints.cs`, `NotificationEndpoints.cs`, and `SystemEndpoints.cs`: ensure concise 2–5 word summaries and detailed descriptions.

## 3. Frontend (OpenAPI Client Type Generation & NPM Script)

- [x] 3.1 Add `gen:api` npm script to `frontend/package.json` utilizing `openapi-typescript` targeting `http://localhost:5000/openapi/v1.json`.
- [x] 3.2 Execute `npm run gen:api` to generate `frontend/types/api.generated.ts`.
- [x] 3.3 Verify generated TypeScript contracts compile without errors.

## 4. Documentation & Verification

- [x] 4.1 Update `AGENTS.md` and `README.md` to reference `/scalar/v1` and OpenAPI spec, purging stale Swagger references.
- [x] 4.2 Verify backend compiles with `dotnet build` and all backend tests pass with `dotnet test`.
- [x] 4.3 Verify all frontend tests pass with `npm test`.
- [x] 4.4 Verify `/openapi/v1.json` output and test Scalar endpoint at `/scalar/v1` (Bearer token auth requirement, clean sidebar summaries, and legacy Swagger 302 redirects).
