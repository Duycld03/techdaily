# Tasks

## 1. Domain & Data (Technology-Agnostic Starter Curriculum Content)

- [x] 1.1 Rename `backend/src/TechDaily.Infrastructure/Data/curriculum-30-days.json` to `senior-engineering-craft-handbook.json` and rewrite content to define handbook chapters as universal conceptual engineering topics (removing Vue, Nuxt, .NET, PostgreSQL branding while keeping practical code snippets in TS, C#, SQL, Go).
- [x] 1.2 Update topic categories and slug definitions in the handbook catalog to be technology-neutral (`mvcc-write-ahead-logging`, `reactive-state-propagation`, `transaction-isolation-levels`, `generational-garbage-collection`, etc.).
- [x] 1.3 Ensure interview scenario questions, options, explanations, and model answers test conceptual senior decision-making rather than framework syntax quirks.

## 2. Application & Infrastructure (User-Centric Starter Book Provisioning)

- [x] 2.1 Implement `IStarterHandbookService` and `StarterHandbookService` in `TechDaily.Application` / `TechDaily.Infrastructure` to encapsulate provisioning of the *Senior Engineering Craft Handbook* (`DocumentBook`, handbook `DocumentChunks`, `UserBookPacer`) for a given user.
- [x] 2.2 Wire `IStarterHandbookService` into `AuthEndpoints.cs` for both standard email registration and Google OAuth registration so every new user receives an owned handbook copy (`CreatedByUserId = user.Id`).
- [x] 2.3 Update `CurriculumSeeder.cs` to seed canonical starter handbook data into the dev user's library (`00000000-0000-0000-0000-000000000001`) and clean up any unowned legacy curriculum records (`CreatedByUserId == null`).

## 3. Api & Application (Library Scoping, Deletion & Empty-State Transitions)

- [x] 3.1 Update `GetBooksHandler.cs` to filter books by the authenticated user (`b.CreatedByUserId == request.UserId && !b.IsDeleted`).
- [x] 3.2 Verify `DeleteBookHandler.cs` permits deleting the user's starter handbook and deactivates associated `UserBookPacer` records.
- [x] 3.3 Update `GetTodayFocusHandler.cs` to return a clean empty-state response (`HasActiveBook: false` or empty payload) when the user has deleted all books instead of falling back to legacy global curriculum topics.

## 4. Frontend (UI Consistency & Zero-Book Empty State Integration)

- [x] 4.1 Update `useLibraryStore.ts` and `useDailyFocusStore.ts` to support user-scoped books and detect zero-book empty states cleanly.
- [x] 4.2 Update `pages/library.vue`, `pages/today.vue`, and `pages/roadmap.vue` to render clear, inviting empty states with call-to-actions to import custom PDF/Web/Markdown documents when zero books exist.
- [x] 4.3 Update localized strings in `frontend/i18n/locales/en.json` and `vi.json` to reflect the *Senior Engineering Craft Handbook* naming and technology-agnostic track descriptions.

## 5. Tests & Verification

- [x] 5.1 Add unit tests for `StarterHandbookService` verifying that provisioned books and chunks are owned by the target user ID.
- [x] 5.2 Add unit tests for `GetBooksHandler` verifying user-scoped catalog isolation.
- [x] 5.3 Run full backend test suite (`dotnet test backend/TechDaily.sln`) to ensure all use cases and domain invariants pass.
- [x] 5.4 Run full frontend test suite (`npm test`) to ensure all component and store tests pass.
