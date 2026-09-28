# Tasks

## 1. Application & Api - Invariant Total Count in Highlights Contract

- [x] 1.1 In `backend/src/TechDaily.Application/Features/Notes/GetHighlights/GetHighlightsResponse.cs`, add `TotalAllCount` to the response record with backward-compatible defaults.
- [x] 1.2 In `backend/src/TechDaily.Application/Features/Notes/GetHighlights/GetHighlightsHandler.cs`, pass `userHighlightMetadata.Count` as `TotalAllCount` to `GetHighlightsResponse`.
- [x] 1.3 In `backend/tests/TechDaily.Tests/Features/Notes/GetHighlightsTests.cs`, update or add unit tests verifying `TotalAllCount` remains invariant when filtering by tag.

## 2. Infrastructure & Api - Shared E2E Test Account Startup Provisioning

- [x] 2.1 In `backend/src/TechDaily.Infrastructure/Persistence/Seeders/E2EAccountSeeder.cs`, implement startup seeding for the test account configured in `.env` (`E2E_PROD_EMAIL` and `E2E_PROD_PASSWORD`), utilizing PBKDF2 hashing (`PasswordHasher.HashPassword`) and verified status.
- [x] 2.2 In `backend/src/TechDaily.Api/Program.cs`, invoke `await E2EAccountSeeder.SeedAsync(context, builder.Configuration, logger)` during relational database startup migration.
- [x] 2.3 Verify backend startup seamlessly provisions and authenticates the test account on local PostgreSQL without manual SQL intervention.

## 3. Frontend - Notes Tag Counter Stability & Styling Alignment

- [x] 3.1 In `frontend/stores/useNotesStore.ts`, define `totalAllCount = ref(0)` and update it from `res.totalAllCount ?? res.totalCount` in `fetchHighlights`.
- [x] 3.2 In `frontend/pages/notes.vue`, bind the "Tất cả" filter badge to `notesStore.totalAllCount || notesStore.totalCount || notesStore.highlights.length` so selecting tags never mutates the total counter.
- [x] 3.3 In `frontend/pages/notes.vue`, update tag filter chips (lines 375-402) to enforce constant `font-semibold` and `transition-colors duration-150`.
- [x] 3.4 In `frontend/tests/pages/notes.spec.ts`, update unit tests to verify the "Tất cả" chip preserves its total count when tags are clicked.

## 4. Verification & Dual-Gate Validation

- [x] 4.1 Run Gate 1 test suite (`npm test` and `dotnet test backend/`) ensuring 100% pass rate.
- [x] 4.2 Run Gate 2 headless browser visual verification on `/notes` on Desktop (1440x900) and Mobile (390x844), confirming "Tất cả (8)" remains invariant when toggling tags `#1` and `#abc`.
