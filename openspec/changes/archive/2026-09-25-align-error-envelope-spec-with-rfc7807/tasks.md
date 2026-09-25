# Tasks

## 1. Spec Reconciliation
- [x] 1.1 In the change delta `specs/core-platform/spec.md`, keep the two MODIFIED requirements (`Standardized Machine-Readable Error Response Envelope`, `Client-Side Dynamic Error Code Localization`) as the source of truth for the sync; no main-spec edit happens here (the sync/archive step applies them to `openspec/specs/core-platform/spec.md`).

## 2. Frontend Error-Parsing Cleanup
- [x] 2.1 `frontend/composables/useApiClient.ts` (`request` error branch, ~lines 285–307): resolve `errorMessage` from `errorJson.detail || errorJson.message || errorJson.title` (drop the retired `errorJson.error`); stop reading the retired top-level `errorJson.details` (keep the whole problem body on `ApiError.data`, and read the RFC 7807 validation `errors` extension only if present). Preserve `errorCode = errorJson.code` and the status-based `errorCode` fallbacks.
- [x] 2.2 `frontend/composables/useApiError.ts` (`formatError`, ~lines 88–110): keep locale-first resolution (`api_errors.<code>` from `responseData.code`), then fall back to `responseData.detail` / `title + detail`; remove the branch that returns `responseData.error`. Preserve the network-error, 500, and `fallbackKey` handling.
- [x] 2.3 Grep the frontend for any remaining reads of `.data?.error`, `._data?.error`, or `err.details` that assume the retired `{ error, details }` shape and update them to read problem-details `detail`/`code` (or `.data`).

## 3. Verification
- [x] 3.1 Run `npm test` (frontend Vitest); update only tests that pin the retired `{ code, error, details }` body shape to assert the problem-details `detail`/`code` instead. No CSS/layout assertions.
- [x] 3.2 Throwaway smoke: call `useApiError().formatError({ data: { detail: "Invalid email or password.", code: "AUTH_INVALID_CREDENTIALS" } })` under `en` and `vi` locales and confirm it returns the localized `api_errors.AUTH_INVALID_CREDENTIALS` string, and that an unknown `code` falls back to `detail`. Remove the scratch script afterward.
- [x] 3.3 Confirm no backend change is required: `ResultHttpExtensions.ToProblem` already emits `application/problem+json` with the `code` extension (spot-check via an existing backend test run if convenient; no new backend test needed).
