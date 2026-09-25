# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Interactive API Documentation & OpenAPI Explorer
The backend system SHALL generate OpenAPI 3.1 specification metadata and serve an interactive developer API reference via `Scalar.AspNetCore` at route `/scalar/v1` during development environment runs. The API documentation SHALL support JWT Bearer authorization input, provide clean navigation, enforce concise operation summaries, and support automated TypeScript client generation:

1. **OpenAPI Security Requirement & Interactive Authorization**:
   - The OpenAPI document at `/openapi/v1.json` SHALL declare the HTTP Bearer JWT security scheme in `components.securitySchemes.Bearer`.
   - The OpenAPI document SHALL declare a document-level `security` requirement (`[{ "Bearer": [] }]`), enabling Scalar's interactive authorization client, auth state indicator, and automatic token header injection (`Authorization: Bearer <token>`).

2. **Concise Operation Summaries & Granular Descriptions Standard**:
   - All Minimal API endpoints across all route groups SHALL strictly define `.WithSummary(...)` using concise, human-readable 2–5 word titles (e.g. `Get Curriculum Roadmap`, `Upload PDF Book`, `Generate AI Insight`).
   - Deep architectural breakdowns, invariants, fallback behaviors, and rate-limiting policies SHALL be defined in `.WithDescription(...)`, ensuring Scalar's navigation sidebar displays clean endpoint titles without unreadable multi-sentence paragraphs.

3. **100% Minimal API Endpoint Tagging & Metadata Coverage**:
   - Every Minimal API endpoint (including all Library, Notes, and HealthCheck routes) SHALL explicitly define `.WithTags(...)`, `.WithSummary(...)`, and `.WithDescription(...)`.
   - The `/health` probe SHALL be categorized under `System Diagnostics & Health` with summary `System Health & Database Liveness`.

4. **Clean Legacy Swagger Redirection**:
   - Requests to `/swagger` and `/swagger/index.html` SHALL redirect to `/scalar/v1` (HTTP 302) and SHALL be excluded from the OpenAPI specification description (`.ExcludeFromDescription()`).
   - Project documentation (`AGENTS.md`, `README.md`) SHALL reference the Scalar API explorer at `/scalar/v1` and OpenAPI spec at `/openapi/v1.json`.

5. **Automated Frontend Client Contract Generation**:
   - The project SHALL provide an automated command (`npm run gen:api` in `frontend/package.json`) utilizing `openapi-typescript` that queries `/openapi/v1.json` and outputs strongly typed TypeScript interfaces to `frontend/types/api.generated.ts`.

#### Scenario: Developer accesses interactive API documentation in development
- **WHEN** a developer navigates to `/scalar/v1` in the development environment
- **THEN** the system serves the Scalar API explorer rendered with dark theme (`ScalarTheme.Moon`)
- **AND** the sidebar renders clean, concise endpoint titles for all 47 Minimal API endpoints without multi-sentence text wrapping.

#### Scenario: Developer authorizes API requests via JWT Bearer in Scalar
- **WHEN** a developer provides a valid JWT token in Scalar's security definition dialog
- **THEN** subsequent test requests executed from the Scalar UI include the `Authorization: Bearer <token>` header.

#### Scenario: Legacy Swagger URL is requested
- **WHEN** a user or client requests `/swagger` or `/swagger/index.html`
- **THEN** the server responds with a redirect to `/scalar/v1`
- **AND** the redirect routes do not appear in the Scalar documentation index.

#### Scenario: Frontend developer generates OpenAPI TypeScript types
- **WHEN** a developer runs `npm run gen:api` in the `frontend` directory with the backend running
- **THEN** the CLI queries `http://localhost:5000/openapi/v1.json`
- **AND** generates a clean TypeScript type definition file at `frontend/types/api.generated.ts` containing all endpoint paths, request bodies, and response schemas.
