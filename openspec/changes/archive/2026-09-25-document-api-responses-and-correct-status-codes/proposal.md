# Proposal

## Why

The Scalar API reference at `/scalar/v1` documents every operation with a single bare `200 OK` and "No Body", because Minimal API handlers return untyped `Results.Ok(...)`/`IResult` with no `.Produces<T>()` metadata — so the OpenAPI generator cannot infer response schemas or the real set of status codes. Consumers (and the generated `frontend/types/api.generated.ts`) get no response shapes and no error contracts. Separately, some operation copy still advertises the retired fixed **"30-day"** curriculum program (e.g. `GET /api/v1/curriculum/roadmap` description), and mutating endpoints return semantically wrong codes (resource creation returns `200` instead of `201 Created`).

## What Changes

- Declare typed success response schemas for every Minimal API endpoint so `/openapi/v1.json` (and Scalar) render the concrete response DTO instead of "No Body" — via `TypedResults` union return types (`Results<Ok<TDto>, ...>`) and/or explicit `.Produces<TDto>(StatusCodes.Status200OK)`.
- Document every producible non-success status code per endpoint with the RFC 7807 problem-details schema: `.ProducesProblem(401)`, `.ProducesValidationProblem()` / `.ProducesProblem(400)`, `.ProducesProblem(404)`, `.ProducesProblem(409)` where applicable.
- **BREAKING (HTTP semantics)**: Correct actual runtime status codes so they match REST semantics and the documented codes — resource-creation endpoints (`POST` that create a record) return `201 Created` (with `Location` where a canonical URL exists), delete/no-content mutations return `204 No Content`, and reads/commands-with-body remain `200 OK`. Documented status codes MUST equal the codes the handler actually emits.
- Replace stale "30-day" / fixed-program wording in operation summaries and descriptions with current technology-agnostic domain language (Senior Engineering Craft Handbook, 4 core technical pillars), consistent with the existing Technology-Agnostic Starter Handbook invariant.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Extend requirement **Interactive API Documentation & OpenAPI Explorer** to mandate typed response-body schemas, complete producible status-code documentation (RFC 7807 problem details for errors), REST-correct runtime status codes (creation → `201`, no-content → `204`), parity between documented and emitted codes, and technology-agnostic operation copy free of retired "30-day" references.

## Impact

- **Backend**: `backend/src/TechDaily.Api/Endpoints/*.cs` and `Program.cs` gain `.Produces*`/`TypedResults` metadata; mutating handlers/delegates that currently emit `200` on creation change to `201`/`204`. Response DTOs are unchanged. Changing creation responses from `200`→`201` and delete → `204` is an observable HTTP-contract change for existing clients.
- **Frontend**: Regenerating `frontend/types/api.generated.ts` via `npm run gen:api` now yields populated response schemas; callers that assumed a `200` on create must accept `201`/`204`.
- **Docs / DX**: `/scalar/v1` shows response bodies and the full status-code matrix per operation; the "Try it" panel reflects accurate outcomes.
- **Dependencies**: No new packages; uses built-in `Microsoft.AspNetCore.Http.TypedResults` and OpenAPI metadata extensions.
