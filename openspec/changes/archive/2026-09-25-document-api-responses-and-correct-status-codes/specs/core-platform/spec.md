# Spec Delta: Core Platform

## MODIFIED Requirements

### Requirement: Interactive API Documentation & OpenAPI Explorer
The backend system SHALL generate OpenAPI 3.1 specification metadata and serve an interactive developer API reference via `Scalar.AspNetCore` at route `/scalar/v1` during development environment runs. The API documentation SHALL support JWT Bearer authorization input, provide clean navigation, enforce concise operation summaries, expose complete and accurate per-operation response contracts (typed bodies and every producible status code), and support automated TypeScript client generation:

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

6. **Typed Success Response Body Schemas**:
   - Every Minimal API endpoint that returns a payload SHALL declare its success response body schema in the OpenAPI document, so `/openapi/v1.json` and Scalar render the concrete response shape instead of an empty "No Body".
   - Each endpoint's declared success schema SHALL reference a concrete named response type (a DTO), not an untyped/anonymous object. Endpoints that currently return inline anonymous objects (authentication, notifications, user profile, system AI health, and `/health`) SHALL expose named response DTOs.
   - Endpoints producing a non-JSON payload (e.g. `text/markdown` file export) SHALL declare the produced content type and status rather than an inferred empty `200`.

7. **Complete Producible Status-Code Documentation with RFC 7807 Errors**:
   - Every endpoint SHALL document all status codes it can produce, not solely `200`. Success codes SHALL be documented with their body schema; error codes (`400`, `401`, `404`, `409` as applicable) SHALL be documented with the RFC 7807 `application/problem+json` problem-details schema.
   - Authenticated endpoints (`.RequireAuthorization()`) SHALL document `401 Unauthorized`. Endpoints performing input validation SHALL document `400 Bad Request` (validation problem). Endpoints resolving a resource by identifier SHALL document `404 Not Found`.
   - Domain and validation error responses returned by endpoints SHALL be emitted as RFC 7807 problem details (carrying the domain error `code`), replacing ad-hoc anonymous `{ code, error }` JSON bodies, so the documented error schema matches the body actually returned.

8. **REST-Correct Runtime Status Codes & Documentation Parity**:
   - Endpoints that persist a new domain resource and return its representation SHALL respond with `201 Created` (including a `Location` header where a canonical resource URL exists); mutations with no response body SHALL respond with `204 No Content`; reads and commands that return a payload SHALL respond with `200 OK`.
   - Authentication/session endpoints (register, login, Google sign-in, token refresh) that return a session token payload SHALL respond with `200 OK` and are not treated as REST resource creation.
   - The set of status codes documented for an operation SHALL equal the set of status codes the handler can actually emit; documented codes and runtime codes SHALL NOT diverge.

9. **Technology-Agnostic Operation Copy**:
   - Operation summaries and descriptions SHALL use current technology-agnostic domain language and SHALL NOT reference the retired fixed "30-day" curriculum program; the curriculum roadmap operation SHALL describe the handbook's core technical pillars without a fixed day-count framing.

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

#### Scenario: Developer inspects an authenticated read operation in Scalar
- **WHEN** a developer opens an authenticated read operation (e.g. `GET /api/v1/curriculum/roadmap`) in Scalar
- **THEN** the operation documents a `200 OK` response whose body schema is the concrete response DTO with its fields
- **AND** the operation also documents a `401 Unauthorized` response using the RFC 7807 problem-details schema
- **AND** the response panel is no longer an empty "No Body".

#### Scenario: Developer inspects and calls a resource-creation operation
- **WHEN** a developer inspects a resource-creation operation that persists a new entity (e.g. `POST /api/v1/insights/generate`, `POST /api/v1/review/cards/from-highlight`, `POST /api/v1/review/cards/from-quiz-mistake`)
- **THEN** the operation documents a `201 Created` response with the created resource's body schema
- **AND** invoking the operation at runtime returns HTTP `201`, matching the documented code.

#### Scenario: Endpoint returns a domain error
- **WHEN** an endpoint returns a domain or validation failure
- **THEN** the response body is an RFC 7807 `application/problem+json` payload that includes the domain error `code`
- **AND** the returned status code is one of the codes documented for that operation.

#### Scenario: Curriculum roadmap operation copy is technology-agnostic
- **WHEN** a developer reads the description of `GET /api/v1/curriculum/roadmap` in Scalar
- **THEN** the description does not contain the phrase "30-day" or any fixed day-count program framing
- **AND** it describes the handbook grouped into its core technical pillars.
