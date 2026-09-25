# Tasks

## 1. Shared Foundations (Api layer)
- [x] 1.1 Add `ResultHttpExtensions` in `TechDaily.Api` with `ToProblem(this Error error, int statusCode)` emitting RFC 7807 (`Results.Problem` / `Results.ValidationProblem` for 400) with the domain `code` in `extensions["code"]`, plus a `WellKnownErrorStatus` map (`RESOURCE_NOT_FOUND`→404, `UNAUTHORIZED`→401, `FORBIDDEN`→403, `VALIDATION_FAILED`→400, `CONFLICT`/`AUTH_EMAIL_EXISTS`→409, default→400).
- [x] 1.2 Add named response DTOs mirroring today's anonymous payloads (no field renames): Auth `AuthSessionResponse` + `AuthUserDto`; Notifications `VapidPublicKeyResponse`, `PushAckResponse`, `PushTestResponse`; User `UserProfileResponse` (+ `UserProfileDto`, `ProfileStatsDto`), `UpdateUserProfileResponse`, `MessageResponse`; System `AiHealthResponse`; Api `HealthStatusResponse`.

## 2. Endpoint Response Metadata, Error Envelope & Status Codes
- [x] 2.1 `AuthEndpoints.cs`: return `AuthSessionResponse` from register/login/google/refresh; add `.Produces<AuthSessionResponse>(200)`, revoke `.Produces(204)`; add `.ProducesValidationProblem()` and `.ProducesProblem(401)`/`.ProducesProblem(409)` per producible error; replace anonymous error bodies with `error.ToProblem(...)`. (Register stays `200` per design D4.)
- [x] 2.2 `CurriculumEndpoints.cs`: `.Produces<CurriculumRoadmapResponse>(200)`, `.ProducesProblem(401)`, `.ProducesValidationProblem()`; convert the `BadRequest` branch to `ToProblem`.
- [x] 2.3 `DailyFocusEndpoints.cs` (today, submit-drill, explain-term, switch-book, chunk-challenge): `.Produces<TResponse>(200)` with each concrete DTO, `.ProducesProblem(401)`, and `.ProducesValidationProblem()`/`.ProducesProblem(404)` where the handler validates or resolves by id; `ToProblem` for error branches.
- [x] 2.4 `InsightsEndpoints.cs`: meta/feed/bookmark `.Produces<TResponse>(200)`; **`POST /insights/generate` → `201 Created`** returning `.Produces<TechInsightDto>(201)` with `Location: /api/v1/insights/{id}` when the id is available; add `401`/validation problem metadata; `ToProblem` for errors.
- [x] 2.5 `KnowledgeGraphEndpoints.cs`: `.Produces<KnowledgeGraphResponse>(200)`, `.ProducesProblem(401)`, `.ProducesValidationProblem()`; `ToProblem`.
- [x] 2.6 `LibraryEndpoints.cs` (11): add `.Produces<TResponse>(...)` for each — reads/curate/crawl `200`, `import` `201`, `upload-pdf`/`import-remote-pdf` `202`, `delete` `204`, `export-markdown` `.Produces(200, contentType: "text/markdown")`; add `401` + `404` (id routes) + validation problem metadata; convert `Match` error branches to `ToProblem`.
- [x] 2.7 `NotesEndpoints.cs` (4): highlights `GET` `200`, `POST` `201`, `PUT` `200`, `DELETE` `204`; add `401`/`404`/validation problem metadata; `ToProblem`.
- [x] 2.8 `NotificationEndpoints.cs` (4): return the new DTOs; vapid-key/subscribe/test `.Produces<T>(200)`, unsubscribe `.Produces(204)`; add `401`/validation problem metadata; `ToProblem`.
- [x] 2.9 `QuizEndpoints.cs` (4): `.Produces<TResponse>(200)` each, `.ProducesProblem(401)`, `.ProducesValidationProblem()`; `ToProblem`.
- [x] 2.10 `ReviewEndpoints.cs` (8): deck/grade/cards/update/reset `.Produces<TResponse>(200)`, delete `.Produces(204)`; **`from-highlight` and `from-quiz-mistake` → `201 Created`** with `.Produces<TResponse>(201)`; add `401`/`404`/validation problem metadata; `ToProblem`.
- [x] 2.11 `SystemEndpoints.cs`: return `AiHealthResponse`; `.Produces<AiHealthResponse>(200)` (`.AllowAnonymous()` retained).
- [x] 2.12 `UserEndpoints.cs` (3): return the new DTOs; profile/update-profile `.Produces<T>(200)`, change-password `.Produces<MessageResponse>(200)`; add `.ProducesProblem(401)` + validation problem metadata; `ToProblem`.
- [x] 2.13 `Program.cs` `/health`: return `HealthStatusResponse`; add `.Produces<HealthStatusResponse>(200)`.

## 3. Technology-Agnostic Operation Copy
- [x] 3.1 Edit `CurriculumEndpoints.cs:34` `.WithDescription(...)` to remove "30-day" and describe the Senior Engineering Craft Handbook roadmap grouped into 4 core technical pillars (doc-facing only; internal 30-based logic/slugs untouched per design Non-Goals).

## 4. Frontend Contract Sync
- [x] 4.1 Regenerate `frontend/types/api.generated.ts` via `npm run gen:api` (backend running) so populated response schemas and status codes are captured.
- [x] 4.2 Audit `useApiClient` (and any RFC 7807 error handling) to read the domain error `code` from problem-details (`extensions.code` / body), updating any code that relied on the old `{ code, error }` anonymous shape.
- [x] 4.3 Update frontend callers of the three creation endpoints (`insights/generate`, `review/cards/from-highlight`, `review/cards/from-quiz-mistake`) to treat `201` as success.

## 5. Verification
- [x] 5.1 Run `dotnet build` then `dotnet test` for the backend; update tests that assert the old `200` on the three create endpoints or the old anonymous `{ code, error }` error body (internal test method names containing "30Days" need no change unless they fail).
- [x] 5.2 Start the backend, `GET /openapi/v1.json`, and confirm each endpoint documents its success body schema plus its error codes; spot-check `GET /curriculum/roadmap` (200 body + 401) and `POST /insights/generate` (documents 201).
- [x] 5.3 Runtime smoke: an authenticated call to a corrected create endpoint returns HTTP `201`, and a triggered domain error returns `application/problem+json` containing the domain `code`.
- [x] 5.4 Open `/scalar/v1`: confirm operations render response bodies (no "No Body"), the full status-code matrix appears, and the curriculum roadmap description contains no "30-day" wording.
