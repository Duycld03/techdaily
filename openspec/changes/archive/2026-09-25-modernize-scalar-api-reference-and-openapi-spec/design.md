# Design: Modernize Scalar API Reference & OpenAPI Specification

## Context
TechDaily migrated its API documentation infrastructure from Swashbuckle to `Scalar.AspNetCore` and `Microsoft.AspNetCore.OpenApi` (.NET 10). However, the initial migration left key gaps:
1. `AddOpenApi` registers a JWT Bearer `OpenApiSecurityScheme` in `components.securitySchemes`, but does not register an `OpenApiSecurityRequirement` in `document.SecurityRequirements`. Consequently, Scalar renders "No authentication selected" and disables interactive bearer token authorization.
2. Many Minimal API endpoints placed full explanatory paragraphs into `.WithSummary(...)` instead of reserving `.WithSummary(...)` for short titles and `.WithDescription(...)` for explanations. Because Scalar uses `summary` as the sidebar title, navigation is visually cluttered with multi-line wrapped text.
3. Multiple endpoint modules (notably `LibraryEndpoints.cs`, `NotesEndpoints.cs`, and the `/health` endpoint) completely lack summaries, descriptions, or proper tags.
4. Legacy `/swagger` redirects need explicit exclusion from the OpenAPI description (`.ExcludeFromDescription()`), and frontend TypeScript types need an automated generation workflow from `/openapi/v1.json`.

See `proposal.md` for motivation and background.

## Goals / Non-Goals

**Goals:**
- Enable interactive Bearer authentication in Scalar by adding a global `OpenApiSecurityRequirement`.
- Refactor all 47 Minimal API endpoints to follow the OpenAPI standard: 2–5 word titles in `.WithSummary(...)` and rich Markdown explanations in `.WithDescription(...)`.
- Achieve 100% metadata coverage across `LibraryEndpoints`, `NotesEndpoints`, `HealthCheck`, and all other endpoint modules with consistent human-readable tags.
- Configure Scalar UI options (`ScalarTheme.Moon`, custom title, default HTTP client).
- Ensure legacy `/swagger` routes redirect cleanly to `/scalar/v1` with `.ExcludeFromDescription()`.
- Add a script `gen:api` to `frontend/package.json` utilizing `openapi-typescript` targeting `/openapi/v1.json`.

**Non-Goals:**
- Changing existing HTTP status codes, route URLs, or business logic.
- Rewriting existing frontend Pinia stores or composables; generated types are additive under `frontend/types/api.generated.ts`.

## Decisions

### 1. Document-Level Security Requirement
In `Program.cs`, within `builder.Services.AddOpenApi(...)`, register a global security requirement:
```csharp
document.SecurityRequirements.Add(new OpenApiSecurityRequirement
{
    [new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    }] = Array.Empty<string>()
});
```
*Rationale:* Scalar detects `document.SecurityRequirements` to display the "Authorization" interactive client banner at the top of the documentation, allowing developers to enter a JWT token once and automatically attach `Authorization: Bearer <token>` to all interactive API explorer requests.

### 2. Standardized Summary & Description Separation
- `.WithSummary(...)`: Strictly 2 to 5 words representing the action (e.g. `Get Curriculum Roadmap`, `Upload PDF Book`, `Generate AI Insight`, `Submit Quiz Answer`).
- `.WithDescription(...)`: Comprehensive markdown documentation detailing purpose, authorization requirements, request/response lifecycle, and domain invariants.

*Rationale:* Scalar renders `Summary` directly in the left navigation sidebar. Concise summaries create a crisp, scannable developer index matching modern API documentation standards (Stripe, GitHub).

### 3. Comprehensive Tag Taxonomy
Standardize all 47 endpoints into 12 domain-aligned tags:
- `Curriculum Roadmap`
- `Tech Insights Feed`
- `Interview Quiz & Mastery Arena`
- `Daily Focus Hub`
- `Spaced Repetition Review`
- `Technical Library`
- `Reading Highlights & Notes`
- `Knowledge Graph`
- `Authentication`
- `User Profile & Settings`
- `Web Push & Notifications`
- `System Diagnostics & Health`

*Rationale:* Replaces raw class names (e.g. `TechDaily.Api`) and fragmented names (`Library`, `Notes`) with clear, descriptive sections in the Scalar sidebar.

### 4. Automated Frontend OpenAPI Code Generation
Add the following npm script to `frontend/package.json`:
```json
"gen:api": "npx openapi-typescript http://localhost:5000/openapi/v1.json -o types/api.generated.ts"
```
*Rationale:* `openapi-typescript` provides zero-runtime overhead, high-speed TypeScript type generation directly from the running backend's OpenAPI 3.1 schema. Developers can refresh contracts with a single command whenever backend endpoints change.

## Risks / Trade-offs

- **Risk:** Running `npm run gen:api` requires the backend API to be running on `http://localhost:5000`.
  - *Mitigation:* Ensure developer documentation (`README.md`, `AGENTS.md`) clearly notes that `run-dev.sh` or `dotnet run` should be active before running `npm run gen:api`.
