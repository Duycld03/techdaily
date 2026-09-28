# Design

## Context

See `proposal.md` for motivation. Currently:
1. On the `/notes` page, `notesStore.fetchHighlights` queries `GET /api/v1/notes/highlights` with optional `tag` and `search` query parameters. The backend returns `TotalCount`, which represents the *filtered* count of matching highlights. The frontend store assigns `notesStore.totalCount = res.totalCount`. Consequently, the "Tất cả" (All) filter chip—which binds to `notesStore.totalCount`—mutates its counter from the user's total library count (e.g. 8) down to the filtered count (e.g. 2 or 1) whenever a tag is active.
2. In `.env`, `E2E_PROD_EMAIL` and `E2E_PROD_PASSWORD` define the credentials for automated testing. However, there is no automatic startup seeder to ensure this account exists in local Docker or fresh production deployments, requiring developers to manually query database hashes or run manual SQL inserts.

## Goals / Non-Goals

**Goals:**
- Guarantee that the "Tất cả" filter button on `/notes` invariantly displays the user's total unfiltered highlight count (`TotalAllCount`) across all tag selections and search queries.
- Enforce constant `font-semibold` and `transition-colors duration-150` on all tag filter chips on `/notes`.
- Automatically provision the shared E2E test account defined in `.env` on backend startup across both local dev (`./run-dev.sh`) and production VPS (`docker-compose.prod.yml`).
- Automatically sync password updates from `.env` to the test account's PBKDF2 hash on startup.

**Non-Goals:**
- Creating fake bypass auth endpoints or bypassing JWT validation (Pillar 1 compliance).
- Modifying how individual highlight tags are stored or indexed in the database.

## Decisions

### 1. Backend Invariant Total Count in `GetHighlightsResponse`
- **Decision**: Add `int TotalAllCount` to `GetHighlightsResponse`. In `GetHighlightsHandler`, assign `TotalAllCount = userHighlightMetadata.Count`.
- **Rationale**: `GetHighlightsHandler` already fetches `userHighlightMetadata` (`Id` and `Tags`) for all non-deleted highlights owned by the user in Step 1 to calculate global tag frequencies. Its count (`userHighlightMetadata.Count`) represents the user's exact unfiltered highlight count. Utilizing this existing query incurs zero additional database I/O.

### 2. Frontend Store and UI Binding
- **Decision**: In `useNotesStore.ts`, define `totalAllCount = ref(0)` and update it from `res.totalAllCount ?? res.totalCount`. In `frontend/pages/notes.vue`, bind the "Tất cả" counter to `notesStore.totalAllCount || notesStore.totalCount || notesStore.highlights.length`.
- **Rationale**: Keeps the global count decoupled from the paginated/filtered `totalCount`. Even when user selects `#1 (2)`, "Tất cả" continues to show `(8)`.

### 3. Tag Filter Styling Alignment
- **Decision**: Standardize all tag buttons in `notes.vue` to use `font-semibold` in the base class and `transition-colors duration-150`.
- **Rationale**: Prevents font weight changes and unconstrained `transition-all` from causing horizontal layout jitter when switching between tags.

### 4. Startup Test Account Provisioning via `E2EAccountSeeder`
- **Decision**: Create `E2EAccountSeeder` in `TechDaily.Infrastructure/Persistence/Seeders/E2EAccountSeeder.cs`, invoked in `Program.cs` during application startup:
  - Reads `E2E_PROD_EMAIL` and `E2E_PROD_PASSWORD` from `IConfiguration`.
  - If both values are present, queries `await context.Users.FirstOrDefaultAsync(u => u.Email == email)`.
  - If user does not exist: creates a `User` entity with `IsEmailVerified = true`, `IsActive = true`, and hashes the password via `PasswordHasher.HashPassword(password)` (PBKDF2 HMAC-SHA256, 600,000 iterations).
  - If user exists: checks `PasswordHasher.VerifyPassword(password, user.PasswordHash)`. If the hash doesn't match the `.env` password, updates `user.PasswordHash = PasswordHasher.HashPassword(password)`.
- **Rationale**: `.env` is already gitignored and the single source of truth for both environments (`run-dev.sh` sources `.env` locally; `docker-compose.prod.yml` injects `env_file: .env`). The test account is thus automatically guaranteed to be ready on any fresh container or database reset without manual SQL steps.

## Risks / Trade-offs

- **Startup Performance**: The seeder performs a single indexed email lookup on startup. Overhead is < 5ms and executes only during initial process bootstrapping.
- **Security Invariance**: Secrets remain in `.env` and are never committed. Standard PBKDF2 hashing is applied, maintaining full compliance with Pillar 1.
