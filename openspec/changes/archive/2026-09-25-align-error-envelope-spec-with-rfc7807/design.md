# Design

## Context

See proposal.md — Why. The backend already emits RFC 7807 `application/problem+json` for every error via `TechDaily.Api.Http.ResultHttpExtensions.ToProblem` (`Results.Problem(detail, statusCode, title, extensions: { ["code"] = error.Code })`). Push endpoints add extra extension members (`sent`, `total`, `stalePurged`) via `Results.Problem(..., extensions: …)`. The frontend already reads `code` and `detail`; `useApiClient.request` and `useApiError.formatError` still contain fallback branches referencing the retired `error`/`details`/`errors` fields. Only the `core-platform` spec text lags behind this shipped reality.

## Goals / Non-Goals

**Goals:**
- Make the two `core-platform` error requirements describe the RFC 7807 envelope actually emitted.
- Remove frontend parsing branches that reference the retired `{ error, details }` shape, so code and spec agree (clean cutover).

**Non-Goals:**
- No change to emitted HTTP status codes, domain `Error` codes, or `ResultHttpExtensions` (backend is already correct).
- No change to the `api_errors.<code>` i18n dictionary contents.
- No new envelope fields or problem `type` URI scheme beyond what `Results.Problem` already produces.

## Decisions

- **Spec envelope = problem-details fields + `code` extension.** Describe `type`, `title`, `status`, `detail`, and a `code` extension member; permit additional extension members for structured metadata. Rationale: matches `Results.Problem` output exactly. Alternative — inventing a bespoke wrapper around problem+json — was rejected as diverging from the framework default already shipped.

- **Remove dead `error`/`details`/`errors` fallbacks rather than keep them.** In `useApiClient.request`, `errorMessage` currently resolves `errorJson.detail || errorJson.error || errorJson.message || errorJson.title` and `errorDetails` resolves `errorJson.details || errorJson.errors || errorJson.detail`; `useApiError.formatError` reads `responseData.error`. Since every backend error is now problem+json, the `error`/`details`/`errors` branches are unreachable for server responses. Decision: drop the server-shape branches, keeping `detail`/`title`/`code` (problem-details) and `message` (thrown `Error` objects). Rationale: clean cutover, no dead references to a retired contract. Alternative — keeping them as defensive fallbacks — was rejected because they describe a shape the server can no longer produce and would mask spec/behavior drift.

- **Preserve locale-first resolution order.** `formatError` still tries `api_errors.<code>` first, then `detail`. Rationale: unchanged externally observable localization behavior; only the source field for the raw fallback message moves from `error` to `detail`.

## Risks / Trade-offs

- [Removing `error`/`details` fallbacks could blank a message if some non-migrated endpoint still returned the old shape] → Mitigation: the prior change migrated every endpoint to `ToProblem`/`Results.Problem`; the `detail`/`title`/`message` chain still covers problem+json and thrown errors. Verified by the existing Vitest suites plus a problem+json localization check.
- [Scenario wording churn] → Mitigation: MODIFIED requirements carry every existing scenario, rewritten to the problem+json body; no scenario is dropped, so `openspec validate`/`archive` accept the delta.
