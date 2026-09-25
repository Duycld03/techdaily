# Proposal

## Why
Following the deprecation of legacy Swashbuckle/Swagger, the modern Scalar API Reference at `/scalar/v1` suffers from several critical documentation deficiencies:
1. **Missing Security Requirements**: The OpenAPI document declares the Bearer JWT security scheme in `components.securitySchemes` but lacks a global `security` requirement, causing Scalar to display "No authentication selected" and disabling the interactive authorization header.
2. **Run-on Sidebar Titles**: Minimal API endpoints place verbose, multi-sentence explanations into `.WithSummary(...)` rather than `.WithDescription(...)`. Because Scalar displays `Summary` as the primary navigation label, the sidebar renders bloated wrapped paragraphs instead of clean endpoint titles.
3. **Missing Tagging & Summaries**: Over 14 endpoints (including all 11 Library endpoints and all Notes endpoints) lack `.WithSummary(...)` and `.WithDescription(...)`, while `/health` is tagged with default assembly namespaces rather than system diagnostics.
4. **Outdated References & Missing Client Types**: Legacy `/swagger` references remain in documentation, while the frontend lacks an automated mechanism to generate synchronized TypeScript types directly from `/openapi/v1.json`.

## What Changes
- **OpenAPI Bearer Security Requirement**: Inject a global `OpenApiSecurityRequirement` referencing the `Bearer` scheme into `AddOpenApi` in `backend/src/TechDaily.Api/Program.cs`, enabling Scalar's interactive authorization client and Bearer token badge.
- **Concise Summaries & Rich Descriptions**: Refactor all Minimal API endpoints in `backend/src/TechDaily.Api/Endpoints/` to use concise 2–5 word titles for `.WithSummary(...)` (e.g. `Get Curriculum Roadmap`, `Generate AI Insight`, `Submit Quiz Answer`) while moving deep architectural explanations into `.WithDescription(...)`.
- **Complete Endpoint Coverage**:
  - Add explicit `.WithTags("Technical Library")`, `.WithSummary(...)`, and `.WithDescription(...)` across all 11 endpoints in `LibraryEndpoints.cs`.
  - Add explicit `.WithTags("Reading Highlights & Notes")`, `.WithSummary(...)`, and `.WithDescription(...)` across all endpoints in `NotesEndpoints.cs`.
  - Add `.WithTags("System Diagnostics & Health")`, `.WithSummary("Health Check")`, and `.WithDescription(...)` to `/health` in `Program.cs`.
- **Legacy Swagger Redirection**: Guarantee clean redirects for `/swagger` and `/swagger/index.html` to `/scalar/v1` with `.ExcludeFromDescription()`, and update project documentation (`AGENTS.md`, `README.md`) to reference `/scalar/v1`.
- **Frontend OpenAPI TypeScript Generation**: Introduce an automated OpenAPI generation script (`npm run gen:api`) in `frontend/package.json` utilizing `openapi-typescript` to produce compile-time type definitions at `frontend/types/api.generated.ts`.

## Capabilities

### Modified Capabilities
- `core-platform`: Updates requirement `Interactive API Documentation & OpenAPI Explorer` to mandate global OpenAPI Bearer security requirements, strict separation of concise operation summaries from detailed descriptions, 100% Minimal API endpoint metadata coverage, and automated client generation.

## Impact
- **Backend**: Zero breaking changes to API routes, HTTP contracts, or database schemas. All changes are purely OpenAPI metadata and routing configuration.
- **Frontend**: Adds `frontend/types/api.generated.ts` and `npm run gen:api` script without modifying existing Pinia stores or composables.
- **Developer Experience**: Transforms `/scalar/v1` into an interactive, beautifully structured API reference with 1-click Bearer token authorization and clear navigation.
