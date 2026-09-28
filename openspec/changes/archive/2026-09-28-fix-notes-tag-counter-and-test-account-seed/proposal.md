# Proposal

## Why

When filtering notes by tags on `/notes` (Highlights & Notes), selecting a tag (such as `#1 (2)` or `#abc (1)`) erroneously alters the count displayed on the "Tất cả" (All) pill from the total highlight count (e.g. `(8)`) down to the filtered result count (`(2)` or `(1)`). The "Tất cả" chip represents the complete collection of highlights and must invariantly display the total unfiltered count. Additionally, the tag filter buttons still utilize unconstrained `transition-all` and inconsistent font weights.

Furthermore, developers and E2E agents currently have to manually insert or re-hash passwords to provision the test account across local Docker and production VPS databases. The backend should automatically provision the test user specified in `.env` (`E2E_PROD_EMAIL` and `E2E_PROD_PASSWORD`) during application startup if not already present, ensuring both local dev and production share the exact same credentials without manual intervention.

## What Changes

- **Notes Highlights API & Store (`GetHighlights`)**:
  - Extend `GetHighlightsResponse` to return `TotalAllCount` (the user's total unfiltered highlights count, calculated during metadata aggregation with zero additional DB queries).
  - In `useNotesStore.ts`, persist `totalAllCount` alongside the paginated `totalCount`.
  - In `frontend/pages/notes.vue`, ensure the "Tất cả" button displays `notesStore.totalAllCount || notesStore.totalCount` so the total highlight counter remains invariant regardless of the selected tag.
  - Standardize tag filter pills in `notes.vue` to use constant `font-semibold` and `transition-colors duration-150`.
- **Shared Test Account Auto-Provisioning**:
  - Introduce `E2EAccountSeeder` in `TechDaily.Infrastructure/Persistence/Seeders/`.
  - In `Program.cs`, invoke `E2EAccountSeeder.SeedAsync(context, configuration, logger)` during relational database startup migrations.
  - Inspect `E2E_PROD_EMAIL` and `E2E_PROD_PASSWORD` from environment variables (`.env`). If present and the user does not exist, automatically provision the account with verified email, active status, and PBKDF2 password hashing (600,000 iterations). If the user exists but the password in `.env` has changed, update the password hash.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `notes`: Update requirement `Highlight Notes System` so tag filtering preserves the global highlight count on the "Tất cả" selector.
- `core-platform`: Update requirement `Standard Email & Password Authentication` and system startup invariants to formally specify environment-driven test account provisioning without dev auth bypasses.

## Impact

- **User Experience**: The "Tất cả" filter button accurately communicates the overall highlight count, preventing confusing count fluctuations when browsing specific tags.
- **Developer & E2E Efficiency**: Both local development (`./run-dev.sh`) and production VPS (`docker-compose.prod.yml`) automatically synchronize the shared test credentials from `.env` without manual SQL scripts or hash computations.
- **Zero Breaking Changes**: Fully backward-compatible; existing unit tests and API contracts remain intact.
