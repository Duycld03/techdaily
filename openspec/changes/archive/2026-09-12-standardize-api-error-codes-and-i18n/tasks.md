# Tasks: Standardize Machine-Readable Error Codes & Complete Frontend i18n Localization

## 1. Backend Tasks (.NET 10)
- [x] 1.1 Update `TechDaily.Application/Common/Error.cs` with standard machine-readable error codes (`AUTH_*`, `USER_*`, `LIBRARY_*`, `RESOURCE_NOT_FOUND`, etc.).
- [x] 1.2 Standardize error responses in `AuthEndpoints.cs`, `UserEndpoints.cs`, `LibraryEndpoints.cs`, and other endpoints to return `{ code, error, details }`.
- [x] 1.3 Verify backend tests compile and pass with `dotnet test backend`.

## 2. Frontend Core Composable Tasks
- [x] 2.1 Update `frontend/composables/useApiClient.ts` to export `ApiError` and parse `code` from error envelopes.
- [x] 2.2 Create `frontend/composables/useApiError.ts` with `formatError(err, fallbackKey)` dynamically resolving `api_errors.<CODE>`.
- [x] 2.3 Populate comprehensive `api_errors` namespace and domain `toast_*` keys in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.

## 3. Frontend Component & Page Refactoring Tasks
- [x] 3.1 Refactor `frontend/components/today/DocReaderPane.vue` and `InterviewChallengePane.vue` to use i18n keys for all toasts, alerts, and banners.
- [x] 3.2 Refactor `frontend/pages/login.vue` to use i18n for toasts, form validations, and header titles.
- [x] 3.3 Refactor `frontend/pages/insights.vue` to use i18n for bookmark toggles and AI generation toasts.
- [x] 3.4 Refactor `frontend/pages/library.vue` to use i18n for document import, PDF validation/upload, crawling, and deletion.
- [x] 3.5 Refactor `frontend/pages/profile.vue` and `frontend/pages/notes.vue` to use i18n for profile saving, password updating, and deletion states.
- [x] 3.6 Refactor `frontend/pages/today.vue` loading and retry texts to use i18n.
- [x] 3.7 Refactor `frontend/stores/useInterviewQuizStore.ts` and `frontend/stores/useDailyFocusStore.ts` to use `formatError`.

## 4. Testing & Verification Tasks
- [x] 4.1 Add unit tests for `useApiError` in `frontend/tests/composables/useApiError.spec.ts`.
- [x] 4.2 Run full automated test suites (`dotnet test backend && npm --prefix frontend test`).
- [x] 4.3 Verify bilingual visual layout on mobile (390x844) and desktop (1280x800) in both English and Vietnamese.
