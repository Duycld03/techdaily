# Core Platform Capability Delta Specification

## Purpose
Defines delta requirements for localized client error handling in `useApiError.ts`, ensuring that HTTP 500 Internal Server Errors and unhandled ProblemDetails payloads map to localized error keys rather than exposing raw English server fault strings to users.

---

## MODIFIED Requirements

### Requirement: Frontend Client Error Resolution & Problem Details
The client error resolution composable `frontend/composables/useApiError.ts` SHALL prioritize RFC 7807 problem details (`detail`) and backend custom error messages (`error`) for client-actionable 4xx errors, while mapping HTTP 500 Internal Server Error responses to localized platform error translations (`api_errors.SERVER_ERROR` or caller-provided `fallbackKey`) to prevent leaking unlocalized English server fault strings.

#### Scenario: Backend returns RFC 7807 ProblemDetails with detail
- **WHEN** an API request fails and the backend returns `{ "detail": "Google Gemini rate limit exceeded", "status": 429 }`
- **AND** the caller invokes `formatError(err, "quiz.generate_error")`
- **THEN** `formatError` returns `"Google Gemini rate limit exceeded"` instead of the generic translation for `"quiz.generate_error"`.

#### Scenario: Backend returns custom error payload
- **WHEN** an API request fails and the backend returns `{ "code": "Embedding.ApiError", "error": "Gemini Embedding API returned status 503" }`
- **AND** the caller invokes `formatError(err, "common.error")`
- **THEN** `formatError` returns `"Gemini Embedding API returned status 503"`.

#### Scenario: Backend returns HTTP 500 ProblemDetails
- **WHEN** an API request encounters an unhandled server error and returns HTTP 500 with `{ "title": "Server Error", "detail": "An unexpected error occurred.", "status": 500 }`
- **AND** the active locale is Vietnamese (`vi`)
- **THEN** `formatError` resolves to the localized server error message `"Đã xảy ra lỗi máy chủ. Vui lòng thử lại sau."` (`api_errors.SERVER_ERROR`) instead of the raw English string.

#### Scenario: Backend returns HTTP 500 with caller fallback key
- **WHEN** an API request fails with HTTP 500 and the caller provides a specific fallback key (e.g., `"today.explain_error"`)
- **THEN** `formatError` resolves to the localized translation of the fallback key or `api_errors.SERVER_ERROR` rather than unlocalized server details.

---

## ADDED Requirements

### Requirement: Resilient Secondary Operation Fault Isolation
Application services performing secondary or auxiliary caching operations (such as embedding generation and cache persistence in `TermExplanationService`) SHALL isolate secondary database interactions within non-blocking exception handlers. Secondary caching failures SHALL NOT fail primary user-facing requests or discard valid LLM generation outputs.

#### Scenario: Auxiliary caching failure does not fail primary user operation
- **WHEN** a primary business operation succeeds (e.g. Gemini generates a term explanation) but secondary caching to PostgreSQL encounters a database exception
- **THEN** the application service catches and logs the exception without propagating an unhandled HTTP 500 error to the client
- **AND** returns the generated output to the user.

#### Scenario: Diagnostic logging on auxiliary caching failure
- **WHEN** a secondary caching failure occurs
- **THEN** the service logs a structured warning containing the entity identifier, error message, and context for operational observability.
