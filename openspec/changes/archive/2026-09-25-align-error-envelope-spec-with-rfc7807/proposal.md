# Proposal

## Why

The archived change `document-api-responses-and-correct-status-codes` migrated every API error response to RFC 7807 `application/problem+json` bodies carrying the domain error `code` as a problem-details extension (`{ type, title, status, detail, code, traceId, … }`). The implementation and the OpenAPI documentation now reflect this envelope.

Two requirements in the `core-platform` spec still describe the retired ad-hoc envelope and now contradict shipped behavior:

- **Standardized Machine-Readable Error Response Envelope** mandates a body of `{ code, error, details }`, including scenarios asserting `{ "code": "VALIDATION_FAILED", "error": "…", "details": null }`. The server no longer emits an `error` string or a top-level `details` object; the human-readable message is `detail` and structured metadata (when present) rides in problem extensions.
- **Client-Side Dynamic Error Code Localization** describes the web client resolving `api_errors.<CODE>` and falling back to `responseData.error`. `responseData.error` is no longer produced; the client now falls back to `detail`.

The spec is the single source of truth (per `AGENTS.md`), so leaving these two requirements describing the retired shape makes the spec internally inconsistent (the API-documentation requirement already states problem+json "replacing ad-hoc anonymous `{ code, error }` JSON bodies") and misleads future contributors and tests.

## What Changes

- Rewrite the **Standardized Machine-Readable Error Response Envelope** requirement to define the RFC 7807 `application/problem+json` envelope: `type`, `title`, `status`, `detail` (human-readable message), and a `code` extension member (uppercase snake_case machine-readable identifier), with optional additional extension members for structured metadata. Update its scenarios to the problem+json body shape.
- Update the **Client-Side Dynamic Error Code Localization** requirement so the client resolves `api_errors.<code>` from the problem-details `code` extension and falls back to the problem-details `detail` (not `error`). Update its scenarios accordingly.
- Clean up the frontend error-parsing code (`useApiClient`, `useApiError`) to drop references to the retired `{ error, details }` fields, reading only problem-details fields (`code`, `detail`) plus generic thrown-error fields (`message`, `title`). This makes the code a clean cutover matching the revised spec; no runtime behavior for the migrated (problem+json) responses changes.

Non-goals: no change to which HTTP status codes endpoints emit, no change to the domain `Error` codes, and no change to the i18n `api_errors.<code>` dictionary contents.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Redefine the error response envelope from `{ code, error, details }` to RFC 7807 `application/problem+json` (with the domain `code` extension), and update client error-code localization to read `code`/`detail` from problem-details.

## Impact

- **Specs**: `openspec/specs/core-platform/spec.md` — two requirements (and their scenarios) rewritten to problem+json.
- **Frontend**: `frontend/composables/useApiClient.ts` and `frontend/composables/useApiError.ts` — remove dead `{ error, details }` fallback references; read `code`/`detail`.
- **Backend**: none (already emits RFC 7807 via `ResultHttpExtensions.ToProblem`).
- **i18n**: none (existing `api_errors.<code>` keys already drive localization).
- **Tests**: existing suites remain green; add/adjust only where a scenario pins the retired body shape.
