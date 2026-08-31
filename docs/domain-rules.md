# TechDaily — Domain Invariants & Engineering Rules

This document defines the strict non-negotiable rules, invariants, and anti-patterns for TechDaily. Violating these breaks platform stability and security.

---

## 1. Authentication & Security Invariants
1. **Zero Fake Data / No Default User Fallbacks:**
   - Handlers and Endpoints that require user context MUST use `.RequireAuthorization()`.
   - Never inject hardcoded fallback Guids (e.g. `00000000-0000-0000-0000-000000000001`) or dummy accounts into production endpoints.
   - If an unauthenticated request reaches an endpoint requiring user context, it MUST immediately return `HTTP 401 Unauthorized`.
2. **Password Security:**
   - Passwords MUST be hashed using PBKDF2 with HMAC-SHA256, a 16-byte cryptographically secure random salt, and 100,000 iterations via `PasswordHasher`.
   - Minimum password length is 6 characters.
3. **JWT Bearer Token Standards:**
   - Tokens MUST include `ClaimTypes.NameIdentifier` (`sub`), `Email`, and `Name`.
   - `Jwt:Issuer` and `Jwt:Audience` MUST be strictly synchronized across token generation (`AuthEndpoints.cs`) and validation pipeline (`Program.cs`).
4. **Secrets Management:**
   - Secrets, API keys, and private tokens MUST NEVER be committed to Git. They reside exclusively in gitignored `appsettings.Local.json` and `.env`.

---

## 2. Frontend Routing & State Invariants
1. **Explicit Route Protection:**
   - Any authenticated user view (`/profile`, `/notes`, `/settings`) MUST be registered in `frontend/middleware/auth.global.ts`.
   - Unauthenticated visitors accessing protected routes MUST be redirected to `/login?redirect={targetPath}`.
   - Authenticated users visiting `/login` MUST be redirected to `/today`.
2. **API Client Contract:**
   - `useApiClient` MUST attach `Authorization: Bearer <token>` from localStorage when available.
   - When an API call returns `401 Unauthorized`, clear local session state and redirect to `/login`.

---

## 3. UI/UX & Interaction Invariants
1. **Text Selection & Tooltip Etiquette:**
   - NEVER attach global or root-level selection listeners that trigger full modals or API calls on `@mouseup`.
   - Selection actions MUST appear as a **discreet floating mini-menu** near the highlighted text inside the reading pane only.
   - Interactive elements (Micro Quizzes, buttons, textareas, inputs) MUST NEVER trigger selection tooltips or AI lookups.
   - Users selecting text to copy (`Ctrl+C`) or translate MUST NOT have their flow interrupted.
2. **Typography & Readability Standards:**
   - Base font size MUST be at least `14px` (`text-sm`) to `16px` (`text-base`) with comfortable line height (`leading-relaxed`).
   - All components MUST support dual theme classes (`bg-white dark:bg-slate-900`, `text-slate-900 dark:text-white`, `border-slate-200 dark:border-slate-800`).
   - Never hardcode background colors (`bg-slate-950`) without light mode equivalents.

---

## 4. AI & Multimodal Evaluation Invariants
1. **Model Assignment:**
   - Multimodal Senior Drill Evaluation: `gemini-3.5-flash` with structured JSON schema.
   - Instant Inline Term Explanation: `gemini-3.5-flash-lite` with semantic caching in `TermExplanationCaches`.
2. **Graceful Degradation:**
   - If AI evaluation or network times out, the backend MUST log the incident and return a clean RFC 7807 problem details response rather than crashing.

---

## 5. Spaced Repetition (SM-2) Invariants
1. **Mathematical Boundaries:**
   - $EF' = EF + (0.1 - (5 - q) \times (0.08 + (5 - q) \times 0.02))$
   - Ease Factor ($EF$) is bounded to $[1.30, 2.50]$.
   - Progression intervals: $I_1 = 1 \text{ day}$, $I_2 = 6 \text{ days}$, $I_n = I_{n-1} \times EF$.
   - A grade of $q < 3$ resets $I$ to 1 and `RepetitionCount` to 0.

---

## 6. Anti-Patterns to NEVER Repeat

| Anti-Pattern | Why it is Forbidden | Correct Approach |
| :--- | :--- | :--- |
| **Dev 1-Click Login Bypass** | Bypasses real security flow and leads to broken production auth | Standard Email/Password registration + Google OAuth 2.0 with real JWT tokens |
| **Hardcoded User Fallback Guids** | Masks missing authentication, leaks fake data to unauthenticated users | Return `401 Unauthorized` and enforce `.RequireAuthorization()` |
| **Indiscriminate `@mouseup` Listeners** | Triggers unwanted popups during quiz clicks, copy, or translate | Scoped selection listener with floating mini-toolbar on reading markdown only |
| **Skipping OpenSpec Planning** | Causes haphazard implementations, missing invariants, and edge-case bugs | Always create or update an OpenSpec change (`proposal.md`, `spec.md`, `tasks.md`) before implementation |
| **Hardcoded Dark Theme Classes** | Breaks light mode and makes UI illegible in bright environments | Dual Tailwind classes (`dark:bg-slate-950 bg-slate-50`) synced with `@nuxtjs/color-mode` |
