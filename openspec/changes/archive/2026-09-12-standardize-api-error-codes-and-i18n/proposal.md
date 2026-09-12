# Proposal: Standardize API Machine-Readable Error Codes & Complete Frontend i18n Localization

## 1. Why (Problem & Motivation)

Error notifications and alerts across TechDaily currently suffer from architectural fragmentation, hardcoded strings, and language mismatches:
1. **Raw English Backend Errors Displayed to Vietnamese Users:** The ASP.NET Core backend adheres to the project's strict `100% English Codebase` rule and returns English error messages (e.g., `"Invalid email or password."`, `"An account with this email already exists."`, `"Current password is incorrect."`). When the frontend executes `toast.error(err.message || fallback)`, Vietnamese users see raw English error messages.
2. **Lack of Machine-Readable Error Codes:** Several endpoints return unstructured `{ error = "..." }` responses rather than standardized error codes (e.g., `AUTH_INVALID_CREDENTIALS`, `AUTH_EMAIL_EXISTS`). Frontend clients cannot reliably determine the semantic cause of an error without brittle string matching.
3. **Hardcoded Client-Side Toasts & Banners:** Multiple pages and components (`login.vue`, `insights.vue`, `library.vue`, `DocReaderPane.vue`, `InterviewChallengePane.vue`, `useInterviewQuizStore.ts`) pass hardcoded strings (some Vietnamese, some English) directly into `toast.success()`, `toast.error()`, `toast.info()`, `toast.warning()`, and inline banner templates.
4. **Decoupled Architecture Best Practice:** Adopting **Approach 1 (Server returns machine-readable error codes + Frontend renders localized i18n messages)** is the industry standard (RFC 7807, Stripe, GitHub, Google Cloud). The backend stays 100% English and lightweight, while the frontend handles all localized user presentation dynamically based on active locale (`en` or `vi`).

---

## 2. What (Scope & Deliverables)

### Capability 1: Standardized Error Envelope in Backend (.NET 10)
- Enhance `TechDaily.Application.Common.Error` and endpoint responses to always return a structured envelope:
  ```json
  {
    "code": "AUTH_INVALID_CREDENTIALS",
    "error": "Invalid email or password.",
    "details": null
  }
  ```
- Standardize all business error codes across domains:
  - Auth: `AUTH_INVALID_CREDENTIALS`, `AUTH_EMAIL_EXISTS`, `AUTH_PASSWORD_TOO_SHORT`, `AUTH_EMAIL_PASSWORD_REQUIRED`, `AUTH_GOOGLE_TOKEN_INVALID`
  - User/Profile: `USER_CURRENT_PASSWORD_INCORRECT`, `USER_NEW_PASSWORD_TOO_SHORT`, `USER_PASSWORDS_MUST_MATCH`
  - Library: `LIBRARY_PDF_REQUIRED`, `LIBRARY_PDF_SIZE_LIMIT`, `LIBRARY_MULTIPART_REQUIRED`
  - Platform: `RESOURCE_NOT_FOUND`, `UNAUTHORIZED`, `FORBIDDEN`, `VALIDATION_FAILED`, `SERVER_ERROR`

### Capability 2: Frontend `ApiError` & `useApiError` Translation Composable
- Enhance `useApiClient` to parse `code` from the response envelope and throw a typed `ApiError(code, message, status, details)`.
- Create `useApiError()` composable with `formatError(err, fallbackKey)` that resolves `api_errors.<CODE>` from `useI18n()`. If the code is not found, it gracefully falls back to `fallbackKey` or a clean localized server error message.

### Capability 3: Refactor All Toasts, Alerts, and Loading States Across Frontend
- Replace 100% of hardcoded strings in toast calls and inline banners across:
  - `components/today/DocReaderPane.vue` (copy, highlight saved, highlight error)
  - `components/today/InterviewChallengePane.vue` (guest sign-in prompt, review banner)
  - `pages/login.vue` (Google auth, email/password login, register, validation, headers)
  - `pages/insights.vue` (bookmark toggle, login prompts, AI generation)
  - `pages/library.vue` (document import, PDF upload, URL crawler, document deletion)
  - `pages/profile.vue` (save profile, password update errors)
  - `pages/notes.vue` (delete highlight button state, error fallbacks)
  - `pages/today.vue` (curriculum day loading text, retry button)
  - `stores/useInterviewQuizStore.ts` (quiz generation, submission, review queue, stats)
  - `stores/useDailyFocusStore.ts` (scenario submission error)

### Capability 4: Comprehensive Bilingual Dictionary (`en.json` & `vi.json`)
- Add all new `api_errors.*` and domain-specific `toast_*` keys to both `en.json` and `vi.json` with complete parity.

### Capability 5: Automated Test Verification
- Add backend unit tests verifying error envelopes and error codes.
- Add frontend vitest tests for `useApiError` translation and localized toast handling.

---

## 3. Impact & Non-Goals
- **Impact:** 100% elimination of hardcoded notification strings, seamless Vietnamese and English error display, clean separation of backend logic and frontend presentation, and adherence to clean architecture and `AGENTS.md` rules.
- **Non-Goals:** Changing existing database schema or modifying third-party external API integrations.
